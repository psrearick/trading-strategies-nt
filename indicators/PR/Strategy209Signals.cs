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
	public class Strategy209Signals : Indicator
	{
		#region Variables
		public EMA emaShort;
		public EMA emaLong;
		public SMA movingAverage;
		public ChoppinessIndex chop;
		public MarketCycle market;
		public MarketCycle marketLong;
		public PriceActionUtils pa;

		public Series<int> ShortScores;
		public Series<int> LongScores;
		public Series<int> Scores;
		public Series<TrendDirection> Signals;

		private int lowerLows = 0;
		private int higherHighs = 0;
		private int maxLowerLows = 0;
		private int maxHigherHighs = 0;
		#endregion

		#region OnStateChange()
		protected override void OnStateChange()
		{
			#region State.SetDefaults
			if (State == State.SetDefaults)
			{
				Description									= @"Enter the description for your new custom Indicator here.";
				Name										= "Strategy 2.0.9 Signals";
				Calculate									= Calculate.OnBarClose;
				IsOverlay									= false;
				DisplayInDataBox							= true;
				DrawOnPricePanel							= true;
				DrawHorizontalGridLines						= true;
				DrawVerticalGridLines						= true;
				PaintPriceMarkers							= true;
				ScaleJustification							= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				IsSuspendedWhileInactive					= true;

				LongPeriod = 10;
				LowerThreshold = 20;
				UpperThreshold = 80;

				AddPlot(Brushes.Red, "Signal Strength");
				AddLine(Brushes.DarkCyan, LowerThreshold, "Lower Threshold");
				AddLine(Brushes.DarkCyan, UpperThreshold, "Upper Threshold");
			}
			#endregion

			#region State.Configure
			if (State == State.Configure)
			{
				emaShort = EMA(10);
				emaLong = EMA(20);
				movingAverage = SMA(100);
				chop = ChoppinessIndex(7);
				market = MarketCycle();
				pa = PriceActionUtils();

				Signals = new Series<TrendDirection>(this, MaximumBarsLookBack.Infinite);
				Scores = new Series<int>(this, MaximumBarsLookBack.Infinite);
				LongScores = new Series<int>(this, MaximumBarsLookBack.Infinite);
				ShortScores = new Series<int>(this, MaximumBarsLookBack.Infinite);

				AddDataSeries(BarsPeriodType.Minute, 20);
				AddDataSeries(BarsPeriodType.Minute, 40);
				AddDataSeries(BarsPeriodType.Minute, 60);
				AddDataSeries(BarsPeriodType.Minute, 80);
				AddDataSeries(BarsPeriodType.Minute, 100);
				AddDataSeries(BarsPeriodType.Minute, 120);
				AddDataSeries(BarsPeriodType.Minute, 140);
				AddDataSeries(BarsPeriodType.Minute, 160);
				AddDataSeries(BarsPeriodType.Minute, 180);
				AddDataSeries(BarsPeriodType.Minute, 200);


			}
			#endregion

			if (State == State.DataLoaded)
			{
				marketLong = MarketCycle(BarsArray[LongPeriod]);
			}
		}
		#endregion

		#region OnBarUpdate()
		protected override void OnBarUpdate()
		{
			if (CurrentBar < 100) {
				Value[0] = 50;
				return;
            }

			if (marketLong.CurrentBar < 1)
			{
				Value[0] = 50;
				marketLong.Update();
				return;
			}

			Value[0] = GetScore();

			Signals[0] = GetSignal();






//			if (Signals[1] == TrendDirection.Bullish)
//			{
//				Draw.ArrowUp(this, "longEntry"+CurrentBar, true, 0, Low[0] - TickSize, Brushes.RoyalBlue);
//			}

//			if (Signals[1] == TrendDirection.Bearish)
//			{
//				Draw.ArrowDown(this, "shortEntry"+CurrentBar, true, 0, High[0] + TickSize, Brushes.Fuchsia);
//			}
		}
		#endregion

		#region GetSignal()
		private TrendDirection GetSignal()
		{
			if (Value[0] > UpperThreshold)
			{
				return TrendDirection.Bullish;
			}

			if (Value[0] < LowerThreshold)
			{
				return TrendDirection.Bearish;
			}

			return TrendDirection.Flat;
		}
		#endregion

		#region GetScore()
		private double GetScore()
		{
			int longScore = 0;
			int shortScore = 0;

			int maxScore = 13;

			bool emaShortRising = pa.IsRising(emaShort, 0, 1);
			bool emaShortFalling = pa.IsFalling(emaShort, 0, 1);
			bool emaLongRising = pa.IsRising(emaLong, 0, 1);
			bool emaLongFalling	= pa.IsFalling(emaLong, 0, 1);
			bool maRising = emaShortRising && emaLongRising;
			bool maFalling = emaShortFalling && emaLongFalling;

			if (emaShortRising)
				longScore++;

			if (emaLongRising)
				longScore++;

			if (maRising)
				longScore++;

			if (emaShortFalling)
				shortScore++;

			if (emaLongFalling)
				shortScore++;

			if (maFalling)
				shortScore++;

			bool aboveMA = Close[0] > movingAverage[0];
			bool belowMA = Close[0] < movingAverage[0];

			if (aboveMA)
				longScore++;

			if (belowMA)
				shortScore++;

			bool lowChop = (chop[0] > 20) && (chop[0] < 40);
			bool highChop = (chop[0] > 60) && (chop[0] < 80);
			bool validChoppiness = lowChop || highChop;

			if (validChoppiness)
			{
				shortScore++;
				longScore++;
			}

			bool rising = pa.LeastBarsUp(2, 4, 1);
			bool falling = pa.LeastBarsDown(2, 4, 1);

			if (rising)
				longScore++;

			if (falling)
				shortScore++;

			bool newHigh = High[0] >= pa.HighestHigh(1, 5);
			bool newLow	= Low[0] <= pa.LowestLow(1, 5);

			if (newHigh)
				longScore++;

			if (newLow)
				shortScore++;

			bool higherHigh	= pa.ConsecutiveHigherHighs(0, 3);
			bool lowerLow = pa.ConsecutiveLowerLows(0, 3);

			if (higherHigh)
				longScore++;

			if (lowerLow)
				shortScore++;

			bool highestInTrend	= pa.HighestHigh(0, 2) >= pa.HighestHigh(0, 5);
			bool lowestInTrend = pa.LowestLow(0, 2) <= pa.LowestLow(0, 5);

			if (highestInTrend)
				longScore++;

			if (lowestInTrend)
				shortScore++;

			int consecutiveHigherHighs = pa.MaxNumberOfConsecutiveHigherHighs(0, 20);
			int consecutiveLowerLows = pa.MaxNumberOfConsecutiveLowerLows(0, 20);

			if (consecutiveHigherHighs > 2)
				longScore++;

			if (consecutiveLowerLows > 2)
				shortScore++;

			higherHighs = consecutiveHigherHighs;
			lowerLows = consecutiveLowerLows;
			maxHigherHighs = Math.Max(maxHigherHighs, higherHighs);
			maxLowerLows = Math.Max(maxLowerLows, lowerLows);

			MarketCycleStage stage = market.Stage[0];
			MarketCycleStage stageLong = marketLong.Stage[0];

			bool validLongMarket = stageLong == MarketCycleStage.BroadChannel || stageLong == MarketCycleStage.TightChannel;
			bool validShortMarket = stage != MarketCycleStage.Breakout;

			bool validMarketDirection = market.Direction[0] == marketLong.Direction[0];

			if (market.Direction[0] == TrendDirection.Bullish)
				longScore++;

			if (marketLong.Direction[0] == TrendDirection.Bullish)
				longScore++;

			if (market.Direction[0] == TrendDirection.Bearish)
				shortScore++;

			if (marketLong.Direction[0] == TrendDirection.Bearish)
				shortScore++;

			if (pa.IsBullishBar(0))
				longScore++;

			if (pa.IsBearishBar(0))
				shortScore++;

			LongScores[0] = longScore;
			ShortScores[0] = shortScore;
			Scores[0] = longScore - shortScore;

			return (((double)(maxScore + longScore - shortScore)) / (maxScore * 2)) * 100;
		}
		#endregion

		#region Properties
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Long Period", Description="Long Period", Order=0, GroupName="Parameters")]
		public int LongPeriod
		{ get; set; }

		[Range(0, 100), NinjaScriptProperty]
		[Display(Name = "Lower Threshold", Description = "Lower Threshold", GroupName = "Parameters", Order = 1)]
		public double LowerThreshold
		{ get; set; }

		[Range(0, 100), NinjaScriptProperty]
		[Display(Name = "Upper Threshold", Description = "Upper Threshold", GroupName = "Parameters", Order = 2)]
		public double UpperThreshold
		{ get; set; }
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private PR.Strategy209Signals[] cacheStrategy209Signals;
		public PR.Strategy209Signals Strategy209Signals(int longPeriod, double lowerThreshold, double upperThreshold)
		{
			return Strategy209Signals(Input, longPeriod, lowerThreshold, upperThreshold);
		}

		public PR.Strategy209Signals Strategy209Signals(ISeries<double> input, int longPeriod, double lowerThreshold, double upperThreshold)
		{
			if (cacheStrategy209Signals != null)
				for (int idx = 0; idx < cacheStrategy209Signals.Length; idx++)
					if (cacheStrategy209Signals[idx] != null && cacheStrategy209Signals[idx].LongPeriod == longPeriod && cacheStrategy209Signals[idx].LowerThreshold == lowerThreshold && cacheStrategy209Signals[idx].UpperThreshold == upperThreshold && cacheStrategy209Signals[idx].EqualsInput(input))
						return cacheStrategy209Signals[idx];
			return CacheIndicator<PR.Strategy209Signals>(new PR.Strategy209Signals(){ LongPeriod = longPeriod, LowerThreshold = lowerThreshold, UpperThreshold = upperThreshold }, input, ref cacheStrategy209Signals);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.PR.Strategy209Signals Strategy209Signals(int longPeriod, double lowerThreshold, double upperThreshold)
		{
			return indicator.Strategy209Signals(Input, longPeriod, lowerThreshold, upperThreshold);
		}

		public Indicators.PR.Strategy209Signals Strategy209Signals(ISeries<double> input , int longPeriod, double lowerThreshold, double upperThreshold)
		{
			return indicator.Strategy209Signals(input, longPeriod, lowerThreshold, upperThreshold);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.PR.Strategy209Signals Strategy209Signals(int longPeriod, double lowerThreshold, double upperThreshold)
		{
			return indicator.Strategy209Signals(Input, longPeriod, lowerThreshold, upperThreshold);
		}

		public Indicators.PR.Strategy209Signals Strategy209Signals(ISeries<double> input , int longPeriod, double lowerThreshold, double upperThreshold)
		{
			return indicator.Strategy209Signals(input, longPeriod, lowerThreshold, upperThreshold);
		}
	}
}

#endregion
