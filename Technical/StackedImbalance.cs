namespace ATAS.Indicators.Technical
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.ComponentModel.DataAnnotations;
	using System.Linq;
	using System.Windows.Media;

	using ATAS.Indicators.Drawing;
	using ATAS.Indicators.Technical.Properties;

	using OFT.Attributes;

	using Pen = System.Drawing.Pen;

	[DisplayName("Stacked Imbalance")]
	[Description("Stacked Imbalance")]
	[HelpLink("https://support.orderflowtrading.ru/knowledge-bases/2/articles/17947-stacked-imbalances")]
	public class StackedImbalance : Indicator
	{
		#region Fields

		private readonly Pen _askBidPen;
		private readonly Pen _bidAskPen;

		private System.Windows.Media.Color _askBidImbalanceColor = Colors.DarkGreen;
		private Color _bidAskImbalanceColor = Colors.DarkRed;
		private int _days;
		private int _drawBarsLength = 10;
		private bool _ignoreZeroValues;
		private int _imbalanceRange = 3;

		private int _imbalanceRatio = 300;
		private int _imbalanceVolume = 30;
		private int _lastCalculatedBar = -1;
		private int _lineWidth = 10;

		private bool _readyToAlert;
		private int _targetBar;

		#endregion

		#region Properties

		[Display(ResourceType = typeof(Resources), Name = "Days", Order = 90)]
		public int Days
		{
			get => _days;
			set
			{
				if (value < 0)
					return;

				_days = value;
				RecalculateValues();
			}
		}

		[Display(ResourceType = typeof(Resources), Name = "AskBidImbalanceColor", Order = 100)]
		public Color AskBidImbalanceColor
		{
			get => _askBidImbalanceColor;
			set
			{
				_askBidImbalanceColor = value;
				RecalculateValues();
			}
		}

		[Display(ResourceType = typeof(Resources), Name = "BidAskImbalanceColor", Order = 110)]
		public Color BidAskImbalanceColor
		{
			get => _bidAskImbalanceColor;
			set
			{
				_bidAskImbalanceColor = value;
				RecalculateValues();
			}
		}

		[Display(ResourceType = typeof(Resources), Name = "ImbalanceRatio", Order = 120)]
		public int ImbalanceRatio
		{
			get => _imbalanceRatio;
			set
			{
				_imbalanceRatio = Math.Max(0, value);
				RecalculateValues();
			}
		}

		[Display(ResourceType = typeof(Resources), Name = "ImbalanceRange", Order = 130)]
		public int ImbalanceRange
		{
			get => _imbalanceRange;
			set
			{
				_imbalanceRange = Math.Max(0, value);
				RecalculateValues();
			}
		}

		[Display(ResourceType = typeof(Resources), Name = "ImbalanceVolume", Order = 140)]
		public int ImbalanceVolume
		{
			get => _imbalanceVolume;
			set
			{
				_imbalanceVolume = Math.Max(0, value);
				RecalculateValues();
			}
		}

		[Display(ResourceType = typeof(Resources), Name = "LineWidth", Order = 150)]
		public int LineWidth
		{
			get => _lineWidth;
			set
			{
				_lineWidth = value;
				RecalculateValues();
			}
		}

		[Display(ResourceType = typeof(Resources), Name = "PrintLineForXBars", Order = 160)]
		public int DrawBarsLength
		{
			get => _drawBarsLength;
			set
			{
				_drawBarsLength = value;
				RecalculateValues();
			}
		}

		[Display(ResourceType = typeof(Resources), Name = "IgnoreZeroValues", Order = 70)]
		public bool IgnoreZeroValues
		{
			get => _ignoreZeroValues;
			set
			{
				_ignoreZeroValues = value;
				RecalculateValues();
			}
		}

		[Display(ResourceType = typeof(Resources), Name = "UseAlerts", GroupName = "Alerts")]
		public bool UseAlerts { get; set; }

		[Display(ResourceType = typeof(Resources), Name = "AlertFile", GroupName = "Alerts")]
		public string AlertFile { get; set; } = "alert1";

		#endregion

		#region ctor

		public StackedImbalance()
			: base(true)
		{
			_days = 20;
			_askBidPen = new Pen(GetDrawingColor(_askBidImbalanceColor));
			_bidAskPen = new Pen(GetDrawingColor(_bidAskImbalanceColor));
			DataSeries[0].IsHidden = true;
			DenyToChangePanel = true;
		}

		#endregion

		#region Protected methods

		protected override void OnCalculate(int bar, decimal value)
		{
			if (bar == 0)
			{
				_askBidPen.Width = _lineWidth;
				_bidAskPen.Width = _lineWidth;
				_askBidPen.Color = GetDrawingColor(_askBidImbalanceColor);
				_bidAskPen.Color = GetDrawingColor(_bidAskImbalanceColor);
				_readyToAlert = false;

				_targetBar = 0;

				if (_days <= 0)
					return;

				var days = 0;

				for (var i = CurrentBar - 1; i >= 0; i--)
				{
					_targetBar = i;

					if (!IsNewSession(i))
						continue;

					days++;

					if (days == _days)
						break;
				}

				return;
			}

			if (bar < _targetBar)
				return;

			if (bar == _lastCalculatedBar)
			{
				_readyToAlert = true;
				return;
			}

			_lastCalculatedBar = bar;
			HorizontalLinesTillTouch.RemoveAll(t => t.FirstBar == bar - 1);
			var volumes = GetVolumes(bar - 1);
			CalculateAskBid(bar - 1, volumes);
			CalculateBidAsk(bar - 1, volumes);

			if (_readyToAlert &&
				UseAlerts &&
				HorizontalLinesTillTouch.Any(x => x.FirstBar == bar - 1)
			)

			{
				AddAlert(AlertFile, "StackedImbalance was triggered");
				_readyToAlert = false;
			}
		}

		#endregion

		#region Private methods

		private List<decimal[]> GetVolumes(int bar)
		{
			var candle = GetCandle(bar);
			var volumes = new List<decimal[]>();

			for (var price = candle.Low; price <= candle.High; price += InstrumentInfo.TickSize)
			{
				var volumeinfo = candle.GetPriceVolumeInfo(price);

				if (volumeinfo == null)
					continue;

				var volume = new decimal[3];
				volume[0] = price;
				volume[1] = volumeinfo.Bid;
				volume[2] = volumeinfo.Ask;
				volumes.Add(volume);
			}

			return volumes;
		}

		private void CalculateAskBid(int bar, List<decimal[]> volumes) // Ask/Bid
		{
			var imbalance = new bool[volumes.Count];

			for (var i = 0; i < volumes.Count - 1; i++)
			{
				var lowVolume = volumes[i];
				var highVolume = volumes[i + 1];
				var askFilterValue = lowVolume[1] * _imbalanceRatio / 100;

				if (_ignoreZeroValues && askFilterValue == 0)
					continue;

				if (highVolume[2] > askFilterValue && highVolume[2] > _imbalanceVolume) // Ask > volume
					imbalance[i] = true;
			}

			var count = 0;

			for (var i = 0; i < volumes.Count; i++)
			{
				if (imbalance[i])
					count++;
				else
				{
					if (count >= _imbalanceRange)
					{
						if (_drawBarsLength != 0)
						{
							for (var k = i - count + 1; k <= i; k++)
								HorizontalLinesTillTouch.Add(new LineTillTouch(bar, volumes[k][0], _askBidPen, _drawBarsLength));
						}
						else
						{
							for (var k = i - count + 1; k <= i; k++)
								HorizontalLinesTillTouch.Add(new LineTillTouch(bar, volumes[k][0], _askBidPen));
						}
					}

					count = 0;
				}
			}
		}

		private void CalculateBidAsk(int bar, List<decimal[]> volumes) // Bid/Ask
		{
			var imbalance = new bool[volumes.Count];

			for (var i = 0; i < volumes.Count - 1; i++)
			{
				var lowVolume = volumes[i];
				var highVolume = volumes[i + 1];
				var bidFilterValue = highVolume[2] * _imbalanceRatio / 100;

				if (_ignoreZeroValues && bidFilterValue == 0)
					continue;

				if (lowVolume[1] > bidFilterValue && lowVolume[1] > _imbalanceVolume) // Bid
					imbalance[i] = true;
			}

			var count = 0;

			for (var i = 0; i < volumes.Count; i++)
			{
				if (imbalance[i])
					count++;
				else
				{
					if (count >= _imbalanceRange)
					{
						if (_drawBarsLength != 0)
						{
							for (var k = i - count + 1; k <= i; k++)
								HorizontalLinesTillTouch.Add(new LineTillTouch(bar, volumes[k - 1][0], _bidAskPen, _drawBarsLength));
						}
						else
						{
							for (var k = i - count + 1; k <= i; k++)
								HorizontalLinesTillTouch.Add(new LineTillTouch(bar, volumes[k - 1][0], _bidAskPen));
						}
					}

					count = 0;
				}
			}
		}

		private System.Drawing.Color GetDrawingColor(Color color)
		{
			var drawingColor = System.Drawing.Color.FromArgb(color.A, color.R, color.G, color.B);
			return drawingColor;
		}

		#endregion
	}
}