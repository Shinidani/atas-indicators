namespace ATAS.Indicators.Technical
{
	using System;
	using System.ComponentModel;
	using System.Linq;
	using System.Windows.Media;

	using ATAS.Indicators.Drawing;

	using Utils.Common.Attributes;

	[HelpLink("https://support.orderflowtrading.ru/knowledge-bases/2/articles/19579-ratio")]
	public class Ratio : Indicator
	{
		#region Nested types

		private class RatioSign
		{
			#region Fields

			public readonly int Bar;
			public readonly int Direction;
			public readonly decimal Price;
			public readonly decimal Ratio;

			#endregion

			#region ctor

			public RatioSign(int bar, int direction, decimal ratio, decimal price)
			{
				Bar = bar;
				Direction = direction;
				Ratio = ratio;
				Price = price;
			}

			#endregion
		}

		#endregion

		#region Static and constants

		public const int Call = 1;
		public const int Put = -1;
		public const int Wait = 0;

		#endregion

		#region Fields

		private readonly int _xoffset;
		private Color _bgColor = Colors.Yellow;
		private int _fontSize;
		private Color _highColor = Colors.Blue;
		private Color _lowColor = Colors.Green;
		private decimal _lowRatio = 0.71m;
		private Color _neutralColor = Colors.Gray;
		private decimal _neutralRatio = 29m;
		public int CallPutCount;

		#endregion

		#region Properties

		[Category("Colors")]
		[DisplayName("01. Low Color")]
		public Color LowColor
		{
			get => _lowColor;
			set
			{
				_lowColor = value;
				ReDraw();
			}
		}

		[Category("Colors")]
		[DisplayName("02. Neutral Color")]
		public Color NeutralColor
		{
			get => _neutralColor;
			set
			{
				_neutralColor = value;
				ReDraw();
			}
		}

		[Category("Colors")]
		[DisplayName("03. High Color")]
		public Color HighColor
		{
			get => _highColor;
			set
			{
				_highColor = value;
				ReDraw();
			}
		}

		[Category("Colors")]
		[DisplayName("04. Background Color")]
		public Color BackgroundColor
		{
			get => _bgColor;
			set
			{
				_bgColor = value;
				ReDraw();
			}
		}

		[Category("Values")]
		[DisplayName("01. Low Ratio")]
		public decimal LowRatio
		{
			get => _lowRatio;
			set
			{
				_lowRatio = value;
				ReDraw();
			}
		}

		[Category("Values")]
		[DisplayName("02. Neutral Ratio")]
		public decimal NeutralRatio
		{
			get => _neutralRatio;
			set
			{
				_neutralRatio = value;
				ReDraw();
			}
		}

		[Category("Values")]
		[DisplayName("03. Font Size")]
		public int FontSize
		{
			get => _fontSize;
			set
			{
				_fontSize = value;
				ReDraw();
			}
		}

		#endregion

		#region ctor

		public Ratio()
			: base(true)
		{
			DataSeries[0].IsHidden = true;
			DenyToChangePanel = true;
		}

		#endregion

		#region Protected methods

		protected override void OnCalculate(int bar, decimal value)
		{
			var rs = CaclulateRatio(bar);
			AddLabel(rs);
		}

		#endregion

		#region Private methods

		private void ReDraw()
		{
			try
			{
				foreach (var l in Labels)
				{
					decimal ratio = 0;
					if (l.Value.Text.Length > 0)
						ratio = Convert.ToDecimal(l.Value.Text);
					l.Value.AutoSize = _fontSize == 0;
					l.Value.FontSize = _fontSize;
					l.Value.XOffset = _xoffset;
					l.Value.FontSize = FontSize;
					l.Value.FillColor = System.Drawing.Color.FromArgb(_bgColor.A, _bgColor.R, _bgColor.G, _bgColor.B);
					if (ratio <= _lowRatio)
						l.Value.Textcolor = System.Drawing.Color.FromArgb(_lowColor.A, _lowColor.R, _lowColor.G, _lowColor.B);
					else if (ratio <= _neutralRatio)
						l.Value.Textcolor = System.Drawing.Color.FromArgb(_neutralColor.A, _neutralColor.R, _neutralColor.G, _neutralColor.B);
					else
						l.Value.Textcolor = System.Drawing.Color.FromArgb(_highColor.A, _highColor.R, _highColor.G, _highColor.B);
				}
			}
			catch (Exception e)
			{
			}
		}

		private RatioSign CaclulateRatio(int bar)
		{
			RatioSign rs;
			var candle = GetCandle(bar);
			if (candle.Open < candle.Close) // bullish
			{
				var lowBid = 0;
				var lowBid2 = 0;
				var volumeinfo = candle.GetPriceVolumeInfo(candle.Low);
				if (volumeinfo != null)
					lowBid = (int)volumeinfo.Bid;

				var volumeinfo2 = candle.GetPriceVolumeInfo(candle.Low + InstrumentInfo.TickSize);
				if (volumeinfo2 != null)
					lowBid2 = (int)volumeinfo2.Bid;
				decimal ratio = 0;
				if (lowBid > 0)
					ratio = (decimal)lowBid2 / lowBid;
				rs = new RatioSign(bar, Call, ratio, candle.Low - 4 * InstrumentInfo.TickSize);
			}
			else if (candle.Open > candle.Close) // bearish
			{
				var highAsk = 0;
				var highAsk2 = 0;

				var volumeinfo = candle.GetPriceVolumeInfo(candle.High);
				if (volumeinfo != null)
					highAsk = (int)volumeinfo.Ask;

				var volumeinfo2 = candle.GetPriceVolumeInfo(candle.High - InstrumentInfo.TickSize);
				if (volumeinfo2 != null)
					highAsk2 = (int)volumeinfo2.Ask;
				decimal ratio = 0;
				if (highAsk > 0)
					ratio = (decimal)highAsk2 / highAsk;
				rs = new RatioSign(bar, Put, ratio, candle.High + 2 * InstrumentInfo.TickSize);
			}
			else
				rs = new RatioSign(bar, 0, 0, 0);

			return rs;
		}

		private void AddLabel(RatioSign rs)
		{
			var bg = System.Drawing.Color.FromArgb(_bgColor.A, _bgColor.R, _bgColor.G, _bgColor.B);
			var price = rs.Price;
			var labelName = "BAR_" + rs.Bar;

			if (Labels.Count > 0)
			{
				var lastLabel = Labels.Last();
				if (lastLabel.Key.Equals(labelName))
					Labels.Remove(lastLabel.Key);
			}

			var sRatio = rs.Ratio.ToString("N2");
			sRatio = sRatio.Replace(",00", "");
			if (rs.Direction == Call)
			{
				if (rs.Ratio <= _lowRatio)
				{
					AddText(labelName, sRatio, true, rs.Bar, price, 0, _xoffset,
						System.Drawing.Color.FromArgb(_lowColor.A, _lowColor.R, _lowColor.G, _lowColor.B), System.Drawing.Color.Transparent, bg, _fontSize,
						DrawingText.TextAlign.Center, _fontSize == 0);
				}
				else if (rs.Ratio <= _neutralRatio)
				{
					AddText(labelName, sRatio, true, rs.Bar, price, 0, _xoffset,
						System.Drawing.Color.FromArgb(_neutralColor.A, _neutralColor.R, _neutralColor.G, _neutralColor.B)
						, System.Drawing.Color.Transparent, bg, _fontSize,
						DrawingText.TextAlign.Center, _fontSize == 0);
				}
				else
				{
					AddText(labelName, sRatio, true, rs.Bar, price, 0, _xoffset,
						System.Drawing.Color.FromArgb(_highColor.A, _highColor.R, _highColor.G, _highColor.B)
						, System.Drawing.Color.Transparent, bg, _fontSize,
						DrawingText.TextAlign.Center, _fontSize == 0);
				}
			}
			else if (rs.Direction == Put)
			{
				if (rs.Ratio <= _lowRatio)
				{
					AddText(labelName, sRatio, true, rs.Bar, price, 0, _xoffset,
						System.Drawing.Color.FromArgb(_lowColor.A, _lowColor.R, _lowColor.G, _lowColor.B)
						, System.Drawing.Color.Transparent, bg, _fontSize, DrawingText.TextAlign.Center, _fontSize == 0);
				}
				else if (rs.Ratio <= _neutralRatio)
				{
					AddText(labelName, sRatio, true, rs.Bar, price, 0, _xoffset,
						System.Drawing.Color.FromArgb(_neutralColor.A, _neutralColor.R, _neutralColor.G, _neutralColor.B)
						, System.Drawing.Color.Transparent, bg, _fontSize,
						DrawingText.TextAlign.Center, _fontSize == 0);
				}
				else
				{
					AddText(labelName, sRatio, true, rs.Bar, price, 0, _xoffset,
						System.Drawing.Color.FromArgb(_highColor.A, _highColor.R, _highColor.G, _highColor.B)
						, System.Drawing.Color.Transparent, bg, _fontSize,
						DrawingText.TextAlign.Center, _fontSize == 0);
				}
			}
			else
			{
				AddText(labelName, "", true, rs.Bar, price, 0, _xoffset,
					System.Drawing.Color.FromArgb(_lowColor.A, _lowColor.R, _lowColor.G, _lowColor.B)
					, System.Drawing.Color.Transparent, bg, _fontSize,
					DrawingText.TextAlign.Center, _fontSize == 0);
			}
		}

		#endregion
	}
}