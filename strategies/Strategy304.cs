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
	public class Strategy304 : Strategy
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

		Func<int, bool>[] longCombination = null;
		Func<int, bool>[] shortCombination = null;

		List<int> timeframeLengths = new List<int> { 5, 8, 10, 12, 15 };
		int currentTimeframeIndex = 0;
		int currentTimeframe = 5;

		private Utils utils = new Utils();
		private ATR atrDaily;
		private EMA emaShort;
		private EMA emaLong;
		private ATR atr;
		private SMA avgAtr;
		private SMA avgClose;
		private ChoppinessIndex chop;
		private RSI rsi;
		private PriceActionUtils pa;

		private Dictionary<string, Dictionary<int, Indicator>> indicators =
			new Dictionary<string, Dictionary<int, Indicator>>()
				{
					["EMAShort"] = new Dictionary<int, Indicator>(),
					["EMALong"] = new Dictionary<int, Indicator>(),
					["ATR"] = new Dictionary<int, Indicator>(),
					["AvgATR"] = new Dictionary<int, Indicator>(),
					["AvgClose"] = new Dictionary<int, Indicator>(),
					["Chop"] = new Dictionary<int, Indicator>(),
					["RSI"] = new Dictionary<int, Indicator>(),
					["PA"] = new Dictionary<int, Indicator>(),
				};

		private double ProfitTarget = 3;
		private double StopLossTarget = 5;
		private double HighATRMultiplier = 3;

		private int lastTradeCount = 0;
		private double recentPerformance = 0;
		private double overallPerformance = 0;
		private int lookbackPeriod = 80;
		private int lookbackQueueLength = 21;
		private int performanceTrackingPeriod = 20;

		private double performanceScore = 0;
		private Queue<double> recentPerformanceScores = new Queue<double>();
		private Queue<double> sharpeRatios = new Queue<double>();
		private Queue<double> drawdownRatios = new Queue<double>();
		private Queue<double> profitLoss = new Queue<double>();
		private Queue<double> volatilityPercentages = new Queue<double>();
		private Queue<double> performanceVolatilityScores = new Queue<double>();

		private DateTime initTime = DateTime.Now;
		private DateTime start = DateTime.Now;
		private int cooldownPeriod = 5;
		private int cooldownCounter = 0;
		private int timeframeCooldownCounter = 0;
		private int timeframeCooldownPeriod = 10;
		private int days = 0;
		[Browsable(false)]
		public int Days
		{
		    get { return days; }
		    set { days = value; }
		}
		private DateTime lastDay = DateTime.MinValue;

		private double PerformanceThreshold = 0.5;
		private int ConditionCount = 1;
		private List<Func<int, bool>> longCriteria;
		private List<Func<int, bool>> shortCriteria;

		private double maxProfit = 0;

		private int calculated = 0;
		#endregion

		#region OnStateChange()
		protected override void OnStateChange()
		{
			#region SetDefaults
			if (State == State.SetDefaults)
			{
				Description										= @"Trend Following Price Action Strategy";
				Name											= "Strategy 3.0.4";
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
				IsInstantiatedOnEachOptimizationIteration		= true;
				IsUnmanaged										= false;

				Risk											= 1;

				PrintTo = PrintTo.OutputTab1;
			}
			#endregion

			#region Configure
			if (State == State.Configure)
			{
				AddDataSeries(Data.BarsPeriodType.Second, 15);
				AddDataSeries(Data.BarsPeriodType.Minute, 30);

				atrDaily			= ATR(BarsArray[2], 14);

				for (int i = 0; i < timeframeLengths.Count; i++)
				{
					int length = timeframeLengths[i];
					AddDataSeries(Data.BarsPeriodType.Minute, length);
					Bars bars = BarsArray[i + 2];

					indicators["EMAShort"][length] = EMA(bars, 9);
					indicators["EMALong"][length] = EMA(bars, 21);
					indicators["ATR"][length] = ATR(bars, 14);
					indicators["AvgATR"][length] = SMA(indicators["ATR"][length], 21);
					indicators["AvgClose"][length] = SMA(bars, 14);
					indicators["Chop"][length] = ChoppinessIndex(bars, 14);
					indicators["RSI"][length] = RSI(bars, 14, 3);
					indicators["PA"][length] = PriceActionUtils(bars);
				}

				emaShort			= (EMA)indicators["EMAShort"][currentTimeframe];
				emaLong				= (EMA)indicators["EMALong"][currentTimeframe];
				atr 				= (ATR)indicators["ATR"][currentTimeframe];
				avgAtr				= (SMA)indicators["AvgATR"][currentTimeframe];
				avgClose	 		= (SMA)indicators["AvgClose"][currentTimeframe];
				chop				= (ChoppinessIndex)indicators["Chop"][currentTimeframe];
				rsi					= (RSI)indicators["RSI"][currentTimeframe];
				pa					= (PriceActionUtils)indicators["PA"][currentTimeframe];
			}
			#endregion

			#region DataLoaded
			if (State == State.DataLoaded) {
				DefineConditions();
			}
			#endregion
		}
		#endregion

		#region OnBarUpdate()
		protected override void OnBarUpdate()
		{
			UpdateIndicators();

			if (CurrentBar < BarsRequiredToTrade || CurrentBars[0] < 1 || CurrentBars[1] < 1 || CurrentBars[2] < 1)
			{
				return;
            }

			exitPositions();


			if (BarsInProgress != (currentTimeframeIndex + 2))
			{
				return;
			}

			AdaptiveTimeframeSelection();

			if (Time[0].Date != lastDay.Date)
			{
				Days++;
				lastDay = Time[0];
			}

			CalculateVolatilityPercentages();

			if (CurrentBar == BarsRequiredToTrade)
	        {
	            OptimizeStrategy();
	        }

			if (ShouldRecalculate())
		    {
				calculated++;
		        Recalculate();
		    }

			calculateQuantity();
			setEntries();
			CalculateAdaptiveLookbackPeriod();
		}
		#endregion

		#region UpdateIndicators()
		private void UpdateIndicators()
		{
			atrDaily.Update();
			emaShort.Update();
			emaLong.Update();
			atr.Update();
			avgAtr.Update();
			avgClose.Update();
			chop.Update();
			rsi.Update();
			pa.Update();
		}
		#endregion

		#region ShouldRecalculate()
		private bool ShouldRecalculate()
		{
			if (SystemPerformance.AllTrades.Count == 0)
			{
				return false;
			}

			if (lastTradeCount == SystemPerformance.AllTrades.Count)
			{
				return false;
			}

			lastTradeCount = SystemPerformance.AllTrades.Count;

			performanceScore = CalculateRecentPerformance();
	    	UpdateOverallPerformance();

			cooldownCounter--;
			if (cooldownCounter > 0)
			{
				return false;
			}

			cooldownCounter = cooldownPeriod;

			return true;
		}
		#endregion

		#region AdaptiveTimeframeSelection()
		private void AdaptiveTimeframeSelection()
		{
			timeframeCooldownCounter--;
			if (timeframeCooldownCounter > 0)
			{
				return;
			}

			timeframeCooldownCounter = timeframeCooldownPeriod;

		    // Calculate market conditions
			// Calculate the current market volatility
		    double volatilityPercentage = (atr[0] / avgClose[0]) * 100;
			double volatility = volatilityPercentages.Count > 0
				? NormalizeValue(volatilityPercentage, volatilityPercentages)
				: 0.5;
			// Calculate the current market trend strength
		    double trendStrength = (pa.GetStrongTrendDirection(0, 10) != TrendDirection.Flat) ? 0.75
				: (pa.GetWeakTrendDirection(0, 10) != TrendDirection.Flat) ? 0.5
				: 0.25;

		    // Select the appropriate timeframe based on market conditions
		    if (volatility > 0.8 && trendStrength > 0.7)
		    {
				ChangeTimeframe(timeframeLengths.Count - 1); // Use the highest timeframe
				return;
		    }

			if (volatility < 0.2 && trendStrength < 0.3)
		    {
		        ChangeTimeframe(0); // Use the lowest timeframe (e.g., 1-minute)
				return;
		    }

	        // Select the timeframe based on a combination of volatility and trend strength
	        int index = (int) Math.Round((volatility + trendStrength) / 2 * (timeframeLengths.Count - 1));

			ChangeTimeframe(index);
		}
		#endregion

		#region ChangeTimeframe()
		private void ChangeTimeframe(int timeframeIndex)
		{
			if (currentTimeframeIndex == timeframeIndex)
			{
				return;
			}

			currentTimeframeIndex = timeframeIndex;
			currentTimeframe = timeframeLengths[currentTimeframeIndex];
			utils.PrintMessage(currentTimeframe);

			emaShort			= (EMA)indicators["EMAShort"][currentTimeframe];
			emaLong				= (EMA)indicators["EMALong"][currentTimeframe];
			atr 				= (ATR)indicators["ATR"][currentTimeframe];
			avgAtr				= (SMA)indicators["AvgATR"][currentTimeframe];
			avgClose	 		= (SMA)indicators["AvgClose"][currentTimeframe];
			chop				= (ChoppinessIndex)indicators["Chop"][currentTimeframe];
			rsi					= (RSI)indicators["RSI"][currentTimeframe];
			pa					= (PriceActionUtils)indicators["PA"][currentTimeframe];

			UpdateIndicators();
			DefineConditions();
		}
		#endregion

		#region Recalculate()
		private void Recalculate()
		{
			Print(CurrentBar + " " + Time[0].ToString() + " ==================== " + (DateTime.Now - start).TotalSeconds + " -- " + (DateTime.Now - initTime).TotalSeconds);
			start = DateTime.Now;
			AdjustDynamicParameters();
			OptimizeStrategy();
		}
		#endregion

		#region DefineConditions()
		private void DefineConditions()
		{
		    longCriteria = new List<Func<int, bool>>
		    {
		        (int i) => emaShort[i] > emaShort[1 + i] && emaLong[i] > emaLong[1 + i] && (pa.GetTrendDirection() == TrendDirection.Bullish),  // maRising
		        (int i) => (Close[i] > Close[1 + i] && Close[1 + i] > Close[2 + i] && Close[2 + i] > Close[3 + i]) && (pa.GetTrendDirection() == TrendDirection.Bullish),  // rising
		        (int i) => (Close[i] > MAX(High, 10)[1 + i]) && (pa.GetTrendDirection() == TrendDirection.Bullish),  // newHigh
				(int i) => (rsi[i] < 70 && rsi[i] > 50) && (pa.GetTrendDirection() == TrendDirection.Bullish),  // validRSI
		        (int i) => (MAX(High, 3)[i] >= MAX(High, 10)[i] && High[i] > High[1 + i] && High[1 + i] > High[2 + i] && High[2 + i] > High[3 + i]) && (pa.GetTrendDirection() == TrendDirection.Bullish),  // upTrend
				(int i) => ((chop[i] < 38.2 || chop[i] > 61.8)) && (pa.GetTrendDirection() == TrendDirection.Bullish),  // validChoppiness
				(int i) => (atr[i] > avgAtr[i]) && (pa.GetTrendDirection() == TrendDirection.Bullish),  // Above Average ATR
		    };

		    shortCriteria = new List<Func<int, bool>>
		    {
		        (int i) => (emaShort[i] < emaShort[1 + i] && emaLong[i] < emaLong[1 + i]) && (pa.GetTrendDirection() == TrendDirection.Bearish),  // maFalling
		        (int i) => (Close[i] < Close[1 + i] && Close[1 + i] < Close[2 + i] && Close[2 + i] < Close[3 + i]) && (pa.GetTrendDirection() == TrendDirection.Bearish),  // falling
		        (int i) => (Close[i] < MIN(Low, 10)[1 + i]) && (pa.GetTrendDirection() == TrendDirection.Bearish),  // newLow
				(int i) => (rsi[i] < 50 && rsi[i] > 30) && (pa.GetTrendDirection() == TrendDirection.Bearish),  // validRSI
		        (int i) => (MIN(Low, 3)[i] <= MIN(Low, 10)[i] && Low[i] < Low[1 + i] && Low[1 + i] < Low[2 + i] && Low[2 + i] < Low[3 + i]) && (pa.GetTrendDirection() == TrendDirection.Bearish),  // downTrend
				(int i) => (chop[i] < 38.2 || chop[i] > 61.8) && (pa.GetTrendDirection() == TrendDirection.Bearish),  // validChoppiness
				(int i) => (atr[i] > avgAtr[i]) && (pa.GetTrendDirection() == TrendDirection.Bearish),  // Above Average
		    };
		}
		#endregion

		#region CalculateRecentPerformance()
		private double CalculateRecentPerformance()
		{
		    int startIndex = Math.Max(0, SystemPerformance.AllTrades.Count - performanceTrackingPeriod);
		    int endIndex = SystemPerformance.AllTrades.Count - 1;

		    double netProfit = 0;
		    double maxDrawdown = 0;
		    double peakProfit = 0;
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

					continue;
		        }

	            maxDrawdown = Math.Max(peakProfit - netProfit, maxDrawdown);
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
			profitLoss.Enqueue(averageProfit);

			if (sharpeRatios.Count > lookbackPeriod) sharpeRatios.Dequeue();
   			if (drawdownRatios.Count > lookbackPeriod) drawdownRatios.Dequeue();

			// Normalize using dynamic historical range
		    double normalizedSharpeRatio = NormalizeValue(sharpeRatio, sharpeRatios);
		    double normalizedDrawdownRatio = NormalizeValue(drawdownRatio, drawdownRatios);
			double normalizedProfitLoss = NormalizeValue(averageProfit, profitLoss);

		    // Calculate the performance score as a weighted average
		    double sharpeWeight = 0.3;
		    double drawdownWeight = 0.2;
			double profitLossWeight = 0.5;
		    double performanceScore = (profitLossWeight * normalizedProfitLoss) + (sharpeWeight * normalizedSharpeRatio) + (drawdownWeight * (1 - normalizedDrawdownRatio));

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

		    return Math.Min(1, Math.Max(0, (value - min) / (max - min)));
		}
		#endregion

		#region UpdateOverallPerformance()
		private void UpdateOverallPerformance()
		{
		    recentPerformanceScores.Enqueue(performanceScore);

		    if (recentPerformanceScores.Count > performanceTrackingPeriod)
		    {
		        recentPerformanceScores.Dequeue();
		    }

		    overallPerformance = recentPerformanceScores.Average();
		}
		#endregion

		#region AdjustDynamicParameters()
		private void AdjustDynamicParameters()
		{
			if (recentPerformanceScores.Count == 0)
			{
				return;
			}

		    double recentAveragePerformance = recentPerformanceScores.Average();
		    double performanceStdDev = CalculateStandardDeviation(recentPerformanceScores.ToList());
			double performanceThresholdAdjustment = 0.2 * performanceStdDev * (performanceScore > recentAveragePerformance ? -1 : 1);

			PerformanceThreshold = Math.Max(0.1, Math.Min(0.9, PerformanceThreshold + performanceThresholdAdjustment));
			PerformanceThreshold = Double.IsNaN(PerformanceThreshold) ? 0.5 : PerformanceThreshold;

			ConditionCount = CalculateConditionCount();

			Print("PerformanceThreshold: " + Math.Round(PerformanceThreshold, 3).ToString()
				+ "; ConditionCount: " + ConditionCount
				+ "; performanceThresholdAdjustment: " + Math.Round(performanceThresholdAdjustment, 3).ToString()
				+ "; performanceStdDev: " + Math.Round(performanceStdDev, 3).ToString()
			);
		}
		#endregion

		#region CalculateConditionCount()
		private int CalculateConditionCount()
		{
			int minConditions = 1;
			int maxConditions = shortCriteria.Count - 1;

			// Calculate the current market volatility
		    double volatilityPercentage = (atr[0] / avgClose[0]) * 100;
			double volatility = volatilityPercentages.Count > 0
				? NormalizeValue(volatilityPercentage, volatilityPercentages)
				: 0.5;

			if (volatility < 0.2 && (pa.GetStrongTrendDirection(0, 10) != TrendDirection.Flat))
			{
				return minConditions;
			}

			if (volatility > 0.8 && (pa.GetStrongTrendDirection(0, 10) == TrendDirection.Flat))
			{
				return maxConditions;
			}

			double trendStrength = (pa.GetStrongTrendDirection(0, 10) != TrendDirection.Flat) ? 0.75
				: (pa.GetWeakTrendDirection(0, 10) != TrendDirection.Flat) ? 0.5
				: 0.25;

			int range = maxConditions - minConditions;
        	double factor = (volatility + (1 - trendStrength)) / 2;
        	return minConditions + (int)Math.Round(range * factor);
		}
		#endregion

		#region OptimizeStrategy()
		private void OptimizeStrategy()
		{
		    // Call the entry criteria optimization function
		    OptimizeEntryCriteria();

		    // Call the exit conditions optimization function
		    OptimizeExitConditions();
		}
		#endregion

		#region calculateQuantity()
		private void calculateQuantity()
		{
			// Calculate the current market volatility (e.g., using ATR)
		    double volatilityPercentage = (atr[0] / avgClose[0]) * 100;
			double volatility = volatilityPercentages.Count > 0
				? NormalizeValue(volatilityPercentage, volatilityPercentages)
				: 0.5;

		    // Calculate the recent performance (e.g., using a performance metric)
			double recentPerformance = performanceScore > 0 ? performanceScore : 0.5;
			double performance = recentPerformanceScores.Count > 0
				? NormalizeValue(recentPerformance, recentPerformanceScores)
				: 0.5;

		    // Define the base position size
		    double basePositionSize = Risk > 0 ? Risk : Account.Get(AccountItem.CashValue, Currency.UsDollar) * 0.02; // 2% of account balance

		    // Adjust the position size based on volatility and recent performance
		    double performanceVolatilityScore = (performance * 0.5) + (volatility * 0.5);
			performanceVolatilityScores.Enqueue(performanceVolatilityScore);

			if (performanceVolatilityScores.Count > performanceTrackingPeriod)
		    {
		        performanceVolatilityScores.Dequeue();
		    }

			double normalizedScore = Math.Max(0.1, NormalizeValue(performanceVolatilityScore, performanceVolatilityScores));

			double adjustedPositionSize = basePositionSize * normalizedScore;

		    // Update the quantity variable
			double stopLossDistance	= Math.Round(atrDaily[0] * StopLossTarget);
		    quantity = (int)Math.Max(1,  Math.Floor(adjustedPositionSize / (stopLossDistance * Instrument.MasterInstrument.PointValue)));
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
		    List<Func<int, bool>> longEntryCriteria = new List<Func<int, bool>> (longCriteria);

		    // Define the list of short entry criteria
		    List<Func<int, bool>> shortEntryCriteria = new List<Func<int, bool>> (shortCriteria);

		    // Generate all possible combinations of long and short entry criteria
		    var longCombinations = GenerateCombinations(longEntryCriteria, ConditionCount);
		    var shortCombinations = GenerateCombinations(shortEntryCriteria, ConditionCount);

		    // Evaluate the performance of each combination and find the best one
		    double bestLongPerformance = double.MinValue;
		    double bestShortPerformance = double.MinValue;
		    Func<int, bool>[] bestLongCombination = null;
		    Func<int, bool>[] bestShortCombination = null;

		    foreach (var combination in longCombinations)
		    {
		        double longPerformance = EvaluatePerformance(combination, true);
		        if (longPerformance > bestLongPerformance)
		        {
		            bestLongPerformance = longPerformance;
		            bestLongCombination = combination;
		        }
		    }

		    foreach (var combination in shortCombinations)
		    {
		        double shortPerformance = EvaluatePerformance(combination, false);
		        if (shortPerformance > bestShortPerformance)
		        {
		            bestShortPerformance = shortPerformance;
		            bestShortCombination = combination;
		        }
		    }

		    // Assign the best combination of entry criteria to the strategy
		    longCombination = bestLongCombination;
		    shortCombination = bestShortCombination;
		}
		#endregion

		#region CalculateVolatilityPercentages()
		private void CalculateVolatilityPercentages()
		{
		    // Calculate the volatility percentage
		    double volatilityPercentage = (atr[0] / avgClose[0]) * 100;

			volatilityPercentages.Enqueue(volatilityPercentage);

			if (volatilityPercentages.Count > lookbackQueueLength) volatilityPercentages.Dequeue();
		}
		#endregion

		#region CalculateAdaptiveLookbackPeriod()
		private void CalculateAdaptiveLookbackPeriod()
		{
		    // Calculate the volatility percentage
		    double volatilityPercentage = (atr[0] / avgClose[0]) * 100;

			double normalizedVolatilityPercentage = NormalizeValue(volatilityPercentage, volatilityPercentages);

		    // Define the volatility thresholds and corresponding lookback periods
		    if (normalizedVolatilityPercentage < 0.33)
		    {
				performanceTrackingPeriod = 30;
				lookbackPeriod = 120; // Low volatility, use a longer lookback period

		        return;
		    }

			if (normalizedVolatilityPercentage < 0.67)
		    {
				performanceTrackingPeriod = 20;
				lookbackPeriod = 90; // Medium volatility, use a medium lookback period

		        return;
		    }

			performanceTrackingPeriod = 10;
			lookbackPeriod = 60; // High volatility, use a shorter lookback period
		}
		#endregion

		#region IsTrendValid()
		private bool IsTrendValid(TrendDirection direction)
		{
			if (direction == TrendDirection.Bullish)
			{
				return  pa.GetTrendDirection() == TrendDirection.Bullish;
			}

			 return pa.GetTrendDirection() == TrendDirection.Bearish;
		}
		#endregion

		#region IsVolatilityValid()
		private bool IsVolatilityValid()
		{
			double volatilityPercentage = (atr[0] / avgClose[0]) * 100;

			double normalizedVolatilityPercentage = NormalizeValue(volatilityPercentage, volatilityPercentages);

		    // Define the valid volatility range
		    double minVolatility = 0.33;
		    double maxVolatility = 0.67;

		    // Check if the volatility is within the valid range
		    return normalizedVolatilityPercentage >= minVolatility && normalizedVolatilityPercentage <= maxVolatility;
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
			double totalLoss = 0;
			double netProfit = 0;
		    int totalTrades = 0;

		    // Iterate over the past N bars
			int startBar = Math.Max(0, CurrentBar - lookbackPeriod - 3);

			if (startBar < 4)
			{
				return 0;
			}

		    for (int i = startBar; i < CurrentBar - 3; i++)
		    {
		        // Check if the current combination matches the entry criteria for the current bar
				bool entryMatched = ConditionsMet(combination, CurrentBar - i);
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
		                    netProfit += profit;
							totalProfit += profit;

		                    totalTrades++;
		                    break;
		                }

						if ((isLong && currentPrice <= stopLoss) || (!isLong && currentPrice >= stopLoss))
		                {
		                    // Calculate the loss for the trade
		                    double loss = isLong ? stopLoss - entryPrice : entryPrice - stopLoss;
		                    netProfit += loss;
							totalLoss += Math.Abs(loss);

		                    totalTrades++;
		                    break;
		                }
		            }
		        }
		    }

			if (totalTrades == 0)
			{
				return 0;
			}

			double averageProfit = totalProfit / totalTrades;
			maxProfit = Math.Max(maxProfit, averageProfit);

			double normalizedProfit = averageProfit / maxProfit;
			double normalizedProfitFactor =  Math.Min(5, Math.Max(0, totalProfit / totalLoss)) / 5;

		    // Calculate and return the average profit per trade
			return (normalizedProfit * 0.25) + (normalizedProfitFactor * 0.75);

		}
		#endregion

		#region  OptimizeExitConditions()
		private void OptimizeExitConditions()
		{
		    // Define the range of values for ProfitTarget, StopLossTarget, and HighATRMultiplier
//		    double[] profitTargetValues = { 1.0, 1.5, 2.0, 2.5, 3.0, 3.5, 4.0, 4.5, 5.0, 5.5, 6.0, 6.5, 7.0, 7.5, 8.0, 8.5, 9.0, 9.5, 10.0 };
//		    double[] stopLossTargetValues = { 1.0, 1.5, 2.0, 2.5, 3.0, 3.5, 4.0, 4.5, 5.0, 5.5, 6.0, 6.5, 7.0, 7.5, 8.0, 8.5, 9.0, 9.5, 10.0 };
//		    double[] highATRMultiplierValues = { 1.0, 1.5, 2.0, 2.5, 3.0, 3.5, 4.0, 4.5, 5.0, 5.5, 6.0, 6.5, 7.0, 7.5, 8.0, 8.5, 9.0, 9.5, 10.0 };

			double[] profitTargetValues = { 1.0, 1.5, 2.0, 2.5, 3.0, 3.5, 4.0, 4.5, 5.0, 5.5, 6.0, 6.5, 7.0, 7.5, 8.0, 8.5, 9.0, 9.5, 10.0 };
		    double[] stopLossTargetValues = { 1.0, 1.5, 2.0, 2.5, 3.0, 3.5, 4.0, 4.5, 5.0 };
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
			int bar = Math.Min(lookbackPeriod, CurrentBar - 3);
		    for (int i = bar - 3; i > 0; i--)
		    {
		        bool entryMatched = false;
		        bool isLong = false;
		        // Check if the current combination matches the entry criteria for the current bar
				if (longCombination != null && longCombination.Length > 0 && ConditionsMet(longCombination, i))
		        {
		            entryMatched = true;
		            isLong = true;
		        }
				if (shortCombination != null && shortCombination.Length > 0 && ConditionsMet(shortCombination, i))
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
			if (conditions == null || conditions.Length == 0)
		    {
		        return false;
		    }

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

			if (longCombination == null || longCombination.Length == 0)
			{
				return;
			}

			if (shortCombination == null || shortCombination.Length == 0)
			{
				return;
			}

			if (longCombination.Count() < ConditionCount || shortCombination.Count() < ConditionCount)
			{
				return;
			}

			double TickValue = Instrument.MasterInstrument.PointValue * TickSize;
			double TicksPerPoint = Instrument.MasterInstrument.PointValue / TickValue;

			SetStopLoss(CalculationMode.Ticks, stopLossDistance * TicksPerPoint);
			SetProfitTarget(CalculationMode.Ticks, profitDistance * TicksPerPoint);

			if (longCombination != null && longCombination.Length > 0 && ConditionsMet(longCombination, 0)
				&& IsTrendValid(TrendDirection.Bullish)
				&& IsVolatilityValid()
				)
			{
				EnterLong(quantity, "longEntry");
			}

			if (shortCombination != null && shortCombination.Length > 0 && ConditionsMet(shortCombination, 0)
				&& IsTrendValid(TrendDirection.Bearish)
				&& IsVolatilityValid()
				)
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

		#endregion
	}
}
