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
	public class Strategy208Signals : Indicator
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
				Name										= "Strategy 2.0.8 Signals";
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
			}
			#endregion

			if (State == State.DataLoaded)
			{
				marketLong = MarketCycle(LongSeries);
			}
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

			Signals[0] = GetSignal();

			if (Signals[1] == TrendDirection.Bullish)
			{
				Draw.ArrowUp(this, "longEntry"+CurrentBar, true, 0, Low[0] - TickSize, Brushes.RoyalBlue);
			}

			if (Signals[1] == TrendDirection.Bearish)
			{
				Draw.ArrowDown(this, "shortEntry"+CurrentBar, true, 0, High[0] + TickSize, Brushes.Fuchsia);
			}
		}
		#endregion

		#region GetSignal()
		private TrendDirection GetSignal()
		{
			int longScore = 0;
			int shortScore = 0;

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

			bool longPatternMatched = marketLong.Direction[0] == TrendDirection.Bullish;

			bool shortPatternMatched = marketLong.Direction[0] == TrendDirection.Bearish;

			LongScores[0] = longScore;
			ShortScores[0] = shortScore;
			Scores[0] = longScore - shortScore;

			if (longPatternMatched)
			{
				return TrendDirection.Bullish;
			}

			if (shortPatternMatched)
			{
				return TrendDirection.Bearish;
			}

			return TrendDirection.Flat;
		}
		#endregion

		#region properties


		[NinjaScriptProperty]
		[Display(Name="Long Series", Description="Long Series", Order=1, GroupName="Parameters")]
		public Bars LongSeries
		{ get; set; }

		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private PR.Strategy208Signals[] cacheStrategy208Signals;
		public PR.Strategy208Signals Strategy208Signals(Bars longSeries)
		{
			return Strategy208Signals(Input, longSeries);
		}

		public PR.Strategy208Signals Strategy208Signals(ISeries<double> input, Bars longSeries)
		{
			if (cacheStrategy208Signals != null)
				for (int idx = 0; idx < cacheStrategy208Signals.Length; idx++)
					if (cacheStrategy208Signals[idx] != null && cacheStrategy208Signals[idx].LongSeries == longSeries && cacheStrategy208Signals[idx].EqualsInput(input))
						return cacheStrategy208Signals[idx];
			return CacheIndicator<PR.Strategy208Signals>(new PR.Strategy208Signals(){ LongSeries = longSeries }, input, ref cacheStrategy208Signals);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.PR.Strategy208Signals Strategy208Signals(Bars longSeries)
		{
			return indicator.Strategy208Signals(Input, longSeries);
		}

		public Indicators.PR.Strategy208Signals Strategy208Signals(ISeries<double> input , Bars longSeries)
		{
			return indicator.Strategy208Signals(input, longSeries);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.PR.Strategy208Signals Strategy208Signals(Bars longSeries)
		{
			return indicator.Strategy208Signals(Input, longSeries);
		}

		public Indicators.PR.Strategy208Signals Strategy208Signals(ISeries<double> input , Bars longSeries)
		{
			return indicator.Strategy208Signals(input, longSeries);
		}
	}
}

#endregion
