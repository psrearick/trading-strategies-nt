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

		public Series<TrendDirection> Signals;

		private int lowerLows = 0;
		private int higherHighs = 0;
		private int maxLowerLows = 0;
		private int maxHigherHighs = 0;

		private Brush brushUp0;
		private Brush brushUp1;
		private Brush brushUp2;
		private Brush brushDown0;
		private Brush brushDown1;
		private Brush brushDown2;
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
				AddDataSeries(BarsPeriodType.Minute, 20);
				emaShort = EMA(10);
				emaLong = EMA(20);
				movingAverage = SMA(100);
				chop = ChoppinessIndex(7);
				market = MarketCycle();
				marketLong = MarketCycle(BarsArray[1]);
				pa = PriceActionUtils();

				Signals = new Series<TrendDirection>(this, MaximumBarsLookBack.Infinite);

				configureBrushes();
			}
			#endregion
		}
		#endregion

		#region OnBarUpdate()
		protected override void OnBarUpdate()
		{
			if (CurrentBar < 100 || CurrentBars[0] < 1 || CurrentBars[1] < 1) {
				return;
            }

			if (BarsInProgress > 0)
			{
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

//			BackBrush = GetBackgroundBrush();
		}
		#endregion

		#region GetSignal()
		private TrendDirection GetSignal()
		{
			bool emaShortRising = pa.IsRising(emaShort, 0, 1);
			bool emaShortFalling = pa.IsFalling(emaShort, 0, 1);
			bool emaLongRising = pa.IsRising(emaLong, 0, 1);
			bool emaLongFalling	= pa.IsFalling(emaLong, 0, 1);
			bool maRising = emaShortRising && emaLongRising;
			bool maFalling = emaShortFalling && emaLongFalling;

			bool aboveMA = Close[0] > movingAverage[0];
			bool belowMA = Close[0] < movingAverage[0];

//			bool lowChop = (chop[0] > 20) && (chop[0] < 40);
//			bool highChop = (chop[0] > 60) && (chop[0] < 80);
//			bool validChoppiness = lowChop || highChop;

//			bool lowChop = chop[0] < 38.2;
//			bool highChop = chop[0] > 61.8;

//			bool rising	= pa.ConsecutiveBarsUp(3, 1);
//			bool falling = pa.ConsecutiveBarsDown(3, 1);

			bool rising = pa.LeastBarsUp(2, 4, 1);
			bool falling = pa.LeastBarsDown(2, 4, 1);

			bool newHigh = High[0] >= pa.HighestHigh(1, 5);
			bool newLow	= Low[0] <= pa.LowestLow(1, 5);

			bool higherHigh	= pa.ConsecutiveHigherHighs(0, 3);
			bool lowerLow = pa.ConsecutiveLowerLows(0, 3);

			bool highestInTrend	= pa.HighestHigh(0, 2) >= pa.HighestHigh(0, 5);
			bool lowestInTrend = pa.LowestLow(0, 2) <= pa.LowestLow(0, 5);

			int consecutiveHigherHighs = pa.MaxNumberOfConsecutiveHigherHighs(0, 20);
			int consecutiveLowerLows = pa.MaxNumberOfConsecutiveLowerLows(0, 20);

			higherHighs = consecutiveHigherHighs;
			lowerLows = consecutiveLowerLows;
			maxHigherHighs = Math.Max(maxHigherHighs, higherHighs);
			maxLowerLows = Math.Max(maxLowerLows, lowerLows);

			MarketCycleStage stage = market.Stage[0];
			MarketCycleStage stageLong = marketLong.Stage[0];

			bool validLongMarket = stageLong == MarketCycleStage.BroadChannel || stageLong == MarketCycleStage.TightChannel;
			bool validShortMarket = stage != MarketCycleStage.Breakout;

			bool validMarketDirection = market.Direction[0] == marketLong.Direction[0];

			bool longPatternMatched = true
				&& chop[0] < 40
//				&& lowChop
//				&& highChop
//				&& (lowChop || highChop)
//				&& validChoppiness
//				&& validLongMarket
//				&& validShortMarket
//				&& validMarket
//				&& validMarketDirection
//				&& emaLongRising
//				&& emaShortRising
				&& maRising
//				&& aboveMA
				&& pa.IsBullishBar(0)
				&& rising
//				&& newHigh
//				&& higherHigh
//				&& highestInTrend
//				&& market.Direction[0] == TrendDirection.Bullish
//				&& marketLong.Direction[0] == TrendDirection.Bullish
//				&& stage != MarketCycleStage.TradingRange
			;

			bool shortPatternMatched = true
				&& chop[0] < 40
//				&& lowChop
//				&& highChop
//				&& (lowChop || highChop)
//				&& validChoppiness
//				&& validLongMarket
//				&& validShortMarket
//				&& validMarket
//				&& validMarketDirection
//				&& emaLongFalling
//				&& emaShortFalling
				&& maFalling
//				&& belowMA
				&& pa.IsBearishBar(0)
				&& falling
//				&& newLow
//				&& lowerLow
//				&& lowestInTrend
//				&& market.Direction[0] == TrendDirection.Bearish
//				&& marketLong.Direction[0] == TrendDirection.Bearish
//				&& stage != MarketCycleStage.TradingRange
			;

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

		#region GetBackgroundBrush()
		private Brush GetBackgroundBrush()
		{
			if (market.Direction[0] == TrendDirection.Flat)
				return null;

			if (market.Stage[0] == MarketCycleStage.TradingRange)
				return null;

			if (market.Direction[0] == TrendDirection.Bullish)
			{
				if (market.Stage[0] == MarketCycleStage.Breakout)
					return brushUp0;

				if (market.Stage[0] == MarketCycleStage.TightChannel)
					return brushUp1;

				if (market.Stage[0] == MarketCycleStage.BroadChannel)
					return brushUp2;
			}

			if (market.Stage[0] == MarketCycleStage.Breakout)
					return brushDown0;

			if (market.Stage[0] == MarketCycleStage.TightChannel)
				return brushDown1;

			if (market.Stage[0] == MarketCycleStage.BroadChannel)
				return brushDown2;

			return null;

		}
		#endregion

		#region configureBrushes()
		private void configureBrushes()
		{
				brushUp0 = Brushes.Green.Clone();
				brushUp0.Opacity = 0.600;
				brushUp0.Freeze();

				brushUp1 = Brushes.Green.Clone();
				brushUp1.Opacity = 0.400;
				brushUp1.Freeze();

				brushUp2 = Brushes.Green.Clone();
				brushUp2.Opacity = 0.200;
				brushUp2.Freeze();

				brushDown0 = Brushes.Red.Clone();
				brushDown0.Opacity = 0.600;
				brushDown0.Freeze();

				brushDown1 = Brushes.Red.Clone();
				brushDown1.Opacity = 0.400;
				brushDown1.Freeze();

				brushDown2 = Brushes.Red.Clone();
				brushDown2.Opacity = 0.200;
				brushDown2.Freeze();
		}
		#endregion

		#region properties

//		[NinjaScriptProperty]
//		[Display(Name="Display Market Cycle", Description="Display Market Cycle", Order=0, GroupName="Parameters")]
//		public bool DisplayMarketCycle
//		{ get; set; }

		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private PR.Strategy208Signals[] cacheStrategy208Signals;
		public PR.Strategy208Signals Strategy208Signals()
		{
			return Strategy208Signals(Input);
		}

		public PR.Strategy208Signals Strategy208Signals(ISeries<double> input)
		{
			if (cacheStrategy208Signals != null)
				for (int idx = 0; idx < cacheStrategy208Signals.Length; idx++)
					if (cacheStrategy208Signals[idx] != null &&  cacheStrategy208Signals[idx].EqualsInput(input))
						return cacheStrategy208Signals[idx];
			return CacheIndicator<PR.Strategy208Signals>(new PR.Strategy208Signals(), input, ref cacheStrategy208Signals);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.PR.Strategy208Signals Strategy208Signals()
		{
			return indicator.Strategy208Signals(Input);
		}

		public Indicators.PR.Strategy208Signals Strategy208Signals(ISeries<double> input )
		{
			return indicator.Strategy208Signals(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.PR.Strategy208Signals Strategy208Signals()
		{
			return indicator.Strategy208Signals(Input);
		}

		public Indicators.PR.Strategy208Signals Strategy208Signals(ISeries<double> input )
		{
			return indicator.Strategy208Signals(input);
		}
	}
}

#endregion
