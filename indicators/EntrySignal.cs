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
	public class EntrySignal : Indicator
	{
		#region Variables

		private int window = 81;

		private Utils utils = new Utils();

		private PriceActionUtils pa;

		public TrendDirection CurrentDirection = TrendDirection.Flat;

		public TrendDirection Direction;
		public MarketCycleStage TrendType;

		public int EntryBar;
		public int PreviousSwing;
		public double PreviousSwingValue;

		public bool IsClosed;
		public bool IsSuccessful;
		public double StopDistance;
		public double DistanceMoved;
		public double HighestHigh;
		public double LowestLow;
		public double GreatestProfit;
		public double GreatestLoss;
		public double ProfitMultiples;

		public double Rsi;
		public double Atr;
		public double StdDevAtr;
		public double AvgAtr;
		public double EmaFast;
		public double EmaSlow;

		public double HighEntry;
		public double LowEntry;
		public double OpenEntry;
		public double CloseEntry;

	    public TrendDirection FastEMADirection;
	    public TrendDirection SlowEMADirection;
	    public double DistanceFromFastEMA;
	    public double DistanceFromSlowEMA;

		public bool IsEMADiverging;
		public bool IsEMAConverging;
		public bool IsWithTrendEMA;
		public bool IsWithTrendFastEMA;
		public bool IsWithTrendSlowEMA;
		public bool LeadsFastEMAByMoreThanATR;

		public double BuySellPressure;
		public bool IsWithTrendPressure;
		public bool IsStrongWithTrendPressure;

		public bool IsWithTrendTrendBar;
		public bool IsBreakoutBarPattern;
		public bool IsWeakBar;
		public bool IsStrongFollowThrough;

		public bool IsBreakout;
		public bool IsBroadChannel;
		public bool IsTightChannel;

		public bool IsWeakTrend;
		public bool IsStrongTrend;

		public bool IsRSIInRange;
		public bool IsAboveAverageATR;
		public bool IsBelowAverageATR;
		public bool IsAboveAverageATRByAStdDev;

		#endregion

		#region OnStateChange()
		protected override void OnStateChange()
		{
			#region State.SetDefaults
			if (State == State.SetDefaults)
			{
				Description									= @"";
				Name										= "Entry";
				Calculate									= Calculate.OnBarClose;
			}
			#endregion
			else if (State == State.Configure)
			{
				pa = PriceActionUtils();
			}
		}
		#endregion

		#region OnBarUpdate()
		protected override void OnBarUpdate()
		{
			pa.Update();
		}
		#endregion

		#region CalculateAdditionalValues()
		public void CalculateAdditionalValues()
	    {
			CalculateEMAValues();
			CalculateStopDistance();
			CalculateBuySellPressure();
			EvaluateBarTrend();
			EvaluateChartTrend();
			EvaluateIndicators();
	    }
		#endregion

		#region EvaluateIndicators()
		private void EvaluateIndicators()
		{
			IsRSIInRange 				= Direction == TrendDirection.Bullish ? (Rsi > 50 && Rsi < 70) : (Rsi > 30 && Rsi < 50);
			IsAboveAverageATR			= Atr > AvgAtr;
			IsBelowAverageATR			= Atr < AvgAtr;
			IsAboveAverageATRByAStdDev	= (Atr - AvgAtr) > StdDevAtr;
		}
		#endregion

		#region EvaluateBarTrend()
		private void EvaluateBarTrend()
		{
			IsWithTrendTrendBar 	= pa.IsTrendBar(0) && (Direction == TrendDirection.Bullish ? pa.IsBullishBar(0) : pa.IsBearishBar(0));
			IsBreakoutBarPattern	= pa.DoesInsideOutsideMatch("ii", 0) || pa.DoesInsideOutsideMatch("ioi", 0);
			IsWeakBar				= pa.IsDoji(0) || pa.IsTradingRangeBar(0);
			IsStrongFollowThrough	= pa.IsStrongFollowThroughBar(0);
		}
		#endregion

		#region EvaluateChartTrend()
		private void EvaluateChartTrend()
		{
			IsBreakout		= TrendType == MarketCycleStage.Breakout;
			IsBroadChannel	= TrendType == MarketCycleStage.BroadChannel;
			IsTightChannel	= TrendType == MarketCycleStage.TightChannel;
			IsWeakTrend		= Direction == TrendDirection.Bullish ? pa.IsWeakBullishTrend(0, PreviousSwing) : pa.IsWeakBearishTrend(0, PreviousSwing);
			IsStrongTrend	= Direction == TrendDirection.Bullish ? pa.IsStrongBullishTrend(0, PreviousSwing) : pa.IsStrongBearishTrend(0, PreviousSwing);
		}
		#endregion

		#region CalculateBuySellPressure()
		public void CalculateBuySellPressure()
		{
			BuySellPressure 			= pa.GetBuySellPressure(0, PreviousSwing);
			IsWithTrendPressure 		= Direction == TrendDirection.Bullish ? BuySellPressure > 75 : BuySellPressure < 25;
			IsStrongWithTrendPressure	= Direction == TrendDirection.Bullish ? BuySellPressure > 90 : BuySellPressure < 10;
		}
		#endregion

		#region CalculateStopDistance()
		private void CalculateStopDistance()
		{
			PreviousSwingValue = Direction == TrendDirection.Bullish ? Low[PreviousSwing] : High[PreviousSwing];
			StopDistance = Direction == TrendDirection.Bullish ? CloseEntry - PreviousSwingValue : PreviousSwingValue - CloseEntry;
		}
		#endregion

		#region UpdateStatus()
		public void UpdateStatus()
	    {
			if (IsClosed) {
				return;
			}

			int period 		= CurrentBar - (EntryBar - PreviousSwing);
			DistanceMoved 	= Direction == TrendDirection.Bullish ? Close[0] - CloseEntry : CloseEntry - Close[0];
			HighestHigh 	= period > 0 ? pa.HighestHigh(0, period) : High[0];
			LowestLow		= period > 0 ? pa.LowestLow(0, period) : Low[0];
			GreatestProfit	= Direction == TrendDirection.Bullish ? HighestHigh - CloseEntry : CloseEntry - LowestLow;
			GreatestLoss	= Direction == TrendDirection.Bullish ? CloseEntry - LowestLow : HighestHigh - CloseEntry;
			ProfitMultiples	= GreatestProfit / StopDistance;

			IsSuccessful = (ProfitMultiples > 1);

			if ((CurrentBar - EntryBar) > window) {
				IsClosed = true;
			}

			if (GreatestLoss > StopDistance) {
				IsClosed = true;
			}

			if (CurrentDirection != TrendDirection.Flat && CurrentDirection != Direction) {
				IsClosed = true;
			}
	    }
		#endregion

		#region CalculateEMAValues()
		private void CalculateEMAValues()
		{
			FastEMADirection = pa.IsEMAFastBullish(0) ? TrendDirection.Bullish : pa.IsEMAFastBearish(0) ? TrendDirection.Bearish : TrendDirection.Flat;
			SlowEMADirection = pa.IsEMASlowBullish(0) ? TrendDirection.Bullish : pa.IsEMASlowBearish(0) ? TrendDirection.Bearish : TrendDirection.Flat;

			DistanceFromFastEMA = CloseEntry - EmaFast;
			DistanceFromSlowEMA = CloseEntry - EmaSlow;

			IsEMADiverging = false;
			IsEMAConverging = false;
			IsWithTrendEMA = false;

			if (Direction == TrendDirection.Bullish) {
				IsEMADiverging				= pa.IsEMABullishDivergence(0, 1);
				IsEMAConverging 			= pa.IsEMABullishConvergence(0, 1);
				IsWithTrendEMA 				= pa.IsEMABullish(0);
				LeadsFastEMAByMoreThanATR	= LowEntry > (EmaFast + Atr);
			}

			if (Direction == TrendDirection.Bearish) {
				IsEMADiverging 				= pa.IsEMABearishDivergence(0, 1);;
				IsEMAConverging 			= pa.IsEMABearishConvergence(0, 1);;
				IsWithTrendEMA 				= pa.IsEMABearish(0);
				LeadsFastEMAByMoreThanATR	= HighEntry < (EmaFast - Atr);
			}

			IsWithTrendFastEMA = FastEMADirection == Direction;
			IsWithTrendSlowEMA = SlowEMADirection == Direction;
		}
		#endregion

		#region Properties
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Trigger Bar", Description="Trigger Bar", Order=0, GroupName="Parameters")]
		public int TriggerBar
		{ get; set; }
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private PR.EntrySignal[] cacheEntrySignal;
		public PR.EntrySignal EntrySignal(int triggerBar)
		{
			return EntrySignal(Input, triggerBar);
		}

		public PR.EntrySignal EntrySignal(ISeries<double> input, int triggerBar)
		{
			if (cacheEntrySignal != null)
				for (int idx = 0; idx < cacheEntrySignal.Length; idx++)
					if (cacheEntrySignal[idx] != null && cacheEntrySignal[idx].TriggerBar == triggerBar && cacheEntrySignal[idx].EqualsInput(input))
						return cacheEntrySignal[idx];
			return CacheIndicator<PR.EntrySignal>(new PR.EntrySignal(){ TriggerBar = triggerBar }, input, ref cacheEntrySignal);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.PR.EntrySignal EntrySignal(int triggerBar)
		{
			return indicator.EntrySignal(Input, triggerBar);
		}

		public Indicators.PR.EntrySignal EntrySignal(ISeries<double> input , int triggerBar)
		{
			return indicator.EntrySignal(input, triggerBar);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.PR.EntrySignal EntrySignal(int triggerBar)
		{
			return indicator.EntrySignal(Input, triggerBar);
		}

		public Indicators.PR.EntrySignal EntrySignal(ISeries<double> input , int triggerBar)
		{
			return indicator.EntrySignal(input, triggerBar);
		}
	}
}

#endregion