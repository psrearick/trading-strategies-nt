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
		private Utils utils = new Utils();

		private int window = 81;
		public bool init = false;
		public EntryEvaluator entryEvaluator;
		public PriceActionUtils pa;

		public TrendDirection CurrentDirection = TrendDirection.Flat;

		public TrendDirection Direction;
		public MarketCycleStage TrendType;

		public int EntryBar;
		public int PreviousSwing;
		public double PreviousSwingValue;

		public bool IsEnabled = true;
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

		public double TrendDirectionChanged = 0;
		public double CounterTrendTightChannel = 0;
		public double CounterTrendBroadChannel = 0;
		public double CounterTrendBreakout = 0;
		public double CounterTrendBreakoutTrend = 0;
		public double CounterTrendLegLong = 0;
		public double CounterTrendLegShort = 0;
		public double CounterTrendLegAfterDoubleTopBottom = 0;
		public double TrailingStopBeyondPreviousExtreme = 0;
		public double MovingAverageCrossover = 0;
		public double DoubleTopBottom = 0;
		public double NoNewExtreme8 = 0;
		public double NoNewExtreme10 = 0;
		public double NoNewExtreme12 = 0;
		public double CounterTrendPressure = 0;
		public double CounterTrendStrongPressure = 0;
		public double CounterTrendWeakTrend = 0;
		public double CounterTrendStrongTrend = 0;
		public double RSIOutOfRange = 0;
		public double ATRAboveAverageATR = 0;
		public double ATRBelowAverageATR = 0;
		public double ATRAboveAverageATRByAStdDev = 0;
		public double ATRBelowAverageATRByAStdDev = 0;
		public double StrongCounterTrendFollowThrough = 0;
		public double ProfitTarget1 = 0;
		public double ProfitTarget2 = 0;
		public double ProfitTarget3 = 0;
		public double ProfitTarget4 = 0;
		public double ProfitTarget5 = 0;

		public double StopLoss = 0;

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

			#region State.Configure
			else if (State == State.Configure)
			{
				pa = PriceActionUtils();
			}
			#endregion
		}
		#endregion

		#region OnBarUpdate()
		protected override void OnBarUpdate()
		{
			if (init) {
				EvaluateExitConditions();
			}
		}
		#endregion

		#region UpdateStopLoss()
		private void UpdateStopLoss()
		{
			if (Direction == TrendDirection.Bullish) {
				double swingLow = entryEvaluator.md.LegLong.BarsAgoStarts[0] > 0 ? MIN(Low, entryEvaluator.md.LegLong.BarsAgoStarts[0])[0] : Low[0];

				if (swingLow > StopLoss && entryEvaluator.md.LegLong[0] > 0) {
					StopLoss = swingLow;
				}
			}

			if (Direction == TrendDirection.Bearish) {
				double swingHigh = entryEvaluator.md.LegLong.BarsAgoStarts[0] > 0 ? MAX(High, entryEvaluator.md.LegLong.BarsAgoStarts[0])[0] : High[0];

				if ((swingHigh < StopLoss || StopLoss == 0) && entryEvaluator.md.LegLong[0] < 0) {
					StopLoss = swingHigh;
				}
			}
		}
		#endregion

		#region CalculateAdditionalValues()
		public void CalculateAdditionalValues()
	    {
//			pa = entryEvaluator.pa;
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
			IsWithTrendTrendBar 		= pa.IsTrendBar(0) && (Direction == TrendDirection.Bullish ? pa.IsBullishBar(0) : pa.IsBearishBar(0));
			IsBreakoutBarPattern		= pa.DoesInsideOutsideMatch("ii", 0) || pa.DoesInsideOutsideMatch("ioi", 0);
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
			HighestHigh 		= period > 0 ? pa.HighestHigh(0, period) : High[0];
			LowestLow		= period > 0 ? pa.LowestLow(0, period) : Low[0];
			GreatestProfit	= Direction == TrendDirection.Bullish ? HighestHigh - CloseEntry : CloseEntry - LowestLow;
			GreatestLoss		= Direction == TrendDirection.Bullish ? CloseEntry - LowestLow : HighestHigh - CloseEntry;
			ProfitMultiples	= GreatestProfit / StopDistance;

			IsSuccessful = (ProfitMultiples > 1);

			if ((CurrentBar - EntryBar) > window) {
				IsClosed = true;
			}

			if (GreatestLoss > StopDistance) {
				IsClosed = true;
			}

//			if (CurrentDirection != TrendDirection.Flat && CurrentDirection != Direction) {
//				IsClosed = true;
//			}
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

		#region EvaluateExitConditions()
		public void EvaluateExitConditions()
		{
			DistanceMoved = Direction == TrendDirection.Bullish ? Close[0] - CloseEntry : CloseEntry - Close[0];
			int barsAgo = entryEvaluator.md.LegLong.BarsAgoStarts[0];
			int previousSwing = Direction == TrendDirection.Bearish
				? entryEvaluator.pa.BarsAgoHigh(0, barsAgo)
				: entryEvaluator.pa.BarsAgoLow(0, barsAgo);

			if (previousSwing == 0) {
				return;
			}

			#region TrendDirectionChanged
			if (TrendDirectionChanged == 0) {
				if (entryEvaluator.md.Direction[0] != TrendDirection.Flat
					&& entryEvaluator.md.Direction[0] != Direction)
				{
					TrendDirectionChanged = DistanceMoved;
				}
			}
			#endregion

			#region CounterTrendTightChannel
			if (CounterTrendTightChannel == 0) {
				if (entryEvaluator.md.TightChannels[0] != TrendDirection.Flat
					&& entryEvaluator.md.TightChannels[0] != Direction)
				{
					CounterTrendTightChannel = DistanceMoved;
				}
			}
			#endregion

			#region CounterTrendBroadChannel
			if (CounterTrendBroadChannel == 0) {
				if (entryEvaluator.md.BroadChannels[0] != TrendDirection.Flat
					&& entryEvaluator.md.BroadChannels[0] != Direction)
				{
					CounterTrendBroadChannel = DistanceMoved;
				}
			}
			#endregion

			#region CounterTrendBreakout
			if (CounterTrendBreakout == 0) {
				if (entryEvaluator.md.Breakouts[0] != TrendDirection.Flat
					&& entryEvaluator.md.Breakouts[0] != Direction)
				{
					CounterTrendBreakout = DistanceMoved;
				}
			}
			#endregion

			#region CounterTrendBreakoutTrend
			if (CounterTrendBreakoutTrend == 0) {
				if (Direction == TrendDirection.Bullish
					&& pa.IsBreakoutTrend(0, entryEvaluator.md.LegLong.BarsAgoStarts[0], TrendDirection.Bearish))
				{
					CounterTrendBreakoutTrend = DistanceMoved;
				}

				if (Direction == TrendDirection.Bearish
					&& pa.IsBreakoutTrend(0, entryEvaluator.md.LegLong.BarsAgoStarts[0], TrendDirection.Bullish))
				{
					CounterTrendBreakoutTrend = DistanceMoved;
				}
			}
			#endregion

			#region CounterTrendLegLong
			if (CounterTrendLegLong == 0) {
				if (entryEvaluator.md.LegLong.LegDirectionAtBar(0) != TrendDirection.Flat
					&& entryEvaluator.md.LegLong.LegDirectionAtBar(0) != Direction)
				{
					CounterTrendLegLong = DistanceMoved;
				}
			}
			#endregion

			#region CounterTrendLegShort
			if (CounterTrendLegShort == 0) {
				if (entryEvaluator.md.LegShort.LegDirectionAtBar(0) != TrendDirection.Flat
					&& entryEvaluator.md.LegShort.LegDirectionAtBar(0) != Direction)
				{
					CounterTrendLegShort = DistanceMoved;
				}
			}
			#endregion

			#region DoubleTopBottom
			if (DoubleTopBottom == 0) {
				if (Direction == TrendDirection.Bullish && entryEvaluator.barsSinceDoubleTop[0] == 0) {
					DoubleTopBottom = DistanceMoved;
				}

				if (Direction == TrendDirection.Bearish && entryEvaluator.barsSinceDoubleBottom[0] == 0) {
					DoubleTopBottom = DistanceMoved;
				}
			}
			#endregion

			#region CounterTrendLegAfterDoubleTopBottom
			if (CounterTrendLegAfterDoubleTopBottom == 0) {
				if (Direction == TrendDirection.Bullish
					&& entryEvaluator.barsSinceDoubleTop[0] > 0
					&& entryEvaluator.barsSinceDoubleTop[0] < 10
					&& entryEvaluator.md.LegLong.LegDirectionAtBar(0) == TrendDirection.Bearish) {
					CounterTrendLegAfterDoubleTopBottom = DistanceMoved;
				}
				if (Direction == TrendDirection.Bearish
					&& entryEvaluator.barsSinceDoubleBottom[0] > 0
					&& entryEvaluator.barsSinceDoubleBottom[0] < 10
					&& entryEvaluator.md.LegLong.LegDirectionAtBar(0) == TrendDirection.Bullish) {
					CounterTrendLegAfterDoubleTopBottom = DistanceMoved;
				}
			}
			#endregion

			#region TrailingStopBeyondPreviousExtreme
			if (TrailingStopBeyondPreviousExtreme == 0) {
				if (Direction == TrendDirection.Bullish && Low[0] < StopLoss)
				{
					TrailingStopBeyondPreviousExtreme = DistanceMoved;
				}

				if (Direction == TrendDirection.Bearish && High[0] > StopLoss)
				{
					TrailingStopBeyondPreviousExtreme = DistanceMoved;
				}
			}
			#endregion

			#region ProfitTargetExits
			double multiple = DistanceMoved / StopDistance;
			if (ProfitTarget1 == 0 && multiple >= 1) ProfitTarget1 = DistanceMoved;
			if (ProfitTarget2 == 0 && multiple >= 2) ProfitTarget2 = DistanceMoved;
			if (ProfitTarget3 == 0 && multiple >= 3) ProfitTarget3 = DistanceMoved;
			if (ProfitTarget4 == 0 && multiple >= 4) ProfitTarget4 = DistanceMoved;
			if (ProfitTarget5 == 0 && multiple >= 5) ProfitTarget5 = DistanceMoved;
			#endregion

			#region MovingAverageCrossover
			if (MovingAverageCrossover == 0) {
				if (Direction == TrendDirection.Bullish && entryEvaluator.emaFast[0] < entryEvaluator.emaSlow[0])
				{
					MovingAverageCrossover = DistanceMoved;
				}

				if (Direction == TrendDirection.Bearish && entryEvaluator.emaFast[0] > entryEvaluator.emaSlow[0])
				{
					MovingAverageCrossover = DistanceMoved;
				}
			}
			#endregion

			#region NoNewExtreme8
			if (NoNewExtreme8 == 0) {
				if (barsAgo > 0) {
					if (Direction == TrendDirection.Bullish
						&& MAX(High, 8)[0] < MAX(High, barsAgo)[0])
					{
						NoNewExtreme8 = DistanceMoved;
					}

					if (Direction == TrendDirection.Bearish
						&& MIN(Low, 8)[0] > MIN(Low, barsAgo)[0])
					{
						NoNewExtreme8 = DistanceMoved;
					}
				}
			}
			#endregion

			#region NoNewExtreme10
			if (NoNewExtreme10 == 0) {
				if (barsAgo > 0) {
					if (Direction == TrendDirection.Bullish
						&& MAX(High, 10)[0] < MAX(High, barsAgo)[0])
					{
						NoNewExtreme10 = DistanceMoved;
					}

					if (Direction == TrendDirection.Bearish
						&& MIN(Low, 10)[0] > MIN(Low, barsAgo)[0])
					{
						NoNewExtreme10 = DistanceMoved;
					}
				}
			}
			#endregion

			#region NoNewExtreme12
			if (NoNewExtreme12 == 0) {
				if (barsAgo > 0) {
					if (Direction == TrendDirection.Bullish
						&& MAX(High, 12)[0] < MAX(High, barsAgo)[0])
					{
						NoNewExtreme12 = DistanceMoved;
					}

					if (Direction == TrendDirection.Bearish
						&& MIN(Low, 12)[0] > MIN(Low, barsAgo)[0])
					{
						NoNewExtreme12 = DistanceMoved;
					}
				}
			}
			#endregion

			#region BuySellPressure
			double currentBuySellPressure = pa.GetBuySellPressure(0, PreviousSwing);

			#region CounterTrendPressure
			if (CounterTrendPressure == 0) {
				if (Direction == TrendDirection.Bullish && currentBuySellPressure < 25) {
					CounterTrendPressure = DistanceMoved;
				}

				if (Direction == TrendDirection.Bearish && currentBuySellPressure > 75) {
					CounterTrendPressure = DistanceMoved;
				}
			}
			#endregion

			#region CounterTrendStrongPressure
			if (CounterTrendStrongPressure == 0) {
				if (Direction == TrendDirection.Bullish && BuySellPressure < 10) {
					CounterTrendStrongPressure = DistanceMoved;
				}

				if (Direction == TrendDirection.Bearish && BuySellPressure > 90) {
					CounterTrendStrongPressure = DistanceMoved;
				}
			}
			#endregion
			#endregion

			#region CounterTrendWeakTrend
			if (CounterTrendWeakTrend == 0) {
				if (Direction == TrendDirection.Bullish && pa.IsWeakBearishTrend(0, PreviousSwing)) {
					CounterTrendWeakTrend = DistanceMoved;
				}

				if (Direction == TrendDirection.Bearish && pa.IsWeakBullishTrend(0, PreviousSwing)) {
					CounterTrendWeakTrend = DistanceMoved;
				}
			}
			#endregion

			#region CounterTrendStrongTrend
			if (CounterTrendStrongTrend == 0) {
				if (Direction == TrendDirection.Bullish && pa.IsStrongBearishTrend(0, PreviousSwing)) {
					CounterTrendStrongTrend = DistanceMoved;
				}

				if (Direction == TrendDirection.Bearish && pa.IsStrongBullishTrend(0, PreviousSwing)) {
					CounterTrendStrongTrend = DistanceMoved;
				}
			}
			#endregion

			#region RSIOutOfRange
			if (RSIOutOfRange == 0) {
				if (Direction == TrendDirection.Bullish && entryEvaluator.rsi[0] < 30) {
					RSIOutOfRange = DistanceMoved;
				}

				if (Direction == TrendDirection.Bearish && entryEvaluator.rsi[0] > 70) {
					RSIOutOfRange = DistanceMoved;
				}
			}
			#endregion

			#region ATRAboveAverageATR
			if (ATRAboveAverageATR == 0 && entryEvaluator.atr[0] > entryEvaluator.avgAtr[0]) {
				ATRAboveAverageATR = DistanceMoved;
			}
			#endregion

			#region ATRBelowAverageATR
			if (ATRBelowAverageATR == 0 && entryEvaluator.atr[0] < entryEvaluator.avgAtr[0]) {
				ATRBelowAverageATR = DistanceMoved;
			}
			#endregion

			#region ATRAboveAverageATRByAStdDev
			if (ATRAboveAverageATRByAStdDev == 0
					&& (entryEvaluator.atr[0] - entryEvaluator.avgAtr[0]) > entryEvaluator.stdDevAtr[0]) {
				ATRAboveAverageATRByAStdDev = DistanceMoved;
			}
			#endregion

			#region ATRBelowAverageATRByAStdDev
			if (ATRBelowAverageATRByAStdDev == 0
					&& (entryEvaluator.avgAtr[0] - entryEvaluator.atr[0]) > entryEvaluator.stdDevAtr[0]) {
				ATRBelowAverageATRByAStdDev = DistanceMoved;
			}
			#endregion

			#region StrongCounterTrendFollowThrough
			if (StrongCounterTrendFollowThrough == 0) {
				if (Direction == TrendDirection.Bullish && pa.IsBearishBar(0) && pa.IsStrongFollowThroughBar(0)) {
					StrongCounterTrendFollowThrough = DistanceMoved;
				}

				if (Direction == TrendDirection.Bearish && pa.IsBullishBar(0) && pa.IsStrongFollowThroughBar(0)) {
					StrongCounterTrendFollowThrough = DistanceMoved;
				}
			}
			#endregion
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
