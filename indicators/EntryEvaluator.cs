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
	public class EntryEvaluator : Indicator
	{
		#region Variables

		public PriceActionUtils pa;
		private MarketDirection md;
		private RSI rsi;
		private ATR atr;
		private StdDev stdDevAtr;
		private SMA avgAtr;
		private EMA emaFast;
		private EMA emaSlow;

		private List<Entry> entries = new List<Entry>(100);
		private Series<int> entryID;
		private Series<int> currentEntryID;
		private Series<int> barsSinceEntry;

		#endregion

		#region OnStateChange()
		protected override void OnStateChange()
		{
			#region State.SetDefaults
			if (State == State.SetDefaults)
			{
				Description									= @"";
				Name										= "EntryEvaluator";
				Calculate									= Calculate.OnBarClose;
				IsOverlay									= false;
				DisplayInDataBox							= true;
				DrawOnPricePanel							= true;
				DrawHorizontalGridLines						= true;
				DrawVerticalGridLines						= true;
				PaintPriceMarkers							= true;
				ScaleJustification							= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				IsSuspendedWhileInactive					= true;
			}
			#endregion

			#region State.Configure
			else if (State == State.Configure)
			{
				pa 						= PriceActionUtils();
				md						= MarketDirection(10, 20);
				atr						= ATR(14);
				stdDevAtr				= StdDev(atr, 20);
				avgAtr					= SMA(atr, 20);
				rsi						= RSI(14, 1);
				emaFast					= EMA(9);
				emaSlow					= EMA(21);
			}
			#endregion

			#region State.DataLoaded
			if (State == State.DataLoaded) {
				entryID 		= new Series<int>(this, MaximumBarsLookBack.Infinite);
				currentEntryID	= new Series<int>(this, MaximumBarsLookBack.Infinite);
				barsSinceEntry	= new Series<int>(this, MaximumBarsLookBack.Infinite);
			}
			#endregion
		}
		#endregion

		#region OnBarUpdate()
		protected override void OnBarUpdate()
		{
			if (CurrentBar < 100) {
				entryID[0] 			= -1;
				currentEntryID[0] 	= -1;
				barsSinceEntry[0]	= -1;

				return;
			}

			LookForEntryBar();
			UpdateEntryStatus();
		}
		#endregion

		#region LookForEntryBar()
		private void LookForEntryBar()
		{
			if (md.Direction[0] == TrendDirection.Bullish && md.Direction[1] != TrendDirection.Bullish) {
				Entry entry 		= new Entry(TrendDirection.Bullish, this);

				AddEntry(entry);

				return;
			}

			if (md.Direction[0] == TrendDirection.Bearish && md.Direction[1] != TrendDirection.Bearish) {
				Entry entry 		= new Entry(TrendDirection.Bearish, this);

				AddEntry(entry);

				return;
			}

			entryID[0] 			= -1;
			currentEntryID[0]	= currentEntryID[1];
			barsSinceEntry[0]	= barsSinceEntry[1] == -1 ? -1 : barsSinceEntry[1]++;
		}
		#endregion

		#region UpdateEntryStatus()
		private void UpdateEntryStatus()
	    {
	        foreach (var entry in entries)
	        {
	            entry.UpdateStatus();
	        }
	    }
		#endregion

		#region AddEntry()
		private void AddEntry(Entry entry)
		{
			entry.PreviousSwing	= entry.Direction == TrendDirection.Bearish
				? pa.BarsAgoHigh(0, md.LegLong.BarsAgoStarts[0]) + 1
				: pa.BarsAgoLow(0, md.LegLong.BarsAgoStarts[0]) + 1;

			entry.EntryBar	= CurrentBar;
			entry.Rsi 		= rsi[0];
			entry.Atr 		= atr[0];
			entry.StdDevAtr	= stdDevAtr[0];
			entry.AvgAtr	= avgAtr[0];
			entry.EmaFast 	= emaFast[0];
			entry.EmaSlow 	= emaSlow[0];
			entry.High 		= High[0];
			entry.Low 		= Low[0];
			entry.Open 		= Open[0];
			entry.Close 	= Close[0];
			entry.TrendType	= md.Stage[0];

			entry.CalculateAdditionalValues();

			if (entries.Count == 100) {
                entries.RemoveAt(0);
            }

			entryID[0] 			= entries.Count;
			currentEntryID[0]	= entryID[0];
			barsSinceEntry[0]	= 0;

			entries.Add(entry);
		}
		#endregion
	}

	public class Entry
	{
		#region Variables

		private int window = 81;

		private EntryEvaluator source;
		private PriceActionUtils pa;
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

		public double High;
		public double Low;
		public double Open;
		public double Close;

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

		#region Entry()
		public Entry(TrendDirection trend, EntryEvaluator ind)
		{
			Direction 	= trend;
			source		= ind;
			pa 			= source.pa;
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
			PreviousSwingValue = Direction == TrendDirection.Bullish ? source.Low[PreviousSwing] : source.High[PreviousSwing];
			StopDistance = Direction == TrendDirection.Bullish ? Close - PreviousSwingValue : PreviousSwingValue - Close;
		}
		#endregion

		#region UpdateStatus()
		public void UpdateStatus()
	    {
			if (IsClosed) {
				return;
			}

			int period 		= source.CurrentBar - EntryBar;
			DistanceMoved 	= Direction == TrendDirection.Bullish ? source.Close[0] - Close : Close - source.Close[0];
			HighestHigh 	= pa.HighestHigh(0, period);
			LowestLow		= pa.LowestLow(0, period);
			GreatestProfit	= Direction == TrendDirection.Bullish ? HighestHigh - Close : Close - LowestLow;
			GreatestLoss	= Direction == TrendDirection.Bullish ? Close - LowestLow : HighestHigh - Close;
			ProfitMultiples	= GreatestProfit / StopDistance;

			if ((source.CurrentBar - EntryBar) > window) {
				IsSuccessful = ProfitMultiples > 1;
				IsClosed = true;

				return;
			}

			if (GreatestLoss > StopDistance) {
				IsSuccessful = false;
				IsClosed = true;
			}
	    }
		#endregion

		#region CalculateEMAValues()
		private void CalculateEMAValues()
		{
			FastEMADirection = pa.IsEMAFastBullish(0) ? TrendDirection.Bullish : pa.IsEMAFastBearish(0) ? TrendDirection.Bearish : TrendDirection.Flat;
			SlowEMADirection = pa.IsEMASlowBullish(0) ? TrendDirection.Bullish : pa.IsEMASlowBearish(0) ? TrendDirection.Bearish : TrendDirection.Flat;

			DistanceFromFastEMA = Close - EmaFast;
			DistanceFromSlowEMA = Close - EmaSlow;

			IsEMADiverging = false;
			IsEMAConverging = false;
			IsWithTrendEMA = false;

			if (Direction == TrendDirection.Bullish) {
				IsEMADiverging				= pa.IsEMABullishDivergence(0, 1);
				IsEMAConverging 			= pa.IsEMABullishConvergence(0, 1);
				IsWithTrendEMA 				= pa.IsEMABullish(0);
				LeadsFastEMAByMoreThanATR	= Low > (EmaFast + Atr);
			}

			if (Direction == TrendDirection.Bearish) {
				IsEMADiverging 				= pa.IsEMABearishDivergence(0, 1);;
				IsEMAConverging 			= pa.IsEMABearishConvergence(0, 1);;
				IsWithTrendEMA 				= pa.IsEMABearish(0);
				LeadsFastEMAByMoreThanATR	= High < (EmaFast = Atr);
			}


			IsWithTrendFastEMA = FastEMADirection == Direction;
			IsWithTrendSlowEMA = SlowEMADirection == Direction;
		}
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private PR.EntryEvaluator[] cacheEntryEvaluator;
		public PR.EntryEvaluator EntryEvaluator()
		{
			return EntryEvaluator(Input);
		}

		public PR.EntryEvaluator EntryEvaluator(ISeries<double> input)
		{
			if (cacheEntryEvaluator != null)
				for (int idx = 0; idx < cacheEntryEvaluator.Length; idx++)
					if (cacheEntryEvaluator[idx] != null &&  cacheEntryEvaluator[idx].EqualsInput(input))
						return cacheEntryEvaluator[idx];
			return CacheIndicator<PR.EntryEvaluator>(new PR.EntryEvaluator(), input, ref cacheEntryEvaluator);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.PR.EntryEvaluator EntryEvaluator()
		{
			return indicator.EntryEvaluator(Input);
		}

		public Indicators.PR.EntryEvaluator EntryEvaluator(ISeries<double> input )
		{
			return indicator.EntryEvaluator(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.PR.EntryEvaluator EntryEvaluator()
		{
			return indicator.EntryEvaluator(Input);
		}

		public Indicators.PR.EntryEvaluator EntryEvaluator(ISeries<double> input )
		{
			return indicator.EntryEvaluator(input);
		}
	}
}

#endregion
