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
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.Data;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript;
using NinjaTrader.NinjaScript.DrawingTools;
using NinjaTrader.NinjaScript.Indicators;
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
        private List<double> successRates = new List<double>();
        private List<bool> tradeOutcomes = new List<bool>();
		private List<WindowSizePerformance> windowSizePerformanceData = new List<WindowSizePerformance>();
        private int rollingWindowSize = 50;
        private MarketPosition tradeDirection = MarketPosition.Flat;
        private double successRateThreshold = 0.05;

        private Dictionary<string, List<double>> entryCriteria =
            new Dictionary<string, List<double>>();
        private Dictionary<string, List<double>> exitCriteria =
            new Dictionary<string, List<double>>();

        private DateTime LastDataDay = new DateTime(2023, 03, 17);
        private DateTime OpenTime = DateTime.Parse(
            "10:00",
            System.Globalization.CultureInfo.InvariantCulture
        );
        private DateTime CloseTime = DateTime.Parse(
            "15:30",
            System.Globalization.CultureInfo.InvariantCulture
        );
        private DateTime LastTradeTime = DateTime.Parse(
            "15:00",
            System.Globalization.CultureInfo.InvariantCulture
        );
        #endregion

        #region OnStateChange()
        protected override void OnStateChange()
        {
            #region State.SetDefaults
            if (State == State.SetDefaults)
            {
                Description = @"";
                Name = "Strategy 3.0.1";
                Calculate = Calculate.OnBarClose;
                EntriesPerDirection = 1;
                EntryHandling = EntryHandling.UniqueEntries;
                IsExitOnSessionCloseStrategy = true;
                ExitOnSessionCloseSeconds = 30;
                IsFillLimitOnTouch = false;
                MaximumBarsLookBack = MaximumBarsLookBack.TwoHundredFiftySix;
                OrderFillResolution = OrderFillResolution.Standard;
                Slippage = 0;
                StartBehavior = StartBehavior.WaitUntilFlat;
                TimeInForce = TimeInForce.Gtc;
                TraceOrders = false;
                RealtimeErrorHandling = RealtimeErrorHandling.StopCancelClose;
                StopTargetHandling = StopTargetHandling.PerEntryExecution;
                BarsRequiredToTrade = 200;
                // Disable this property for performance gains in Strategy Analyzer optimizations
                // See the Help Guide for additional information
                IsInstantiatedOnEachOptimizationIteration = true;
                TimeShift = -6;
                Period = 10;
                Quantity = 1;
                Window = 8;
            }
            #endregion

            #region State.Configure
            else if (State == State.Configure)
            {
                entryEvaluator = EntryEvaluator(Period, Window);
                entry = EntrySignal(1);

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
            if (State == State.DataLoaded)
            {
                //				tradesExporter			= TradesExporter(Name, Instrument.MasterInstrument.Name);
                marketDirection = entryEvaluator.md;
                legs = marketDirection.LegLong;
            }
            #endregion
        }
        #endregion

        #region OnBarUpdate()
        protected override void OnBarUpdate()
        {
            entry.Update();
            entry.UpdateStatus();

            tradeDirection = Position.MarketPosition;

            if (CurrentBar < BarsRequiredToTrade || CurrentBars[0] < 1)
            {
                return;
            }

            exitPositions();

            setEntries();
        }
        #endregion

        #region OnExecutionUpdate()
        protected override void OnExecutionUpdate(
            Execution execution,
            string executionId,
            double price,
            int quantity,
            MarketPosition marketPosition,
            string orderId,
            DateTime time
        )
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
            if (tradesTracking.Contains(trade.TradeNumber))
            {
                return;
            }

            tradesTracking.Add(trade.TradeNumber);

            UpdateTradeOutcomes(entry.IsSuccessful);
            previousSuccessRate = successRate;
            UpdateSuccessRates();

            foreach (var criterion in entryCriteria)
            {
                if (
                    entry.entryConditions.ContainsKey(criterion.Key)
                    && entry.entryConditions[criterion.Key] == 1
                )
                {
                    entryCriteria[criterion.Key].Add(entry.DistanceMoved);
                    continue;
                }

                entryCriteria[criterion.Key].Add(0);
            }

            foreach (var criterion in exitCriteria)
            {
                if (
                    entry.exitConditions.ContainsKey(criterion.Key)
                    && entry.exitConditions[criterion.Key] == 1
                )
                {
                    exitCriteria[criterion.Key].Add(entry.DistanceMoved);
                    continue;
                }

                exitCriteria[criterion.Key].Add(0);
            }

			WindowSizePerformance performance = new WindowSizePerformance
		    {
		        WindowSize = entryEvaluator.Window,
		        ATR = entry.AvgAtr,
		        SuccessRate = entry.IsSuccessful ? 1.0 : 0.0,
		        Trades = 1
		    };

		    windowSizePerformanceData.Add(performance);

            AdjustStrategy();
        }
        #endregion

        #region shouldExit()
        private bool shouldExit()
        {
            if (Position.MarketPosition != MarketPosition.Flat)
            {
                if (entryEvaluator.EvaluateExitCriteria(entry) > successRate * 0.5)
                {
                    return true;
                }
                //				if (entryEvaluator.EvaluateExitCriteria(entry) > 0) {
                //					return true;
                //				}
            }

            return false;
        }
        #endregion

        #region exitPositions()
        private void exitPositions()
        {
            if (isValidTradeTime() && !shouldExit())
            {
                return;
            }

            if (Position.MarketPosition == MarketPosition.Long)
            {
                ExitLong();
            }

            if (Position.MarketPosition == MarketPosition.Short)
            {
                ExitShort();
            }
        }
        #endregion

        #region isValidEntryTime()
        private bool isValidEntryTime()
        {
            int now = ToTime(Time[0]);

            double shift = Time[0] > LastDataDay ? 0.0 : TimeShift;

            if (now < ToTime(OpenTime.AddHours(shift)))
            {
                return false;
            }

            if (now > ToTime(LastTradeTime.AddHours(shift)))
            {
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

            if (now > ToTime(CloseTime.AddHours(shift)))
            {
                return false;
            }

            if (now < ToTime(OpenTime.AddHours(shift)))
            {
                return false;
            }

            return true;
        }
        #endregion

        #region setEntries()
        private void setEntries()
        {
            if (!isValidEntryTime())
            {
                return;
            }

            if (Position.MarketPosition != MarketPosition.Flat)
            {
                return;
            }

            if (!entryPatternMatched())
            {
                return;
            }

            entryEvaluator.InitializeEntry(entry);

            double entryRating = evaluateEntry();

            double entryThreshold = successRate > successRateThreshold ? 0.4 : 0.7;

            if (entryRating < entryThreshold)
            {
                return;
            }

            int quantity = Math.Max(
                1,
                (int)Math.Round(((successRate + entryRating) / 2) * (double)Quantity, 0)
            );
            double stopLossAdjustment =
                10
                * (
                    (
                        (entryEvaluator.avgAtr[0] - entryEvaluator.atr[0])
                        / entryEvaluator.stdDevAtr[0]
                    )
                );
            double stopLossThreshold =
                20
                + (
                    successRate < successRateThreshold ? stopLossAdjustment / 2 : stopLossAdjustment
                );

            if (marketDirection.Direction[0] == TrendDirection.Bullish)
            {
                double swingLow =
                    legs.BarsAgoStarts[0] > 0
                        ? Math.Min(MIN(Low, legs.BarsAgoStarts[0])[0], MIN(Low, 4)[0])
                        : Low[0];
                double stopLossDistance = 4 * (Close[0] - swingLow) + 1;
                double profitDistance = (0.75 * successRate + 0.25) * stopLossDistance;

                if (stopLossDistance > stopLossThreshold)
                {
                    return;
                }

                entry.StopLossUsed = stopLossDistance;
                entry.ProfitTargetUsed = profitDistance;

                if (swingLow < Low[0])
                {
                    SetStopLoss(CalculationMode.Ticks, stopLossDistance);
//                    if (successRate < successRateThreshold)
//                    {
//                        	SetProfitTarget("LongEntry1", CalculationMode.Ticks, profitDistance);
//                    }

                    EnterLong(quantity, "LongEntry1");
                }
            }

            if (marketDirection.Direction[0] == TrendDirection.Bearish)
            {
                double swingHigh =
                    legs.BarsAgoStarts[0] > 0
                        ? Math.Max(MAX(High, legs.BarsAgoStarts[0])[0], MAX(High, 4)[0])
                        : High[0];
                double stopLossDistance = 4 * (swingHigh - Close[0]) + 1;
                double profitDistance = (0.75 * successRate + 0.25) * stopLossDistance;

                if (stopLossDistance > stopLossThreshold)
                {
                    return;
                }

                entry.StopLossUsed = stopLossDistance;
                entry.ProfitTargetUsed = profitDistance;

                if (swingHigh > High[0])
                {
                    SetStopLoss(CalculationMode.Ticks, stopLossDistance);
//                    if (successRate < successRateThreshold)
//                    {
//    						SetProfitTarget("ShortEntry1", CalculationMode.Ticks, profitDistance);
//                    }

                    EnterShort(quantity, "ShortEntry1");
                }
            }
        }
        #endregion

        #region evaluateEntry()
        private double evaluateEntry()
        {
            entry.entryConditions = entryEvaluator.EvaluateCriteria(0);

            double weightedSum = 0;
            double totalCount = 0;
            double defaultWinRate = entryEvaluator.matched[0];
            int windowSize = 30;
            int minimumTrades = 5;

            foreach (var criterion in entryCriteria)
            {
                List<double> historicalPerformance = criterion.Value;

                if (historicalPerformance.Count > windowSize)
                {
                    historicalPerformance.RemoveAt(0);
                }

                historicalPerformance = historicalPerformance.Where(c => c > 0).ToList<double>();

                double winRate =
                    historicalPerformance.Count >= minimumTrades
                        ? (double)historicalPerformance.Count(p => p > 0)
                            / historicalPerformance.Count
                        : defaultWinRate;

                if (
                    entry.entryConditions.ContainsKey(criterion.Key)
                    && entry.entryConditions[criterion.Key] == 1
                )
                {
                    weightedSum += winRate;
                    totalCount++;
                }
            }

            if (totalCount > 0)
            {
                double normalizedWeight = weightedSum / totalCount;
                return Math.Min(1, Math.Max(0, normalizedWeight));
            }
            else
            {
                return defaultWinRate;
            }
        }
        #endregion

        #region entryPatternMatched()
        private bool entryPatternMatched()
        {
            if (marketDirection.Direction[0] == TrendDirection.Flat)
            {
                return false;
            }

            if (entryEvaluator.matched[0] == 0)
            {
                return false;
            }

            if (legs.BarsAgoStarts[0] < 4)
            {
                return false;
            }

            if (legs.BarsAgoStarts[0] > 8)
            {
                return false;
            }

            return true;
        }
        #endregion

        #region UpdateTradeOutcomes()
        private void UpdateTradeOutcomes(bool isSuccessful)
        {
            tradeOutcomes.Add(isSuccessful);

            if (tradeOutcomes.Count > rollingWindowSize)
            {
                tradeOutcomes.RemoveAt(0);
            }
        }
        #endregion

        #region UpdateSuccessRates()
        private void UpdateSuccessRates()
        {
            successRate = CalculateSuccessRate();
            successRates.Add(successRate);

            if (successRates.Count > 10)
            {
                successRates.RemoveAt(0);
            }
        }
        #endregion

        #region CalculateSuccessRate()
        private double CalculateSuccessRate()
        {
            if (tradeOutcomes.Count == 0)
            {
                return 0.0;
            }

            int successfulTrades = tradeOutcomes.Count(outcome => outcome);

            return (double)successfulTrades / (double)tradeOutcomes.Count;
        }
        #endregion

        #region AdjustStrategy()
        private void AdjustStrategy()
        {
            double successRateStdDev = utils.StandardDeviation(successRates);
            double successRateAvg = successRates.Average();
            successRateThreshold = successRateAvg + successRateStdDev * 0.25;

			if (
                entryEvaluator.avgAtr[0] + 0.5 * entryEvaluator.stdDevAtr[0]
                < entryEvaluator.avgAtrFast[0]
            )
            {
                entryEvaluator.Window = Window * 0.8;
            }
            else if (
                entryEvaluator.avgAtr[0] - 0.5 * entryEvaluator.stdDevAtr[0]
                > entryEvaluator.avgAtrFast[0]
            )
            {
                entryEvaluator.Window = Window * 1.2;
            }
            else
            {
                entryEvaluator.Window = Window;
            }

			OptimizeWindowSize();
        }
        #endregion

		#region OptimizeWindowSize()
		private void OptimizeWindowSize()
		{
		    double currentATR = entryEvaluator.avgAtr[0];
		    double atrTolerance = 0.1 * currentATR;

		    List<WindowSizePerformance> relevantPerformanceData = windowSizePerformanceData
		        .Where(p => Math.Abs(p.ATR - currentATR) <= atrTolerance)
		        .ToList();

		    if (relevantPerformanceData.Count >= 15)
		    {
		        var groupedPerformanceData = relevantPerformanceData
		            .GroupBy(p => p.WindowSize)
		            .Select(g => new WindowSizePerformance
		            {
		                WindowSize = g.Key,
		                SuccessRate = g.Sum(p => p.SuccessRate) / g.Sum(p => p.Trades),
		                Trades = g.Sum(p => p.Trades)
		            })
		            .OrderByDescending(p => p.SuccessRate)
		            .ToList();

		        if (groupedPerformanceData.Count > 0)
		        {
		            WindowSizePerformance bestPerformance = groupedPerformanceData[0];
		            entryEvaluator.Window = bestPerformance.WindowSize;
					return;
		        }
		    }
		}
		#endregion

        #region Properties

        [NinjaScriptProperty]
        [Range(2, int.MaxValue)]
        [Display(Name = "Period", Description = "Period", Order = 0, GroupName = "Parameters")]
        public int Period { get; set; }

        [NinjaScriptProperty]
        [Range(int.MinValue, int.MaxValue)]
        [Display(
            Name = "Time Shift (Hours)",
            Description = "Time Shift",
            Order = 1,
            GroupName = "Parameters"
        )]
        public int TimeShift { get; set; }

        [NinjaScriptProperty]
        [Range(1, int.MaxValue)]
        [Display(Name = "Quantity", Description = "Quantity", Order = 2, GroupName = "Parameters")]
        public int Quantity { get; set; }

        [NinjaScriptProperty]
        [Range(1, 200)]
        [Display(Name = "Window", Description = "Window", Order = 3, GroupName = "Parameters")]
        public double Window { get; set; }

        //		[NinjaScriptProperty]
        //		[Display(Name="Export Trades", Description="Export Trades", Order=6, GroupName="Parameters")]
        //		public bool TradesExporterActivated
        //		{ get; set; }

        #endregion
    }

	public class WindowSizePerformance
	{
	    public double WindowSize { get; set; }
	    public double ATR { get; set; }
	    public double SuccessRate { get; set; }
	    public int Trades { get; set; }
	}
}
