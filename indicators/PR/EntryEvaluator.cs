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
		private Utils utils = new Utils();

		public PriceActionUtils pa;
		public MarketDirection md;
		public PriceActionPatterns paPatterns;
		public RSI rsi;
		public ATR atr;
		public StdDev stdDevAtr;
		public SMA avgAtr;
		public SMA avgAtrFast;
		public EMA emaFast;
		public EMA emaSlow;
		public Series<int> barsSinceDoubleTop;
		public Series<int> barsSinceDoubleBottom;
		private List<EntrySignal> entries = new List<EntrySignal>(200);
		private Dictionary<string, double> correlations = new Dictionary<string, double>();
		public Dictionary<string, double> significantCorrelations = new Dictionary<string, double>();
		private Dictionary<string, double?> exitCorrelations = new Dictionary<string, double?>();
		private Dictionary<string, double> filteredExitCorrelations = new Dictionary<string, double>();
		public Dictionary<string, double> significantExitCorrelations = new Dictionary<string, double>();
		public Series<double> matched;
		private int WindowSize;
		private int InitialWindow = 200;
		private int nextEntryIndex = 0;
		public double successRate = 0.5;
		private int frequency;
		private double StopLossMultiplier = 1;
		#endregion

		#region OnStateChange()
		protected override void OnStateChange()
		{
			#region State.SetDefaults
			if (State == State.SetDefaults)
			{
				Description									= @"";
				Name											= "Entry Evaluator";
				Calculate									= Calculate.OnBarClose;
				IsOverlay									= false;
				DisplayInDataBox								= true;
				DrawOnPricePanel								= true;
				DrawHorizontalGridLines						= true;
				DrawVerticalGridLines						= true;
				PaintPriceMarkers							= true;
				ScaleJustification							= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				IsSuspendedWhileInactive						= true;
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
				stdDevAtr				= StdDev(atr, 21);
				avgAtr					= SMA(atr, 21);
				avgAtrFast				= SMA(atr, 9);
				matched					= new Series<double>(this, MaximumBarsLookBack.TwoHundredFiftySix);

//				for (int i = 0; i < InitialWindow; i++) {
//					entries.Add(EntrySignal(i + 1));
//				}

				frequency	= (int) Math.Max(10, (int) Math.Floor(Window / 10));
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

			WindowSize = Math.Min(180, Math.Max(10, (int) Math.Floor((double)Window * atr[0])));
			if (nextEntryIndex > WindowSize - 1) {
				nextEntryIndex = 0;
			}

			int enabledCount = entries.Where(e => e.IsEnabled).Count();
			if (enabledCount > WindowSize) {
				int toDisable = WindowSize - enabledCount;

				for (int i = 0; i < toDisable; i++) {
					int positionToDisable = nextEntryIndex + i;
					if (positionToDisable > WindowSize - 1) positionToDisable -= WindowSize;
					entries[positionToDisable].IsEnabled = true;
				}
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
				CalculateExitCorrelations();
	        }

			EvaluateCriteria(0);

			successRate = (double) entries.Count(e => e.IsEnabled && e.IsSuccessful) / entries.Where(e => e.IsEnabled).Count();
		}
		#endregion

		#region CalculateExitCorrelations()
		private void CalculateExitCorrelations()
		{
			exitCorrelations.Clear();

			if (entries.Count == 0) {
				significantExitCorrelations.Clear();
				return;
			}

			List<EntrySignal> openEntries = entries.Where(e => !e.IsClosed && e.IsEnabled).ToList();

			if (openEntries.Count == 0) {
				significantExitCorrelations.Clear();
				return;
			}

			exitCorrelations["TrendDirectionChanged"] = openEntries
				.Where(e => e.TrendDirectionChanged > 0).Average(e => (double?)e.TrendDirectionChanged);
			exitCorrelations["CounterTrendTightChannel"] = openEntries
				.Where(e => e.CounterTrendTightChannel > 0).Average(e => (double?)e.CounterTrendTightChannel);
			exitCorrelations["CounterTrendBroadChannel"] = openEntries
				.Where(e => e.CounterTrendBroadChannel > 0).Average(e => (double?)e.CounterTrendBroadChannel);
			exitCorrelations["CounterTrendBreakout"] = openEntries
				.Where(e => e.CounterTrendBreakout > 0).Average(e => (double?)e.CounterTrendBreakout);
			exitCorrelations["CounterTrendBreakoutTrend"] = openEntries
				.Where(e => e.CounterTrendBreakoutTrend > 0).Average(e => (double?)e.CounterTrendBreakoutTrend);
			exitCorrelations["CounterTrendLegLong"] = openEntries
				.Where(e => e.CounterTrendLegLong > 0).Average(e => (double?)e.CounterTrendLegLong);
			exitCorrelations["CounterTrendLegShort"] = openEntries
				.Where(e => e.CounterTrendLegShort > 0).Average(e => (double?)e.CounterTrendLegShort);
			exitCorrelations["CounterTrendLegAfterDoubleTopBottom"] = openEntries
				.Where(e => e.CounterTrendLegAfterDoubleTopBottom > 0).Average(e => (double?)e.CounterTrendLegAfterDoubleTopBottom);
			exitCorrelations["TrailingStopBeyondPreviousExtreme"] = openEntries
				.Where(e => e.TrailingStopBeyondPreviousExtreme > 0).Average(e => (double?)e.TrailingStopBeyondPreviousExtreme);
			exitCorrelations["MovingAverageCrossover"] = openEntries
				.Where(e => e.MovingAverageCrossover > 0).Average(e => (double?)e.MovingAverageCrossover);
			exitCorrelations["DoubleTopBottom"] = openEntries
				.Where(e => e.DoubleTopBottom > 0).Average(e => (double?)e.DoubleTopBottom);
			exitCorrelations["NoNewExtreme8"] = openEntries
				.Where(e => e.NoNewExtreme8 > 0).Average(e => (double?)e.NoNewExtreme8);
			exitCorrelations["NoNewExtreme10"] = openEntries
				.Where(e => e.NoNewExtreme10 > 0).Average(e => (double?)e.NoNewExtreme10);
			exitCorrelations["NoNewExtreme12"] = openEntries
				.Where(e => e.NoNewExtreme12 > 0).Average(e => (double?)e.NoNewExtreme12);
			exitCorrelations["CounterTrendPressure"] = openEntries
				.Where(e => e.CounterTrendPressure > 0).Average(e => (double?)e.CounterTrendPressure);
			exitCorrelations["CounterTrendStrongPressure"] = openEntries
				.Where(e => e.CounterTrendStrongPressure > 0).Average(e => (double?)e.CounterTrendStrongPressure);
			exitCorrelations["CounterTrendWeakTrend"] = openEntries
				.Where(e => e.CounterTrendWeakTrend > 0).Average(e => (double?)e.CounterTrendWeakTrend);
			exitCorrelations["CounterTrendStrongTrend"] = openEntries
				.Where(e => e.CounterTrendStrongTrend > 0).Average(e => (double?)e.CounterTrendStrongTrend);
			exitCorrelations["RSIOutOfRange"] = openEntries
				.Where(e => e.RSIOutOfRange > 0).Average(e => (double?)e.RSIOutOfRange);
			exitCorrelations["ATRAboveAverageATR"] = openEntries
				.Where(e => e.ATRAboveAverageATR > 0).Average(e => (double?)e.ATRAboveAverageATR);
			exitCorrelations["ATRBelowAverageATR"] = openEntries
				.Where(e => e.ATRBelowAverageATR > 0).Average(e => (double?)e.ATRBelowAverageATR);
			exitCorrelations["ATRAboveAverageATRByAStdDev"] = openEntries
				.Where(e => e.ATRAboveAverageATRByAStdDev > 0).Average(e => (double?)e.ATRAboveAverageATRByAStdDev);
			exitCorrelations["ATRBelowAverageATRByAStdDev"] = openEntries
				.Where(e => e.ATRBelowAverageATRByAStdDev > 0).Average(e => (double?)e.ATRBelowAverageATRByAStdDev);
			exitCorrelations["StrongCounterTrendFollowThrough"] = openEntries
				.Where(e => e.StrongCounterTrendFollowThrough > 0).Average(e => (double?)e.StrongCounterTrendFollowThrough);
			exitCorrelations["ProfitTarget1"] = openEntries.Where(e => e.ProfitTarget1 > 0).Average(e => (double?)e.ProfitTarget1);
			exitCorrelations["ProfitTarget2"] = openEntries.Where(e => e.ProfitTarget2 > 0).Average(e => (double?)e.ProfitTarget2);
			exitCorrelations["ProfitTarget3"] = openEntries.Where(e => e.ProfitTarget3 > 0).Average(e => (double?)e.ProfitTarget3);
			exitCorrelations["ProfitTarget4"] = openEntries.Where(e => e.ProfitTarget4 > 0).Average(e => (double?)e.ProfitTarget4);
			exitCorrelations["ProfitTarget5"] = openEntries.Where(e => e.ProfitTarget5 > 0).Average(e => (double?)e.ProfitTarget5);

		    filteredExitCorrelations = exitCorrelations.Where(c => (c.Value.HasValue && !double.IsNaN((double)c.Value))).ToDictionary(i => i.Key, i => (double)i.Value);

			if (filteredExitCorrelations.Count > 0) {
			    double mean = filteredExitCorrelations.Values.Average();
			    double stdDev = utils.StandardDeviation(filteredExitCorrelations.Values);

		    		double threshold = successRate;
		   	 	double significanceThreshold = mean + threshold * stdDev;

		    		significantExitCorrelations = filteredExitCorrelations.Where(c => Math.Abs(c.Value) > significanceThreshold).ToDictionary(i => i.Key, i => i.Value);
			} else {
		        significantExitCorrelations.Clear();
		    }
		}
		#endregion

		#region CalculateCorrelations()
		private void CalculateCorrelations()
		{
			correlations.Clear();

		    correlations = GetEntryCorrelations();

			if (correlations.Count > 0) {
			    double mean = correlations.Values.Average();
			    double stdDev = utils.StandardDeviation(correlations.Values);

		    		double threshold = 4 - 4 * successRate;

		   	 	double significanceThreshold = mean + threshold * stdDev;

		    		significantCorrelations = correlations.Where(c => Math.Abs(c.Value) > significanceThreshold).ToDictionary(i => i.Key, i => i.Value);
			} else {
		        significantCorrelations.Clear();
		    }
		}
		#endregion

		#region GetEntryCorrelations()
		public Dictionary<string, double> GetEntryCorrelations()
		{
			List<EntrySignal> closedEntries = entries.Where(e => e.IsEnabled && (e.IsClosed || e.IsSuccessful)).ToList();

			Dictionary<string, double> entryCorrelations = new Dictionary<string, double>();

	        entryCorrelations["RSI"] = correlationCoefficient(closedEntries.Select(e => e.Rsi).ToArray(), closedEntries.Select(e => e.IsSuccessful ? 1.0 : 0.0).ToArray());
	        entryCorrelations["ATR"] = correlationCoefficient(closedEntries.Select(e => e.Atr).ToArray(), closedEntries.Select(e => e.IsSuccessful ? 1.0 : 0.0).ToArray());
	        entryCorrelations["IsEMADiverging"] = correlationCoefficient(closedEntries.Select(e => e.IsEMADiverging ? 1.0 : 0.0).ToArray(), closedEntries.Select(e => e.IsSuccessful ? 1.0 : 0.0).ToArray());
	        entryCorrelations["IsEMAConverging"] = correlationCoefficient(closedEntries.Select(e => e.IsEMAConverging ? 1.0 : 0.0).ToArray(), closedEntries.Select(e => e.IsSuccessful ? 1.0 : 0.0).ToArray());
			entryCorrelations["IsWithTrendEMA"] = correlationCoefficient(closedEntries.Select(e => e.IsWithTrendEMA ? 1.0 : 0.0).ToArray(), closedEntries.Select(e => e.IsSuccessful ? 1.0 : 0.0).ToArray());
			entryCorrelations["IsWithTrendFastEMA"] = correlationCoefficient(closedEntries.Select(e => e.IsWithTrendFastEMA ? 1.0 : 0.0).ToArray(), closedEntries.Select(e => e.IsSuccessful ? 1.0 : 0.0).ToArray());
			entryCorrelations["IsWithTrendSlowEMA"] = correlationCoefficient(closedEntries.Select(e => e.IsWithTrendSlowEMA ? 1.0 : 0.0).ToArray(), closedEntries.Select(e => e.IsSuccessful ? 1.0 : 0.0).ToArray());
			entryCorrelations["LeadsFastEMAByMoreThanATR"] = correlationCoefficient(closedEntries.Select(e => e.LeadsFastEMAByMoreThanATR ? 1.0 : 0.0).ToArray(), closedEntries.Select(e => e.IsSuccessful ? 1.0 : 0.0).ToArray());
			entryCorrelations["IsWithTrendPressure"] = correlationCoefficient(closedEntries.Select(e => e.IsWithTrendPressure ? 1.0 : 0.0).ToArray(), closedEntries.Select(e => e.IsSuccessful ? 1.0 : 0.0).ToArray());
			entryCorrelations["IsStrongWithTrendPressure"] = correlationCoefficient(closedEntries.Select(e => e.IsStrongWithTrendPressure ? 1.0 : 0.0).ToArray(), closedEntries.Select(e => e.IsSuccessful ? 1.0 : 0.0).ToArray());
			entryCorrelations["IsWithTrendTrendBar"] = correlationCoefficient(closedEntries.Select(e => e.IsWithTrendTrendBar ? 1.0 : 0.0).ToArray(), closedEntries.Select(e => e.IsSuccessful ? 1.0 : 0.0).ToArray());
			entryCorrelations["IsBreakoutBarPattern"] = correlationCoefficient(closedEntries.Select(e => e.IsBreakoutBarPattern ? 1.0 : 0.0).ToArray(), closedEntries.Select(e => e.IsSuccessful ? 1.0 : 0.0).ToArray());
			entryCorrelations["IsWeakBar"] = correlationCoefficient(closedEntries.Select(e => e.IsWeakBar ? 1.0 : 0.0).ToArray(), closedEntries.Select(e => e.IsSuccessful ? 1.0 : 0.0).ToArray());
			entryCorrelations["IsStrongFollowThrough"] = correlationCoefficient(closedEntries.Select(e => e.IsStrongFollowThrough ? 1.0 : 0.0).ToArray(), closedEntries.Select(e => e.IsSuccessful ? 1.0 : 0.0).ToArray());
			entryCorrelations["IsBreakout"] = correlationCoefficient(closedEntries.Select(e => e.IsBreakout ? 1.0 : 0.0).ToArray(), closedEntries.Select(e => e.IsSuccessful ? 1.0 : 0.0).ToArray());
			entryCorrelations["IsBroadChannel"] = correlationCoefficient(closedEntries.Select(e => e.IsBroadChannel ? 1.0 : 0.0).ToArray(), closedEntries.Select(e => e.IsSuccessful ? 1.0 : 0.0).ToArray());
			entryCorrelations["IsTightChannel"] = correlationCoefficient(closedEntries.Select(e => e.IsTightChannel ? 1.0 : 0.0).ToArray(), closedEntries.Select(e => e.IsSuccessful ? 1.0 : 0.0).ToArray());
	        entryCorrelations["IsWeakTrend"] = correlationCoefficient(closedEntries.Select(e => e.IsWeakTrend ? 1.0 : 0.0).ToArray(), closedEntries.Select(e => e.IsSuccessful ? 1.0 : 0.0).ToArray());
			entryCorrelations["IsStrongTrend"] = correlationCoefficient(closedEntries.Select(e => e.IsStrongTrend ? 1.0 : 0.0).ToArray(), closedEntries.Select(e => e.IsSuccessful ? 1.0 : 0.0).ToArray());
			entryCorrelations["IsRSIInRange"] = correlationCoefficient(closedEntries.Select(e => e.IsRSIInRange ? 1.0 : 0.0).ToArray(), closedEntries.Select(e => e.IsSuccessful ? 1.0 : 0.0).ToArray());
			entryCorrelations["IsAboveAverageATR"] = correlationCoefficient(closedEntries.Select(e => e.IsAboveAverageATR ? 1.0 : 0.0).ToArray(), closedEntries.Select(e => e.IsSuccessful ? 1.0 : 0.0).ToArray());
			entryCorrelations["IsBelowAverageATR"] = correlationCoefficient(closedEntries.Select(e => e.IsBelowAverageATR ? 1.0 : 0.0).ToArray(), closedEntries.Select(e => e.IsSuccessful ? 1.0 : 0.0).ToArray());
			entryCorrelations["IsAboveAverageATRByAStdDev"] = correlationCoefficient(closedEntries.Select(e => e.IsAboveAverageATRByAStdDev ? 1.0 : 0.0).ToArray(), closedEntries.Select(e => e.IsSuccessful ? 1.0 : 0.0).ToArray());

			return entryCorrelations.Where(c => !double.IsNaN(c.Value)).ToDictionary(i => i.Key, i => i.Value);
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

			AddEntry();
		}
		#endregion

		#region UpdateStopLossMultiplier()
		public void UpdateStopLossMultiplier(double stopLossMultiplier)
		{
			StopLossMultiplier = stopLossMultiplier;
		}
		#endregion

		#region UpdateStopLoss()
		private void UpdateStopLoss(EntrySignal entry)
		{
			double atrStopLossDistance = atr[0] * StopLossMultiplier;
			double previousSwing = entry.Direction == TrendDirection.Bearish
				? pa.HighestHigh(0, md.LegLong.BarsAgoStarts[0])
				: pa.LowestLow(0, md.LegLong.BarsAgoStarts[0]);
			double swingDistance = Math.Abs(Close[0] - previousSwing);
			double currentBarExtreme = entry.Direction == TrendDirection.Bearish ? High[0] : Low[0];
			double currentBarExtremeDistance = Math.Abs(Close[0] - currentBarExtreme);

			entry.StopDistance = Math.Max(currentBarExtremeDistance, Math.Min(swingDistance, atrStopLossDistance));

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
		private void AddEntry()
		{
			if (nextEntryIndex >= entries.Count) {
				entries.Add(EntrySignal(CurrentBar));
			}

			EntrySignal entry = entries[nextEntryIndex];
			nextEntryIndex++;

			if (nextEntryIndex == WindowSize) {
				nextEntryIndex = 0;
			}

			InitializeEntry(entry);
		}
		#endregion

		#region GetNewEntry()
		public EntrySignal GetNewEntry()
		{
			EntrySignal entry = EntrySignal(CurrentBar);
			return InitializeEntry(entry);
		}
		#endregion

		#region InitializeEntry()
		public EntrySignal InitializeEntry(EntrySignal entry)
		{
			entry.Direction = md.Direction[0];
			entry.PreviousSwing	= entry.Direction == TrendDirection.Bearish
				? pa.BarsAgoHigh(0, md.LegLong.BarsAgoStarts[0])
				: pa.BarsAgoLow(0, md.LegLong.BarsAgoStarts[0]);

			entry.IsEnabled			= true;
			entry.init				= true;
			entry.EntryBar			= CurrentBar;
			entry.Rsi 				= rsi[0];
			entry.Atr 				= atr[0];
			entry.StdDevAtr			= stdDevAtr[0];
			entry.AvgAtrFast			= avgAtrFast[0];
			entry.AvgAtr				= avgAtr[0];
			entry.EmaFast 			= emaFast[0];
			entry.EmaSlow 			= emaSlow[0];
			entry.HighEntry			= High[0];
			entry.LowEntry			= Low[0];
			entry.OpenEntry			= Open[0];
			entry.CloseEntry 		= Close[0];
			entry.TrendType			= md.Stage[0];

			entry.IsClosed 			= false;
			entry.IsSuccessful 		= false;
			entry.entryEvaluator		= this;

			entry.CalculateAdditionalValues();
			entry.Update();

			UpdateStopLoss(entry);

			return entry;
		}
		#endregion

		#region EvaluateExitCriteria()
		public double EvaluateExitCriteria(EntrySignal entry)
		{
			int significantExits = significantExitCorrelations.Count;
			Dictionary<string, int> criteria = new Dictionary<string, int>();

			if (significantExits == 0) {
				return -1;
			}

			foreach (var criterion in significantExitCorrelations) {
				if (criterion.Key == "TrendDirectionChanged") criteria[criterion.Key] = entry.TrendDirectionChanged > 0 ? 1 : 0;
				if (criterion.Key == "CounterTrendTightChannel") criteria[criterion.Key] = entry.CounterTrendTightChannel > 0 ? 1 : 0;
				if (criterion.Key == "CounterTrendBroadChannel") criteria[criterion.Key] = entry.CounterTrendBroadChannel > 0 ? 1 : 0;
				if (criterion.Key == "CounterTrendBreakout") criteria[criterion.Key] = entry.CounterTrendBreakout > 0 ? 1 : 0;
				if (criterion.Key == "CounterTrendBreakoutTrend") criteria[criterion.Key] = entry.CounterTrendBreakoutTrend > 0 ? 1 : 0;
				if (criterion.Key == "CounterTrendLegLong") criteria[criterion.Key] = entry.CounterTrendLegLong > 0 ? 1 : 0;
				if (criterion.Key == "CounterTrendLegShort") criteria[criterion.Key] = entry.CounterTrendLegShort > 0 ? 1 : 0;
				if (criterion.Key == "CounterTrendLegAfterDoubleTopBottom") criteria[criterion.Key] = entry.CounterTrendLegAfterDoubleTopBottom > 0 ? 1 : 0;
				if (criterion.Key == "TrailingStopBeyondPreviousExtreme") criteria[criterion.Key] = entry.TrailingStopBeyondPreviousExtreme > 0 ? 1 : 0;
				if (criterion.Key == "MovingAverageCrossover") criteria[criterion.Key] = entry.MovingAverageCrossover > 0 ? 1 : 0;
				if (criterion.Key == "DoubleTopBottom") criteria[criterion.Key] = entry.DoubleTopBottom > 0 ? 1 : 0;
				if (criterion.Key == "NoNewExtreme8") criteria[criterion.Key] = entry.NoNewExtreme8 > 0 ? 1 : 0;
				if (criterion.Key == "NoNewExtreme10") criteria[criterion.Key] = entry.NoNewExtreme10 > 0 ? 1 : 0;
				if (criterion.Key == "NoNewExtreme12") criteria[criterion.Key] = entry.NoNewExtreme12 > 0 ? 1 : 0;
				if (criterion.Key == "CounterTrendPressure") criteria[criterion.Key] = entry.CounterTrendPressure > 0 ? 1 : 0;
				if (criterion.Key == "CounterTrendStrongPressure") criteria[criterion.Key] = entry.CounterTrendStrongPressure > 0 ? 1 : 0;
				if (criterion.Key == "CounterTrendStrongTrend") criteria[criterion.Key] = entry.CounterTrendStrongTrend > 0 ? 1 : 0;
				if (criterion.Key == "RSIOutOfRange") criteria[criterion.Key] = entry.RSIOutOfRange > 0 ? 1 : 0;
				if (criterion.Key == "ATRAboveAverageATR") criteria[criterion.Key] = entry.ATRAboveAverageATR > 0 ? 1 : 0;
				if (criterion.Key == "ATRBelowAverageATR") criteria[criterion.Key] = entry.ATRBelowAverageATR > 0 ? 1 : 0;
				if (criterion.Key == "ATRAboveAverageATRByAStdDev") criteria[criterion.Key] = entry.ATRAboveAverageATRByAStdDev > 0 ? 1 : 0;
				if (criterion.Key == "ATRBelowAverageATRByAStdDev") criteria[criterion.Key] = entry.ATRBelowAverageATRByAStdDev > 0 ? 1 : 0;
				if (criterion.Key == "StrongCounterTrendFollowThrough") criteria[criterion.Key] = entry.StrongCounterTrendFollowThrough > 0 ? 1 : 0;
				if (criterion.Key == "ProfitTarget1") criteria[criterion.Key] = entry.ProfitTarget1 > 0 ? 1 : 0;
				if (criterion.Key == "ProfitTarget2") criteria[criterion.Key] = entry.ProfitTarget2 > 0 ? 1 : 0;
				if (criterion.Key == "ProfitTarget3") criteria[criterion.Key] = entry.ProfitTarget3 > 0 ? 1 : 0;
				if (criterion.Key == "ProfitTarget4") criteria[criterion.Key] = entry.ProfitTarget4 > 0 ? 1 : 0;
				if (criterion.Key == "ProfitTarget5") criteria[criterion.Key] = entry.ProfitTarget5 > 0 ? 1 : 0;
			}

			entry.exitConditions = criteria;

			return (double) criteria.Values.Sum() / (double) significantExits;
		}
		#endregion

		#region EvaluateCriteria()
		public Dictionary<string, int> EvaluateCriteria(int barsAgo)
		{
			int criteriaCount = significantCorrelations.Count;
			Dictionary<string, int> criteria = new Dictionary<string, int>();

			if (criteriaCount == 0) {
				matched[0] = 0;
				return criteria;
			}

			foreach (var criterion in significantCorrelations) {
				bool positive = criterion.Value > 0;
				if (criterion.Key == "RSI") {
					criteria[criterion.Key] = EvaluateRSI(barsAgo, positive) ? 1 : 0;
				}
				if (criterion.Key == "ATR") {
					criteria[criterion.Key] = EvaluateATR(barsAgo, positive) ? 1 : 0;
				}
				if (criterion.Key == "IsEMADiverging") {
					criteria[criterion.Key] = EvaluateEMADiverging(barsAgo, positive) ? 1 : 0;
				}
				if (criterion.Key == "IsEMAConverging") {
					criteria[criterion.Key] = EvaluateEMAConverging(barsAgo, positive) ? 1 : 0;
				}
				if (criterion.Key == "IsWithTrendEMA") {
					criteria[criterion.Key] = EvaluateWithTrendEMA(barsAgo, positive) ? 1 : 0;
				}
				if (criterion.Key == "IsWithTrendFastEMA") {
					criteria[criterion.Key] = EvaluateWithTrendFastEMA(barsAgo, positive) ? 1 : 0;
				}
				if (criterion.Key == "IsWithTrendSlowEMA") {
					criteria[criterion.Key] = EvaluateWithTrendSlowEMA(barsAgo, positive) ? 1 : 0;
				}
				if (criterion.Key == "LeadsFastEMAByMoreThanATR") {
					criteria[criterion.Key] = EvaluateLeadsFastEMAByMoreThanATR(barsAgo, positive) ? 1 : 0;
				}
				if (criterion.Key == "IsWithTrendPressure") {
					criteria[criterion.Key] = EvaluateWithTrendPressure(barsAgo, positive) ? 1 : 0;
				}
				if (criterion.Key == "IsStrongWithTrendPressure") {
					criteria[criterion.Key] = EvaluateStrongWithTrendPressure(barsAgo, positive) ? 1 : 0;
				}
				if (criterion.Key == "IsWithTrendTrendBar") {
					criteria[criterion.Key] = EvaluateWithTrendTrendBar(barsAgo, positive) ? 1 : 0;
				}
				if (criterion.Key == "IsBreakoutBarPattern") {
					criteria[criterion.Key] = EvaluateBreakoutBarPattern(barsAgo, positive) ? 1 : 0;
				}
				if (criterion.Key == "IsWeakBar") {
					criteria[criterion.Key] = EvaluateWeakBar(barsAgo, positive) ? 1 : 0;
				}
				if (criterion.Key == "IsStrongFollowThrough") {
					criteria[criterion.Key] = EvaluateStrongFollowThrough(barsAgo, positive) ? 1 : 0;
				}
				if (criterion.Key == "IsBreakout") {
					criteria[criterion.Key] = EvaluateBreakout(barsAgo, positive) ? 1 : 0;
				}
				if (criterion.Key == "IsBroadChannel") {
					criteria[criterion.Key] = EvaluateBroadChannel(barsAgo, positive) ? 1 : 0;
				}
				if (criterion.Key == "IsTightChannel") {
					criteria[criterion.Key] = EvaluateTightChannel(barsAgo, positive) ? 1 : 0;
				}
				if (criterion.Key == "IsWeakTrend") {
					criteria[criterion.Key] = EvaluateWeakTrend(barsAgo, positive) ? 1 : 0;
				}
				if (criterion.Key == "IsStrongTrend") {
					criteria[criterion.Key] = EvaluateStrongTrend(barsAgo, positive) ? 1 : 0;
				}
				if (criterion.Key == "IsRSIInRange") {
					criteria[criterion.Key] = EvaluateRSIInRange(barsAgo, positive) ? 1 : 0;
				}
				if (criterion.Key == "IsAboveAverageATR") {
					criteria[criterion.Key] = EvaluateAboveAverageATR(barsAgo, positive) ? 1 : 0;
				}
				if (criterion.Key == "IsBelowAverageATR") {
					criteria[criterion.Key] = EvaluateBelowAverageATR(barsAgo, positive) ? 1 : 0;
				}
				if (criterion.Key == "IsAboveAverageATRByAStdDev") {
					criteria[criterion.Key] = EvaluateAboveAverageATRByAStdDev(barsAgo, positive) ? 1 : 0;
				}
			}

			matched[0] = (double) criteria.Values.Sum() / (double) criteriaCount;

			return criteria;
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
		public double Window
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
		public PR.EntryEvaluator EntryEvaluator(int period, double window)
		{
			return EntryEvaluator(Input, period, window);
		}

		public PR.EntryEvaluator EntryEvaluator(ISeries<double> input, int period, double window)
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
		public Indicators.PR.EntryEvaluator EntryEvaluator(int period, double window)
		{
			return indicator.EntryEvaluator(Input, period, window);
		}

		public Indicators.PR.EntryEvaluator EntryEvaluator(ISeries<double> input , int period, double window)
		{
			return indicator.EntryEvaluator(input, period, window);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.PR.EntryEvaluator EntryEvaluator(int period, double window)
		{
			return indicator.EntryEvaluator(Input, period, window);
		}

		public Indicators.PR.EntryEvaluator EntryEvaluator(ISeries<double> input , int period, double window)
		{
			return indicator.EntryEvaluator(input, period, window);
		}
	}
}

#endregion
