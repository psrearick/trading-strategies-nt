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
		public MarketDirection md;
		public PriceActionPatterns paPatterns;
		public RSI rsi;
		public ATR atr;
		public StdDev stdDevAtr;
		public SMA avgAtr;
		public EMA emaFast;
		public EMA emaSlow;
		private Series<int> barsSinceDoubleTop;
		private Series<int> barsSinceDoubleBottom;
//		private int window = 100;
		private List<EntrySignal> entries = new List<EntrySignal>(200);
		private Dictionary<string, double> correlations = new Dictionary<string, double>();
		private Dictionary<string, double> significantCorrelations = new Dictionary<string, double>();
		public Series<double> matched;
		private int nextEntryIndex = 0;
		public double successRate;
		private int frequency;
		#endregion

		#region OnStateChange()
		protected override void OnStateChange()
		{
			#region State.SetDefaults
			if (State == State.SetDefaults)
			{
				Description									= @"";
				Name										= "Entry Evaluator";
				Calculate									= Calculate.OnBarClose;
				IsOverlay									= false;
				DisplayInDataBox							= true;
				DrawOnPricePanel							= true;
				DrawHorizontalGridLines						= true;
				DrawVerticalGridLines						= true;
				PaintPriceMarkers							= true;
				ScaleJustification							= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				IsSuspendedWhileInactive					= true;
				Period										= 10;
				Window										= 100;
			}
			#endregion

			#region State.Configure
			else if (State == State.Configure)
			{
				pa 						= PriceActionUtils();
				paPatterns				= PriceActionPatterns();
				md						= MarketDirection(Period, Period * 2);
				atr						= ATR(14);
				rsi						= RSI(14, 3);
				emaFast					= EMA(9);
				emaSlow					= EMA(21);
			}
			#endregion

			#region State.DataLoaded
			if (State == State.DataLoaded) {
				barsSinceDoubleTop		= new Series<int>(this);
				barsSinceDoubleBottom	= new Series<int>(this);
				stdDevAtr				= StdDev(atr, 20);
				avgAtr					= SMA(atr, 20);
				matched					= new Series<double>(this, MaximumBarsLookBack.TwoHundredFiftySix);

				for (int i = 0; i < Window; i++) {
					entries.Add(EntrySignal(i + 1));
				}

				frequency	= (int) Math.Max(10, (int) Math.Floor((double) Window / 10));
			}
			#endregion
		}
		#endregion

		#region OnBarUpdate()
		protected override void OnBarUpdate()
		{
			if (CurrentBar < 200) {
				matched[0]	= 0;
				return;
			}

			barsSinceDoubleBottom[0] = barsSinceDoubleBottom[1] + 1;
			if (paPatterns.IsDoubleBottom(0, 30, 3)) {
				barsSinceDoubleBottom[0] = 0;
			}

			barsSinceDoubleTop[0] = barsSinceDoubleTop[1] + 1;
			if (paPatterns.IsDoubleTop(0, 30, 3)) {
				barsSinceDoubleTop[0] = 0;
			}

			LookForEntryBar();
			UpdateEntryStatus();

			if (CurrentBar % frequency == 0) {
	            CalculateCorrelations();
	        }

			EvaluateCriteria(0);

			successRate = (double) entries.Count(e => e.IsSuccessful) / Window;
		}
		#endregion

		#region CalculateCorrelations()
		private void CalculateCorrelations()
		{
			correlations.Clear();
			List<EntrySignal> closedEntries = entries.Where(e => e.IsClosed || e.IsSuccessful).Select(e => e).ToList();

	        correlations["RSI"] = correlationCoefficient(closedEntries.Select(e => e.Rsi).ToArray(), closedEntries.Select(e => e.IsSuccessful ? 1.0 : 0.0).ToArray());
	        correlations["ATR"] = correlationCoefficient(closedEntries.Select(e => e.Atr).ToArray(), closedEntries.Select(e => e.IsSuccessful ? 1.0 : 0.0).ToArray());
	        correlations["IsEMADiverging"] = correlationCoefficient(closedEntries.Select(e => e.IsEMADiverging ? 1.0 : 0.0).ToArray(), closedEntries.Select(e => e.IsSuccessful ? 1.0 : 0.0).ToArray());
	        correlations["IsEMAConverging"] = correlationCoefficient(closedEntries.Select(e => e.IsEMAConverging ? 1.0 : 0.0).ToArray(), closedEntries.Select(e => e.IsSuccessful ? 1.0 : 0.0).ToArray());
			correlations["IsWithTrendEMA"] = correlationCoefficient(closedEntries.Select(e => e.IsWithTrendEMA ? 1.0 : 0.0).ToArray(), closedEntries.Select(e => e.IsSuccessful ? 1.0 : 0.0).ToArray());
			correlations["IsWithTrendFastEMA"] = correlationCoefficient(closedEntries.Select(e => e.IsWithTrendFastEMA ? 1.0 : 0.0).ToArray(), closedEntries.Select(e => e.IsSuccessful ? 1.0 : 0.0).ToArray());
			correlations["IsWithTrendSlowEMA"] = correlationCoefficient(closedEntries.Select(e => e.IsWithTrendSlowEMA ? 1.0 : 0.0).ToArray(), closedEntries.Select(e => e.IsSuccessful ? 1.0 : 0.0).ToArray());
			correlations["LeadsFastEMAByMoreThanATR"] = correlationCoefficient(closedEntries.Select(e => e.LeadsFastEMAByMoreThanATR ? 1.0 : 0.0).ToArray(), closedEntries.Select(e => e.IsSuccessful ? 1.0 : 0.0).ToArray());
			correlations["IsWithTrendPressure"] = correlationCoefficient(closedEntries.Select(e => e.IsWithTrendPressure ? 1.0 : 0.0).ToArray(), closedEntries.Select(e => e.IsSuccessful ? 1.0 : 0.0).ToArray());
			correlations["IsStrongWithTrendPressure"] = correlationCoefficient(closedEntries.Select(e => e.IsStrongWithTrendPressure ? 1.0 : 0.0).ToArray(), closedEntries.Select(e => e.IsSuccessful ? 1.0 : 0.0).ToArray());
			correlations["IsWithTrendTrendBar"] = correlationCoefficient(closedEntries.Select(e => e.IsWithTrendTrendBar ? 1.0 : 0.0).ToArray(), closedEntries.Select(e => e.IsSuccessful ? 1.0 : 0.0).ToArray());
			correlations["IsBreakoutBarPattern"] = correlationCoefficient(closedEntries.Select(e => e.IsBreakoutBarPattern ? 1.0 : 0.0).ToArray(), closedEntries.Select(e => e.IsSuccessful ? 1.0 : 0.0).ToArray());
			correlations["IsWeakBar"] = correlationCoefficient(closedEntries.Select(e => e.IsWeakBar ? 1.0 : 0.0).ToArray(), closedEntries.Select(e => e.IsSuccessful ? 1.0 : 0.0).ToArray());
			correlations["IsStrongFollowThrough"] = correlationCoefficient(closedEntries.Select(e => e.IsStrongFollowThrough ? 1.0 : 0.0).ToArray(), closedEntries.Select(e => e.IsSuccessful ? 1.0 : 0.0).ToArray());
			correlations["IsBreakout"] = correlationCoefficient(closedEntries.Select(e => e.IsBreakout ? 1.0 : 0.0).ToArray(), closedEntries.Select(e => e.IsSuccessful ? 1.0 : 0.0).ToArray());
			correlations["IsBroadChannel"] = correlationCoefficient(closedEntries.Select(e => e.IsBroadChannel ? 1.0 : 0.0).ToArray(), closedEntries.Select(e => e.IsSuccessful ? 1.0 : 0.0).ToArray());
			correlations["IsTightChannel"] = correlationCoefficient(closedEntries.Select(e => e.IsTightChannel ? 1.0 : 0.0).ToArray(), closedEntries.Select(e => e.IsSuccessful ? 1.0 : 0.0).ToArray());
	        correlations["IsWeakTrend"] = correlationCoefficient(closedEntries.Select(e => e.IsWeakTrend ? 1.0 : 0.0).ToArray(), closedEntries.Select(e => e.IsSuccessful ? 1.0 : 0.0).ToArray());
			correlations["IsStrongTrend"] = correlationCoefficient(closedEntries.Select(e => e.IsStrongTrend ? 1.0 : 0.0).ToArray(), closedEntries.Select(e => e.IsSuccessful ? 1.0 : 0.0).ToArray());
			correlations["IsRSIInRange"] = correlationCoefficient(closedEntries.Select(e => e.IsRSIInRange ? 1.0 : 0.0).ToArray(), closedEntries.Select(e => e.IsSuccessful ? 1.0 : 0.0).ToArray());
			correlations["IsAboveAverageATR"] = correlationCoefficient(closedEntries.Select(e => e.IsAboveAverageATR ? 1.0 : 0.0).ToArray(), closedEntries.Select(e => e.IsSuccessful ? 1.0 : 0.0).ToArray());
			correlations["IsBelowAverageATR"] = correlationCoefficient(closedEntries.Select(e => e.IsBelowAverageATR ? 1.0 : 0.0).ToArray(), closedEntries.Select(e => e.IsSuccessful ? 1.0 : 0.0).ToArray());
			correlations["IsAboveAverageATRByAStdDev"] = correlationCoefficient(closedEntries.Select(e => e.IsAboveAverageATRByAStdDev ? 1.0 : 0.0).ToArray(), closedEntries.Select(e => e.IsSuccessful ? 1.0 : 0.0).ToArray());

			// Remove any NaN values from the correlations dictionary
		    correlations = correlations.Where(c => !double.IsNaN(c.Value)).ToDictionary(i => i.Key, i => i.Value);

			if (correlations.Count > 0) {
				// Calculate the mean and standard deviation of the correlation coefficients
			    double mean = correlations.Values.Average();
			    double stdDev = StandardDeviation(correlations.Values);

			    // Determine the significance threshold based on the standard deviation
		    		double threshold = 1; // Adjust the multiplier as needed
		   	 	double significanceThreshold = mean + threshold * stdDev;

		    		// Filter significant correlations based on the dynamic threshold
		    		significantCorrelations = correlations.Where(c => Math.Abs(c.Value) > significanceThreshold).ToDictionary(i => i.Key, i => i.Value);
			} else {
		        significantCorrelations.Clear();
		    }
		}
		#endregion

		#region StandardDeviation()
		private double StandardDeviation(IEnumerable<double> values)
		{
		    double avg = values.Average();
		    double sum = values.Sum(d => Math.Pow(d - avg, 2));
		    double denominator = values.Count() - 1;
		    return Math.Sqrt(sum / denominator);
		}
		#endregion

		#region correlationCoefficient()
		private double correlationCoefficient(double []X, double []Y)
	    {
			int n = X.Length;
	        double sum_X = 0, sum_Y = 0, sum_XY = 0;
	        double squareSum_X = 0, squareSum_Y = 0;

	        for (int i = 0; i < n; i++)
	        {
	            // sum of elements of array X.
	            sum_X = sum_X + X[i];

	            // sum of elements of array Y.
	            sum_Y = sum_Y + Y[i];

	            // sum of X[i] * Y[i].
	            sum_XY = sum_XY + X[i] * Y[i];

	            // sum of square of array elements.
	            squareSum_X = squareSum_X + X[i] * X[i];
	            squareSum_Y = squareSum_Y + Y[i] * Y[i];
	        }

	        // use formula for calculating correlation
	        // coefficient.
	        double corr = (double)(n * sum_XY - sum_X * sum_Y)/
	                     (double)(Math.Sqrt((n * squareSum_X -
	                     sum_X * sum_X) * (n * squareSum_Y -
	                     sum_Y * sum_Y)));

	        return corr;
	    }
		#endregion

		#region LookForEntryBar()
		private void LookForEntryBar()
		{
			if (md.Direction.Count < 2) {
				return;
			}

			if (md.LegLong.BarsAgoStarts[0] < 4) {
				return;
			}

			if (md.LegLong.BarsAgoStarts[0] > 8) {
				return;
			}

			if (md.Direction[0] == TrendDirection.Flat) {
				return;
			}

			AddEntry(md.Direction[0]);
		}
		#endregion

		#region UpdateEntryStatus()
		private void UpdateEntryStatus()
	    {
	        foreach (var entry in entries) {
	            entry.UpdateStatus();
	        }
	    }
		#endregion

		#region AddEntry()
		private void AddEntry(TrendDirection direction)
		{
			EntrySignal entry = entries[nextEntryIndex];
			nextEntryIndex++;

			if (nextEntryIndex == Window) {
				nextEntryIndex = 0;
			}

			entry.Direction = direction;
			entry.PreviousSwing	= entry.Direction == TrendDirection.Bearish
				? pa.BarsAgoHigh(0, md.LegLong.BarsAgoStarts[0])
				: pa.BarsAgoLow(0, md.LegLong.BarsAgoStarts[0]);

			entry.EntryBar			= CurrentBar;
			entry.Rsi 				= rsi[0];
			entry.Atr 				= atr[0];
			entry.StdDevAtr			= stdDevAtr[0];
			entry.AvgAtr				= avgAtr[0];
			entry.EmaFast 			= emaFast[0];
			entry.EmaSlow 			= emaSlow[0];
			entry.HighEntry			= High[0];
			entry.LowEntry			= Low[0];
			entry.OpenEntry			= Open[0];
			entry.CloseEntry 		= Close[0];
			entry.TrendType			= md.Stage[0];

			entry.IsClosed = false;
			entry.IsSuccessful = false;

			entry.entryEvaluator		= this;

			entry.CalculateAdditionalValues();
		}
		#endregion

		#region EvaluateCriteria()
		public bool EvaluateCriteria(int barsAgo)
		{
			int criteriaCount = significantCorrelations.Count;

			if (criteriaCount == 0) {
				matched[0] = 0;
				return false;
			}

			int matchedCount = 0;

			foreach (var criterion in significantCorrelations) {
				bool positive = criterion.Value > 0;
				if (criterion.Key == "RSI") {
					matchedCount += EvaluateRSI(barsAgo, positive) ? 1 : 0;
				}
				if (criterion.Key == "ATR") {
					matchedCount += EvaluateATR(barsAgo, positive) ? 1 : 0;
				}
				if (criterion.Key == "IsEMADiverging") {
					matchedCount += EvaluateEMADiverging(barsAgo, positive) ? 1 : 0;
				}
				if (criterion.Key == "IsEMAConverging") {
					matchedCount += EvaluateEMAConverging(barsAgo, positive) ? 1 : 0;
				}
				if (criterion.Key == "IsWithTrendEMA") {
					matchedCount += EvaluateWithTrendEMA(barsAgo, positive) ? 1 : 0;
				}
				if (criterion.Key == "IsWithTrendFastEMA") {
					matchedCount += EvaluateWithTrendFastEMA(barsAgo, positive) ? 1 : 0;
				}
				if (criterion.Key == "IsWithTrendSlowEMA") {
					matchedCount += EvaluateWithTrendSlowEMA(barsAgo, positive) ? 1 : 0;
				}
				if (criterion.Key == "LeadsFastEMAByMoreThanATR") {
					matchedCount += EvaluateLeadsFastEMAByMoreThanATR(barsAgo, positive) ? 1 : 0;
				}
				if (criterion.Key == "IsWithTrendPressure") {
					matchedCount += EvaluateWithTrendPressure(barsAgo, positive) ? 1 : 0;
				}
				if (criterion.Key == "IsStrongWithTrendPressure") {
					matchedCount += EvaluateStrongWithTrendPressure(barsAgo, positive) ? 1 : 0;
				}
				if (criterion.Key == "IsWithTrendTrendBar") {
					matchedCount += EvaluateWithTrendTrendBar(barsAgo, positive) ? 1 : 0;
				}
				if (criterion.Key == "IsBreakoutBarPattern") {
					matchedCount += EvaluateBreakoutBarPattern(barsAgo, positive) ? 1 : 0;
				}
				if (criterion.Key == "IsWeakBar") {
					matchedCount += EvaluateWeakBar(barsAgo, positive) ? 1 : 0;
				}
				if (criterion.Key == "IsStrongFollowThrough") {
					matchedCount += EvaluateStrongFollowThrough(barsAgo, positive) ? 1 : 0;
				}
				if (criterion.Key == "IsBreakout") {
					matchedCount += EvaluateBreakout(barsAgo, positive) ? 1 : 0;
				}
				if (criterion.Key == "IsBroadChannel") {
					matchedCount += EvaluateBroadChannel(barsAgo, positive) ? 1 : 0;
				}
				if (criterion.Key == "IsTightChannel") {
					matchedCount += EvaluateTightChannel(barsAgo, positive) ? 1 : 0;
				}
				if (criterion.Key == "IsWeakTrend") {
					matchedCount += EvaluateWeakTrend(barsAgo, positive) ? 1 : 0;
				}
				if (criterion.Key == "IsStrongTrend") {
					matchedCount += EvaluateStrongTrend(barsAgo, positive) ? 1 : 0;
				}
				if (criterion.Key == "IsRSIInRange") {
					matchedCount += EvaluateRSIInRange(barsAgo, positive) ? 1 : 0;
				}
				if (criterion.Key == "IsAboveAverageATR") {
					matchedCount += EvaluateAboveAverageATR(barsAgo, positive) ? 1 : 0;
				}
				if (criterion.Key == "IsBelowAverageATR") {
					matchedCount += EvaluateBelowAverageATR(barsAgo, positive) ? 1 : 0;
				}
				if (criterion.Key == "IsAboveAverageATRByAStdDev") {
					matchedCount += EvaluateAboveAverageATRByAStdDev(barsAgo, positive) ? 1 : 0;
				}
			}

			double match = (double) matchedCount / (double) criteriaCount;
			matched[0] = match;

			return match == 1;
		}
		#endregion

		#region EvaluateRSI()
		private bool EvaluateRSI(int barsAgo, bool positive) {
			return positive ? rsi[barsAgo] > 70 : rsi[barsAgo] < 30;
		}
		#endregion

		#region EvaluateATR()
		private bool EvaluateATR(int barsAgo, bool positive) {
			double atrMax = MAX(atr, 20)[barsAgo];
			double atrMin = MIN(atr, 20)[barsAgo];

			double atrUpper = ((atrMax - atrMin) * 0.7) + atrMin;
			double atrLower = ((atrMax - atrMin) * 0.3) + atrMin;

			return positive ? atr[barsAgo] > atrUpper : atr[barsAgo] < atrLower;
		}
		#endregion

		#region EvaluateEMADiverging()
		private bool EvaluateEMADiverging(int barsAgo, bool positive) {
			bool divergence = md.Direction[barsAgo] == TrendDirection.Bullish
				? pa.IsEMABullishDivergence(barsAgo, 1)
				: pa.IsEMABearishDivergence(barsAgo, 1);

			return positive ? divergence : !divergence;
		}
		#endregion

		#region EvaluateEMAConverging()
		private bool EvaluateEMAConverging(int barsAgo, bool positive) {
			bool convergence = md.Direction[barsAgo] == TrendDirection.Bullish
				? pa.IsEMABullishConvergence(barsAgo, 1)
				: pa.IsEMABearishConvergence(barsAgo, 1);

			return positive ? convergence : !convergence;
		}
		#endregion

		#region EvaluateWithTrendEMA()
		private bool EvaluateWithTrendEMA(int barsAgo, bool positive) {
			bool withTrend = md.Direction[barsAgo] == TrendDirection.Bullish
				? pa.IsEMABullish(barsAgo) : pa.IsEMABearish(barsAgo);

			return positive ? withTrend : !withTrend;
		}
		#endregion

		#region EvaluateWithTrendFastEMA()
		private bool EvaluateWithTrendFastEMA(int barsAgo, bool positive) {
			TrendDirection FastEMADirection = pa.IsEMAFastBullish(barsAgo) ? TrendDirection.Bullish : pa.IsEMAFastBearish(barsAgo) ? TrendDirection.Bearish : TrendDirection.Flat;

			return positive ? FastEMADirection == md.Direction[barsAgo] : FastEMADirection != md.Direction[barsAgo];
		}
		#endregion

		#region EvaluateWithTrendSlowEMA()
		private bool EvaluateWithTrendSlowEMA(int barsAgo, bool positive) {
			TrendDirection SlowEMADirection = pa.IsEMASlowBullish(barsAgo) ? TrendDirection.Bullish : pa.IsEMASlowBearish(barsAgo) ? TrendDirection.Bearish : TrendDirection.Flat;

			return positive ? SlowEMADirection == md.Direction[barsAgo] : SlowEMADirection != md.Direction[barsAgo];
		}
		#endregion

		#region EvaluateLeadsFastEMAByMoreThanATR()
		private bool EvaluateLeadsFastEMAByMoreThanATR(int barsAgo, bool positive) {
			bool leads = md.Direction[barsAgo] == TrendDirection.Bullish
				? Low[barsAgo] > (emaFast[barsAgo] + atr[barsAgo])
				: High[barsAgo] < (emaFast[barsAgo] - atr[barsAgo]);

			return positive ? leads : !leads;
		}
		#endregion

		#region EvaluateWithTrendPressure()
		private bool EvaluateWithTrendPressure(int barsAgo, bool positive) {
			int previousSwing		= md.Direction[barsAgo] == TrendDirection.Bearish
				? pa.BarsAgoHigh(barsAgo, md.LegLong.BarsAgoStarts[barsAgo])
				: pa.BarsAgoLow(barsAgo, md.LegLong.BarsAgoStarts[barsAgo]);
			double buySellPressure 	= pa.GetBuySellPressure(barsAgo, previousSwing);
			bool hasPressure		= md.Direction[barsAgo] == TrendDirection.Bullish ? buySellPressure > 75 : buySellPressure < 25;

			return positive ? hasPressure : !hasPressure;
		}
		#endregion

		#region EvaluateStrongWithTrendPressure()
		private bool EvaluateStrongWithTrendPressure(int barsAgo, bool positive) {
			int previousSwing		= md.Direction[barsAgo] == TrendDirection.Bearish
				? pa.BarsAgoHigh(barsAgo, md.LegLong.BarsAgoStarts[barsAgo])
				: pa.BarsAgoLow(barsAgo, md.LegLong.BarsAgoStarts[barsAgo]);
			double buySellPressure 	= pa.GetBuySellPressure(barsAgo, previousSwing);
			bool hasPressure		= md.Direction[barsAgo] == TrendDirection.Bullish ? buySellPressure > 90 : buySellPressure < 10;

			return positive ? hasPressure : !hasPressure;
		}
		#endregion

		#region EvaluateWithTrendTrendBar()
		private bool EvaluateWithTrendTrendBar(int barsAgo, bool positive) {
			bool IsWithTrendTrendBar = pa.IsTrendBar(barsAgo) && (md.Direction[barsAgo] == TrendDirection.Bullish ? pa.IsBullishBar(barsAgo) : pa.IsBearishBar(barsAgo));

			return positive ? IsWithTrendTrendBar : !IsWithTrendTrendBar;
		}
		#endregion

		#region EvaluateBreakoutBarPattern()
		private bool EvaluateBreakoutBarPattern(int barsAgo, bool positive) {
			bool IsBreakoutBarPattern = pa.DoesInsideOutsideMatch("ii", barsAgo) || pa.DoesInsideOutsideMatch("ioi", barsAgo);

			return positive ? IsBreakoutBarPattern : !IsBreakoutBarPattern;
		}
		#endregion

		#region EvaluateWeakBar()
		private bool EvaluateWeakBar(int barsAgo, bool positive) {
			bool IsWeakBar = pa.IsDoji(barsAgo) || pa.IsTradingRangeBar(barsAgo);

			return positive ? IsWeakBar : !IsWeakBar;
		}
		#endregion

		#region EvaluateStrongFollowThrough()
		private bool EvaluateStrongFollowThrough(int barsAgo, bool positive) {
			bool IsStrongFollowThrough = pa.IsStrongFollowThroughBar(barsAgo);

			return positive ? IsStrongFollowThrough : !IsStrongFollowThrough;
		}
		#endregion

		#region EvaluateBreakout()
		private bool EvaluateBreakout(int barsAgo, bool positive) {
			bool IsBreakout	= md.Stage[barsAgo] == MarketCycleStage.Breakout;

			return positive ? IsBreakout : !IsBreakout;
		}
		#endregion

		#region EvaluateBroadChannel()
		private bool EvaluateBroadChannel(int barsAgo, bool positive) {
			bool IsBroadChannel = md.Stage[barsAgo] == MarketCycleStage.BroadChannel;

			return positive ? IsBroadChannel : !IsBroadChannel;
		}
		#endregion

		#region EvaluateTightChannel()
		private bool EvaluateTightChannel(int barsAgo, bool positive) {
			bool IsTightChannel = md.Stage[barsAgo] == MarketCycleStage.TightChannel;

			return positive ? IsTightChannel : !IsTightChannel;
		}
		#endregion

		#region EvaluateWeakTrend()
		private bool EvaluateWeakTrend(int barsAgo, bool positive) {
			int previousSwing = md.Direction[barsAgo] == TrendDirection.Bearish
				? pa.BarsAgoHigh(barsAgo, md.LegLong.BarsAgoStarts[barsAgo])
				: pa.BarsAgoLow(barsAgo, md.LegLong.BarsAgoStarts[barsAgo]);

			bool IsWeakTrend = md.Direction[barsAgo] == TrendDirection.Bullish ? pa.IsWeakBullishTrend(barsAgo, previousSwing) : pa.IsWeakBearishTrend(barsAgo, previousSwing);

			return positive ? IsWeakTrend : !IsWeakTrend;
		}
		#endregion

		#region EvaluateStrongTrend()
		private bool EvaluateStrongTrend(int barsAgo, bool positive) {
			int previousSwing = md.Direction[barsAgo] == TrendDirection.Bearish
				? pa.BarsAgoHigh(barsAgo, md.LegLong.BarsAgoStarts[barsAgo])
				: pa.BarsAgoLow(barsAgo, md.LegLong.BarsAgoStarts[barsAgo]);

			bool IsStrongTrend = md.Direction[barsAgo] == TrendDirection.Bullish ? pa.IsStrongBullishTrend(barsAgo, previousSwing) : pa.IsStrongBearishTrend(barsAgo, previousSwing);

			return positive ? IsStrongTrend : !IsStrongTrend;
		}
		#endregion

		#region EvaluateRSIInRange()
		private bool EvaluateRSIInRange(int barsAgo, bool positive) {
			bool IsRSIInRange = md.Direction[barsAgo] == TrendDirection.Bullish ? (rsi[barsAgo] > 50 && rsi[barsAgo] < 70) : (rsi[barsAgo] > 30 && rsi[barsAgo] < 50);

			return positive ? IsRSIInRange : !IsRSIInRange;
		}
		#endregion

		#region EvaluateAboveAverageATR()
		private bool EvaluateAboveAverageATR(int barsAgo, bool positive) {
			bool IsAboveAverageATR = atr[barsAgo] > avgAtr[barsAgo];

			return positive ? IsAboveAverageATR : !IsAboveAverageATR;
		}
		#endregion

		#region EvaluateBelowAverageATR()
		private bool EvaluateBelowAverageATR(int barsAgo, bool positive) {
			bool IsBelowAverageATR = atr[barsAgo] < avgAtr[barsAgo];

			return positive ? IsBelowAverageATR : !IsBelowAverageATR;
		}
		#endregion

		#region EvaluateAboveAverageATRByAStdDev()
		private bool EvaluateAboveAverageATRByAStdDev(int barsAgo, bool positive) {

			bool IsAboveAverageATRByAStdDev	= (atr[barsAgo] - avgAtr[barsAgo]) > stdDevAtr[barsAgo];

			return positive ? IsAboveAverageATRByAStdDev : !IsAboveAverageATRByAStdDev;
		}
		#endregion

		#region Properties
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Period", Description="Period", Order=0, GroupName="Parameters")]
		public int Period
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, 200)]
		[Display(Name="Window", Description="Window", Order=1, GroupName="Parameters")]
		public int Window
		{ get; set; }
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private PR.EntryEvaluator[] cacheEntryEvaluator;
		public PR.EntryEvaluator EntryEvaluator(int period, int window)
		{
			return EntryEvaluator(Input, period, window);
		}

		public PR.EntryEvaluator EntryEvaluator(ISeries<double> input, int period, int window)
		{
			if (cacheEntryEvaluator != null)
				for (int idx = 0; idx < cacheEntryEvaluator.Length; idx++)
					if (cacheEntryEvaluator[idx] != null && cacheEntryEvaluator[idx].Period == period && cacheEntryEvaluator[idx].Window == window && cacheEntryEvaluator[idx].EqualsInput(input))
						return cacheEntryEvaluator[idx];
			return CacheIndicator<PR.EntryEvaluator>(new PR.EntryEvaluator(){ Period = period, Window = window }, input, ref cacheEntryEvaluator);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.PR.EntryEvaluator EntryEvaluator(int period, int window)
		{
			return indicator.EntryEvaluator(Input, period, window);
		}

		public Indicators.PR.EntryEvaluator EntryEvaluator(ISeries<double> input , int period, int window)
		{
			return indicator.EntryEvaluator(input, period, window);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.PR.EntryEvaluator EntryEvaluator(int period, int window)
		{
			return indicator.EntryEvaluator(Input, period, window);
		}

		public Indicators.PR.EntryEvaluator EntryEvaluator(ISeries<double> input , int period, int window)
		{
			return indicator.EntryEvaluator(input, period, window);
		}
	}
}

#endregion
