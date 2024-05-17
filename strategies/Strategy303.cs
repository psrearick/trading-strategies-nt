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
	public class Strategy303 : Strategy
	{
		#region Variables
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

		private int quantity;

		private bool longPatternMatched;
		private bool shortPatternMatched;

		Func<int, bool>[] longCombination = null;
		Func<int, bool>[] shortCombination = null;

		private Utils utils = new Utils();

		private EMA emaShort;
		private EMA emaLong;
		private ATR atr;
		private ATR atrDaily;
		private ChoppinessIndex chop;
		public RSI rsi;

		private double ProfitTarget = 3;
		private double StopLossTarget = 5;
		private double HighATRMultiplier = 3;

		private int lastTradeCount = 0;
		private int lastBarOptimized = 0;
		private double recentPerformance = 0;
		private double overallPerformance = 0;
		private int optimizationPeriod = 80;
		private int lookbackPeriod = 80;
		private int performanceTrackingPeriod = 21;

		private Queue<double> recentPerformanceScores = new Queue<double>();
		private Queue<double> sharpeRatios = new Queue<double>();
		private Queue<double> drawdownRatios = new Queue<double>();

		private DateTime initTime = DateTime.Now;
		private DateTime start = DateTime.Now;
		private int cooldownPeriod = 2;
		private int cooldownCounter = 0;

		#endregion

		#region OnStateChange()
		protected override void OnStateChange()
		{
			#region SetDefaults
			if (State == State.SetDefaults)
			{
				Description										= @"Trend Following Price Action Strategy";
				Name											= "Strategy 3.0.3";
				Calculate										= Calculate.OnBarClose;
				EntriesPerDirection								= 1;
				EntryHandling									= EntryHandling.AllEntries;
				IsExitOnSessionCloseStrategy					= true;
				ExitOnSessionCloseSeconds						= 30;
				IsFillLimitOnTouch								= false;
				MaximumBarsLookBack								= MaximumBarsLookBack.TwoHundredFiftySix;
				OrderFillResolution								= OrderFillResolution.Standard;
				Slippage										= 0;
				StartBehavior									= StartBehavior.WaitUntilFlat;
				TimeInForce										= TimeInForce.Day;
				TraceOrders										= false;
				RealtimeErrorHandling							= RealtimeErrorHandling.StopCancelCloseIgnoreRejects;
				StopTargetHandling								= StopTargetHandling.PerEntryExecution;
				BarsRequiredToTrade								= 81;

				IncludeTradeHistoryInBacktest					= true;
				IsInstantiatedOnEachOptimizationIteration		= false;
				IsUnmanaged										= false;

				Risk											= 0;
				TradeQuantity									= 1;
				PerformanceThreshold 							= 0.5;
				ConditionCount									= 3;
			}
			#endregion

			#region Configure
			else if (State == State.Configure)
			{
				AddDataSeries(Data.BarsPeriodType.Second, 15);
				AddDataSeries(Data.BarsPeriodType.Minute, 30);
			}
			#endregion

			#region DataLoaded
			if (State == State.DataLoaded) {
				emaShort			= EMA(9);
				emaLong				= EMA(21);
				atr 				= ATR(14);
				atrDaily			= ATR(BarsArray[2], 14);
				rsi					= RSI(14, 3);
				chop				= ChoppinessIndex(14);
			}
			#endregion
		}
		#endregion

		#region OnBarUpdate()
		protected override void OnBarUpdate()
		{
			if (CurrentBar < BarsRequiredToTrade || CurrentBars[0] < 1 || CurrentBars[1] < 1 || CurrentBars[2] < 1)
			{
				return;
            }

			exitPositions();

			if (BarsInProgress != 0)
			{
				return;
			}


			if (lastBarOptimized == 0)
			{
				OptimizeStrategy();
			} else if (CurrentBar % optimizationPeriod == 0)
		    {
				Print(CurrentBar + " " + Time[0].ToString() + " ==================== " + (DateTime.Now - start).TotalSeconds + " -- " + (DateTime.Now - initTime).TotalSeconds);
				start = DateTime.Now;

				OptimizeStrategy();
				AdjustDynamicParameters();
		    }

			if (SystemPerformance.AllTrades.Count > 0 && SystemPerformance.AllTrades.Count != recentPerformanceScores.Count && lastTradeCount < SystemPerformance.AllTrades.Count)
		    {
		        double currentPerformance = CalculateRecentPerformance();
		        UpdateOverallPerformance(currentPerformance);

//				Print("Current Performance: " + currentPerformance + " Overall Performance: " + overallPerformance + " Threshold: " + PerformanceThreshold);

				if (currentPerformance < PerformanceThreshold || overallPerformance < PerformanceThreshold)
		        {
					OptimizeStrategy();
		        }

				lastTradeCount = SystemPerformance.AllTrades.Count;
		    }

			calculateQuantity();
			setEntries();
		}
		#endregion

		#region CalculateRecentPerformance()
		private double CalculateRecentPerformance()
		{
		    int numTrades = SystemPerformance.AllTrades.Count;
		    int startIndex = Math.Max(0, numTrades - performanceTrackingPeriod);
		    int endIndex = numTrades - 1;

		    double netProfit = 0;
		    double maxDrawdown = 0;
		    double peakProfit = 0;
		    double currentDrawdown = 0;
		    List<double> profitLosses = new List<double>();

		    for (int i = startIndex; i <= endIndex; i++)
		    {
		        Trade trade = SystemPerformance.AllTrades[i];
		        double profitLoss = trade.ProfitCurrency;
		        netProfit += profitLoss;
		        profitLosses.Add(profitLoss);

		        if (netProfit > peakProfit)
		        {
		            peakProfit = netProfit;
		            currentDrawdown = 0;
		        }
		        else
		        {
		            currentDrawdown = peakProfit - netProfit;
		            if (currentDrawdown > maxDrawdown)
		            {
		                maxDrawdown = currentDrawdown;
		            }
		        }
		    }

		    double averageProfit = netProfit / (endIndex - startIndex + 1);
		    double profitStdDev = profitLosses.Count > 1 ? CalculateStandardDeviation(profitLosses) : 0;

		    double performanceScore = CalculatePerformanceScore(averageProfit, maxDrawdown, profitStdDev);

		    return performanceScore;
		}
		#endregion

		#region CalculateStandardDeviation()
		private double CalculateStandardDeviation(List<double> values)
		{
			if (values.Count <= 1)
			{
				return 0;
			}

		    double average = values.Average();
		    double sumOfSquares = values.Sum(x => Math.Pow(x - average, 2));
		    double variance = sumOfSquares / (values.Count - 1);
		    double stdDev = Math.Sqrt(variance);

		    return stdDev;
		}
		#endregion

		#region CalculatePerformanceScore()
		private double CalculatePerformanceScore(double averageProfit, double maxDrawdown, double profitStdDev)
		{
			double sharpeRatio = profitStdDev != 0 ? averageProfit / profitStdDev : 0;
    		double drawdownRatio = averageProfit != 0 ? maxDrawdown / Math.Abs(averageProfit) : 1;  // Set to 1 if averageProfit is zero to avoid division by zero

		    // Save the historical values
    		sharpeRatios.Enqueue(sharpeRatio);
    		drawdownRatios.Enqueue(drawdownRatio);

			if (sharpeRatios.Count > lookbackPeriod) sharpeRatios.Dequeue();
   			if (drawdownRatios.Count > lookbackPeriod) drawdownRatios.Dequeue();

			// Normalize using dynamic historical range
		    double normalizedSharpeRatio = NormalizeValue(sharpeRatio, sharpeRatios);
		    double normalizedDrawdownRatio = NormalizeValue(drawdownRatio, drawdownRatios);

		    // Calculate the performance score as a weighted average
		    double sharpeWeight = 0.7;
		    double drawdownWeight = 0.3;
		    double performanceScore = (sharpeWeight * normalizedSharpeRatio) + (drawdownWeight * (1 - normalizedDrawdownRatio));

		    return performanceScore;
		}
		#endregion

		#region NormalizeValue()
		private double NormalizeValue(double value, Queue<double> historicalValues)
		{
		    double min = historicalValues.Min();
		    double max = historicalValues.Max();
		    // Ensure division by zero doesn't occur
		    if (max == min) return 1;
		    return (value - min) / (max - min);
		}
		#endregion

		#region UpdateOverallPerformance()
		private void UpdateOverallPerformance(double currentPerformance)
		{
		    recentPerformanceScores.Enqueue(currentPerformance);

		    if (recentPerformanceScores.Count > lookbackPeriod)
		    {
		        recentPerformanceScores.Dequeue();
		    }

		    overallPerformance = recentPerformanceScores.Average();
		}
		#endregion

		#region AdjustDynamicParameters()
		private void AdjustDynamicParameters()
		{
			if (recentPerformanceScores.Count < 2)
			{
				return;
			}

		    if (cooldownCounter > 0)
		    {
		        cooldownCounter--;
		        return;
		    }

		    double recentAveragePerformance = recentPerformanceScores.Average();
		    double performanceStdDev = CalculateStandardDeviation(recentPerformanceScores.ToList());
			double performanceThresholdAdjustment = (recentAveragePerformance - PerformanceThreshold) * 0.2;
			double conditionCountAdjustment = (performanceStdDev - 0.15) > 0 ? 1 : -1;

			PerformanceThreshold = Math.Max(0.1, Math.Min(0.8, PerformanceThreshold + performanceThresholdAdjustment));
   			ConditionCount = Math.Max(1, Math.Min(3, ConditionCount + (int)conditionCountAdjustment));

		    // Reset the cooldown counter
		    cooldownCounter = cooldownPeriod;

			Print("PerformanceThreshold: " + PerformanceThreshold + "; ConditionCount: " + ConditionCount);

//			Print("PerformanceThreshold: " + PerformanceThreshold + "-" + recentAveragePerformance + "; ConditionCount: " + ConditionCount + "-" + performanceStdDev);
		}
		#endregion

		#region OptimizeStrategy()
		private void OptimizeStrategy()
		{
		    // Call the entry criteria optimization function
		    OptimizeEntryCriteria();

		    // Call the exit conditions optimization function
		    OptimizeExitConditions();

		    // Reset the performance tracking variables
		    recentPerformance = 0;
		    overallPerformance = 0;
			lastBarOptimized = CurrentBar;
		}
		#endregion

		#region calculateQuantity()
		private void calculateQuantity()
		{
			quantity = TradeQuantity;

			if (Risk > 0) {
				double stopLossDistance	= Math.Round((atrDaily[0] * StopLossTarget) / 4);
				quantity = (int) Math.Floor(Math.Max(quantity, Risk / (stopLossDistance * 50)));
			}
		}
		#endregion

		#region exitPositions
		private void exitPositions() {
			if (isValidTradeTime()) {
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

		#region OptimizeEntryCriteria()
		private void OptimizeEntryCriteria()
		{
		    // Define the list of long entry criteria
		    List<Func<int, bool>> longEntryCriteria = new List<Func<int, bool>>
		    {
//		        (int i) => emaShort[0 + i] > emaShort[1 + i] && emaLong[0 + i] > emaLong[1 + i],  // maRising
		        (int i) => Close[0 + i] > Close[1 + i] && Close[1 + i] > Close[2 + i] && Close[2 + i] > Close[3 + i],  // rising
//		        (int i) => Close[0 + i] > MAX(High, 10)[1 + i],  // newHigh
		        (int i) => chop[0 + i] < 38.2 || chop[0 + i] > 61.8,  // validChoppiness
				(int i) => rsi[i] < 70 && rsi[i] > 50,  // validRSI
		        (int i) => MAX(High, 3)[0 + i] >= MAX(High, 10)[0 + i] && High[0 + i] > High[1 + i] && High[1 + i] > High[2 + i] && High[2 + i] > High[3 + i]  // upTrend
		    };

		    // Define the list of short entry criteria
		    List<Func<int, bool>> shortEntryCriteria = new List<Func<int, bool>>
		    {
//		        (int i) => emaShort[0 + i] < emaShort[1 + i] && emaLong[0 + i] < emaLong[1 + i],  // maFalling
		        (int i) => Close[0 + i] < Close[1 + i] && Close[1 + i] < Close[2 + i] && Close[2 + i] < Close[3 + i],  // falling
//		        (int i) => Close[0 + i] < MIN(Low, 10)[1 + i],  // newLow
		        (int i) => chop[0 + i] < 38.2 || chop[0 + i] > 61.8,  // validChoppiness
				(int i) => rsi[i] < 50 && rsi[i] > 30,  // validRSI
		        (int i) => MIN(Low, 3)[0 + i] <= MIN(Low, 10)[0 + i] && Low[0 + i] < Low[1 + i] && Low[1 + i] < Low[2 + i] && Low[2 + i] < Low[3 + i]  // downTrend
		    };

		    // Generate all possible combinations of 3 long entry criteria
		    var longCombinations = GenerateCombinations(longEntryCriteria, ConditionCount);

		    // Generate all possible combinations of 3 short entry criteria
		    var shortCombinations = GenerateCombinations(shortEntryCriteria, ConditionCount);

		    // Evaluate the performance of each long combination over the past N bars
		    double bestLongPerformance = double.MinValue;
		    Func<int, bool>[] bestLongCombination = null;

		    foreach (var combination in longCombinations)
		    {
		        double performance = EvaluatePerformance(combination, true);
		        if (performance > bestLongPerformance)
		        {
		            bestLongPerformance = performance;
		            bestLongCombination = combination;
		        }
		    }

		    // Evaluate the performance of each short combination over the past N bars
		    double bestShortPerformance = double.MinValue;
		    Func<int, bool>[] bestShortCombination = null;

		    foreach (var combination in shortCombinations)
		    {
		        double performance = EvaluatePerformance(combination, false);
		        if (performance > bestShortPerformance)
		        {
		            bestShortPerformance = performance;
		            bestShortCombination = combination;
		        }
		    }

		    shortCombination = bestShortCombination;
			longCombination = bestLongCombination;
		}
		#endregion

		#region GenerateCombinations()
		private IEnumerable<Func<int, bool>[]> GenerateCombinations(List<Func<int, bool>> criteria, int combinationSize)
		{
		    if (combinationSize == 1)
		    {
		        foreach (var item in criteria)
		        {
		            yield return new Func<int, bool>[] { item };
		        }
		    }
		    else
		    {
		        for (int i = 0; i < criteria.Count - combinationSize + 1; i++)
		        {
		            var head = criteria[i];
		            var tail = criteria.Skip(i + 1).ToList();
		            foreach (var subCombination in GenerateCombinations(tail, combinationSize - 1))
		            {
		                yield return new Func<int, bool>[] { head }.Concat(subCombination).ToArray();
		            }
		        }
		    }
		}
		#endregion

		#region EvaluatePerformance()
		private double EvaluatePerformance(Func<int, bool>[] combination, bool isLong)
		{
		    double totalProfit = 0;
		    int totalTrades = 0;

		    // Iterate over the past N bars
		    for (int i = lookbackPeriod - 3; i > 0; i--)
		    {
		        // Check if the current combination matches the entry criteria for the current bar
				bool entryMatched = ConditionsMet(combination, i);

		        // Simulate the trade if the entry criteria are met
		        if (entryMatched)
		        {
		            double entryPrice = Close[i];
		            double profitTarget = (entryPrice + (atrDaily[0] * ProfitTarget * (isLong ? 1 : -1)));
		            double stopLoss = (entryPrice - (atrDaily[0] * StopLossTarget * (isLong ? 1 : -1)));

		            // Iterate over the subsequent bars to find the exit point
		            for (int j = i - 1; j > 0; j--)
		            {
		                double currentPrice = Close[j];

		                // Check if the profit target or stop loss is hit
		                if ((isLong && currentPrice >= profitTarget) || (!isLong && currentPrice <= profitTarget))
		                {
		                    // Calculate the profit for the trade
		                    double profit = isLong ? profitTarget - entryPrice : entryPrice - profitTarget;
		                    totalProfit += profit;
		                    totalTrades++;
		                    break;
		                }
		                else if ((isLong && currentPrice <= stopLoss) || (!isLong && currentPrice >= stopLoss))
		                {
		                    // Calculate the loss for the trade
		                    double loss = isLong ? stopLoss - entryPrice : entryPrice - stopLoss;
		                    totalProfit += loss;
		                    totalTrades++;
		                    break;
		                }
		            }
		        }
		    }

		    // Calculate and return the average profit per trade
		    if (totalTrades > 0)
		    {
		        return totalProfit / totalTrades;
		    }
		    else
		    {
		        return 0;
		    }
		}
		#endregion

		#region  OptimizeExitConditions()
		private void OptimizeExitConditions()
		{
		    // Define the range of values for ProfitTarget, StopLossTarget, and HighATRMultiplier
		    double[] profitTargetValues = { 1.0, 1.5, 2.0, 2.5, 3.0, 3.5, 4.0, 4.5, 5.0, 5.5, 6.0 };
		    double[] stopLossTargetValues = { 1.0, 1.5, 2.0, 2.5, 3.0, 3.5, 4.0, 4.5, 5.0, 5.5, 6.0 };
		    double[] highATRMultiplierValues = { 1.0, 1.5, 2.0, 2.5, 3.0, 3.5, 4.0, 4.5, 5.0 };


		    // Initialize variables to store the best performance and parameters
		    double bestPerformance = double.MinValue;
		    double bestProfitTarget = profitTargetValues[0];
		    double bestStopLossTarget = stopLossTargetValues[0];
		    double bestHighATRMultiplier = highATRMultiplierValues[0];

		    // Iterate over all possible combinations of exit parameters
		    foreach (double profitTarget in profitTargetValues)
		    {
		        foreach (double stopLossTarget in stopLossTargetValues)
		        {
		            foreach (double highATRMultiplier in highATRMultiplierValues)
		            {
		                // Evaluate the performance of the current combination
		                double performance = EvaluateExitPerformance(profitTarget, stopLossTarget, highATRMultiplier);

		                // Check if the current combination performs better than the best one so far
		                if (performance > bestPerformance)
		                {
		                    bestPerformance = performance;
		                    bestProfitTarget = profitTarget;
		                    bestStopLossTarget = stopLossTarget;
		                    bestHighATRMultiplier = highATRMultiplier;
		                }
		            }
		        }
		    }

		    // Update the exit parameters with the best-performing values
		    ProfitTarget = bestProfitTarget;
		    StopLossTarget = bestStopLossTarget;
		    HighATRMultiplier = bestHighATRMultiplier;
		}
		#endregion

		#region EvaluateExitPerformance()
		private double EvaluateExitPerformance(double profitTargetMultiplier, double stopLossTargetMultiplier, double highATRMultiplier)
		{
		    double totalProfit = 0;
		    int totalTrades = 0;

		    // Iterate over the past N bars
		    for (int i = lookbackPeriod - 3; i > 0; i--)
		    {
		        bool entryMatched = false;
		        bool isLong = false;

		        // Check if the current combination matches the entry criteria for the current bar
				if (ConditionsMet(longCombination, i))
		        {
		            entryMatched = true;
		            isLong = true;
		        }

				if (ConditionsMet(shortCombination, i))
		        {
		            entryMatched = true;
		        }

		        // Simulate the trade if the entry criteria are met
		        if (entryMatched)
		        {
		            double entryPrice = Close[i];

		            // Use the current bar's atrDaily value if the historical bar is out of range
		            int atrIndex = Math.Min(i, atrDaily.CurrentBar - 1);
		            double atrDailyValue = atrDaily[atrIndex];

		            double profitTarget = entryPrice + ((atrDailyValue * profitTargetMultiplier * (isLong ? 1 : -1)));
		            double stopLoss = entryPrice - ((atrDailyValue * stopLossTargetMultiplier * (isLong ? 1 : -1)));

		            // Check if the high ATR multiplier condition is met
		            if (atr[i] > (atrDailyValue * 0.5))
		            {
		                profitTarget = entryPrice + (atrDailyValue * profitTargetMultiplier * highATRMultiplier * (isLong ? 1 : -1));
		            }

		            bool tradeExited = false;
		            double tradeProfit = 0;

		            // Iterate over the subsequent bars to find the exit point
		            for (int j = i - 1; j > 0 && !tradeExited; j--)
		            {
		                double currentPrice = Close[j];

		                // Check if the profit target or stop loss is hit
		                if ((isLong && currentPrice >= profitTarget) || (!isLong && currentPrice <= profitTarget))
		                {
		                    // Calculate the profit for the trade
		                    tradeProfit = isLong ? profitTarget - entryPrice : entryPrice - profitTarget;
		                    tradeExited = true;
		                }
		                else if ((isLong && currentPrice <= stopLoss) || (!isLong && currentPrice >= stopLoss))
		                {
		                    // Calculate the loss for the trade
		                    tradeProfit = isLong ? stopLoss - entryPrice : entryPrice - stopLoss;
		                    tradeExited = true;
		                }
		            }

		            // Update the total profit and trade count if the trade was exited
		            if (tradeExited)
		            {
		                totalProfit += tradeProfit;
		                totalTrades++;
		            }
		        }
		    }

		    // Calculate and return the weighted average of profit per trade and number of trades
		    if (totalTrades > 0)
		    {
		        double avgProfit = totalProfit / totalTrades;
		        double weightedScore = (avgProfit * 0.7) + (totalTrades * 0.3);
		        return weightedScore;
		    }
		    else
		    {
		        return double.MinValue;
		    }
		}
		#endregion

		#region ConditionsMet()
		private bool ConditionsMet(Func<int, bool>[] conditions, int i = 0)
		{
			foreach (var condition in conditions)
			{
				if (!condition.Invoke(i)) {
					return false;
				}
			}

			return true;
		}
		#endregion

		#region isValidEntryTime()
		private bool isValidEntryTime()
		{
			int now = ToTime(Time[0]);

			if (now < ToTime(OpenTime)) {
				return false;
			}

			if (now > ToTime(LastTradeTime)) {
				return false;
			}

			return true;
		}
		#endregion

		#region isValidTradeTime()
		private bool isValidTradeTime()
		{
			int now = ToTime(Time[0]);

			if (now > ToTime(CloseTime)) {
				return false;
			}

			if (now < ToTime(OpenTime)) {
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

			if (quantity == 0) {
				return;
			}

			double profitDistance			= atrDaily[0] * ProfitTarget;
			double stopLossDistance			= atrDaily[0] * StopLossTarget;

			if (atr[0] > (atrDaily[0] * 0.5))
			{
				profitDistance = profitDistance * HighATRMultiplier;
			}

			double TickValue = Instrument.MasterInstrument.PointValue * TickSize;
			double TicksPerPoint = Instrument.MasterInstrument.PointValue / TickValue;

			SetStopLoss(CalculationMode.Ticks, stopLossDistance * TicksPerPoint);
			SetProfitTarget(CalculationMode.Ticks, profitDistance * TicksPerPoint);

			if (longCombination.Count() < ConditionCount || shortCombination.Count() < ConditionCount)
			{
				return;
			}

			if (ConditionsMet(longCombination, 0))
			{
				EnterLong(quantity, "longEntry");
			}

			if (ConditionsMet(shortCombination, 0))
			{
				EnterShort(quantity, "shortEntry");
			}
		}
		#endregion

		#region Properties

		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name="Risk", Description="Risk", Order=0, GroupName="Parameters")]
		public int Risk
		{ get; set; }

		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name="Trade Quantity", Description="Trade Quantity", Order=1, GroupName="Parameters")]
		public int TradeQuantity
		{ get; set; }

		[NinjaScriptProperty]
		[Range(0, 1)]
		[Display(Name="Performance Threshold", Description="Performance Threshold", Order=2, GroupName="Parameters")]
		public double PerformanceThreshold
		{ get; set; }

		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name="Conditions", Description="Conditions", Order=3, GroupName="Parameters")]
		public int ConditionCount
		{ get; set; }

		#endregion
	}
}
