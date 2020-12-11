﻿namespace ATAS.Indicators.Technical
{
	using System.ComponentModel;
	using System.ComponentModel.DataAnnotations;

	using ATAS.Indicators.Technical.Properties;

	[DisplayName("BollingerBands: Bandwidth")]
	public class BollingerBandsBandwidth : Indicator
	{
		#region Fields

		private readonly BollingerBands _bb = new BollingerBands();

		private readonly ValueDataSeries _renderSeries = new ValueDataSeries(Resources.Visualization);

		#endregion

		#region Properties

		[Display(ResourceType = typeof(Resources), Name = "Period", GroupName = "Settings", Order = 100)]
		public int Period
		{
			get => _bb.Period;
			set
			{
				if (value <= 0)
					return;

				_bb.Period = value;
				RecalculateValues();
			}
		}
		[Display(ResourceType = typeof(Resources), Name = "BBandsWidth", GroupName = "Settings", Order = 110)]
		public decimal Width
		{
			get => _bb.Width;
			set
			{
				if (value <= 0)
					return;

				_bb.Width = value;
				RecalculateValues();
			}
		}

		#endregion

		#region ctor

		public BollingerBandsBandwidth()
		{
			Panel = IndicatorDataProvider.NewPanel;
			_bb.Period = 10;
			_bb.Width = 1;
			DataSeries[0] = _renderSeries;
		}

		#endregion

		#region Protected methods

		protected override void OnCalculate(int bar, decimal value)
		{
			_bb.Calculate(bar, value);
			var sma = ((ValueDataSeries)_bb.DataSeries[0])[bar];
			var top = ((ValueDataSeries)_bb.DataSeries[1])[bar];
			var bot = ((ValueDataSeries)_bb.DataSeries[2])[bar];

			if (sma == 0)
				return;

			_renderSeries[bar] = (top - bot) / sma;
		}

		#endregion
	}
}