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

#endregion

//This namespace holds Optimization fitnesses in this folder and is required. Do not change it.
namespace NinjaTrader.NinjaScript.OptimizationFitnesses
{
	public class Strategy303Fitness : OptimizationFitness
	{
		private double weightedSum;
		private double netProfit;
		private double maxDrawdown;
		private int tradeCount;
		private double profitFactor;
		private double sharpeRatio;
		private double avgMae;


		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"";
				Name										= "Strategy 303 Fitness";
				PrintTo = PrintTo.OutputTab2;
			}
			else if (State == State.Configure)
			{
			}
		}

		protected override void OnCalculatePerformanceValue(StrategyBase strategy)
		{
		    netProfit = strategy.SystemPerformance.AllTrades.TradesPerformance.GrossProfit + strategy.SystemPerformance.AllTrades.TradesPerformance.GrossLoss;
		    maxDrawdown = Math.Abs(strategy.SystemPerformance.AllTrades.TradesPerformance.Currency.Drawdown);
		    tradeCount = strategy.SystemPerformance.AllTrades.Count;
		    profitFactor = strategy.SystemPerformance.AllTrades.TradesPerformance.ProfitFactor;
		    sharpeRatio = strategy.SystemPerformance.AllTrades.TradesPerformance.SharpeRatio;
		    avgMae = strategy.SystemPerformance.AllTrades.TradesPerformance.Currency.AverageMae;

		    // Weights for each performance metric
		    const double netProfitWeight = 0.1;
		    const double maxDrawdownWeight = 0.1;
		    const double tradeCountWeight = 0.05;
		    const double profitFactorWeight = 0.2;
		    const double sharpeRatioWeight = 0.5;
		    const double avgMaeWeight = 0.05;

		    // Minimum trade count threshold
		    const int minTradeCount = 5;

			int days = 30;
			if (strategy.GetType().GetProperty("Days") != null)
			{
				days = (int) strategy.GetType().GetProperty("Days").GetValue(strategy);
			}

		    // Calculate normalized values for each metric
		    double normalizedNetProfit = Math.Round(NormalizeValue(100 + (netProfit / days), 0, 200),3);
		    double normalizedMaxDrawdown = Math.Round(1 - NormalizeValue(maxDrawdown / days, 0, 100),3);
		    double normalizedTradeCount = Math.Round(NormalizeValue((double)tradeCount / (double)days, 0, 1),3);
		    double normalizedProfitFactor = Math.Round(NormalizeValue(profitFactor, 0, 8),3);
		    double normalizedSharpeRatio = Math.Round(NormalizeValue(0.5 + (sharpeRatio / days), 0, 1),3);
		    double normalizedAvgMae = Math.Round(1 - NormalizeValue(avgMae / days, 0, 20),3);

		    // Calculate the weighted sum of normalized values
		    weightedSum = (normalizedNetProfit * netProfitWeight) +
		                         (normalizedMaxDrawdown * maxDrawdownWeight) +
		                         (normalizedTradeCount * tradeCountWeight) +
		                         (normalizedProfitFactor * profitFactorWeight) +
		                         (normalizedSharpeRatio * sharpeRatioWeight) +
		                         (normalizedAvgMae * avgMaeWeight);

//			Print($"weightedSum: {weightedSum} days: {days} normalizedAvgMae: {normalizedAvgMae} normalizedSharpeRatio: {normalizedSharpeRatio} normalizedProfitFactor: {normalizedProfitFactor} normalizedTradeCount: {normalizedTradeCount} normalizedMaxDrawdown: {normalizedMaxDrawdown} normalizedNetProfit: {normalizedNetProfit}");

		    // Apply a penalty if the trade count is below the minimum threshold
		    if ((tradeCount < minTradeCount) || (netProfit < 0))
		    {
		        weightedSum *= 0.3;
		    }

		    Value = weightedSum;
			if (strategy.GetType().GetProperty("Days") != null)
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

			LogString += $"{netProfit},{maxDrawdown},{tradeCount},{profitFactor},{sharpeRatio},{avgMae},{strategy.SystemPerformance.AllTrades.TradesPerformance.GrossProfit.ToString()},{strategy.SystemPerformance.AllTrades.TradesPerformance.GrossLoss.ToString()}";

			// Period, Parameters, Score, Net Profit, Max Drawdown, Trades, Profit Factor, Sharpe Ratio, Max Adverse Excursion, Gross Profit, Gross Loss
			Print(LogString);
		}

		private void printTrades(StrategyBase strategy)
		{
			if ((int) strategy.GetType().GetProperty("Days").GetValue(strategy) > strategy.TestPeriod)
			{
				return;
			}

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
		    return Math.Min(Math.Max((value - min) / (max - min), 0), 1);
		}
	}
}
