﻿namespace ATAS.Indicators.Technical
{
	using System.ComponentModel;
	using System.ComponentModel.DataAnnotations;
	using System.Drawing;

	using ATAS.Indicators.Drawing;
	using ATAS.Indicators.Technical.Properties;

	[DisplayName("ADR")]
	public class ADR : Indicator
	{
		#region Fields

		private readonly SMA _sma = new SMA();

		private readonly Pen _style = new Pen(Color.Green, 2);
		private decimal _adrHigh;
		private decimal _adrLow;
		private bool _lastSessionAdded;
		private int _startSession;

		#endregion

		#region Properties

		[Parameter]
		[Display(ResourceType = typeof(Resources), Name = "Period", GroupName = "Common", Order = 20)]
		public int Period
		{
			get => _sma.Period;
			set
			{
				if (value <= 0)
					return;

				_sma.Period = value;
				RecalculateValues();
			}
		}

		#endregion

		#region ctor

		public ADR()
			: base(true)
		{
			_sma.Period = 10;
			_startSession = 0;
			_adrHigh = 0;
			_adrLow = 0;
			_lastSessionAdded = false;
		}

		#endregion

		#region Protected methods

		protected override void OnCalculate(int bar, decimal value)
		{
			if (bar == 0)
			{
				HorizontalLinesTillTouch.Clear();
				_startSession = bar;
				_adrHigh = 0;
				_adrLow = 0;
				_lastSessionAdded = false;
			}

			if (IsNewSession(bar))
			{
				var lineLength = bar - _startSession;
				HorizontalLinesTillTouch.Add(new LineTillTouch(_startSession, _adrHigh, _style, lineLength));
				HorizontalLinesTillTouch.Add(new LineTillTouch(_startSession, _adrLow, _style, lineLength));
				_startSession = bar;
				_adrHigh = 0;
				_adrLow = 0;
			}

			var currentCandle = GetCandle(bar);
			var difference = currentCandle.High - currentCandle.Low;
			var adr = _sma.Calculate(bar, difference);

			var adrHigh = difference < adr
				? currentCandle.Low + adr
				: currentCandle.Close >= currentCandle.Open
					? currentCandle.Low + adr
					: currentCandle.High;

			var adrLow = difference < adr
				? currentCandle.High - adr
				: currentCandle.Close >= currentCandle.Open
					? currentCandle.Low
					: currentCandle.High - adr;

			if (_adrHigh < adrHigh)
				_adrHigh = adrHigh;

			if (_adrLow > adrLow || bar == 0 || IsNewSession(bar))
				_adrLow = adrLow;

			if (bar == SourceDataSeries.Count - 1)
			{
				if (IsNewSession(bar))
					_lastSessionAdded = false;

				var lineLength = bar - _startSession;

				if (!_lastSessionAdded)
				{
					HorizontalLinesTillTouch.Add(new LineTillTouch(_startSession, _adrHigh, _style, lineLength));
					HorizontalLinesTillTouch.Add(new LineTillTouch(_startSession, _adrLow, _style, lineLength));
					_lastSessionAdded = true;
				}
				else
				{
					var lastInd = HorizontalLinesTillTouch.FindLastIndex(x => true);

					HorizontalLinesTillTouch[lastInd].FirstPrice = HorizontalLinesTillTouch[lastInd].SecondPrice
						= _adrHigh;

					HorizontalLinesTillTouch[lastInd - 1].FirstPrice = HorizontalLinesTillTouch[lastInd - 1].SecondPrice
						= _adrLow;
				}
			}
		}

		#endregion
	}
}