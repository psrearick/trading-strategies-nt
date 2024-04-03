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
	public class Legs : Indicator
	{
		#region Variables
		private PriceActionUtils PA;
		private ATR Atr;
		public Series<int> Starts;
		public Series<int> BarsAgoStarts;
		public Series<double> AverageLegLengths;
		public Series<double> LegLengthStandardDeviations;
		public Series<double> LegDirectionRatios;
		public Series<int> LegBars;
		private Series<double> LegLengths;
		private Series<double> LegDirections;
		private int WindowSize = 81;
		private Utils Utls = new Utils();
		public int Bars = 0;
		#endregion

		#region OnStateChange()
		protected override void OnStateChange()
		{
			#region State.SetDefaults
			if (State == State.SetDefaults)
			{
				Description									= @"Enter the description for your new custom Indicator here.";
				Name										= "Legs";
				Calculate									= Calculate.OnBarClose;
				IsOverlay									= false;
				DisplayInDataBox							= true;
				DrawOnPricePanel							= true;
				DrawHorizontalGridLines						= true;
				DrawVerticalGridLines						= true;
				PaintPriceMarkers							= true;
				ScaleJustification							= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				//Disable this property if your indicator requires custom values that cumulate with each new market data event.
				//See Help Guide for additional information.
				IsSuspendedWhileInactive					= true;

				Period										= 16;

				AddPlot(new Stroke(Brushes.Green, 3), PlotStyle.Line, "Leg");
				AddPlot(Brushes.DarkCyan, "Signal");
			}
			#endregion

			#region State.Configure
			else if (State == State.Configure)
			{
				PA 			= PriceActionUtils();
				Atr			= ATR(10);
			}
			#endregion

			#region State.DataLoaded
			else if (State == State.DataLoaded)
			{
				BarsAgoStarts				= new Series<int>(this, MaximumBarsLookBack.Infinite);
				Starts						= new Series<int>(this, MaximumBarsLookBack.Infinite);
				LegBars						= new Series<int>(this, MaximumBarsLookBack.Infinite);
				AverageLegLengths			= new Series<double>(this, MaximumBarsLookBack.Infinite);
				LegLengthStandardDeviations	= new Series<double>(this, MaximumBarsLookBack.Infinite);
				LegDirectionRatios			= new Series<double>(this, MaximumBarsLookBack.Infinite);
				LegLengths					= new Series<double>(this, MaximumBarsLookBack.Infinite);
				LegDirections				= new Series<double>(this, MaximumBarsLookBack.Infinite);
			}
			#endregion
		}
		#endregion

		#region OnBarUpdate()
		protected override void OnBarUpdate()
		{
			PA.Update();

			Bars = CurrentBar;
			if (CurrentBar < WindowSize) {
				Values[0][0] 		= 0;
				Values[1][0] 		= 0;
				Starts[0] 			= 0;
		        LegLengths[0] 		= 0;
		        LegDirections[0]	= 0;
				return;
			}

			int AdjustedPeriod = GetPeriod();

			// For the last 12 bars, count bars that close lower than they open, have lower lows, and do not have higher highs
			int bearishBars = PA.NumberOfBearishFallingBars(0, AdjustedPeriod);

			// For the last 12 bars, count bars that close higher than they open, have higher highs, and do not have lower lows
			int bullishBars = PA.NumberOfBullishRisingBars(0, AdjustedPeriod);

			// Get bar of highest high for the last Period bars
			int barsAgoHigh	= PA.BarsAgoHigh(0, AdjustedPeriod);

			// Get bar of lowest low for the last Period bars
			int barsAgoLow	= PA.BarsAgoLow(0, AdjustedPeriod);

			int directionValue = bullishBars > bearishBars ? 1 : bearishBars > bullishBars ? -1 : 0;

			// Ensure that the trend has moved enough the be significant
			bool longEnough = false;
			bool tallEnough = false;

			if (directionValue == 1) {
				longEnough 	= barsAgoLow > 1;
				tallEnough	= (High[0] - Low[barsAgoLow]) > (0.7 * Atr[0]);
			}

			if (directionValue == -1) {
				longEnough 	= barsAgoHigh > 1;
				tallEnough	= (High[barsAgoHigh] - Low[0]) > (0.7 * Atr[0]);
			}

			// Set Value for Signal Bars
			if (!longEnough || !tallEnough) {
				Values[1][0] = Values[1][1];
			} else if ((bearishBars + bullishBars) < (int) Math.Round((double) AdjustedPeriod / 2, 0)) {
				Values[1][0] = 0;
			} else {
				Values[1][0] = directionValue;
			}

			LegBars[0] = Math.Abs(barsAgoHigh - barsAgoLow);

	        LegLengths[0] 		= 0;
	        LegDirections[0]	= 0;

			// Store leg lengths and directions
			if (longEnough && tallEnough) {
				int legLength = Math.Abs(barsAgoHigh - barsAgoLow);
		        LegLengths[0] 		= legLength;
		        LegDirections[0]	= directionValue;
			}

			// Set the number of bars since the most recent swing high/low
			// If the trend direction has not changed, use the existing swing high/low, incrementing the bar count by 1
			// Otherwise, If the new trend is bullish, set the bar count to the bars since the highest bar
			BarsAgoStarts[0] = Values[1][0] == Values[1][1]
				? BarsAgoStarts[1] + 1
				: Values[1][0] == 1 // Bullish
					? barsAgoLow
					: Values[1][0] == -1 // Bearish
						? barsAgoHigh
						: Math.Max(barsAgoHigh, barsAgoLow);

			// Set the bar number of the most recent swing high/low
			Starts[0] = CurrentBar - BarsAgoStarts[0];

			// Set the previous bars since the swing high/low to the current trend direction
			for (int i = 0; i <= BarsAgoStarts[0]; i++) {
				Values[0][i] 					= Values[1][0];
				LegDirections[i]				= Values[1][0];
			}

			for (int i = 0; i <= BarsAgoStarts[0]; i++) {
				AverageLegLengths[i] 			= CalculateAverageLegLength(i, WindowSize);
				LegLengthStandardDeviations[i]	= CalculateLegLengthStandardDeviation(i, WindowSize);
				LegDirectionRatios[i]			= CalculateLegDirectionRatio(i, WindowSize);
			}
		}
		#endregion

		#region GetPeriod()
		// Adjust the Period based on the recent average bars size compared to the average bar size over a longer period of time
		// Greater bar sizes result on a shorter Period
		private int GetPeriod()
		{
			int Window = (int) Math.Floor((double) (Period * (Period * 0.75)));
			double AtrWindow = ATR(Window)[0];
			double AtrThreshold = AtrWindow / 2;

			if (Atr[0] > AtrThreshold) {
				return (int) Math.Round((double) Period / 2, 0);
			}

			return (int) Math.Round((double) Period * 1.5, 0);
		}
		#endregion

		#region CalculateAverageLegLength()
		public double CalculateAverageLegLength(int barsAgo, int length)
		{
			if (length == 0) return 0;
		    if (Bars < (length + barsAgo)) return 0;

			List<double> lengths = new List<double>();

			for (int i = barsAgo; i < (barsAgo + length); i++) {
				lengths.Add(LegLengths[i]);
			}

			return lengths.Average();
		}
		#endregion

		#region CalculateLegLengthStandardDeviation()
		public double CalculateLegLengthStandardDeviation(int barsAgo, int length)
		{
			if (length == 0) return 0;
		    if (Bars < (length + barsAgo)) return 0;
			double average = CalculateAverageLegLength(barsAgo, length);

			double sumOfSquaresOfDifferences = 0;

			for (int i = barsAgo; i < (barsAgo + length); i++) {
				sumOfSquaresOfDifferences += ((LegLengths[i] - average) * (LegLengths[i] - average));
			}

		    return Math.Sqrt(sumOfSquaresOfDifferences / length);
		}
		#endregion

		#region CalculateLegDirectionRatio()
		public double CalculateLegDirectionRatio(int barsAgo, int length)
		{
			if (length == 0) return 0;
		    if (Bars < (length + barsAgo)) return 0;

			int bullishCount = 0;
			int bearishCount = 0;

			for (int i = barsAgo; i < (barsAgo + length); i++) {
				if (Values[0][i] > 0) {
					bullishCount++;
				}

				if (Values[0][i] < 0) {
					bearishCount++;
				}
			}

			return bullishCount / (double) (bullishCount + bearishCount);
		}
		#endregion

		#region ValFromStart()
		public int ValFromStart(int barsAgo = 0)
		{
			return (int) Value[barsAgo];
		}
		#endregion

		#region LegDirectionAtBar()
		public TrendDirection LegDirectionAtBar(int barsAgo = 0)
		{
			return ValFromStart(barsAgo) > 0 ? TrendDirection.Bullish : ValFromStart(barsAgo) < 0 ? TrendDirection.Bearish : TrendDirection.Flat;
		}
		#endregion

		#region IsCounterTrendLeg()
		public bool IsCounterTrendLeg(int barsAgo, TrendDirection channelDirection)
		{
		    TrendDirection legDirection = LegDirectionAtBar(barsAgo);
		    return legDirection != TrendDirection.Flat && legDirection != channelDirection;
		}
		#endregion

		#region Properties
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Period", Description="Period", Order=0, GroupName="Parameters")]
		public int Period
		{ get; set; }
		#endregion
	}
}

#region NinjaScript Legacy/Convenience Constructors
namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		public PR.Legs Legs()
		{
			return Legs(Input, 16);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.PR.Legs Legs()
		{
			return indicator.Legs(Input, 16);
		}
	}
}
#endregion

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private PR.Legs[] cacheLegs;
		public PR.Legs Legs(int period)
		{
			return Legs(Input, period);
		}

		public PR.Legs Legs(ISeries<double> input, int period)
		{
			if (cacheLegs != null)
				for (int idx = 0; idx < cacheLegs.Length; idx++)
					if (cacheLegs[idx] != null && cacheLegs[idx].Period == period && cacheLegs[idx].EqualsInput(input))
						return cacheLegs[idx];
			return CacheIndicator<PR.Legs>(new PR.Legs(){ Period = period }, input, ref cacheLegs);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.PR.Legs Legs(int period)
		{
			return indicator.Legs(Input, period);
		}

		public Indicators.PR.Legs Legs(ISeries<double> input , int period)
		{
			return indicator.Legs(input, period);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.PR.Legs Legs(int period)
		{
			return indicator.Legs(Input, period);
		}

		public Indicators.PR.Legs Legs(ISeries<double> input , int period)
		{
			return indicator.Legs(input, period);
		}
	}
}

#endregion
