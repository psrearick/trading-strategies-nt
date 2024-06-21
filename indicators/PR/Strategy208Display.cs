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
	public class Strategy208Display : Indicator
	{
		#region Variables
		public EMA emaShort;
		public EMA emaLong;
		public SMA movingAverage;
		public ChoppinessIndex chop;
		public MarketCycle market;
		public MarketCycle marketLong;
		public PriceActionUtils pa;

		public List<double> minScores = new List<double>();
		public List<double> maxScores = new List<double>();

		public Series<double> ShortScores;
		public Series<double> LongScores;
		public Series<double> Scores;
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
				Description									= @"";
				Name										= "Strategy 2.0.8";
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
				LowerThreshold = 15;
				UpperThreshold = 85;

				AddPlot(Brushes.Black, "Signal Strength");
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
				Scores = new Series<double>(this, MaximumBarsLookBack.Infinite);
				LongScores = new Series<double>(this, MaximumBarsLookBack.Infinite);
				ShortScores = new Series<double>(this, MaximumBarsLookBack.Infinite);

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

			#region State.DataLoaded
			if (State == State.DataLoaded)
			{
				marketLong = MarketCycle(BarsArray[LongPeriod]);
			}
			#endregion
		}
		#endregion

		#region OnBarUpdate()
		protected override void OnBarUpdate()
		{
			if (CurrentBar < 100) {
				return;
            }

			if (marketLong.CurrentBar < 1)
			{
				marketLong.Update();
				return;
			}

			Value[0] = GetScore();

			Signals[0] = GetSignal();

			BackBrush = marketLong.background;
		}
		#endregion

		#region GetSignal()
		private TrendDirection GetSignal()
		{
			if (Value[0] >= UpperThreshold)
			{
				return TrendDirection.Bullish;
			}

			if (Value[0] <= LowerThreshold)
			{
				return TrendDirection.Bearish;
			}

			return TrendDirection.Flat;
		}
		#endregion

		#region GetScore()
		private double GetScore()
		{
			double longScore = 0;
			double shortScore = 0;

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

//			int maxScore = Math.Max(longScore, shortScore);
//			int minScore = Math.Min(longScore, shortScore);

//			int maxScore = 13;
//			int minScore = 0;

			maxScores.Add(Math.Max(longScore, shortScore));
			if (maxScores.Count > 300)
				maxScores.RemoveAt(0);

			minScores.Add(Math.Min(longScore, shortScore));
			if (minScores.Count > 300)
				minScores.RemoveAt(0);




			double maxScore = maxScores.Max();
			double minScore = minScores.Min();
//			double maxScore = MAX(LongScores, 100)[0];
//			double minScore = MIN(LongScores, 100)[0];

			Scores[0] = (((maxScore + longScore - shortScore)) / (maxScore * 2 - minScore)) * 100;
//			Scores[0] = (((maxScore + longScore - shortScore)) / (maxScore * 2)) * 100;

			return Scores[0];
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
		private PR.Strategy208Display[] cacheStrategy208Display;
		public PR.Strategy208Display Strategy208Display(int longPeriod, double lowerThreshold, double upperThreshold)
		{
			return Strategy208Display(Input, longPeriod, lowerThreshold, upperThreshold);
		}

		public PR.Strategy208Display Strategy208Display(ISeries<double> input, int longPeriod, double lowerThreshold, double upperThreshold)
		{
			if (cacheStrategy208Display != null)
				for (int idx = 0; idx < cacheStrategy208Display.Length; idx++)
					if (cacheStrategy208Display[idx] != null && cacheStrategy208Display[idx].LongPeriod == longPeriod && cacheStrategy208Display[idx].LowerThreshold == lowerThreshold && cacheStrategy208Display[idx].UpperThreshold == upperThreshold && cacheStrategy208Display[idx].EqualsInput(input))
						return cacheStrategy208Display[idx];
			return CacheIndicator<PR.Strategy208Display>(new PR.Strategy208Display(){ LongPeriod = longPeriod, LowerThreshold = lowerThreshold, UpperThreshold = upperThreshold }, input, ref cacheStrategy208Display);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.PR.Strategy208Display Strategy208Display(int longPeriod, double lowerThreshold, double upperThreshold)
		{
			return indicator.Strategy208Display(Input, longPeriod, lowerThreshold, upperThreshold);
		}

		public Indicators.PR.Strategy208Display Strategy208Display(ISeries<double> input , int longPeriod, double lowerThreshold, double upperThreshold)
		{
			return indicator.Strategy208Display(input, longPeriod, lowerThreshold, upperThreshold);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.PR.Strategy208Display Strategy208Display(int longPeriod, double lowerThreshold, double upperThreshold)
		{
			return indicator.Strategy208Display(Input, longPeriod, lowerThreshold, upperThreshold);
		}

		public Indicators.PR.Strategy208Display Strategy208Display(ISeries<double> input , int longPeriod, double lowerThreshold, double upperThreshold)
		{
			return indicator.Strategy208Display(input, longPeriod, lowerThreshold, upperThreshold);
		}
	}
}

#endregion
