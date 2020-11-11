namespace ATAS.Indicators.Technical
{
	using System;
	using System.ComponentModel;
	using System.ComponentModel.DataAnnotations;
	using System.Windows.Media;

	using ATAS.Indicators.Technical.Properties;

	using OFT.Attributes;

	[DisplayName("VWAP/TWAP")]
	[HelpLink("https://support.orderflowtrading.ru/knowledge-bases/2/articles/8569-vwap")]
	public class VWAP : Indicator
	{
		#region Nested types

		public enum VWAPMode
		{
			VWAP = 0,
			TWAP = 1
		}

		public enum VWAPPeriodType
		{
			Daily,
			Weekly,
			Monthly,
			All
		}

		#endregion

		#region Fields

		private int _lastbar = -1;
		private ValueDataSeries _lower = new ValueDataSeries("Lower std1") { Color = Colors.DodgerBlue };
		private ValueDataSeries _lower1 = new ValueDataSeries("Lower std2") { Color = Colors.DodgerBlue };
		private ValueDataSeries _lower2 = new ValueDataSeries("Lower std3") { Color = Colors.DodgerBlue, VisualType = VisualMode.Hide };
		private int _n;

		private int _period = 300;
		private VWAPPeriodType _periodType = VWAPPeriodType.Daily;

		private ValueDataSeries _sqrt = new ValueDataSeries("sqrt");
		private decimal _stdev = 1;
		private decimal _stdev1 = 2;
		private decimal _stdev2 = 2.5m;
		private decimal _sum;
		private ValueDataSeries _totalVolToClose = new ValueDataSeries("volToClose");

		private ValueDataSeries _totalVolume = new ValueDataSeries("totalVolume");
		private VWAPMode _twapMode = VWAPMode.VWAP;
		private ValueDataSeries _upper = new ValueDataSeries("Upper std1") { Color = Colors.DodgerBlue };
		private ValueDataSeries _upper1 = new ValueDataSeries("Upper std2") { Color = Colors.DodgerBlue };
		private ValueDataSeries _upper2 = new ValueDataSeries("Upper std3") { Color = Colors.DodgerBlue, VisualType = VisualMode.Hide };
		private int _zeroBar;

		#endregion

		#region Properties

		[Display(ResourceType = typeof(Resources), Name = "Period")]
		public VWAPPeriodType Type
		{
			get => _periodType;
			set
			{
				_periodType = value;
				RecalculateValues();
			}
		}

		[Display(ResourceType = typeof(Resources), Name = "FirstDev")]
		public decimal StDev
		{
			get => _stdev;
			set
			{
				_stdev = Math.Max(value, 0);
				RecalculateValues();
			}
		}

		[Display(ResourceType = typeof(Resources), Name = "SecondDev")]
		public decimal StDev1
		{
			get => _stdev1;
			set
			{
				_stdev1 = Math.Max(value, 0);
				RecalculateValues();
			}
		}

		[Display(ResourceType = typeof(Resources), Name = "ThirdDev")]
		public decimal StDev2
		{
			get => _stdev2;
			set
			{
				_stdev2 = Math.Max(value, 0);
				RecalculateValues();
			}
		}

		[Display(ResourceType = typeof(Resources), Name = "Period")]
		public int Period
		{
			get => _period;
			set
			{
				_period = Math.Max(value, 1);
				RecalculateValues();
			}
		}

		[Display(ResourceType = typeof(Resources), Name = "Mode")]
		public VWAPMode TWAPMode
		{
			get => _twapMode;
			set
			{
				_twapMode = value;
				RecalculateValues();
			}
		}

		#endregion

		#region ctor

		public VWAP()
		{
			var series = (ValueDataSeries)DataSeries[0];
			series.Color = Colors.Firebrick;
			DataSeries.Add(_lower2);
			DataSeries.Add(_upper2);
			DataSeries.Add(_lower1);
			DataSeries.Add(_upper1);
			DataSeries.Add(_lower);
			DataSeries.Add(_upper);
		}

		#endregion

		#region Protected methods

		protected override void OnCalculate(int bar, decimal value)
		{
			var needReset = false;
			var candle = GetCandle(bar);
			var volume = Math.Max(1, candle.Volume);
			var typical = (candle.Open + candle.Close + candle.High + candle.Low) / 4;

			if (bar == 0)
			{
				_zeroBar = bar;
				_n = 0;
				_sum = 0;

				if (_twapMode == VWAPMode.TWAP)
					this[bar] = _totalVolToClose[bar] = _upper[bar] = _lower[bar] = _upper1[bar] = _lower1[bar] = _upper2[bar] = _lower2[bar] = typical;
				else
				{
					this[bar] = _upper[bar] = _lower[bar] = _upper1[bar] = _lower1[bar] = _upper1[bar] = _lower1[bar] = candle.Close;
					_totalVolToClose[bar] = 0;
				}

				return;
			}

			if (Type == VWAPPeriodType.Daily && IsNewSession(bar))
				needReset = true;
			else if (Type == VWAPPeriodType.Weekly && IsNewWeek(bar))
				needReset = true;
			else if (Type == VWAPPeriodType.Monthly && IsNewMonth(bar))
				needReset = true;

			var setStartOfLine = needReset;

			if (setStartOfLine && Type == VWAPPeriodType.Daily && TimeFrame == "Daily")
				setStartOfLine = false;

			if (bar == 0 || needReset)
			{
				_zeroBar = bar;
				_n = 0;
				_sum = 0;
				_totalVolume[bar] = volume;
				_totalVolToClose[bar] = _twapMode == VWAPMode.TWAP ? typical : candle.Close * volume;

				if (setStartOfLine)
				{
					((ValueDataSeries)DataSeries[0]).SetPointOfEndLine(bar - 1);
					_upper.SetPointOfEndLine(bar - 1);
					_lower.SetPointOfEndLine(bar - 1);
					_upper1.SetPointOfEndLine(bar - 1);
					_lower1.SetPointOfEndLine(bar - 1);
					_upper2.SetPointOfEndLine(bar - 1);
					_lower2.SetPointOfEndLine(bar - 1);
				}
			}
			else
			{
				_totalVolume[bar] = _totalVolume[bar - 1] + volume;
				_totalVolToClose[bar] = _totalVolToClose[bar - 1] + (_twapMode == VWAPMode.TWAP ? typical : candle.Close * volume);
			}

			if (_twapMode == VWAPMode.TWAP)
				this[bar] = _totalVolToClose[bar] / (bar - _zeroBar + 1);
			else
				this[bar] = _totalVolToClose[bar] / _totalVolume[bar];

			var sqrt = (decimal)Math.Pow((double)((candle.Close - this[bar]) / TickSize), 2);
			_sqrt[bar] = sqrt;

			var k = bar;
			if (_lastbar != bar)
			{
				_n = 0;
				_sum = 0;
				for (var j = 0; j < Period; j++, _n++, k--)
				{
					if (k < _zeroBar)
						break;

					_sum += _sqrt[k];
				}
			}

			var summ = _sum + sqrt;
			var stdDev = (decimal)Math.Sqrt((double)summ / (_n + 1));

			_upper[bar] = this[bar] + stdDev * _stdev * TickSize;
			_lower[bar] = this[bar] - stdDev * _stdev * TickSize;
			_upper1[bar] = this[bar] + stdDev * _stdev1 * TickSize;
			_lower1[bar] = this[bar] - stdDev * _stdev1 * TickSize;
			_upper2[bar] = this[bar] + stdDev * _stdev2 * TickSize;
			_lower2[bar] = this[bar] - stdDev * _stdev2 * TickSize;
		}

		#endregion
	}
}