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
	public class PullbackCounter : Indicator
	{
		#region Parameters
		private HighsAndLowsCounter HLCount;
		private Gui.Tools.SimpleFont textFont;
		#endregion

		#region OnStageChange()
		protected override void OnStateChange()
		{
			#region State.SetDefaults
			if (State == State.SetDefaults)
			{
				Description									= @"Counts Bullish and Bearish Pullbacks";
				Name										= "Pullback Counter";
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

				AddPlot(new Stroke(Brushes.Green, 3), PlotStyle.Line, "Bull Attempts");
				AddPlot(new Stroke(Brushes.Red, 3), PlotStyle.Line, "Bear Attempts");
			}
			#endregion

			#region State.Configure
			else if (State == State.Configure)
			{
				HLCount = HighsAndLowsCounter();
			}
			#endregion

			#region State.DataLoaded
			else if (State == State.DataLoaded)
			{
			}
			#endregion
		}
		#endregion

		#region OnBarUpdate()
		protected override void OnBarUpdate()
		{
			Values[0][0] = HLCount.BullContinuationAttempts[0];
			Values[1][0] = HLCount.BearContinuationAttempts[1];

			double yBull = (High[0] + (TickSize * 12));
			double yBear = (Low[0] - (TickSize * 12));

			if ( HLCount.BullContinuationAttempts[0] != HLCount.BullContinuationAttempts[1] ) {
				Draw.Text(this,"bullTag" + CurrentBar, true, HLCount.BullContinuationAttempts[0].ToString(), 0, yBull, 0, Brushes.Black, textFont, TextAlignment.Center, Brushes.Transparent, Brushes.Transparent, 0);
			}

			if ( HLCount.BearContinuationAttempts[0] != HLCount.BearContinuationAttempts[1] ) {
				Draw.Text(this,"bearTag" + CurrentBar, true, HLCount.BearContinuationAttempts[0].ToString(), 0, yBear, 0, Brushes.Black, textFont, TextAlignment.Center, Brushes.Transparent, Brushes.Transparent, 0);
			}
		}
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private PR.PullbackCounter[] cachePullbackCounter;
		public PR.PullbackCounter PullbackCounter()
		{
			return PullbackCounter(Input);
		}

		public PR.PullbackCounter PullbackCounter(ISeries<double> input)
		{
			if (cachePullbackCounter != null)
				for (int idx = 0; idx < cachePullbackCounter.Length; idx++)
					if (cachePullbackCounter[idx] != null &&  cachePullbackCounter[idx].EqualsInput(input))
						return cachePullbackCounter[idx];
			return CacheIndicator<PR.PullbackCounter>(new PR.PullbackCounter(), input, ref cachePullbackCounter);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.PR.PullbackCounter PullbackCounter()
		{
			return indicator.PullbackCounter(Input);
		}

		public Indicators.PR.PullbackCounter PullbackCounter(ISeries<double> input )
		{
			return indicator.PullbackCounter(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.PR.PullbackCounter PullbackCounter()
		{
			return indicator.PullbackCounter(Input);
		}

		public Indicators.PR.PullbackCounter PullbackCounter(ISeries<double> input )
		{
			return indicator.PullbackCounter(input);
		}
	}
}

#endregion
