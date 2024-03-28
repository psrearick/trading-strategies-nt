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
	public class MarketDirection : Indicator
	{
		#region Variables
		private PriceActionUtils PA;
		private Legs LegShort;
		private Legs LegLong;
		private Series<TrendDirection> Direction;
		private Series<TrendDirection> BreakoutDirection;
		private Series<TrendDirection> TightChannels;
		private Series<TrendDirection> BroadChannels;
		private Brush brushUp1;
		private Brush brushUp2;
		private Brush brushDown1;
		private Brush brushDown2;
//		private bool hasBullishTrend;
//		private bool hasBearishTrend;
//		private Ray bullishTrendline;
//		private Ray bullishTrendChannelLine;
//		private Ray bearishTrendline;
//		private Ray bearishTrendChannelLine;
		int WindowSize = 81;
		#endregion

		#region OnStateChange()
		protected override void OnStateChange()
		{
			#region State.SetDefaults
			if (State == State.SetDefaults)
			{
				Description									= @"";
				Name										= "Market Direction";
				Calculate									= Calculate.OnBarClose;
				IsOverlay									= true;
				DisplayInDataBox							= true;
				DrawOnPricePanel							= true;
				DrawHorizontalGridLines						= true;
				DrawVerticalGridLines						= true;
				PaintPriceMarkers							= true;
				ScaleJustification							= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				IsSuspendedWhileInactive					= true;

//				brushUp1 = Brushes.Green.Clone();
//				brushUp1.Opacity = 1.000;
//				brushUp1.Freeze();

//				brushUp2 = Brushes.Green.Clone();
//				brushUp2.Opacity = 0.600;
//				brushUp2.Freeze();

//				brushDown1 = Brushes.Red.Clone();
//				brushDown1.Opacity = 1.000;
//				brushDown1.Freeze();

//				brushDown2 = Brushes.Red.Clone();
//				brushDown2.Opacity = 0.600;
//				brushDown2.Freeze();

				brushUp1 = Brushes.Green.Clone();
				brushUp1.Opacity = 0.500;
				brushUp1.Freeze();

				brushUp2 = Brushes.Green.Clone();
				brushUp2.Opacity = 0.250;
				brushUp2.Freeze();

				brushDown1 = Brushes.Red.Clone();
				brushDown1.Opacity = 0.500;
				brushDown1.Freeze();

				brushDown2 = Brushes.Red.Clone();
				brushDown2.Opacity = 0.250;
				brushDown2.Freeze();
			}
			#endregion

			#region State.Configure
			else if (State == State.Configure)
			{
				LegShort	= Legs(6);
				LegLong		= Legs(20);
				PA			= PriceActionUtils();
			}
			#endregion

			#region State.DataLoaded
			else if (State == State.DataLoaded)
			{
				Direction 			= new Series<TrendDirection>(this, MaximumBarsLookBack.Infinite);
				BreakoutDirection 	= new Series<TrendDirection>(this, MaximumBarsLookBack.Infinite);
				TightChannels 		= new Series<TrendDirection>(this, MaximumBarsLookBack.Infinite);
				BroadChannels 		= new Series<TrendDirection>(this, MaximumBarsLookBack.Infinite);
			}
			#endregion
		}
		#endregion

		#region OnBarUpdate()
		protected override void OnBarUpdate()
		{
			if (CurrentBar < WindowSize) {
				Direction[0] = TrendDirection.Flat;
				return;
			}

			BackBrush = null;

			double stdDev 				= LegLong.LegLengthStandardDeviations[0];
			SMA StdDevSMA				= SMA(LegLong.LegLengthStandardDeviations, 10);
			double legLengthAvg			= LegLong.AverageLegLengths[0];
			SMA legLengthSMA 			= SMA(LegLong.AverageLegLengths, 10);
			double legDir				= LegLong.LegDirectionRatios[0];

			bool longLegs 				= legLengthAvg > legLengthSMA[0];
//			bool highStdDev				= stdDev < 0.1;
//			bool lowStdDev				= stdDev >= 0.1;
			bool highStdDev				= stdDev < StdDevSMA[0];
			bool lowStdDev				= stdDev >= StdDevSMA[0];

			bool tightChannelLegs		= lowStdDev && longLegs;
			bool broadChannelLegs		= highStdDev && longLegs;
			bool longerUpLegs			= legDir > 0.5;
			bool longerDownLegs			= legDir <= 0.5;

			int currentTrendLength		= LegShort.BarsAgoStarts[0];
			int currentLongTrendLength	= LegLong.BarsAgoStarts[0];

			TrendDirection currentLongDirection	= LegLong[0] > 0 ? TrendDirection.Bullish : LegLong[0] < 0 ? TrendDirection.Bearish : TrendDirection.Flat;

//			int lastBarOfLongTrend			= currentLongTrendLength + 1;
//			int previousLongTrendLength		= LegLong.BarsAgoStarts[lastBarOfLongTrend];
//			int firstBarOfPreviousLongTrend	= currentLongTrendLength + previousLongTrendLength;

			bool isBreakout = PA.IsBreakoutTrend(0, currentTrendLength);

			if (isBreakout) {
				for (int i = 0; i < currentTrendLength; i++) {
					BreakoutDirection[i] = LegShort[0] > 0
						? TrendDirection.Bullish
						: LegShort[0] < 0
							? TrendDirection.Bearish
							: TrendDirection.Flat;
				}
			}

			SetChannels();



//				BackBrush = LegLong[0] > 0 ? brushUp2
//							: LegLong[0] < 0 ? brushDown2
//							: null;
//			if (highStdDev) {

////			Print(legDir);

//				BackBrush = legDir > 0.5 ? brushUp2
//							: legDir <= 0.5 ? brushDown2
//							: null;




//			}

//			double pullbackSize = PA.LargestPullbackInTrend(0, CurrentLongTrendLength, currentLongDirection);
//			double range 		= MAX(High, CurrentLongTrendLength)[0] - MIN(Low, CurrentLongTrendLength)[0];

//			if (broadChannelLegs && PA.IsBroadChannel(0, currentLongTrendLength, currentLongDirection)) {
//				for (int i = 0; i < currentLongTrendLength; i++) {
//					BroadChannels[i] = LegLong[0] > 0
//						? TrendDirection.Bullish
//						: LegLong[0] < 0
//							? TrendDirection.Bearish
//							: TrendDirection.Flat;

//					if (BroadChannels[i] == TrendDirection.Bullish) {
//						BackBrushes[i] = brushUp2;
//					}

//					if (BroadChannels[i] == TrendDirection.Bearish) {
//						BackBrushes[i] = brushDown2;
//					}
//				}
//			}

//			if (tightChannelLegs && PA.IsTightChannel(0, currentLongTrendLength, currentLongDirection)) {
//				Print(currentLongDirection);
//				for (int i = 0; i < currentLongTrendLength; i++) {
//					TightChannels[i] = LegLong[0] > 0
//						? TrendDirection.Bullish
//						: LegLong[0] < 0
//							? TrendDirection.Bearish
//							: TrendDirection.Flat;

//					if (TightChannels[i] == TrendDirection.Bullish) {
//						BackBrushes[i] = brushUp1;
//					}

//					if (TightChannels[i] == TrendDirection.Bearish) {
//						BackBrushes[i] = brushDown1;
//					}
//				}
//			}

//			if (broadChannelLegs ||tightChannelLegs && (pullbackSize >= 0.67)) {}


//			double longDirection = LegShort.CalculateLegDirectionRatioForPeriod(0, currentLongTrendLength);

//			for (int i = 0; i < currentLongTrendLength; i++) {
//				if (longDirection > 0.5 && LegLong[0] > 0) {
//					BackBrush = brushUp1;
//				}

//				if (longDirection < 0.5 && LegLong[0] < 0) {
//					BackBrush = brushDown1;
//				}
//			}

//			double previousLongDirection = LegShort.CalculateLegDirectionRatioForPeriod(lastBarOfLongTrend, firstBarOfPreviousLongTrend);
//			for (int i = lastBarOfLongTrend; i <= firstBarOfPreviousLongTrend; i++) {
//				if (longDirection > 0.5 && LegLong[0] > 0) {
//					BackBrush = brushUp1;
//				}

//				if (longDirection < 0.5 && LegLong[0] < 0) {
//					BackBrush = brushDown1;
//				}
//			}








//			int currentTrendLength	= LegShort.BarsAgoStarts[0];
//			int lastBarOfTrend		= currentTrendLength + 1;
//			int previousTrendLength	= LegShort.BarsAgoStarts[lastBarOfTrend];
//			int firstBarOfTrend		= currentTrendLength + previousTrendLength;

//			for (int i = lastBarOfTrend; i <= firstBarOfTrend; i++) {
//				Direction[i] 							= LegShort.LegDirectionAtBar(i);
//			}

//			if (Leg.Starts[0] != Leg.Starts[1] && Direction[firstBarOfTrend] != TrendDirection.Flat) {
//				Brush TrendBrush 			= Direction[firstBarOfTrend] == TrendDirection.Bullish ? brushUp1 : brushDown1;
//				Brush TrendChannelBrush 	= Direction[firstBarOfTrend] == TrendDirection.Bullish ? brushUp2 : brushDown2;
//				double startYHigh 			= High[firstBarOfTrend];
//				double endYHigh 			= High[lastBarOfTrend];
//				double startYLow			= Low[firstBarOfTrend];
//				double endYLow				= Low[lastBarOfTrend];

//				if (Direction[firstBarOfTrend] == TrendDirection.Bullish && hasBullishTrend) {
//					double previousStartYHigh	= bullishTrendChannelLine.StartAnchor.Price;
//					double previousStartYLow	= bullishTrendline.StartAnchor.Price;

//					if (previousStartYHigh < startYHigh && previousStartYLow < startYLow) {
//						bullishTrendChannelLine.EndAnchor.Price	= endYHigh;
//						bullishTrendline.StartAnchor.Price		= endYLow;

//						return;
//					}
//				} else if (Direction[firstBarOfTrend] == TrendDirection.Bullish) {

//					double trendStartY 			= Direction[firstBarOfTrend] == TrendDirection.Bullish ? startYLow : startYHigh;
//					double trendEndY 			= Direction[firstBarOfTrend] == TrendDirection.Bullish ? endYLow : endYHigh;
//					double trendChannelStartY 	= Direction[firstBarOfTrend] == TrendDirection.Bullish ? startYHigh : startYLow;
//					double trendChannelEndY		= Direction[firstBarOfTrend] == TrendDirection.Bullish ? endYHigh : endYLow;

//					bullishTrendline 		= Draw.Ray(this, "rayTrend" + (CurrentBar - lastBarOfTrend).ToString(), firstBarOfTrend, trendStartY, lastBarOfTrend, trendEndY, TrendBrush);
//					bullishTrendChannelLine = Draw.Ray(this, "rayTrendChannel" + (CurrentBar - lastBarOfTrend).ToString(), firstBarOfTrend, trendChannelStartY, lastBarOfTrend, trendChannelEndY, TrendChannelBrush);
//				} else if (hasBearishTrend) {
//					double previousStartYHigh	= bearishTrendline.StartAnchor.Price;
//					double previousStartYLow	= bearishTrendChannelLine.StartAnchor.Price;

//					if (previousStartYHigh > startYHigh && previousStartYLow > startYLow) {
//						bearishTrendline.StartAnchor.Price			= endYHigh;
//						bearishTrendChannelLine.StartAnchor.Price	= endYLow;

//						return;
//					}
//				} else {
//					double trendStartY 			= Direction[firstBarOfTrend] == TrendDirection.Bullish ? startYLow : startYHigh;
//					double trendEndY 			= Direction[firstBarOfTrend] == TrendDirection.Bullish ? endYLow : endYHigh;
//					double trendChannelStartY 	= Direction[firstBarOfTrend] == TrendDirection.Bullish ? startYHigh : startYLow;
//					double trendChannelEndY		= Direction[firstBarOfTrend] == TrendDirection.Bullish ? endYHigh : endYLow;
//					Draw.Ray(this, "rayTrend" + lastBarOfTrend, firstBarOfTrend, trendStartY, lastBarOfTrend, trendEndY, TrendBrush);
//					Draw.Ray(this, "rayTrendChannel" + lastBarOfTrend, firstBarOfTrend, trendChannelStartY, lastBarOfTrend, trendChannelEndY, TrendChannelBrush);
//				}
//			}
		}
		#endregion

		#region SetChannels()
		private bool SetChannels() {
			List<int> bullishIdx	 			= new List<int>();
			List<int> bearishIdx	 			= new List<int>();
			List<int> tradingRangeIdx	 		= new List<int>();

			List<int> legLongDirections 		= new List<int>();
			List<int> legDirections 			= new List<int>();
			List<int> legIndexes	 			= new List<int>();

			List<int> legLengths 				= new List<int>();
			List<double> legChanges 			= new List<double>();
			List<double> legLengthStdDevs		= new List<double>();

			List<int> bullishLegLengths 		= new List<int>();
			List<double> bullishLegChanges 		= new List<double>();
			List<double> bullishLegStdDevs 		= new List<double>();

			List<int> bearishLegLengths 		= new List<int>();
			List<double> bearishLegChanges 		= new List<double>();
			List<double> bearishLegStdDevs 		= new List<double>();

			List<int> tradingRangeLegLengths 	= new List<int>();
			List<double> tradingRangeLegChanges = new List<double>();
			List<double> tradingRangeLegStdDevs	= new List<double>();

			for (int i = WindowSize - 1; i >= 0; i++) {
				int idx 		= i + 1;
				Legs L 			= LegShort;
				int lDir 		= (int) L[idx];
				int longLDir	= (int) LegLong[idx];

				if (longLDir != 0 && longLDir != lDir) {
					lDir = 0;
				}

				if ((int) L[i] == lDir) {
					continue;
				}

				int length 		= L.BarsAgoStarts[idx];
				double change	= lDir > 0 ? High[idx] - Low[L.BarsAgoStarts[idx]]
									: lDir < 0 ? High[L.BarsAgoStarts[idx]] - Low[idx]
									:  Close[idx] - Close[L.BarsAgoStarts[idx]];
				double stdDev	= L.LegLengthStandardDeviations[idx];



				if (lDir > 0) {
					bullishLegLengths.Add(length);
					bullishLegChanges.Add(change);
					bullishLegStdDevs.Add(stdDev);
				}

				if (lDir < 0) {
					bearishLegLengths.Add(length);
					bearishLegChanges.Add(change);
					bearishLegStdDevs.Add(stdDev);
				}

				if (lDir == 0) {
					tradingRangeLegLengths.Add(length);
					tradingRangeLegChanges.Add(change);
					tradingRangeLegStdDevs.Add(stdDev);
				}

				bullishIdx.Add(bullishLegLengths.Count - 1);
				bearishIdx.Add(bearishLegLengths.Count - 1);
				tradingRangeIdx.Add(tradingRangeLegLengths.Count - 1);

				legLongDirections.Add(longLDir);
				legDirections.Add(lDir);
				legIndexes.Add(i);

				legLengths.Add(length);
				legChanges.Add(change);
				legLengthStdDevs.Add(stdDev);
			}

			List<int> channelDirections 	= new List<int>();
			List<int> channelDirectionIdxs 	= new List<int>();

			int previousDirectionalLegDirection = 0;
			int previousLegDirection = 0;
			int previousLegDirectionLength = 0;
			double previousLegChange = 0;
			int currentDirection = 0;
			int currentDirectionLength = 0;
			double currentDirectionChange = 0;
			int currentLegDirection = 0;
			int currentLegLength = 0;
			double currentLegChange = 0;

			for (int i = 0; i < legDirections.Count - 1; i++) {
				if (i == 0) {
					currentLegDirection 	= legDirections[i];
					currentLegLength 		=  legLengths[i];
					currentDirection 		= legDirections[i];
					currentDirectionLength	= legLengths[i];

					channelDirections.Add(currentDirection);
					channelDirectionIdxs.Add(legIndexes[i]);
					continue;
				}

				if (legDirections[i] == 0 && legLengths[i] >= 20) {
					currentLegDirection 			= 0;
					currentLegLength 				= legLengths[i];
					currentDirectionLength 			= legLengths[i];
					currentDirection 				= 0;
					previousLegDirection 			= 0;
					previousDirectionalLegDirection	= 0;

					channelDirections.Add(0);
					channelDirectionIdxs.Add(legIndexes[i]);

					continue;
				}

				if (legDirections[i] == 0) {
					currentDirectionLength += legLengths[i];
					currentLegDirection 	= legDirections[i];
					currentLegLength 		= legLengths[i];
					currentLegChange		= legChanges[i];

					channelDirections.Add(0);
					channelDirectionIdxs.Add(legIndexes[i]);

					continue;
				}

				if (legDirections[i] == currentDirection) {
					currentDirectionLength += legLengths[i];
					currentDirectionChange += legChanges[i];
					currentLegDirection 	= legDirections[i];
					currentLegLength 		= legLengths[i];
					currentLegChange		= legChanges[i];

					channelDirections.Add(0);
					channelDirectionIdxs.Add(legIndexes[i]);

					continue;
				}



				// if in same direction - channel stays the same
				// if in trading range - channel stays the same
				// if reverses most recent direction (greater change) - channel reverses
				// if has more bars that most recent direction - channel reverses
				// if has more legs than most recent direction - channel reverses
				// if trading range > 20 - becomes trading range, previous direction does not matter

			}

			return false;
		}
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private PR.MarketDirection[] cacheMarketDirection;
		public PR.MarketDirection MarketDirection()
		{
			return MarketDirection(Input);
		}

		public PR.MarketDirection MarketDirection(ISeries<double> input)
		{
			if (cacheMarketDirection != null)
				for (int idx = 0; idx < cacheMarketDirection.Length; idx++)
					if (cacheMarketDirection[idx] != null &&  cacheMarketDirection[idx].EqualsInput(input))
						return cacheMarketDirection[idx];
			return CacheIndicator<PR.MarketDirection>(new PR.MarketDirection(), input, ref cacheMarketDirection);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.PR.MarketDirection MarketDirection()
		{
			return indicator.MarketDirection(Input);
		}

		public Indicators.PR.MarketDirection MarketDirection(ISeries<double> input )
		{
			return indicator.MarketDirection(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.PR.MarketDirection MarketDirection()
		{
			return indicator.MarketDirection(Input);
		}

		public Indicators.PR.MarketDirection MarketDirection(ISeries<double> input )
		{
			return indicator.MarketDirection(input);
		}
	}
}

#endregion
