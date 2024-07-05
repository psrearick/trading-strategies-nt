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
	public class ZoneCombinationEnsembleStrategy : Strategy
	{
	    private class StrategyPerformanceMetrics
	    {
	        public double CumulativeReturn { get; set; }
	        public double SharpeRatio { get; set; }
	        public int WinCount { get; set; }
	        public int LossCount { get; set; }
	        public double AverageWin { get; set; }
	        public double AverageLoss { get; set; }
	        public double MaxDrawdown { get; set; }
	        public double RecentPerformance { get; set; }
	    }

		private List<CombinationSupplyDemandLogic> strategies = new List<CombinationSupplyDemandLogic>();
		private Series<double> trailingStopPrice;
		private Dictionary<CombinationSupplyDemandLogic, StrategyPerformanceMetrics> strategyPerformance = new Dictionary<CombinationSupplyDemandLogic, StrategyPerformanceMetrics>();

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

		[NinjaScriptProperty]
	    [Range(1, int.MaxValue)]
	    [Display(Name="Total Quantity", Description="Total number of contracts to trade", Order=1, GroupName="Parameters")]
	    public int TotalQuantity { get; set; }

	    [NinjaScriptProperty]
	    [Range(1, int.MaxValue)]
	    [Display(Name="Performance Lookback Period", Description="Number of bars to look back for performance calculation", Order=2, GroupName="Parameters")]
	    public int PerformanceLookbackPeriod { get; set; }

		[NinjaScriptProperty]
	    [Range(1, 10)]
	    [Display(Name="Trailing Stop ATR Multiplier", Description="Multiplier for ATR to set trailing stop", Order=4, GroupName="Parameters")]
	    public double TrailingStopATRMultiplier { get; set; }

		[NinjaScriptProperty]
	    [Range(1, 252)]
	    [Display(Name="Short-term Lookback", Description="Number of bars for recent performance calculation", Order=5, GroupName="Parameters")]
	    public int ShortTermLookback { get; set; }

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"An ensemble of supply and demand zone strategies";
				Name										= "Supply Demand Combination Ensemble";
				Calculate									= Calculate.OnBarClose;
				EntriesPerDirection							= 1;
				EntryHandling								= EntryHandling.AllEntries;
				IsExitOnSessionCloseStrategy				= true;
				ExitOnSessionCloseSeconds					= 30;
				IsFillLimitOnTouch							= false;
				MaximumBarsLookBack							= MaximumBarsLookBack.TwoHundredFiftySix;
				OrderFillResolution							= OrderFillResolution.Standard;
				Slippage									= 0;
				StartBehavior								= StartBehavior.WaitUntilFlat;
				TimeInForce									= TimeInForce.Gtc;
				TraceOrders									= false;
				RealtimeErrorHandling						= RealtimeErrorHandling.StopCancelClose;
				StopTargetHandling							= StopTargetHandling.PerEntryExecution;
				BarsRequiredToTrade							= 20;
				IsInstantiatedOnEachOptimizationIteration	= true;

				TrailingStopATRMultiplier = 1;
				TotalQuantity = 4;
            	PerformanceLookbackPeriod = 100;
				ShortTermLookback = 20;

			}

			if (State == State.Configure)
			{
				GenerateStrategies();
			}

			if (State == State.DataLoaded)
	        {
	            trailingStopPrice = new Series<double>(this);
	        }

			if (State == State.Historical)
	        {
	            foreach (var strategy in strategies)
	            {
					strategyPerformance[strategy] = new StrategyPerformanceMetrics();
	            }
	        }
		}

		private void GenerateStrategies()
	    {
	        // Get the current bar period in minutes
	        int barPeriodMinutes = BarsPeriod.Value;

	        // Strategy 1: Short-term, responsive
	        strategies.Add(new CombinationSupplyDemandLogic
	        {
	            LookbackPeriod = (int)(3 * Math.Sqrt(barPeriodMinutes)),
	            ZoneThreshold = Math.Max(3, (int)(1 * Math.Sqrt(barPeriodMinutes))),
	            ZoneSizePoints = 0.5 * Math.Sqrt(barPeriodMinutes),
	            StopLossATRMultiplier = 1.5,
	            ATRPeriod = 14,
	            VolumeThresholdMultiplier = 1.2
	        });

	        // Strategy 2: Medium-term, balanced
	        strategies.Add(new CombinationSupplyDemandLogic
	        {
	            LookbackPeriod = (int)(6 * Math.Sqrt(barPeriodMinutes)),
	            ZoneThreshold = Math.Max(5, (int)(2 * Math.Sqrt(barPeriodMinutes))),
	            ZoneSizePoints = 1.0 * Math.Sqrt(barPeriodMinutes),
	            StopLossATRMultiplier = 2.0,
	            ATRPeriod = 14,
	            VolumeThresholdMultiplier = 1.5
	        });

	        // Strategy 3: Long-term, trend-following
	        strategies.Add(new CombinationSupplyDemandLogic
	        {
	            LookbackPeriod = (int)(12 * Math.Sqrt(barPeriodMinutes)),
	            ZoneThreshold = Math.Max(7, (int)(3 * Math.Sqrt(barPeriodMinutes))),
	            ZoneSizePoints = 1.5 * Math.Sqrt(barPeriodMinutes),
	            StopLossATRMultiplier = 2.5,
	            ATRPeriod = 14,
	            VolumeThresholdMultiplier = 1.8
	        });

	        // Strategy 4: Volume-focused
	        strategies.Add(new CombinationSupplyDemandLogic
	        {
	            LookbackPeriod = (int)(9 * Math.Sqrt(barPeriodMinutes)),
	            ZoneThreshold = Math.Max(4, (int)(1.5 * Math.Sqrt(barPeriodMinutes))),
	            ZoneSizePoints = 0.8 * Math.Sqrt(barPeriodMinutes),
	            StopLossATRMultiplier = 1.8,
	            ATRPeriod = 14,
	            VolumeThresholdMultiplier = 2.0
	        });

	        // Strategy 5: Wide-zone, conservative
	        strategies.Add(new CombinationSupplyDemandLogic
	        {
	            LookbackPeriod = (int)(15 * Math.Sqrt(barPeriodMinutes)),
	            ZoneThreshold = Math.Max(6, (int)(2.5 * Math.Sqrt(barPeriodMinutes))),
	            ZoneSizePoints = 2.0 * Math.Sqrt(barPeriodMinutes),
	            StopLossATRMultiplier = 3.0,
	            ATRPeriod = 14,
	            VolumeThresholdMultiplier = 1.6
	        });
	    }

		protected override void OnBarUpdate()
	    {
	        if (CurrentBar < BarsRequiredToTrade)
	            return;

			ExitPositions();

			if (!IsValidEntryTime())
				return;

			if (Position.MarketPosition != MarketPosition.Flat)
			{
//				UpdateTrailingStop();

				return;
			}

	        int longStrategies = 0, shortStrategies = 0;
	        List<CombinationSupplyDemandLogic> strategiesToTrade = new List<CombinationSupplyDemandLogic>();

	        foreach (var strategy in strategies)
	        {
	            if (!strategy.IsReadyToTrade(CurrentBar))
	                continue;

	            if (strategy.ShouldEnterLong(Open, High, Low, Close, Volume, this))
	            {
	                longStrategies++;
	                strategiesToTrade.Add(strategy);
	            }
	            else if (strategy.ShouldEnterShort(Open, High, Low, Close, Volume, this))
	            {
	                shortStrategies++;
	                strategiesToTrade.Add(strategy);
	            }
	        }

	        if (longStrategies > 0 && shortStrategies > 0)
	            return;

	        if (strategiesToTrade.Count > 0)
	        {
	            ExecuteTrades(strategiesToTrade, longStrategies > 0);
	        }

	        UpdateStrategyPerformance();
	    }

		private void ExecuteTrades(List<CombinationSupplyDemandLogic> strategiesToTrade, bool isLong)
	    {
	        int remainingQuantity = TotalQuantity - strategiesToTrade.Count;

	        var performanceScores = strategiesToTrade
	            .ToDictionary(s => s, s => CalculateOverallPerformanceScore(strategyPerformance[s]));

	        double totalScore = performanceScores.Values.Sum();

	        foreach (var strategy in strategiesToTrade)
	        {
	            int quantity = 1;
	            if (remainingQuantity > 0 && totalScore > 0)
	            {
	                double performanceWeight = performanceScores[strategy] / totalScore;
	                quantity += (int)Math.Round(remainingQuantity * performanceWeight);
	            }

	            if (isLong)
	                EnterLongWithDynamicExits(strategy, quantity);
	            else
	                EnterShortWithDynamicExits(strategy, quantity);
	        }
	    }

		private void EnterLongWithDynamicExits(CombinationSupplyDemandLogic strategy, int quantity)
	    {
	        double stopLossPrice = strategy.CalculateStopLossPrice(true, Open, High, Low, Close, this);
	        double takeProfitPrice = strategy.CalculateTakeProfitPrice(true, Close[0], TickSize);

	        if ((stopLossPrice - Close[0]) > -0.5)
	            return;

	        EnterLong(quantity);
	        SetStopLoss(CalculationMode.Price, stopLossPrice);
	        SetProfitTarget(CalculationMode.Price, takeProfitPrice);
	    }

	    private void EnterShortWithDynamicExits(CombinationSupplyDemandLogic strategy, int quantity)
	    {
	        double stopLossPrice = strategy.CalculateStopLossPrice(false, Open, High, Low, Close, this);
	        double takeProfitPrice = strategy.CalculateTakeProfitPrice(false, Close[0], TickSize);

	        if ((Close[0] - stopLossPrice) > -0.5)
	            return;

	        EnterShort(quantity);
	        SetStopLoss(CalculationMode.Price, stopLossPrice);
	        SetProfitTarget(CalculationMode.Price, takeProfitPrice);
	    }

		private void UpdateStrategyPerformance()
	    {
	        foreach (var strategy in strategies)
	        {
	            List<double> returns = new List<double>();
	            double cumulativeReturn = 1;
	            double peakValue = 1;
	            double maxDrawdown = 0;
	            int winCount = 0, lossCount = 0;
	            double totalWin = 0, totalLoss = 0;

				int history = Math.Min(CurrentBar, PerformanceLookbackPeriod);
	            for (int i = history - 1; i >= 0; i--)
	            {
	                double dailyReturn = 1;
	                if (strategy.ShouldEnterLong(Open, High, Low, Close, Volume, this))
	                    dailyReturn = Close[0] / Close[i];
	                else if (strategy.ShouldEnterShort(Open, High, Low, Close, Volume, this))
	                    dailyReturn = Close[i] / Close[0];

	                returns.Add(dailyReturn - 1);
	                cumulativeReturn *= dailyReturn;

	                if (cumulativeReturn > peakValue)
	                    peakValue = cumulativeReturn;
	                else
	                {
	                    double drawdown = (peakValue - cumulativeReturn) / peakValue;
	                    maxDrawdown = Math.Max(maxDrawdown, drawdown);
	                }

	                if (dailyReturn > 1)
	                {
	                    winCount++;
	                    totalWin += dailyReturn - 1;
	                }
	                else if (dailyReturn < 1)
	                {
	                    lossCount++;
	                    totalLoss += 1 - dailyReturn;
	                }
	            }

	            double averageReturn = returns.Average();
	            double stdDev = Math.Sqrt(returns.Select(r => Math.Pow(r - averageReturn, 2)).Average());
	            double sharpeRatio = stdDev != 0 ? averageReturn / stdDev * Math.Sqrt(252) : 0; // Annualized Sharpe Ratio

	            strategyPerformance[strategy].CumulativeReturn = cumulativeReturn - 1;
	            strategyPerformance[strategy].SharpeRatio = sharpeRatio;
	            strategyPerformance[strategy].WinCount = winCount;
	            strategyPerformance[strategy].LossCount = lossCount;
	            strategyPerformance[strategy].AverageWin = winCount > 0 ? totalWin / winCount : 0;
	            strategyPerformance[strategy].AverageLoss = lossCount > 0 ? totalLoss / lossCount : 0;
	            strategyPerformance[strategy].MaxDrawdown = maxDrawdown;

	            double recentReturn = returns.Take(ShortTermLookback).Aggregate((a, b) => (1 + a) * (1 + b) - 1);
	            strategyPerformance[strategy].RecentPerformance = recentReturn;
	        }
	    }

	    private double CalculateOverallPerformanceScore(StrategyPerformanceMetrics metrics)
	    {
	        double winRate = metrics.WinCount + metrics.LossCount > 0 ?
	            (double)metrics.WinCount / (metrics.WinCount + metrics.LossCount) : 0;

	        double profitFactor = metrics.AverageLoss != 0 ?
	            metrics.AverageWin / metrics.AverageLoss : 0;

	        // Combine different metrics into a single score
	        double score =
	            0.3 * metrics.CumulativeReturn +
	            0.2 * metrics.SharpeRatio +
	            0.15 * winRate +
	            0.15 * profitFactor +
	            0.1 * (1 - metrics.MaxDrawdown) + // Lower drawdown is better
	            0.1 * metrics.RecentPerformance;

	        return Math.Max(0, score); // Ensure non-negative score
	    }

		private void UpdateTrailingStop()
	    {
	        double atr = ATR(14)[0];
	        double newStopPrice = Position.MarketPosition == MarketPosition.Long
	            ? Close[0] - (atr * TrailingStopATRMultiplier)
	            : Close[0] + (atr * TrailingStopATRMultiplier);

			if (trailingStopPrice[1] > 0)
			{
				newStopPrice = Position.MarketPosition == MarketPosition.Long
	            ? Math.Max(trailingStopPrice[1], newStopPrice)
	            : Math.Min(trailingStopPrice[1], newStopPrice);
			}

	        trailingStopPrice[0] = newStopPrice;
	        SetStopLoss(CalculationMode.Price, newStopPrice);
	    }

		private double CalculateEnsembleStopLoss(bool isLong)
	    {
	        List<double> stopLosses = strategies.Select(s => s.CalculateStopLossPrice(isLong, Open, High, Low, Close, this)).ToList();
	        return isLong ? stopLosses.Min() : stopLosses.Max();
	    }

	    private double CalculateEnsembleTakeProfit(bool isLong)
	    {
	        List<double> takeProfits = strategies.Select(s => s.CalculateTakeProfitPrice(isLong, Close[0], TickSize)).ToList();
	        return isLong ? takeProfits.Max() : takeProfits.Min();
	    }

		private void ExitPositions()
		{
			if (IsValidTradeTime()) {
				return;
			}

			if (Position.MarketPosition == MarketPosition.Long) {
				ExitLong();
			}

			if (Position.MarketPosition == MarketPosition.Short) {
				ExitShort();
			}
        }

		private bool IsValidEntryTime()
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

		private bool IsValidTradeTime()
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
	}

	public class CombinationSupplyDemandLogic
	{
	    public int LookbackPeriod { get; set; }
	    public int ZoneThreshold { get; set; }
	    public double ZoneSizePoints { get; set; }
	    public double StopLossATRMultiplier { get; set; }
    	public int ATRPeriod { get; set; }
	    public double VolumeThresholdMultiplier { get; set; }

	    private double averageVolume;

	    public bool IsReadyToTrade(int currentBar)
	    {
	        return currentBar >= Math.Max(LookbackPeriod, ZoneThreshold);
	    }

	    public bool ShouldEnterLong(ISeries<double> open, ISeries<double> high, ISeries<double> low, ISeries<double> close, ISeries<double> volume, Strategy strategy)
	    {
	        UpdateAverageVolume(volume, strategy);
	        return IsSignificantDemandZone(low, close, volume, strategy);
	    }

	    public bool ShouldEnterShort(ISeries<double> open, ISeries<double> high, ISeries<double> low, ISeries<double> close, ISeries<double> volume, Strategy strategy)
	    {
	        UpdateAverageVolume(volume, strategy);
	        return IsSignificantSupplyZone(high, close, volume, strategy);
	    }

	    public double CalculateStopLossPrice(bool isLong, ISeries<double> open, ISeries<double> high, ISeries<double> low, ISeries<double> close, Strategy strategy)
	    {
	        double atr = strategy.ATR(ATRPeriod)[0];
	        double stopLossDistance = atr * StopLossATRMultiplier;

	        return isLong
	            ? Math.Min(low[0] - stopLossDistance, close[0] - stopLossDistance)
	            : Math.Max(high[0] + stopLossDistance, close[0] + stopLossDistance);
	    }

	    public double CalculateTakeProfitPrice(bool isLong, double entryPrice, double tickSize)
	    {
	        return isLong
	            ? entryPrice + (ZoneSizePoints * 2 * tickSize)
	            : entryPrice - (ZoneSizePoints * 2 * tickSize);
	    }

	    private void UpdateAverageVolume(ISeries<double> volume, Strategy strategy)
	    {
	        averageVolume = strategy.SMA(volume, LookbackPeriod)[0];
	    }

	    private bool IsSignificantDemandZone(ISeries<double> low, ISeries<double> close, ISeries<double> volume, Strategy strategy)
	    {
	        return low[0] == strategy.MIN(low, ZoneThreshold)[0]
	            && volume[0] > averageVolume * VolumeThresholdMultiplier
	            && close[0] > low[0] + (ZoneSizePoints * strategy.TickSize / 2);
	    }

	    private bool IsSignificantSupplyZone(ISeries<double> high, ISeries<double> close, ISeries<double> volume, Strategy strategy)
	    {
	        return high[0] == strategy.MAX(high, ZoneThreshold)[0]
	            && volume[0] > averageVolume * VolumeThresholdMultiplier
	            && close[0] < high[0] - (ZoneSizePoints * strategy.TickSize / 2);
	    }
	}
}
