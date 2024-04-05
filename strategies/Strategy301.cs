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
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.DrawingTools;
using NinjaTrader.NinjaScript.Indicators.PR;
#endregion

//This namespace holds Strategies in this folder and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Strategies
{
	public class Strategy301 : Strategy
	{
		#region Variables
		private Utils utils = new Utils();
		private Legs legs;
		private MarketDirection marketDirection;
		private EntryEvaluator entryEvaluator;
//		private TradesExporter tradesExporter;
		private ISet<int> tradesTracking = new HashSet<int>();
		private EntrySignal entry;
		private double previousSuccessRate;
		private double successRate;
		private List<double> successRates 		= new List<double>();
		private List<bool> tradeOutcomes 		= new List<bool>();
		private int rollingWindowSize 			= 50;
		private MarketPosition tradeDirection	= MarketPosition.Flat;
		private double successRateThreshold 		= 0.05;

		private Dictionary<string, List<double>> entryCriteria = new Dictionary<string, List<double>>();
		private Dictionary<string, List<double>> exitCriteria = new Dictionary<string, List<double>>();

		private DateTime LastDataDay		= new DateTime(2023, 03, 17);
		private DateTime OpenTime		= DateTime.Parse("10:00", System.Globalization.CultureInfo.InvariantCulture);
		private DateTime CloseTime		= DateTime.Parse("15:30", System.Globalization.CultureInfo.InvariantCulture);
		private DateTime LastTradeTime	= DateTime.Parse("15:00", System.Globalization.CultureInfo.InvariantCulture);
		#endregion

		#region OnStateChange()
		protected override void OnStateChange()
		{
			#region State.SetDefaults
			if (State == State.SetDefaults)
			{
				Description									= @"";
				Name											= "Strategy 3.0.1";
				Calculate									= Calculate.OnBarClose;
				EntriesPerDirection							= 1;
				EntryHandling								= EntryHandling.UniqueEntries;
				IsExitOnSessionCloseStrategy					= true;
				ExitOnSessionCloseSeconds					= 30;
				IsFillLimitOnTouch							= false;
				MaximumBarsLookBack							= MaximumBarsLookBack.TwoHundredFiftySix;
				OrderFillResolution							= OrderFillResolution.Standard;
				Slippage										= 0;
				StartBehavior								= StartBehavior.WaitUntilFlat;
				TimeInForce									= TimeInForce.Gtc;
				TraceOrders									= false;
				RealtimeErrorHandling						= RealtimeErrorHandling.StopCancelClose;
				StopTargetHandling							= StopTargetHandling.PerEntryExecution;
				BarsRequiredToTrade							= 200;
				// Disable this property for performance gains in Strategy Analyzer optimizations
				// See the Help Guide for additional information
				IsInstantiatedOnEachOptimizationIteration	= true;
				TimeShift									= -6;
				Period 										= 10;
				Quantity 									= 1;
//				TargetMultiplier								= 1;
//				QuantityMultiplier							= 2;
				Window										= 20;
			}
			#endregion

			#region State.Configure
			else if (State == State.Configure)
			{
				entryEvaluator			= EntryEvaluator(Period, Window);
				entry					= EntrySignal(1);

				exitCriteria["TrendDirectionChanged"] = new List<double>();
				exitCriteria["CounterTrendTightChannel"] = new List<double>();
				exitCriteria["CounterTrendBroadChannel"] = new List<double>();
				exitCriteria["CounterTrendBreakout"] = new List<double>();
				exitCriteria["CounterTrendBreakoutTrend"] = new List<double>();
				exitCriteria["CounterTrendLegLong"] = new List<double>();
				exitCriteria["CounterTrendLegShort"] = new List<double>();
				exitCriteria["CounterTrendLegAfterDoubleTopBottom"] = new List<double>();
				exitCriteria["TrailingStopBeyondPreviousExtreme"] = new List<double>();
				exitCriteria["MovingAverageCrossover"] = new List<double>();
				exitCriteria["DoubleTopBottom"] = new List<double>();
				exitCriteria["NoNewExtreme8"] = new List<double>();
				exitCriteria["NoNewExtreme10"] = new List<double>();
				exitCriteria["NoNewExtreme12"] = new List<double>();
				exitCriteria["CounterTrendPressure"] = new List<double>();
				exitCriteria["CounterTrendStrongPressure"] = new List<double>();
				exitCriteria["CounterTrendStrongTrend"] = new List<double>();
				exitCriteria["RSIOutOfRange"] = new List<double>();
				exitCriteria["ATRAboveAverageATR"] = new List<double>();
				exitCriteria["ATRBelowAverageATR"] = new List<double>();
				exitCriteria["ATRAboveAverageATRByAStdDev"] = new List<double>();
				exitCriteria["ATRBelowAverageATRByAStdDev"] = new List<double>();
				exitCriteria["StrongCounterTrendFollowThrough"] = new List<double>();
				exitCriteria["ProfitTarget1"] = new List<double>();
				exitCriteria["ProfitTarget2"] = new List<double>();
				exitCriteria["ProfitTarget3"] = new List<double>();
				exitCriteria["ProfitTarget4"] = new List<double>();
				exitCriteria["ProfitTarget5"] = new List<double>();

				entryCriteria["RSI"] = new List<double>();
		        entryCriteria["ATR"] = new List<double>();
		        entryCriteria["IsEMADiverging"] = new List<double>();
		        entryCriteria["IsEMAConverging"] = new List<double>();
				entryCriteria["IsWithTrendEMA"] = new List<double>();
				entryCriteria["IsWithTrendFastEMA"] = new List<double>();
				entryCriteria["IsWithTrendSlowEMA"] = new List<double>();
				entryCriteria["LeadsFastEMAByMoreThanATR"] = new List<double>();
				entryCriteria["IsWithTrendPressure"] = new List<double>();
				entryCriteria["IsStrongWithTrendPressure"] = new List<double>();
				entryCriteria["IsWithTrendTrendBar"] = new List<double>();
				entryCriteria["IsBreakoutBarPattern"] = new List<double>();
				entryCriteria["IsWeakBar"] = new List<double>();
				entryCriteria["IsStrongFollowThrough"] = new List<double>();
				entryCriteria["IsBreakout"] = new List<double>();
				entryCriteria["IsBroadChannel"] = new List<double>();
				entryCriteria["IsTightChannel"] = new List<double>();
		        entryCriteria["IsWeakTrend"] = new List<double>();
				entryCriteria["IsStrongTrend"] = new List<double>();
				entryCriteria["IsRSIInRange"] = new List<double>();
				entryCriteria["IsAboveAverageATR"] = new List<double>();
				entryCriteria["IsBelowAverageATR"] = new List<double>();
				entryCriteria["IsAboveAverageATRByAStdDev"] = new List<double>();
			}
			#endregion

			#region State.DataLoaded
			if (State == State.DataLoaded) {
//				tradesExporter			= TradesExporter(Name, Instrument.MasterInstrument.Name);
				marketDirection 			= entryEvaluator.md;
				legs 					= marketDirection.LegLong;
			}
			#endregion
		}
		#endregion

		#region OnBarUpdate()
		protected override void OnBarUpdate()
		{
			entry.Update();
			entry.UpdateStatus();

			foreach (var criterion in entryCriteria) {
				if (criterion.Value.Count > 20) {
					criterion.Value.RemoveAt(0);
				}
			}

			foreach (var criterion in exitCriteria) {
				if (criterion.Value.Count > 20) {
					criterion.Value.RemoveAt(0);
				}
			}

//			if (Position.MarketPosition == MarketPosition.Flat && tradeDirection != MarketPosition.Flat) {
//				UpdateTradeOutcomes(entry.IsSuccessful);
//				previousSuccessRate = successRate;
//				UpdateSuccessRates();
//				AdjustStrategy();
//			}

			tradeDirection = Position.MarketPosition;

			if (CurrentBar < BarsRequiredToTrade || CurrentBars[0] < 1) {
				return;
            }

			exitPositions();

			setEntries();
		}
		#endregion

		#region OnExecutionUpdate()
		protected override void OnExecutionUpdate(Execution execution, string executionId, double price, int quantity, MarketPosition marketPosition, string orderId, DateTime time)
		{
			if (SystemPerformance.AllTrades.Count > 0)
			{
				Trade trade = SystemPerformance.AllTrades[SystemPerformance.AllTrades.Count - 1];
				OnNewTrade(trade);

//				if (TradesExporterActivated)
//				{
//					tradesExporter.OnNewTrade(trade);
//				}
			}
		}
		#endregion

		#region OnNewTrade()
		private void OnNewTrade(Trade trade)
		{
			if (tradesTracking.Contains(trade.TradeNumber)) {
				return;
			}

			tradesTracking.Add(trade.TradeNumber);

			UpdateTradeOutcomes(entry.IsSuccessful);
			previousSuccessRate = successRate;
			UpdateSuccessRates();
			AdjustStrategy();
		}
		#endregion

		#region shouldExit()
		private bool shouldExit() {
			if (Position.MarketPosition != MarketPosition.Flat) {
				if (entryEvaluator.EvaluateExitCriteria(entry) > successRate) {
					return true;
				}
			}

			return false;
		}
		#endregion

		#region exitPositions()
		private void exitPositions() {
			if (isValidTradeTime() && !shouldExit()) {
				return;
			}

			if (Position.MarketPosition == MarketPosition.Long) {
				ExitLong();
			}

			if (Position.MarketPosition == MarketPosition.Short) {
				ExitShort();
			}
        }
		#endregion

		#region isValidEntryTime()
		private bool isValidEntryTime()
		{
			int now = ToTime(Time[0]);

			double shift = Time[0] > LastDataDay ? 0.0 : TimeShift;

			if (now < ToTime(OpenTime.AddHours(shift))) {
				return false;
			}

			if (now > ToTime(LastTradeTime.AddHours(shift))) {
				return false;
			}

			return true;
		}
		#endregion

		#region isValidTradeTime()
		private bool isValidTradeTime()
		{
			int now = ToTime(Time[0]);

			double shift = Time[0] > LastDataDay ? 0.0 : TimeShift;

			if (now > ToTime(CloseTime.AddHours(shift))) {
				return false;
			}

			if (now < ToTime(OpenTime.AddHours(shift))) {
				return false;
			}

			return true;
		}
		#endregion

		#region setEntries()
		private void setEntries()
		{
			if (!isValidEntryTime()) {
				return;
            }

			if (Position.MarketPosition != MarketPosition.Flat) {
				return;
			}

			if (!entryPatternMatched()) {
				return;
			}

			entryEvaluator.InitializeEntry(entry);

			double entryRating = evaluateEntry();

			double probabilityOfTrading = entryRating;
			Random rand = new Random();
       		double randomValue = rand.NextDouble();

        		if (randomValue > probabilityOfTrading) {
				return;
			}




			int quantity = Math.Max(1, (int) Math.Round(entryRating * (double) Quantity, 0));

//			int quantity = Math.Max(1, (int) Math.Round(entryEvaluator.matched[0] * (double) Quantity, 0));

			if (marketDirection.Direction[0] == TrendDirection.Bullish) {
				double swingLow = legs.BarsAgoStarts[0] > 0 ? Math.Min(MIN(Low, legs.BarsAgoStarts[0])[0], MIN(Low, 4)[0]) : Low[0];
				double stopLossDistance = 4 * (Close[0] - swingLow) + 1;
				double profitDistance = (0.75 * successRate + 0.25) * stopLossDistance;

				entry.StopLossUsed = stopLossDistance;
				entry.ProfitTargetUsed = profitDistance;

				if (swingLow < Low[0]) {
					SetStopLoss(CalculationMode.Ticks, stopLossDistance);
					if (successRate < successRateThreshold) {
						SetProfitTarget("LongEntry1", CalculationMode.Ticks, profitDistance);
					}

					EnterLong(quantity, "LongEntry1");
				}
			}

			if (marketDirection.Direction[0] == TrendDirection.Bearish) {
				double swingHigh = legs.BarsAgoStarts[0] > 0 ? Math.Max(MAX(High, legs.BarsAgoStarts[0])[0], MAX(High, 4)[0]) : High[0];
				double stopLossDistance = 4 * (swingHigh - Close[0]) + 1;
				double profitDistance = (0.75 * successRate + 0.25) * stopLossDistance;

				entry.StopLossUsed = stopLossDistance;
				entry.ProfitTargetUsed = profitDistance;

				if (swingHigh > High[0]) {
					SetStopLoss(CalculationMode.Ticks, stopLossDistance);
					if (successRate < successRateThreshold) {
						SetProfitTarget("ShortEntry1", CalculationMode.Ticks, profitDistance);
					}
					EnterShort(quantity, "ShortEntry1");
				}
			}
		}
		#endregion

		#region
		private double evaluateEntry()
		{
			entry.entryConditions = entryEvaluator.EvaluateCriteria(0);

			double weightedSum = 0;
		    double totalWeight = 0;
			double defaultWeight = 0.2;

		    foreach (var criterion in entryCriteria)
		    {
		        List<double> historicalPerformance = criterion.Value;
		        double averagePerformance = historicalPerformance.Count > 0 ? historicalPerformance.Average() : 0;
				double weight = historicalPerformance.Count > 0 ? Math.Max(0, averagePerformance) : defaultWeight;

		        if (entry.entryConditions.ContainsKey(criterion.Key) && entry.entryConditions[criterion.Key] == 1)
		        {
		            weightedSum += weight;
		        }

		        totalWeight += weight;
		    }

		    if (totalWeight > 0)
		    {
		        double normalizedWeight = weightedSum / totalWeight;
		        return Math.Min(1, Math.Max(0, normalizedWeight));
		    }
		    else
		   	{
		        return 0;
		    }

		}
		#endregion

		#region entryPatternMatched()
		private bool entryPatternMatched()
		{
			if (marketDirection.Direction[0] == TrendDirection.Flat) {
				return false;
			}

			if (entryEvaluator.matched[0] == 0) {
				return false;
			}

			if (legs.BarsAgoStarts[0] < 4) {
				return false;
			}

			if (legs.BarsAgoStarts[0] > 8) {
				return false;
			}

			return true;
		}
		#endregion

		#region UpdateTradeOutcomes()
		private void UpdateTradeOutcomes(bool isSuccessful)
		{
		    tradeOutcomes.Add(isSuccessful);

		    if (tradeOutcomes.Count > rollingWindowSize) {
		        tradeOutcomes.RemoveAt(0);
		    }
		}
		#endregion

		#region UpdateSuccessRates()
		private void UpdateSuccessRates()
		{
			successRate = CalculateSuccessRate();
			successRates.Add(successRate);

		    if (successRates.Count > 10) {
		        successRates.RemoveAt(0);
		    }
		}
		#endregion

		#region CalculateSuccessRate()
		private double CalculateSuccessRate()
		{
		    if (tradeOutcomes.Count == 0) {
		        return 0.0;
		    }

		    int successfulTrades = tradeOutcomes.Count(outcome => outcome);

		    return (double) successfulTrades / (double) tradeOutcomes.Count;
		}
		#endregion

		#region AdjustStrategy()
		private void AdjustStrategy()
		{
			double successRateStdDev 	= utils.StandardDeviation(successRates);
			double successRateAvg		= successRates.Average();
			double successRateStep		= successRateAvg * 0.01;
			double successRateMultiple	= ((successRates[successRates.Count - 1] - successRateAvg) / successRateStdDev - 1) / 0.5;
			successRateThreshold 		= successRateAvg;// + successRateStep * successRateMultiple;

//			double windowMin 			= Window * 0.75;
//			double windowMax	 			= Window * 1.25;

//			if (successRate < successRateAvg) {
////				Print("Below");
//				entryEvaluator.Window = entryEvaluator.Window == windowMin ? Window : windowMax;
//			} else {
////				Print("Above");
//				entryEvaluator.Window = entryEvaluator.Window == windowMax ? Window : windowMin;
//			}
//			Print(entryEvaluator.Window);
//			Print("==========");


//			double windowAdjustmentSize	= Window * 0.02;
//			double atrMultiple 			= (entryEvaluator.atr[0] - entryEvaluator.avgAtr[0]) / entryEvaluator.stdDevAtr[0];
//			double windowAdjustment 		= windowAdjustmentSize * atrMultiple;
//			double WindowMin 			= Window * 0.75;
//			double WindowMax	 			= Window * 1.25;
//			double windowAdjusted 		= Math.Max(WindowMin, Math.Min(WindowMax, entryEvaluator.Window + windowAdjustment));

//			if (successRate < successRateAvg) {

//				if (Math.Abs(atrMultiple) > 1) {
//					entryEvaluator.Window = Math.Max(1, windowAdjusted);
//				}
//			}

//			Print(Time[0]);
//			Print(windowAdjustment);
//			Print(successRateThreshold);
//			Print(entryEvaluator.Window);

//			Print(entry.StopLossUsed);
//			Print(entry.ProfitTargetUsed);
//			Print(entry.Atr);
//			Print(entry.IsSuccessful);
//			Print(entry.DistanceMoved > 0);
//			Print("==========");

//			Print(entry.StopLossUsed + "," + entry.ProfitTargetUsed + "," + entry.Atr + "," + entry.IsSuccessful + "," + (entry.DistanceMoved > 0) + "," + entry.DistanceMoved);
			foreach (var criterion in entry.entryConditions) {
				entryCriteria[criterion.Key].Add(entry.DistanceMoved);
			}

			foreach (var criterion in entry.exitConditions) {
				exitCriteria[criterion.Key].Add(entry.DistanceMoved);
			}

//			Print("==========");
//			Print("Entry");
//			foreach (var criterion in entryCriteria) {
//				Print(criterion.Key + "," + String.Join(",", criterion.Value));
//			}
//			Print("Exit");
//			foreach (var criterion in exitCriteria) {
//				Print(criterion.Key + "," + String.Join(",", criterion.Value));
//			}
		}
		#endregion

		#region Properties

		[NinjaScriptProperty]
		[Range(2, int.MaxValue)]
		[Display(Name="Period", Description="Period", Order=0, GroupName="Parameters")]
		public int Period
		{ get; set; }

		[NinjaScriptProperty]
		[Range(int.MinValue, int.MaxValue)]
		[Display(Name="Time Shift (Hours)", Description="Time Shift", Order=1, GroupName="Parameters")]
		public int TimeShift
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Quantity", Description="Quantity", Order=2, GroupName="Parameters")]
		public int Quantity
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, 200)]
		[Display(Name="Window", Description="Window", Order=3, GroupName="Parameters")]
		public double Window
		{ get; set; }

//		[NinjaScriptProperty]
//		[Display(Name="Export Trades", Description="Export Trades", Order=6, GroupName="Parameters")]
//		public bool TradesExporterActivated
//		{ get; set; }

		#endregion
	}
}
