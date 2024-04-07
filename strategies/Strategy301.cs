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
        private EntrySignal entry;
        private MarketPosition tradeDirection = MarketPosition.Flat;

        //		private TradesExporter tradesExporter;
        private double previousSuccessRate;
        private double successRate;
        private int rollingWindowSize = 50;
        private double successRateThreshold = 0.5;
		private double entryThreshold = 0.7;
		private double CurrentStopLossMultiplier = 1.5;
		private double CurrentStopLossLimitMultiplier = 5;
		private double CurrentTakeProfitMultiplier = 5;

        private ISet<int> tradesTracking = new HashSet<int>();
        private List<double> successRates = new List<double>();
        private List<bool> tradeOutcomes = new List<bool>();
		private List<PerformanceData> livePerformanceData = new List<PerformanceData>();
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
//                Window = 10;
            }
            #endregion

            #region State.Configure
            else if (State == State.Configure)
            {
                entryEvaluator = EntryEvaluator(Period, 10);
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
			UpdateEntryCriteria();
			UpdateExitCriteria();
            UpdatePerformanceData();

            AdjustStrategy();
        }
        #endregion

        #region shouldExit()
        private bool shouldExit()
        {
            if (Position.MarketPosition == MarketPosition.Flat)
            {
				return false;
			}

			double exitMatched = entryEvaluator.EvaluateExitCriteria(entry);
			double exitRating = evaluateExit();
			double exitThresholdModifier = Math.Round((exitMatched * 0.15) / 0.05, 0) * 0.05;
            double exitThreshold = 0.5 + exitThresholdModifier * (successRate > successRateThreshold ? -1 : 1);

			if (exitMatched <= 0) {
				return false;
			}

			if (exitRating > exitThreshold) {
				return true;
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

//			double entryThresholdModifier = Math.Round((entryEvaluator.matched[0] * 0.15) / 0.05, 0) * 0.05;
//            double entryThreshold = 0.5 + entryThresholdModifier * (successRate > successRateThreshold ? -1 : 1);
//			entryThreshold = Math.Min(entryThreshold, 0.9);

            if (entryRating < entryThreshold)
            {
                return;
            }

            int quantity = Math.Max(
                1,
                (int)Math.Round(((successRate + entryRating) / 2) * (double)Quantity, 0)
            );

			double maxStopLossDistancePoints = entryEvaluator.avgAtrFast[0] * CurrentStopLossLimitMultiplier;
			double maxStopLossDistanceTicks = maxStopLossDistancePoints * 4;
			double atrStopLossPoints = entryEvaluator.atr[0] * CurrentStopLossMultiplier;

			#region Bullish Entry
            if (marketDirection.Direction[0] == TrendDirection.Bullish)
            {
                double swingLow =
                    legs.BarsAgoStarts[0] > 0
                        ? Math.Min(MIN(Low, legs.BarsAgoStarts[0])[0], MIN(Low, 4)[0])
                        : Low[0];

				double barLow = Low[0];
		        double stopLossDistancePoints = Math.Min((Close[0] - swingLow), atrStopLossPoints);
		        double stopLossDistanceTicks = stopLossDistancePoints * 4;
				double stopLossPrice = Close[0] - stopLossDistancePoints;

				if (stopLossDistanceTicks > maxStopLossDistanceTicks)
		        {
		            return;
		        }

		        entry.StopLossUsed = stopLossDistanceTicks;
		        entry.ProfitTargetUsed = stopLossDistanceTicks * CurrentTakeProfitMultiplier;

                if (swingLow < barLow && stopLossPrice < barLow)
                {
					entry.StopLossMultiplier = CurrentStopLossMultiplier;
					entry.StopLossLimitMultiplier = CurrentStopLossLimitMultiplier;
                    SetStopLoss(CalculationMode.Ticks, stopLossDistanceTicks);
					SetProfitTarget(CalculationMode.Ticks, stopLossDistanceTicks * CurrentTakeProfitMultiplier);

                    EnterLong(quantity);
                }
            }
			#endregion

			#region Bearish Entry
            if (marketDirection.Direction[0] == TrendDirection.Bearish)
            {
                double swingHigh =
                    legs.BarsAgoStarts[0] > 0
                        ? Math.Max(MAX(High, legs.BarsAgoStarts[0])[0], MAX(High, 4)[0])
                        : High[0];
				double barHigh = High[0];
				double stopLossDistancePoints = Math.Min((swingHigh - Close[0]), atrStopLossPoints);
		        double stopLossDistanceTicks = stopLossDistancePoints * 4;
				double stopLossPrice = Close[0] + stopLossDistancePoints;

				if (stopLossDistanceTicks > maxStopLossDistanceTicks)
		        {
		            return;
		        }

                entry.StopLossUsed = stopLossDistanceTicks;
                entry.ProfitTargetUsed = stopLossDistanceTicks * CurrentTakeProfitMultiplier;

                if (swingHigh > barHigh && stopLossPrice > barHigh)
                {
					entry.StopLossMultiplier = CurrentStopLossMultiplier;
					entry.StopLossLimitMultiplier = CurrentStopLossLimitMultiplier;
                    SetStopLoss(CalculationMode.Ticks, stopLossDistanceTicks);
					SetProfitTarget(CalculationMode.Ticks, stopLossDistanceTicks * CurrentTakeProfitMultiplier);

                    EnterShort(quantity);
                }
            }
			#endregion
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

		#region evaluateExit()
        private double evaluateExit()
        {
            double weightedSum = 0;
            double totalCount = 0;
            double defaultWinRate = entryEvaluator.matched[0];
            int windowSize = 30;
            int minimumTrades = 5;

            foreach (var criterion in exitCriteria)
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
                    entry.exitConditions.ContainsKey(criterion.Key)
                    && entry.exitConditions[criterion.Key] == 1
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

		#region UpdateEntryCriteria()
		private void UpdateEntryCriteria()
		{
			foreach (var criterion in entryCriteria)
            {
                if (
                    entry.entryConditions.ContainsKey(criterion.Key)
                    && entry.entryConditions[criterion.Key] == 1
                )
                {
                    entryCriteria[criterion.Key].Add(entry.DistanceMoved);
                } else {
                	entryCriteria[criterion.Key].Add(0);
				}

				if (entryCriteria[criterion.Key].Count > rollingWindowSize)
				{
					entryCriteria[criterion.Key].RemoveAt(0);
				}
            }
		}
		#endregion

		#region UpdateExitCriteria()
		private void UpdateExitCriteria()
		{
			foreach (var criterion in exitCriteria)
            {
                if (
                    entry.exitConditions.ContainsKey(criterion.Key)
                    && entry.exitConditions[criterion.Key] == 1
                )
                {
                    exitCriteria[criterion.Key].Add(entry.DistanceMoved);
                } else {
					exitCriteria[criterion.Key].Add(0);
				}

				if (exitCriteria[criterion.Key].Count > rollingWindowSize)
				{
					exitCriteria[criterion.Key].RemoveAt(0);
				}
            }
		}
		#endregion

		#region UpdatePerformanceData()
		private void UpdatePerformanceData()
		{
			PerformanceData performance = new PerformanceData
		    {
		        WindowSize = entryEvaluator.Window,
		        ATR = entry.AvgAtr,
		        SuccessRate = entry.IsSuccessful ? 1.0 : 0.0,
		        Trades = 1,
				StopLossMultiplier = entry.StopLossMultiplier,
				StopLossLimitMultiplier = entry.StopLossLimitMultiplier,
				TakeProfitMultiplier = entry.TakeProfitMultiplier,
				EntryThreshold = entryThreshold
		    };

		    livePerformanceData.Add(performance);

			if (livePerformanceData.Count > rollingWindowSize)
			{
				livePerformanceData.RemoveAt(0);
			}
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
			double adjustedAverage = successRateAvg + successRateStdDev * 0.25;
            successRateThreshold = Double.IsNaN(adjustedAverage) ? successRateThreshold : adjustedAverage;

//			OptimizeWindowSize();
//			OptimizeStopLossMultiplier();
//			OptimizeStopLossLimitMultiplier();
//			OptimizeTakeProfitMultiplier();
//			OptimizeEntryThreshold();


			OptimizeParameters();
			entryEvaluator.UpdateStopLossMultiplier(CurrentStopLossMultiplier);
			entryEvaluator.UpdateTakeProfitMultiplier(CurrentTakeProfitMultiplier);
        }
        #endregion

		#region OptimizeWindowSize()
		private void OptimizeWindowSize()
		{
		    double currentATR = entryEvaluator.avgAtr[0];
		    double atrTolerance = 0.1 * currentATR;
			double minWindowSize = 20;
		    double maxWindowSize = 50;
		    double stepSize = 5;
    		double comparisonTolerance = 0.001;

			List<double> windowSizes = GenerateValues(minWindowSize, maxWindowSize, stepSize);

		    List<PerformanceData> relevantPerformanceData = livePerformanceData
		        .Where(p => Math.Abs(p.ATR - currentATR) <= atrTolerance)
		        .ToList();

		    if (relevantPerformanceData.Count >= 10)
		    {
		        var groupedPerformanceData = relevantPerformanceData
					.Where(p => windowSizes.Any(t => Math.Abs(p.WindowSize - t) <= comparisonTolerance))
            		.GroupBy(p => windowSizes.First(t => Math.Abs(p.WindowSize - t) <= comparisonTolerance))
		            .Select(g => new PerformanceData
		            {
		                WindowSize = g.Key,
		                SuccessRate = g.Sum(p => p.SuccessRate) / g.Sum(p => p.Trades),
		                Trades = g.Sum(p => p.Trades)
		            })
		            .OrderByDescending(p => p.SuccessRate)
		            .ToList();

		        if (groupedPerformanceData.Count > 0)
		        {
		            PerformanceData bestPerformance = groupedPerformanceData[0];
		            entryEvaluator.Window = bestPerformance.WindowSize;
		        }
		    }
			else
		    {
		        entryEvaluator.Window = CalculateATRBasedValue(currentATR, minWindowSize, maxWindowSize, stepSize);
		    }
		}
		#endregion

		#region OptimizeStopLossMultiplier()
		private void OptimizeStopLossMultiplier()
		{
		    double currentATR = entryEvaluator.avgAtr[0];
		    double atrTolerance = 0.1 * currentATR;
			double minMultiplier = 0.5;
    		double maxMultiplier = 5;
    		double stepSize = 0.5;

			List<double> multipliers = GenerateValues(minMultiplier, maxMultiplier, stepSize);

		    List<PerformanceData> relevantPerformanceData = livePerformanceData
		        .Where(p => Math.Abs(p.ATR - currentATR) <= atrTolerance)
		        .ToList();

		    if (relevantPerformanceData.Count >= 10)
		    {
		        var groupedPerformanceData = relevantPerformanceData
            		.Where(p => multipliers.Contains(p.StopLossMultiplier))
		            .GroupBy(p => p.StopLossMultiplier)
		            .Select(g => new PerformanceData
		            {
		                StopLossMultiplier = g.Key,
		                SuccessRate = g.Sum(p => p.SuccessRate) / g.Sum(p => p.Trades),
		                Trades = g.Sum(p => p.Trades)
		            })
		            .OrderByDescending(p => p.SuccessRate)
		            .ToList();

		        if (groupedPerformanceData.Count > 0)
		        {
		            PerformanceData bestPerformance = groupedPerformanceData[0];
		            CurrentStopLossMultiplier = bestPerformance.StopLossMultiplier;
		        }
		    }
			else
		    {
		        CurrentStopLossMultiplier = CalculateATRBasedValue(currentATR, minMultiplier, maxMultiplier, stepSize);
		    }

			entryEvaluator.UpdateStopLossMultiplier(CurrentStopLossMultiplier);
		}
		#endregion

		#region OptimizeStopLossLimitMultiplier()
		private void OptimizeStopLossLimitMultiplier()
		{
		    double currentATR = entryEvaluator.avgAtr[0];
		    double atrTolerance = 0.1 * currentATR;
			double minMultiplier = 1;
    		double maxMultiplier = 10;
    		double stepSize = 1;
    		double comparisonTolerance = 0.001;

			List<double> multipliers = GenerateValues(minMultiplier, maxMultiplier, stepSize);

		    List<PerformanceData> relevantPerformanceData = livePerformanceData
		        .Where(p => Math.Abs(p.ATR - currentATR) <= atrTolerance)
		        .ToList();

		    if (relevantPerformanceData.Count >= 10)
		    {
		        var groupedPerformanceData = relevantPerformanceData
					.Where(p => multipliers.Any(t => Math.Abs(p.StopLossLimitMultiplier - t) <= comparisonTolerance))
            		.GroupBy(p => multipliers.First(t => Math.Abs(p.StopLossLimitMultiplier - t) <= comparisonTolerance))
		            .Select(g => new PerformanceData
		            {
		                StopLossLimitMultiplier = g.Key,
		                SuccessRate = g.Sum(p => p.SuccessRate) / g.Sum(p => p.Trades),
		                Trades = g.Sum(p => p.Trades)
		            })
		            .OrderByDescending(p => p.SuccessRate)
		            .ToList();

		        if (groupedPerformanceData.Count > 0)
		        {
		            PerformanceData bestPerformance = groupedPerformanceData[0];
		            CurrentStopLossLimitMultiplier = bestPerformance.StopLossLimitMultiplier;
		        }
		    }
			else
		    {
		        CurrentStopLossLimitMultiplier = CalculateATRBasedValue(currentATR, minMultiplier, maxMultiplier, stepSize);
		    }
		}
		#endregion

		#region OptimizeTakeProfitMultiplier()
		private void OptimizeTakeProfitMultiplier()
		{
		    double currentATR = entryEvaluator.avgAtr[0];
		    double atrTolerance = 0.1 * currentATR;
			double minMultiplier = 1;
		    double maxMultiplier = 10;
		    double stepSize = 1;
    		double comparisonTolerance = 0.001;

			List<double> multipliers = GenerateValues(minMultiplier, maxMultiplier, stepSize);

		    List<PerformanceData> relevantPerformanceData = livePerformanceData
		        .Where(p => Math.Abs(p.ATR - currentATR) <= atrTolerance)
		        .ToList();

		    if (relevantPerformanceData.Count >= 10)
		    {
		        var groupedPerformanceData = relevantPerformanceData
					.Where(p => multipliers.Any(t => Math.Abs(p.TakeProfitMultiplier - t) <= comparisonTolerance))
            		.GroupBy(p => multipliers.First(t => Math.Abs(p.TakeProfitMultiplier - t) <= comparisonTolerance))
		            .Select(g => new PerformanceData
		            {
		                TakeProfitMultiplier = g.Key,
		                SuccessRate = g.Sum(p => p.SuccessRate) / g.Sum(p => p.Trades),
		                Trades = g.Sum(p => p.Trades)
		            })
		            .OrderByDescending(p => p.SuccessRate)
		            .ToList();

		        if (groupedPerformanceData.Count > 0)
		        {
		            PerformanceData bestPerformance = groupedPerformanceData[0];
		            CurrentTakeProfitMultiplier = bestPerformance.TakeProfitMultiplier;
		        }
		    }
			else
		    {
		        CurrentTakeProfitMultiplier = CalculateATRBasedValue(currentATR, minMultiplier, maxMultiplier, stepSize);
		    }

			entryEvaluator.UpdateTakeProfitMultiplier(CurrentTakeProfitMultiplier);
		}
		#endregion

		#region OptimizeEntryThreshold()
		private void OptimizeEntryThreshold()
		{
		    double currentATR = entryEvaluator.avgAtr[0];
		    double atrTolerance = 0.1 * currentATR;
			double minThreshold = 0.4;
		    double maxThreshold = 1;
		    double stepSize = 0.05;
    		double comparisonTolerance = 0.001;

			List<double> thresholds = GenerateValues(minThreshold, maxThreshold, stepSize);

		    List<PerformanceData> relevantPerformanceData = livePerformanceData
		        .Where(p => Math.Abs(p.ATR - currentATR) <= atrTolerance)
		        .ToList();

		    if (relevantPerformanceData.Count >= 10)
		    {
		        var groupedPerformanceData = relevantPerformanceData
					.Where(p => thresholds.Any(t => Math.Abs(p.EntryThreshold - t) <= comparisonTolerance))
            		.GroupBy(p => thresholds.First(t => Math.Abs(p.EntryThreshold - t) <= comparisonTolerance))
		            .Select(g => new PerformanceData
		            {
		                EntryThreshold = g.Key,
		                SuccessRate = g.Sum(p => p.SuccessRate) / g.Sum(p => p.Trades),
		                Trades = g.Sum(p => p.Trades)
		            })
		            .OrderByDescending(p => p.SuccessRate)
		            .ToList();

		        if (groupedPerformanceData.Count > 0)
		        {
		            PerformanceData bestPerformance = groupedPerformanceData[0];
		            entryThreshold = bestPerformance.EntryThreshold;
		        }
		    }
			else
		    {
		        entryThreshold = CalculateATRBasedValue(currentATR, minThreshold, maxThreshold, stepSize, true);
		    }
		}
		#endregion

		#region OptimizeParameters()
		private void OptimizeParameters()
		{
		    double[] lowerBounds = { 8, 0.5, 1, 1, 0.5 }; // Lower bounds for window size, stop loss multiplier, stop loss limit multiplier, take profit multiplier, and entry threshold
		    double[] upperBounds = { 12, 3.0, 4, 10, 0.9 }; // Upper bounds for window size, stop loss multiplier, stop loss limit multiplier, take profit multiplier, and entry threshold
		    int numParticles = 20;
		    int maxIterations = 50;

		    Func<double[], double> fitnessFunction = CalculateFitness;

		    double[] bestPosition = ParticleSwarmOptimization.Optimize(fitnessFunction, lowerBounds, upperBounds, numParticles, maxIterations);

//			Print("Window: " + bestPosition[0] + ", SL: " + bestPosition[1] + ", SLL: " + bestPosition[2] + ", TP: " + bestPosition[3] + ", ET: " + bestPosition[4]);

		    entryEvaluator.Window = bestPosition[0];
		    CurrentStopLossMultiplier = bestPosition[1];
		    CurrentStopLossLimitMultiplier = bestPosition[2];
		    CurrentTakeProfitMultiplier = bestPosition[3];
		    entryThreshold = bestPosition[4];
		}
		#endregion

		#region CalculateFitness()
		private double CalculateFitness(double[] position)
		{
		    double windowSize = position[0];
		    double stopLossMultiplier = position[1];
		    double stopLossLimitMultiplier = position[2];
		    double takeProfitMultiplier = position[3];
		    double entryThreshold = position[4];

		    // Calculate the fitness based on historical performance data
		    // You can use the livePerformanceData list to evaluate the performance
		    // of the strategy with the given parameter values
		    // Return a fitness score (e.g., success rate, profit factor, Sharpe ratio)

		    // Example fitness calculation based on success rate
		    int totalTrades = livePerformanceData.Count;
		    int successfulTrades = livePerformanceData.Count(p =>
		        p.WindowSize == windowSize &&
		        p.StopLossMultiplier == stopLossMultiplier &&
		        p.StopLossLimitMultiplier == stopLossLimitMultiplier &&
		        p.TakeProfitMultiplier == takeProfitMultiplier &&
		        p.EntryThreshold == entryThreshold &&
		        p.SuccessRate == 1.0);

		    double successRate = (double)successfulTrades / totalTrades;
		    return successRate;
		}
		#endregion

		#region GenerateValues()
		private List<double> GenerateValues(double minValue, double maxValue, double stepSize)
		{
		    List<double> values = new List<double>();
		    for (double value = minValue; value <= maxValue; value += stepSize)
		    {
		        values.Add(value);
		    }
		    return values;
		}
		#endregion

		#region CalculateATRBasedValue()
		private double CalculateATRBasedValue(double currentATR, double minValue, double maxValue, double stepSize, bool negate = false)
		{
		    double normalizedATR = (currentATR - entryEvaluator.minATR[0]) / (entryEvaluator.maxATR[0] - entryEvaluator.minATR[0]);
		    int stepCount = (int)((maxValue - minValue) / stepSize);
		    int selectedStep = (int)(normalizedATR * stepCount);

			if (negate) {
				return maxValue - (selectedStep * stepSize);
			}

		    return minValue + (selectedStep * stepSize);
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

//        [NinjaScriptProperty]
//        [Range(1, 200)]
//        [Display(Name = "Window", Description = "Window", Order = 3, GroupName = "Parameters")]
//        public double Window { get; set; }

//        [NinjaScriptProperty]
//        [Range(0, 200)]
//        [Display(Name = "SL", Description = "SL", Order = 3, GroupName = "Parameters")]
//        public double SL { get; set; }

//        [NinjaScriptProperty]
//        [Range(0, 200)]
//        [Display(Name = "SLLimit", Description = "SLLimit", Order = 3, GroupName = "Parameters")]
//        public double SLLimit { get; set; }

        //		[NinjaScriptProperty]
        //		[Display(Name="Export Trades", Description="Export Trades", Order=6, GroupName="Parameters")]
        //		public bool TradesExporterActivated
        //		{ get; set; }

        #endregion
    }

	public class PerformanceData
	{
	    public double WindowSize { get; set; }
	    public double ATR { get; set; }
	    public double SuccessRate { get; set; }
	    public int Trades { get; set; }
		public double StopLossMultiplier { get; set; }
		public double StopLossLimitMultiplier { get; set; }
		public double TakeProfitMultiplier { get; set; }
		public double EntryThreshold { get; set; }
	}
}
