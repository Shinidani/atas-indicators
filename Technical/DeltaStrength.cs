﻿namespace ATAS.Indicators.Technical
{
	using System.ComponentModel;
	using System.ComponentModel.DataAnnotations;
	using System.Windows.Media;

	using ATAS.Indicators.Technical.Properties;

	using OFT.Attributes;

	[DisplayName("Delta Strength")]
	[HelpLink("https://support.atas.net/ru/knowledge-bases/2/articles/45992-delta-strength")]
	public class DeltaStrength : Indicator
	{
		#region Nested types

		public enum FilterType
		{
			[Display(ResourceType = typeof(Resources), Name = "Bullish")]
			Bull,

			[Display(ResourceType = typeof(Resources), Name = "Bearlish")]
			Bear,

			[Display(ResourceType = typeof(Resources), Name = "Any")]
			All
		}

		#endregion

		#region Fields

		private FilterType _negFilter;

		private ValueDataSeries _negSeries = new(Resources.Negative);
		private int _percentage;
		private FilterType _posFilter;
		private ValueDataSeries _posSeries = new(Resources.Positive);

		#endregion

		#region Properties

		[Display(ResourceType = typeof(Resources), Name = "MaxValue", GroupName = "Settings", Order = 100)]
		[Range(0, 100)]
		public Filter MaxFilter { get; set; }

		[Display(ResourceType = typeof(Resources), Name = "MinValue", GroupName = "Settings", Order = 110)]
		[Range(0, 100)]
		public Filter MinFilter { get; set; }

		[Display(ResourceType = typeof(Resources), Name = "PositiveDelta", GroupName = "Filter", Order = 200)]
		public FilterType PosFilter
		{
			get => _posFilter;
			set
			{
				_posFilter = value;
				RecalculateValues();
			}
		}

		[Display(ResourceType = typeof(Resources), Name = "NegativeDelta", GroupName = "Filter", Order = 210)]
		public FilterType NegFilter
		{
			get => _negFilter;
			set
			{
				_negFilter = value;
				RecalculateValues();
			}
		}

		#endregion

		#region ctor

		public DeltaStrength()
			: base(true)
		{
			DenyToChangePanel = true;
			_posFilter = _negFilter = FilterType.All;
			_percentage = 98;
			_posSeries.Color = Colors.Green;
			_negSeries.Color = Colors.Red;
			_posSeries.VisualType = _negSeries.VisualType = VisualMode.Dots;
			_posSeries.Width = _negSeries.Width = 4;

			MaxFilter = new Filter
			{
				Enabled = true,
				Value = 98
			};

			MinFilter = new Filter
			{
				Enabled = true,
				Value = 90
			};

			MaxFilter.PropertyChanged += FilterChanged;
			MinFilter.PropertyChanged += FilterChanged;
			DataSeries[0] = _posSeries;
			DataSeries.Add(_negSeries);
		}

		#endregion

		#region Protected methods

		protected override void OnCalculate(int bar, decimal value)
		{
			if (bar == 0)
				DataSeries.ForEach(x => x.Clear());

			if (!MaxFilter.Enabled && !MinFilter.Enabled)
				return;

			var candle = GetCandle(bar);

			if (candle.Delta < 0 && candle.MinDelta < 0
				&& candle.Delta <= candle.MinDelta * 0.01m * MinFilter.Value
				&& candle.Delta >= candle.MinDelta * 0.01m * MaxFilter.Value)
			{
				if (_negFilter == FilterType.All
					|| _negFilter == FilterType.Bull && candle.Close > candle.Open
					|| _negFilter == FilterType.Bear && candle.Close < candle.Open)
					_negSeries[bar] = candle.High + 2 * InstrumentInfo.TickSize;
				else
					_negSeries[bar] = 0;
			}
			else
				_negSeries[bar] = 0;

			if (candle.Delta > 0 && candle.MaxDelta > 0
				&& (candle.Delta >= candle.MaxDelta * 0.01m * MinFilter.Value || !MinFilter.Enabled)
				&& (candle.Delta <= candle.MaxDelta * 0.01m * MaxFilter.Value || !MaxFilter.Enabled))
			{
				if (_posFilter == FilterType.All
					|| _posFilter == FilterType.Bull && candle.Close > candle.Open
					|| _posFilter == FilterType.Bear && candle.Close < candle.Open)
					_posSeries[bar] = candle.Low - 2 * InstrumentInfo.TickSize;
				else
					_posSeries[bar] = 0;
			}
			else
				_posSeries[bar] = 0;
		}

		#endregion

		#region Private methods

		private void FilterChanged(object sender, PropertyChangedEventArgs e)
		{
			if (!MaxFilter.Enabled && !MinFilter.Enabled)
				return;

			RecalculateValues();
		}

		#endregion
	}
}