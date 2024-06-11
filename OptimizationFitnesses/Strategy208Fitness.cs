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
using NinjaTrader.NinjaScript.Optimizers;

#endregion

//This namespace holds Optimization fitnesses in this folder and is required. Do not change it.
namespace NinjaTrader.NinjaScript.OptimizationFitnesses
{
	public class Strategy208Fitness : OptimizationFitness
	{
		private double weightedSum;
		private Dictionary<string, double> metrics;
		private Dictionary<string, double> weightedMetrics;

		private DateTime lastEndDate = DateTime.MinValue;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"";
				Name										= "Strategy 2.0.8 Fitness";
			}
		}

		protected override void OnCalculatePerformanceValue(StrategyBase strategy)
		{
			int days = 0;
			if (strategy.GetType().GetProperty("Days") != null)
			{
				days = (int) strategy.GetType().GetProperty("Days").GetValue(strategy);
			}

			int metricDays = days;

			if (days == 0)
			{
				double tradeDays = strategy.SystemPerformance.AllTrades.TradesPerformance.TradesCount / strategy.SystemPerformance.AllTrades.TradesPerformance.TradesPerDay;

				metricDays = (int) Math.Round(tradeDays * 365 / 255, 0);
			}

            bool logTrades = true;
			if (strategy.GetType().GetProperty("LogTrades") != null)
			{
				logTrades = (bool) strategy.GetType().GetProperty("LogTrades").GetValue(strategy);
			}

			double loss = strategy.SystemPerformance.AllTrades.TradesPerformance.GrossLoss;
			double profit = strategy.SystemPerformance.AllTrades.TradesPerformance.GrossProfit;

            metrics = new Dictionary<string, double>() {
			    ["netProfit"] = profit + loss,
			    ["maxDrawdown"] = Math.Abs(strategy.SystemPerformance.AllTrades.TradesPerformance.Currency.Drawdown),
				["avgProfit"] = Math.Abs(strategy.SystemPerformance.AllTrades.TradesPerformance.Currency.AverageProfit),
			    ["tradeCount"] = (double) strategy.SystemPerformance.AllTrades.Count,
			    ["profitFactor"] = loss != 0 ? Math.Abs(profit / loss) : 99,
			    ["sharpeRatio"] = strategy.SystemPerformance.AllTrades.TradesPerformance.SharpeRatio,
			    ["avgMae"] = strategy.SystemPerformance.AllTrades.TradesPerformance.Currency.AverageMae,
			    ["avgMfe"] = strategy.SystemPerformance.AllTrades.TradesPerformance.Currency.AverageMfe,
				["profitDrawdownRatio"] = 0,
				["metricDays"] = (double) metricDays,
				["days"] = (double) days,
			};

			metrics["profitDrawdownRatio"] = metrics["maxDrawdown"] != 0 ? metrics["netProfit"] / metrics["maxDrawdown"] : 1;

		    // Weights for each performance metric
		    const double netProfitWeight = 0;
		    const double maxDrawdownWeight = 0;
		    const double avgProfitWeight = 0.0;
		    const double tradeCountWeight = 0.0;
		    const double profitFactorWeight = 0.5;
		    const double sharpeRatioWeight = 0.5;
		    const double avgMaeWeight = 0.0;
		    const double avgMfeWeight = 0.0;
			const double profitDrawdownRatioWeight = 0;

			bool shouldNormalize = metrics["metricDays"] > 0;

		    // Calculate normalized values for each metric
			double normalizedNetProfit = shouldNormalize ? Math.Round(NormalizeValue(metrics["netProfit"] / metrics["metricDays"], -150, 150), 3) : 0;
		    double normalizedMaxDrawdown = shouldNormalize ? Math.Round(1 - NormalizeValue(metrics["maxDrawdown"] /  metrics["metricDays"], 0, 150), 3) : 0;
		    double normalizedAverageProfit = shouldNormalize ? Math.Round(NormalizeValue( metrics["avgProfit"], -60, 180), 3) : 0;
		    double normalizedTradeCount = shouldNormalize ? Math.Round(NormalizeValue(metrics["tradeCount"] /  metrics["metricDays"], 0.1, 1.5), 3) : 0;
		    double normalizedProfitFactor = shouldNormalize ? Math.Round(NormalizeValue(Math.Log10(metrics["profitFactor"] + 1) / Math.Log10(4), 0.25, 0.75), 3) : 0;
		    double normalizedSharpeRatio = shouldNormalize ? Math.Round(NormalizeValue(metrics["sharpeRatio"], -2, 2), 3) : 0;
		    double normalizedAvgMae = shouldNormalize ? Math.Round(1 - NormalizeValue(metrics["avgMae"] /  metrics["metricDays"], 0.5, 13.5), 3) : 0;
		    double normalizedAvgMfe = shouldNormalize ? Math.Round(1 - NormalizeValue(metrics["avgMfe"] /  metrics["metricDays"], -1, 20), 3) : 0;
			double normalizedProfitDrawdownRatio = shouldNormalize ? Math.Round(NormalizeValue(metrics["profitDrawdownRatio"], -2, 2.5), 3) : 0;

			var weightedMetrics = new Dictionary<string, double>() {
				["Net Profit"] = (normalizedNetProfit * netProfitWeight),
            	["Max Drawdown"] = (normalizedMaxDrawdown * maxDrawdownWeight),
				["Average Profit"] = (normalizedAverageProfit * avgProfitWeight),
            	["Trade Count"] = (normalizedTradeCount * tradeCountWeight),
            	["Profit Factor"] = (normalizedProfitFactor * profitFactorWeight),
            	["Sharpe Ratio"] = (normalizedSharpeRatio * sharpeRatioWeight),
            	["Average MAE"] = (normalizedAvgMae * avgMaeWeight),
            	["Average MFE"] = (normalizedAvgMfe * avgMfeWeight),
				["Profit Drawdown Ratio"] = (normalizedProfitDrawdownRatio * profitDrawdownRatioWeight)
			};

		    // Calculate the weighted sum of normalized values
		    weightedSum = weightedMetrics.Values.Sum();

			weightedSum += normalizedProfitFactor / 100;

            Random autoRand = new Random(Guid.NewGuid().GetHashCode());
			double rand = autoRand.NextDouble() / 1000;

            Value = weightedSum + rand;

			if (logTrades)
			{
				printTrades(strategy);
			}

//			printBacktestLog(strategy);
		}

		private void printBacktestLog(StrategyBase strategy)
		{
			string LogString = strategy.BarsArray[0].BarsPeriod.Value.ToString() + ",";

			foreach (var parameter in strategy.OptimizationParameters)
			{
				if (parameter.Name.IsNullOrEmpty())
				{
					continue;
				}

				LogString += $"{parameter.Value},";
			}

			LogString += weightedSum.ToString() + ",";

			LogString += string.Join(",", metrics.Values) + ",";

			LogString += $"{strategy.SystemPerformance.AllTrades.TradesPerformance.GrossProfit},{strategy.SystemPerformance.AllTrades.TradesPerformance.GrossLoss}";

			Print(LogString);
		}

		private void printTrades(StrategyBase strategy)
		{
//			Print(strategy.Optimizer.NumberOfIterations);

			if (strategy.Optimizer.NumberOfIterations != 10)
			{
				return;
			}


			int runDays = (strategy.To - strategy.From).Days + 1;
//			Print($"to: {strategy.To} | from: {strategy.From} |  run days: {runDays} | test period: {strategy.TestPeriod} | optimization period: {strategy.OptimizationPeriod} | iterations: {strategy.Optimizer.NumberOfIterations}");

			foreach (Trade trade in strategy.SystemPerformance.AllTrades)
			{
				string TradeString = "";
				TradeString += trade.Entry.Time.ToString("MM/dd/yyyy") + ","; // Entry Date
				TradeString += trade.Entry.Time.ToString("HH:mm:ss") + ",";// Entry Time
				TradeString +=  $"{trade.Entry.MarketPosition},"; // Direction
				TradeString +=  $"{trade.Entry.Price},";// Entry Price
				TradeString +=  $"{trade.Exit.Price},";// Exit Price
				TradeString +=  $"{trade.Entry.Quantity},";// Quantity
				TradeString +=  trade.Exit.Time.ToString("MM/dd/yyyy") + ",";// Exit Date
				TradeString +=  trade.Exit.Time.ToString("HH:mm:ss") + ",";// Exit Time
				TradeString +=  $"{trade.Entry.Slippage},";// Entry Slippage
				TradeString +=  $"{trade.Exit.Slippage},";// Exit Slippage
				TradeString +=  $"{trade.ProfitCurrency},";// PnL
				TradeString +=  $"{trade.Commission},";// Commision
				TradeString +=  $"{trade.Entry.Instrument}";// Instrument
				Print(TradeString);
            }


		}

		private double NormalizeValue(double value, double min, double max)
		{
			if (max == min)
			{
				return 0;
			}

		    return Math.Min(Math.Max((value - min) / (max - min), 0), 1);
		}
	}
}
