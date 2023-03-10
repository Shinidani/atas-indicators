namespace ATAS.Indicators.Technical
{
	using System;
	using System.ComponentModel;
	using System.ComponentModel.DataAnnotations;
	using System.Windows.Media;

	using ATAS.Indicators.Technical.Properties;

	using OFT.Attributes;

	using Utils.Common.Logging;

	[DisplayName("Speed of Tape")]
	[Category("Order Flow")]
	[HelpLink("https://support.orderflowtrading.ru/knowledge-bases/2/articles/430-speed-of-tape")]
	public class SpeedOfTape : Indicator
	{
		#region Nested types

		[Serializable]
		public enum SpeedOfTapeType
		{
			[Display(ResourceType = typeof(Resources), Name = "Volume")]
			Volume,

			[Display(ResourceType = typeof(Resources), Name = "Ticks")]
			Ticks,

			[Display(ResourceType = typeof(Resources), Name = "Buys")]
			Buys,

			[Display(ResourceType = typeof(Resources), Name = "Sells")]
			Sells,

			[Display(ResourceType = typeof(Resources), Name = "Delta")]
			Delta
		}

		#endregion

		#region Fields

		private readonly ValueDataSeries _maxSpeed = new("Maximum Speed") { Color = Colors.Yellow, VisualType = VisualMode.Histogram };
		private readonly PaintbarsDataSeries _paintBars = new("Paint bars");

		private readonly SMA _sma = new()
			{ Name = "Filter line" };

		private readonly ValueDataSeries _smaSeries;
		private bool _autoFilter = true;
		private int _lastAlertBar = -1;
		private int _sec = 15;
		private int _trades = 100;

		private SpeedOfTapeType _type = SpeedOfTapeType.Ticks;

		#endregion

		#region Properties

		[Display(ResourceType = typeof(Resources), Name = "PaintBars")]
		public bool PaintBars
		{
			get => _paintBars.Visible;
			set
			{
				_paintBars.Visible = value;
				RecalculateValues();
			}
		}

		[Display(ResourceType = typeof(Resources), Name = "AutoFilter", GroupName = "Filters")]
		public bool AutoFilter
		{
			get => _autoFilter;
			set
			{
				_autoFilter = value;
				RecalculateValues();
			}
		}

		[Display(ResourceType = typeof(Resources), Name = "AutoFilterPeriod", GroupName = "Filters")]
		public int AutoFilterPeriod
		{
			get => _sma.Period;
			set
			{
				_sma.Period = value;
				RecalculateValues();
			}
		}

		[Display(ResourceType = typeof(Resources), Name = "TimeFilterSec", GroupName = "Filters")]
		public int Sec
		{
			get => _sec;
			set
			{
				_sec = Math.Max(1, value);
				RecalculateValues();
			}
		}

		[Display(ResourceType = typeof(Resources), Name = "TradesFilter", GroupName = "Filters")]
		public int Trades
		{
			get => _trades;
			set
			{
				_trades = Math.Max(1, value);
				RecalculateValues();
			}
		}

		[Display(ResourceType = typeof(Resources), Name = "CalculationMode", GroupName = "Filters")]
		public SpeedOfTapeType Type
		{
			get => _type;
			set
			{
				_type = value;
				RecalculateValues();
			}
		}

		[Display(ResourceType = typeof(Resources), Name = "UseAlerts", GroupName = "Alerts")]
		public bool UseAlerts { get; set; }

		[Display(ResourceType = typeof(Resources), Name = "AlertFile", GroupName = "Alerts")]
		public string AlertFile { get; set; } = "alert1";

		[Display(ResourceType = typeof(Resources), Name = "FontColor", GroupName = "Alerts")]
		public Color AlertForeColor { get; set; } = Color.FromArgb(255, 247, 249, 249);

		[Display(ResourceType = typeof(Resources), Name = "BackGround", GroupName = "Alerts")]
		public Color AlertBgColor { get; set; } = Color.FromArgb(255, 75, 72, 72);

		#endregion

		#region ctor

		public SpeedOfTape()
			: base(true)
		{
			Panel = IndicatorDataProvider.NewPanel;
			var main = (ValueDataSeries)DataSeries[0];
			main.Color = Colors.Aqua;
			main.VisualType = VisualMode.Histogram;

			((ValueDataSeries)_sma.DataSeries[0]).Name = "Filter line";
			_smaSeries = (ValueDataSeries)_sma.DataSeries[0];
			_smaSeries.Width = 2;
			_smaSeries.Color = Colors.LightBlue;
			DataSeries.Add(_maxSpeed);
			DataSeries.Add(_smaSeries);
			DataSeries.Add(_paintBars);

			_paintBars.IsHidden = true;

			_maxSpeed.PropertyChanged += delegate
			{
				if (SourceDataSeries == null)
					return;

				try
				{
					for (var i = 0; i < CurrentBar; i++)
					{
						if (_paintBars[i] != null)
							_paintBars[i] = _maxSpeed.Color;
					}
				}
				catch (Exception e)
				{
					this.LogError("PaintBars error", e);
				}
			};
		}

		#endregion

		#region Protected methods

		protected override void OnCalculate(int bar, decimal value)
		{
			var j = bar;
			var pace = 0m;
			var currentCandle = GetCandle(bar);

			while (j >= 0)
			{
				var candle = GetCandle(j);
				var ts = currentCandle.Time - candle.Time;

				if (ts.TotalSeconds < Sec)
				{
					if (_type == SpeedOfTapeType.Volume)
						pace += candle.Volume;

					if (_type == SpeedOfTapeType.Ticks)
						pace += candle.Ticks;
					else if (_type == SpeedOfTapeType.Buys)
						pace += candle.Ask;
					else if (_type == SpeedOfTapeType.Sells)
						pace += candle.Bid;
					else if (_type == SpeedOfTapeType.Delta)
						pace += candle.Delta;
				}
				else
				{
					pace = pace * Sec / (decimal)ts.TotalSeconds;
					break;
				}

				j--;
			}

			_sma.Calculate(bar, pace * 1.5m);

			if (!AutoFilter)
				_smaSeries[bar] = Trades;
			this[bar] = pace;

			if (Math.Abs(pace) > _smaSeries[bar])
			{
				_maxSpeed[bar] = pace;
				_paintBars[bar] = _maxSpeed.Color;

				if (UseAlerts && bar == CurrentBar - 1 && bar != _lastAlertBar)
				{
					AddAlert(AlertFile, InstrumentInfo.Instrument, $"Speed of tape is increased to {pace} value", AlertBgColor, AlertForeColor);
					_lastAlertBar = bar;
				}
			}
			else
			{
				_maxSpeed[bar] = 0;
				_paintBars[bar] = null;
			}
		}

		#endregion
	}
}