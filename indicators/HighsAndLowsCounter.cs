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
	public class HighsAndLowsCounter : Indicator
	{
		#region Parameters
		private PriceActionUtils PA;
		public Series<double> Highs;
		public Series<double> Lows;
		public Series<double> Reversal;
		public Series<double> SwingHighs;
		public Series<double> SwingLows;
		public Series<double> BullContinuationAttempts;
		public Series<double> BearContinuationAttempts;
		private TrendDirection Direction;
		#endregion

		#region OnStageChange()
		protected override void OnStateChange()
		{
			#region State.SetDefaults
			if (State == State.SetDefaults)
			{
				Description									= @"Counts Bullish and Bearish Pullbacks";
				Name										= "Highs and Lows Counter";
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
			}
			#endregion

			#region State.Configure
			else if (State == State.Configure)
			{
				PA = PriceActionUtils();
			}
			#endregion

			#region State.DataLoaded
			else if (State == State.DataLoaded)
			{
				Highs		= new Series<double>(this);
				Lows 		= new Series<double>(this);
				Reversal 	= new Series<double>(this);
				SwingHighs	= new Series<double>(this);
				SwingLows 	= new Series<double>(this);

				BullContinuationAttempts = new Series<double>(this);
				BearContinuationAttempts = new Series<double>(this);
			}
			#endregion
		}
		#endregion

		#region OnBarUpdate()
		protected override void OnBarUpdate()
		{
			if (CurrentBar < 3)
			{
				Highs[0] 		= High[0];
				SwingHighs[0] 	= High[0];
				Lows[0]			= Low[0];
				SwingLows[0]	= Low[0];

				return;
			}

			bool higherLow	= PA.isHigherLow(0, 1);
			bool lowerLow 	= PA.isLowerLow(0, 1);
			bool higherHigh = PA.isHigherHigh(0, 1);
			bool lowerhigh 	= PA.isLowerHigh(0, 1);

			bool previousLowerHigh	= PA.isLowerHigh(1, 1);
			bool previousHigherLow	= PA.isHigherLow(1, 1);

			Lows[0] 	= lowerLow ? Low[0] : Lows[1];
			Highs[0]	= higherHigh ? High[0] : Highs[0];

			if (High[0] > SwingHighs[1] || BullContinuationAttempts[0] == 6) {
				SwingHighs[0] 				= High[0];
				BullContinuationAttempts[0]	= 0;
			} else {
				BullContinuationAttempts[0]	= higherHigh && previousLowerHigh ? BullContinuationAttempts[1] + 1 : BullContinuationAttempts[1];
			}

			if (Low[0] < SwingLows[0] || BearContinuationAttempts[0] == 6) {
				SwingLows[0]				= Low[0];
				BearContinuationAttempts[0]	= 0;
			} else {
				BearContinuationAttempts[0]	= lowerLow && previousHigherLow ? BearContinuationAttempts[1] + 1 : BearContinuationAttempts[1];
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
		private PR.HighsAndLowsCounter[] cacheHighsAndLowsCounter;
		public PR.HighsAndLowsCounter HighsAndLowsCounter()
		{
			return HighsAndLowsCounter(Input);
		}

		public PR.HighsAndLowsCounter HighsAndLowsCounter(ISeries<double> input)
		{
			if (cacheHighsAndLowsCounter != null)
				for (int idx = 0; idx < cacheHighsAndLowsCounter.Length; idx++)
					if (cacheHighsAndLowsCounter[idx] != null &&  cacheHighsAndLowsCounter[idx].EqualsInput(input))
						return cacheHighsAndLowsCounter[idx];
			return CacheIndicator<PR.HighsAndLowsCounter>(new PR.HighsAndLowsCounter(), input, ref cacheHighsAndLowsCounter);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.PR.HighsAndLowsCounter HighsAndLowsCounter()
		{
			return indicator.HighsAndLowsCounter(Input);
		}

		public Indicators.PR.HighsAndLowsCounter HighsAndLowsCounter(ISeries<double> input )
		{
			return indicator.HighsAndLowsCounter(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.PR.HighsAndLowsCounter HighsAndLowsCounter()
		{
			return indicator.HighsAndLowsCounter(Input);
		}

		public Indicators.PR.HighsAndLowsCounter HighsAndLowsCounter(ISeries<double> input )
		{
			return indicator.HighsAndLowsCounter(input);
		}
	}
}

#endregion
