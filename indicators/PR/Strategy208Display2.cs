#region Using declarations
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

//This namespace holds Indicators in this folder and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Indicators.PR
{
	public class Strategy208Display2 : Indicator
	{
		#region Variables
		private Strategy208Signals signals;
		private Brush brushUp;
		private Brush brushDown;
		#endregion

		#region OnStateChange()
		protected override void OnStateChange()
		{
			#region State.SetDefaults
			if (State == State.SetDefaults)
			{
				Description									= @"Enter the description for your new custom Indicator here.";
				Name										= "Strategy 2.0.8 Display";
				Calculate									= Calculate.OnBarClose;
				IsOverlay									= false;
				DisplayInDataBox							= true;
				DrawOnPricePanel							= true;
				DrawHorizontalGridLines						= true;
				DrawVerticalGridLines						= true;
				PaintPriceMarkers							= true;
				ScaleJustification							= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				//Disable this property if your indicator requires custom values that cumulate with each new market data event.
				//See Help Guide for additional information.
				IsSuspendedWhileInactive					= true;

				Threshold = 10;

				AddLine(Brushes.Black, Threshold, "Threshold");
				AddPlot(Brushes.Green, "Bullish Score");
				AddPlot(Brushes.Red, "Bearish Score");

				brushUp = Brushes.Green.Clone();
				brushUp.Opacity = 0.200;
				brushUp.Freeze();

				brushDown = Brushes.Red.Clone();
				brushDown.Opacity = 0.200;
				brushDown.Freeze();
			}
			#endregion
			#region State.Configure
			else if (State == State.Configure)
			{
				AddDataSeries(BarsPeriodType.Minute, 20);
				AddDataSeries(BarsPeriodType.Minute, 40);
				AddDataSeries(BarsPeriodType.Minute, 60);
				AddDataSeries(BarsPeriodType.Minute, 80);
				AddDataSeries(BarsPeriodType.Minute, 100);
				AddDataSeries(BarsPeriodType.Minute, 120);
				AddDataSeries(BarsPeriodType.Minute, 140);
				AddDataSeries(BarsPeriodType.Minute, 160);
				AddDataSeries(BarsPeriodType.Minute, 180);
				AddDataSeries(BarsPeriodType.Minute, 200);
			}
			#endregion

			#region State.DataLoaded
			if (State == State.DataLoaded)
			{
				signals = Strategy208Signals(BarsArray[LongPeriod]);
			}
			#endregion
		}
		#endregion

		#region OnBarUpdate()
		protected override void OnBarUpdate()
		{
			if (CurrentBar < 100) {
				return;
            }

			Values[0][0] = signals.LongScores[0];
			Values[1][0] = signals.ShortScores[0];

			BackBrush = signals.Signals[0] == TrendDirection.Bullish
				? brushUp
				: signals.Signals[0] == TrendDirection.Bearish
					? brushDown
					: Brushes.Transparent;
		}
		#endregion

		#region Properties
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Long Period", Description="Long Period", Order=0, GroupName="Parameters")]
		public int LongPeriod
		{ get; set; }

		[Range(0, 100), NinjaScriptProperty]
		[Display(Name = "Threshold", Description = "Threshold", GroupName = "Parameters", Order = 1)]
		public double Threshold
		{ get; set; }
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private PR.Strategy208Display2[] cacheStrategy208Display2;
		public PR.Strategy208Display2 Strategy208Display2(int longPeriod, double threshold)
		{
			return Strategy208Display2(Input, longPeriod, threshold);
		}

		public PR.Strategy208Display2 Strategy208Display2(ISeries<double> input, int longPeriod, double threshold)
		{
			if (cacheStrategy208Display2 != null)
				for (int idx = 0; idx < cacheStrategy208Display2.Length; idx++)
					if (cacheStrategy208Display2[idx] != null && cacheStrategy208Display2[idx].LongPeriod == longPeriod && cacheStrategy208Display2[idx].Threshold == threshold && cacheStrategy208Display2[idx].EqualsInput(input))
						return cacheStrategy208Display2[idx];
			return CacheIndicator<PR.Strategy208Display2>(new PR.Strategy208Display2(){ LongPeriod = longPeriod, Threshold = threshold }, input, ref cacheStrategy208Display2);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.PR.Strategy208Display2 Strategy208Display2(int longPeriod, double threshold)
		{
			return indicator.Strategy208Display2(Input, longPeriod, threshold);
		}

		public Indicators.PR.Strategy208Display2 Strategy208Display2(ISeries<double> input , int longPeriod, double threshold)
		{
			return indicator.Strategy208Display2(input, longPeriod, threshold);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.PR.Strategy208Display2 Strategy208Display2(int longPeriod, double threshold)
		{
			return indicator.Strategy208Display2(Input, longPeriod, threshold);
		}

		public Indicators.PR.Strategy208Display2 Strategy208Display2(ISeries<double> input , int longPeriod, double threshold)
		{
			return indicator.Strategy208Display2(input, longPeriod, threshold);
		}
	}
}

#endregion
