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

				AttemptsUntilReversal						= 7;
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

			ProcessBar(0);
		}
		#endregion

		#region ProcessBar()
		private void ProcessBar(int barsAgo = 0)
		{
			bool higherLow	= PA.IsHigherLow(barsAgo, 1);
			bool lowerLow 	= PA.IsLowerLow(barsAgo, 1);
			bool higherHigh = PA.IsHigherHigh(barsAgo, 1);
			bool lowerhigh 	= PA.IsLowerHigh(barsAgo, 1);

			bool previousLowerHigh	= PA.IsLowerHigh(barsAgo + 1, 1);
			bool previousHigherLow	= PA.IsHigherLow(barsAgo + 1, 1);

			Lows[barsAgo] 	= lowerLow ? Low[barsAgo] : Lows[barsAgo + 1];
			Highs[barsAgo]	= higherHigh ? High[barsAgo] : Highs[barsAgo + 1];

			if (High[barsAgo] > SwingHighs[barsAgo + 1] || (BullContinuationAttempts[barsAgo + 1] == AttemptsUntilReversal && higherHigh && previousLowerHigh)) {
				SwingHighs[barsAgo] 				= High[barsAgo];
				BullContinuationAttempts[barsAgo]	= 0;
			} else {
				SwingHighs[barsAgo] 				= SwingHighs[barsAgo + 1];
				BullContinuationAttempts[barsAgo]	= higherHigh && previousLowerHigh ? BullContinuationAttempts[barsAgo + 1] + 1 : BullContinuationAttempts[barsAgo + 1];
			}

			if (Low[barsAgo] < SwingLows[barsAgo + 1] || (BearContinuationAttempts[barsAgo + 1] == AttemptsUntilReversal && lowerLow && previousHigherLow)) {
				SwingLows[barsAgo]				= Low[barsAgo];
				BearContinuationAttempts[barsAgo]	= 0;
			} else {
				SwingLows[barsAgo]				= SwingLows[barsAgo + 1];
				BearContinuationAttempts[barsAgo]	= lowerLow && previousHigherLow ? BearContinuationAttempts[barsAgo + 1] + 1 : BearContinuationAttempts[barsAgo + 1];
			}
		}
		#endregion

		#region ResetToBar()
		public void ResetToBar(int barsAgo)
		{
			if (CurrentBar == 0) {
				return;
			}

			int firstBar = Math.Min(barsAgo, CurrentBar - 1);
			int primerBar = Math.Min(barsAgo + 1, CurrentBar);

			Highs[primerBar] 					= High[primerBar];
			SwingHighs[primerBar] 				= High[primerBar];
			Lows[primerBar]						= Low[primerBar];
			SwingLows[primerBar]				= Low[primerBar];
			BullContinuationAttempts[primerBar]	= 0;
			BearContinuationAttempts[primerBar]	= 0;

			for (int i = firstBar; i >= 0; i--) {
				ProcessBar(i);
			}
		}
		#endregion

		#region Properties
		[NinjaScriptProperty]
		[Range(-1, int.MaxValue)]
		[Display(Name="Attempts Until Reversal", Description="Attempts Until Reversal", Order=0, GroupName="Parameters")]
		public int AttemptsUntilReversal
		{ get; set; }
		#endregion
	}
}
#region NinjaScript Legacy/Convenience Constructors
namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		public PR.HighsAndLowsCounter HighsAndLowsCounter()
		{
			return HighsAndLowsCounter(Input, 7);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.PR.HighsAndLowsCounter HighsAndLowsCounter()
		{
			return indicator.HighsAndLowsCounter(Input, 7);
		}
	}
}
#endregion

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private PR.HighsAndLowsCounter[] cacheHighsAndLowsCounter;
		public PR.HighsAndLowsCounter HighsAndLowsCounter(int attemptsUntilReversal)
		{
			return HighsAndLowsCounter(Input, attemptsUntilReversal);
		}

		public PR.HighsAndLowsCounter HighsAndLowsCounter(ISeries<double> input, int attemptsUntilReversal)
		{
			if (cacheHighsAndLowsCounter != null)
				for (int idx = 0; idx < cacheHighsAndLowsCounter.Length; idx++)
					if (cacheHighsAndLowsCounter[idx] != null && cacheHighsAndLowsCounter[idx].AttemptsUntilReversal == attemptsUntilReversal && cacheHighsAndLowsCounter[idx].EqualsInput(input))
						return cacheHighsAndLowsCounter[idx];
			return CacheIndicator<PR.HighsAndLowsCounter>(new PR.HighsAndLowsCounter(){ AttemptsUntilReversal = attemptsUntilReversal }, input, ref cacheHighsAndLowsCounter);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.PR.HighsAndLowsCounter HighsAndLowsCounter(int attemptsUntilReversal)
		{
			return indicator.HighsAndLowsCounter(Input, attemptsUntilReversal);
		}

		public Indicators.PR.HighsAndLowsCounter HighsAndLowsCounter(ISeries<double> input , int attemptsUntilReversal)
		{
			return indicator.HighsAndLowsCounter(input, attemptsUntilReversal);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.PR.HighsAndLowsCounter HighsAndLowsCounter(int attemptsUntilReversal)
		{
			return indicator.HighsAndLowsCounter(Input, attemptsUntilReversal);
		}

		public Indicators.PR.HighsAndLowsCounter HighsAndLowsCounter(ISeries<double> input , int attemptsUntilReversal)
		{
			return indicator.HighsAndLowsCounter(input, attemptsUntilReversal);
		}
	}
}

#endregion
