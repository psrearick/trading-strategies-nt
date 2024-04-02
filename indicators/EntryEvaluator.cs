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

		private PriceActionUtils pa;
		private MarketDirection md;
		private RSI rsi;
		private ATR atr;
		private StdDev stdDevAtr;
		private SMA avgAtr;
		private EMA emaFast;
		private EMA emaSlow;

		private List<EntrySignal> entries = new List<EntrySignal>(20);

		private Dictionary<string, double> correlations = new Dictionary<string, double>();
    	private List<string> significantCorrelations = new List<string>();

		#endregion

		#region OnStateChange()
		protected override void OnStateChange()
		{
			#region State.SetDefaults
			if (State == State.SetDefaults)
			{
				Description									= @"";
				Name										= "Entry Evaluator";
				Calculate									= Calculate.OnBarClose;
				IsOverlay									= false;
				DisplayInDataBox							= true;
				DrawOnPricePanel							= true;
				DrawHorizontalGridLines						= true;
				DrawVerticalGridLines						= true;
				PaintPriceMarkers							= true;
				ScaleJustification							= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				IsSuspendedWhileInactive					= true;
			}
			#endregion

			#region State.Configure
			else if (State == State.Configure)
			{
				pa 						= PriceActionUtils();
				md						= MarketDirection(10, 20);
				atr						= ATR(14);
				rsi						= RSI(14, 3);
				emaFast					= EMA(9);
				emaSlow					= EMA(21);
			}
			#endregion

			#region State.DataLoaded
			if (State == State.DataLoaded) {
				stdDevAtr				= StdDev(atr, 20);
				avgAtr					= SMA(atr, 20);
			}
			#endregion
		}
		#endregion

		#region OnBarUpdate()
		protected override void OnBarUpdate()
		{
			pa.Update();
			md.Update();
			rsi.Update();
			atr.Update();
			stdDevAtr.Update();
			avgAtr.Update();
			emaFast.Update();
			emaSlow.Update();

			if (CurrentBar < 100) {

				return;
			}

			LookForEntryBar();
			UpdateEntryStatus();

			if (CurrentBar % 5 == 0) // Perform analysis every 10 bars
	        {
	            CalculateCorrelations();
				Print(String.Join(",", significantCorrelations));
	        }
		}
		#endregion

		#region CalculateCorrelations()
		private void CalculateCorrelations()
		{
			correlations.Clear();
			List<EntrySignal> closedEntries = entries.Where(e => e.IsClosed).Select(e => e).ToList();
//		    if (closedEntries.Count < 30)
//		    {
//		        significantCorrelations.Clear();
//		        return;
//		    }

//			correlations.Clear();

//			List<EntrySignal> closedEntries = entries.Where(e => e.IsClosed).Select(e => e).ToList();

	        correlations["RSI"] = correlationCoefficient(closedEntries.Select(e => e.Rsi).ToArray(), closedEntries.Select(e => e.IsSuccessful ? 1.0 : 0.0).ToArray());
	        correlations["ATR"] = correlationCoefficient(closedEntries.Select(e => e.Atr).ToArray(), closedEntries.Select(e => e.IsSuccessful ? 1.0 : 0.0).ToArray());
	        correlations["IsEMADiverging"] = correlationCoefficient(closedEntries.Select(e => e.IsEMADiverging ? 1.0 : 0.0).ToArray(), closedEntries.Select(e => e.IsSuccessful ? 1.0 : 0.0).ToArray());
	        correlations["IsEMAConverging"] = correlationCoefficient(closedEntries.Select(e => e.IsEMAConverging ? 1.0 : 0.0).ToArray(), closedEntries.Select(e => e.IsSuccessful ? 1.0 : 0.0).ToArray());
			correlations["IsWithTrendEMA"] = correlationCoefficient(closedEntries.Select(e => e.IsWithTrendEMA ? 1.0 : 0.0).ToArray(), closedEntries.Select(e => e.IsSuccessful ? 1.0 : 0.0).ToArray());
			correlations["IsWithTrendFastEMA"] = correlationCoefficient(closedEntries.Select(e => e.IsWithTrendFastEMA ? 1.0 : 0.0).ToArray(), closedEntries.Select(e => e.IsSuccessful ? 1.0 : 0.0).ToArray());
			correlations["IsWithTrendSlowEMA"] = correlationCoefficient(closedEntries.Select(e => e.IsWithTrendSlowEMA ? 1.0 : 0.0).ToArray(), closedEntries.Select(e => e.IsSuccessful ? 1.0 : 0.0).ToArray());
			correlations["LeadsFastEMAByMoreThanATR"] = correlationCoefficient(closedEntries.Select(e => e.LeadsFastEMAByMoreThanATR ? 1.0 : 0.0).ToArray(), closedEntries.Select(e => e.IsSuccessful ? 1.0 : 0.0).ToArray());
			correlations["IsWithTrendPressure"] = correlationCoefficient(closedEntries.Select(e => e.IsWithTrendPressure ? 1.0 : 0.0).ToArray(), closedEntries.Select(e => e.IsSuccessful ? 1.0 : 0.0).ToArray());
			correlations["IsStrongWithTrendPressure"] = correlationCoefficient(closedEntries.Select(e => e.IsStrongWithTrendPressure ? 1.0 : 0.0).ToArray(), closedEntries.Select(e => e.IsSuccessful ? 1.0 : 0.0).ToArray());
			correlations["IsWithTrendTrendBar"] = correlationCoefficient(closedEntries.Select(e => e.IsWithTrendTrendBar ? 1.0 : 0.0).ToArray(), closedEntries.Select(e => e.IsSuccessful ? 1.0 : 0.0).ToArray());
			correlations["IsBreakoutBarPattern"] = correlationCoefficient(closedEntries.Select(e => e.IsBreakoutBarPattern ? 1.0 : 0.0).ToArray(), closedEntries.Select(e => e.IsSuccessful ? 1.0 : 0.0).ToArray());
			correlations["IsWeakBar"] = correlationCoefficient(closedEntries.Select(e => e.IsWeakBar ? 1.0 : 0.0).ToArray(), closedEntries.Select(e => e.IsSuccessful ? 1.0 : 0.0).ToArray());
			correlations["IsStrongFollowThrough"] = correlationCoefficient(closedEntries.Select(e => e.IsStrongFollowThrough ? 1.0 : 0.0).ToArray(), closedEntries.Select(e => e.IsSuccessful ? 1.0 : 0.0).ToArray());
			correlations["IsBreakout"] = correlationCoefficient(closedEntries.Select(e => e.IsBreakout ? 1.0 : 0.0).ToArray(), closedEntries.Select(e => e.IsSuccessful ? 1.0 : 0.0).ToArray());
			correlations["IsBroadChannel"] = correlationCoefficient(closedEntries.Select(e => e.IsBroadChannel ? 1.0 : 0.0).ToArray(), closedEntries.Select(e => e.IsSuccessful ? 1.0 : 0.0).ToArray());
			correlations["IsTightChannel"] = correlationCoefficient(closedEntries.Select(e => e.IsTightChannel ? 1.0 : 0.0).ToArray(), closedEntries.Select(e => e.IsSuccessful ? 1.0 : 0.0).ToArray());
	        correlations["IsWeakTrend"] = correlationCoefficient(closedEntries.Select(e => e.IsWeakTrend ? 1.0 : 0.0).ToArray(), closedEntries.Select(e => e.IsSuccessful ? 1.0 : 0.0).ToArray());
			correlations["IsStrongTrend"] = correlationCoefficient(closedEntries.Select(e => e.IsStrongTrend ? 1.0 : 0.0).ToArray(), closedEntries.Select(e => e.IsSuccessful ? 1.0 : 0.0).ToArray());
			correlations["IsRSIInRange"] = correlationCoefficient(closedEntries.Select(e => e.IsRSIInRange ? 1.0 : 0.0).ToArray(), closedEntries.Select(e => e.IsSuccessful ? 1.0 : 0.0).ToArray());
			correlations["IsAboveAverageATR"] = correlationCoefficient(closedEntries.Select(e => e.IsAboveAverageATR ? 1.0 : 0.0).ToArray(), closedEntries.Select(e => e.IsSuccessful ? 1.0 : 0.0).ToArray());
			correlations["IsBelowAverageATR"] = correlationCoefficient(closedEntries.Select(e => e.IsBelowAverageATR ? 1.0 : 0.0).ToArray(), closedEntries.Select(e => e.IsSuccessful ? 1.0 : 0.0).ToArray());
			correlations["IsAboveAverageATRByAStdDev"] = correlationCoefficient(closedEntries.Select(e => e.IsAboveAverageATRByAStdDev ? 1.0 : 0.0).ToArray(), closedEntries.Select(e => e.IsSuccessful ? 1.0 : 0.0).ToArray());

	        significantCorrelations = correlations.Where(c => Math.Abs(c.Value) > 0.5).Select(c => c.Key).ToList();
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

			if (md.Direction[0] == TrendDirection.Bullish && md.Direction[1] != TrendDirection.Bullish) {
				EntrySignal entry 		= EntrySignal(CurrentBar);
				entry.Direction = TrendDirection.Bullish;

				AddEntry(entry);

				return;
			}

			if (md.Direction[0] == TrendDirection.Bearish && md.Direction[1] != TrendDirection.Bearish) {
				EntrySignal entry 		= EntrySignal(CurrentBar);
				entry.Direction = TrendDirection.Bearish;

				AddEntry(entry);

				return;
			}
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
		private void AddEntry(EntrySignal entry)
		{
			entry.PreviousSwing	= entry.Direction == TrendDirection.Bearish
				? pa.BarsAgoHigh(0, md.LegLong.BarsAgoStarts[0])
				: pa.BarsAgoLow(0, md.LegLong.BarsAgoStarts[0]);


			entry.EntryBar		= CurrentBar;
			entry.Rsi 			= rsi[0];
			entry.Atr 			= atr[0];
			entry.StdDevAtr		= stdDevAtr[0];
			entry.AvgAtr		= avgAtr[0];
			entry.EmaFast 		= emaFast[0];
			entry.EmaSlow 		= emaSlow[0];
			entry.HighEntry		= High[0];
			entry.LowEntry		= Low[0];
			entry.OpenEntry 	= Open[0];
			entry.CloseEntry 	= Close[0];
			entry.TrendType		= md.Stage[0];


			entry.CalculateAdditionalValues();


			if (entries.Count == 20) {
                entries.RemoveAt(0);
            }

			entries.Add(entry);
		}
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private PR.EntryEvaluator[] cacheEntryEvaluator;
		public PR.EntryEvaluator EntryEvaluator()
		{
			return EntryEvaluator(Input);
		}

		public PR.EntryEvaluator EntryEvaluator(ISeries<double> input)
		{
			if (cacheEntryEvaluator != null)
				for (int idx = 0; idx < cacheEntryEvaluator.Length; idx++)
					if (cacheEntryEvaluator[idx] != null &&  cacheEntryEvaluator[idx].EqualsInput(input))
						return cacheEntryEvaluator[idx];
			return CacheIndicator<PR.EntryEvaluator>(new PR.EntryEvaluator(), input, ref cacheEntryEvaluator);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.PR.EntryEvaluator EntryEvaluator()
		{
			return indicator.EntryEvaluator(Input);
		}

		public Indicators.PR.EntryEvaluator EntryEvaluator(ISeries<double> input )
		{
			return indicator.EntryEvaluator(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.PR.EntryEvaluator EntryEvaluator()
		{
			return indicator.EntryEvaluator(Input);
		}

		public Indicators.PR.EntryEvaluator EntryEvaluator(ISeries<double> input )
		{
			return indicator.EntryEvaluator(input);
		}
	}
}

#endregion
