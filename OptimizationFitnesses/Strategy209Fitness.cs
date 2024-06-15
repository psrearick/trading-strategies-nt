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
	public class Strategy209Fitness : OptimizationFitness
	{
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"";
				Name										= "Strategy 2.0.9 Fitness";
			}
			else if (State == State.Configure)
			{
			}
		}

		protected override void OnCalculatePerformanceValue(StrategyBase strategy)
		{
			int tradeCount = strategy.SystemPerformance.AllTrades.Count;

			if (tradeCount < 2)
			{
				Value = 0;

				return;
			}

			double countFactor = 1;

			if (tradeCount <= 5)
			{
				countFactor = 0.7;
			}

			double loss = strategy.SystemPerformance.AllTrades.TradesPerformance.GrossLoss;
			double profit = strategy.SystemPerformance.AllTrades.TradesPerformance.GrossProfit;
			double sharpe = strategy.SystemPerformance.AllTrades.TradesPerformance.SharpeRatio;

			double profitFactor = 0.99;

			if (loss > 0)
			{
				profitFactor = Math.Abs(profit / loss);
			}

			double normalizedProfitFactor = Math.Round(NormalizeValue(Math.Log10(profitFactor + 1) / Math.Log10(4), 0.15, 1.5), 3);
		    double normalizedSharpeRatio = Math.Round(NormalizeValue(sharpe, -0.75, 1.75), 3);

			Value = ((normalizedProfitFactor * 0.5) + (normalizedSharpeRatio * 0.5)) * countFactor;
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
