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
		private Utils Utls;
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
				Utls		= new Utils();
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

			for (int i = 0; i < WindowSize; i++) {
				BackBrushes[i] =
					TightChannels[i] == TrendDirection.Bullish ? brushUp1
					: TightChannels[i] == TrendDirection.Bearish ? brushDown1
					: BroadChannels[i] == TrendDirection.Bullish ? brushUp2
					: BroadChannels[i] == TrendDirection.Bearish ? brushDown2
					: null;
			}
		}
		#endregion

		#region SetChannels()
		private void SetChannels() {
//			List<int> bullishIdx	 			= new List<int>();
//			List<int> bearishIdx	 			= new List<int>();
//			List<int> tradingRangeIdx	 		= new List<int>();

			List<int> legLongDirections 		= new List<int>();
			List<int> legDirections 			= new List<int>();
			List<int> legIndexes	 			= new List<int>();

			List<int> legLengths 				= new List<int>();
			List<double> legChanges 			= new List<double>();
			List<double> legLengthStdDevs		= new List<double>();

//			List<int> bullishLegLengths 		= new List<int>();
//			List<double> bullishLegChanges 		= new List<double>();
//			List<double> bullishLegStdDevs 		= new List<double>();

//			List<int> bearishLegLengths 		= new List<int>();
//			List<double> bearishLegChanges 		= new List<double>();
//			List<double> bearishLegStdDevs 		= new List<double>();

//			List<int> tradingRangeLegLengths 	= new List<int>();
//			List<double> tradingRangeLegChanges = new List<double>();
//			List<double> tradingRangeLegStdDevs	= new List<double>();


			for (int i = WindowSize - 1; i >= 0; i--) {
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

//				if (lDir > 0) {
//					bullishLegLengths.Add(length);
//					bullishLegChanges.Add(change);
//					bullishLegStdDevs.Add(stdDev);
//				}

//				if (lDir < 0) {
//					bearishLegLengths.Add(length);
//					bearishLegChanges.Add(change);
//					bearishLegStdDevs.Add(stdDev);
//				}

//				if (lDir == 0) {
//					tradingRangeLegLengths.Add(length);
//					tradingRangeLegChanges.Add(change);
//					tradingRangeLegStdDevs.Add(stdDev);
//				}

//				bullishIdx.Add(bullishLegLengths.Count - 1);
//				bearishIdx.Add(bearishLegLengths.Count - 1);
//				tradingRangeIdx.Add(tradingRangeLegLengths.Count - 1);

				legLongDirections.Add(longLDir);
				legDirections.Add(lDir);
				legIndexes.Add(i);

				legLengths.Add(length);
				legChanges.Add(change);
				legLengthStdDevs.Add(stdDev);
			}

			if (legIndexes.Count == 0) {
				return;
			}

			List<int> channelDirections 			= new List<int>();
			List<double> channelDirectionStdDevs 	= new List<double>();
			List<int> channelDirectionIdxs 			= new List<int>();
			List<int> channelDirectionLengths 		= new List<int>();

			int previousDirectionalLegDirection = 0;
			double previousDirectionalLegChange = 0;
			int previousDirectionalLegLength = 0;
			int previousLegDirection = 0;
			int previousLegLength = 0;
			double previousLegChange = 0;
			int counterDirectionalLegsInCurrentDirection = 0;
			int consecutiveCounterDirectionalLegsInCurrentDirection = 0;
			int directionalLegsInCurrentDirection = 0;
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

					if (legDirections[i] != 0) {
						directionalLegsInCurrentDirection = 1;
					}

					channelDirections.Add(currentDirection);
					channelDirectionIdxs.Add(legIndexes[i]);
					channelDirectionLengths.Add(currentDirectionLength);
					continue;
				}

				// if trading range > 20 - becomes trading range, previous direction does not matter
				if (legDirections[i] == 0 && legLengths[i] >= 20) {
					currentLegDirection 				= 0;
					currentLegLength 					= legLengths[i];
					currentDirectionLength 				= legLengths[i];
					currentDirection 					= 0;
					previousLegDirection 				= 0;
					previousLegLength					= 0;
					previousDirectionalLegDirection		= 0;

					directionalLegsInCurrentDirection			= 0;
					counterDirectionalLegsInCurrentDirection 	= 0;

					consecutiveCounterDirectionalLegsInCurrentDirection 	= 0;

					channelDirections.Add(0);
					channelDirectionIdxs.Add(legIndexes[i]);
					channelDirectionLengths.Add(currentDirectionLength);

					continue;
				}

				previousLegDirection 	= currentLegDirection;
				previousLegLength 		= currentLegLength;
				previousLegChange 		= currentLegChange;

				if (previousLegDirection != 0) {
					previousDirectionalLegDirection = previousLegDirection;
					previousDirectionalLegChange	= previousLegChange;
					previousDirectionalLegLength	= previousLegLength;
				}

				currentLegDirection 	= legDirections[i];
				currentLegLength 		= legLengths[i];
				currentLegChange		= legChanges[i];

				// if in trading range - channel stays the same
				if (legDirections[i] == 0) {
					currentDirectionLength += legLengths[i];
					currentDirectionChange += legChanges[i];

					channelDirections.Add(legDirections[i]);
					channelDirectionIdxs.Add(legIndexes[i]);
					channelDirectionLengths.Add(currentDirectionLength);

					continue;
				}

				// if in same direction - channel stays the same
				if (legDirections[i] == currentDirection) {
					currentDirectionLength += legLengths[i];
					currentDirectionChange += legChanges[i];

					directionalLegsInCurrentDirection	+= 1;
					consecutiveCounterDirectionalLegsInCurrentDirection = 0;

					channelDirections.Add(legDirections[i]);
					channelDirectionIdxs.Add(legIndexes[i]);
					channelDirectionLengths.Add(currentDirectionLength);

					continue;
				}

				// Counter-trend leg
				counterDirectionalLegsInCurrentDirection 	+= 1;

				if (currentLegDirection == previousDirectionalLegDirection) {
					consecutiveCounterDirectionalLegsInCurrentDirection++;
				} else {
					consecutiveCounterDirectionalLegsInCurrentDirection = 1;
				}

				bool reversed = false;

				// if has more legs than most recent direction - channel reverses
				if (counterDirectionalLegsInCurrentDirection > directionalLegsInCurrentDirection) {
					reversed = true;
				}

				// if reverses most recent direction (greater change) - channel reverses
				if (currentLegChange > previousDirectionalLegChange && previousDirectionalLegDirection != currentLegDirection) {
					reversed = true;
				}

				// if reverses direction (greater change) - channel reverses
				if (currentLegChange > currentDirectionChange) {
					reversed = true;
				}

				// if has more bars that most recent direction - channel reverses
				if (currentLegLength > previousDirectionalLegLength && previousDirectionalLegDirection != currentLegDirection) {
					reversed = true;
				}

				// if two consecutive countertrend legs - channel reverses
				if (previousDirectionalLegDirection == currentLegDirection) {
					reversed = true;
				}

				if (reversed) {
					currentDirectionLength = consecutiveCounterDirectionalLegsInCurrentDirection;
					currentDirectionChange = legChanges[i];

					directionalLegsInCurrentDirection	= 1;

					channelDirections.Add(legDirections[i]);
					channelDirectionIdxs.Add(legIndexes[i]);
					channelDirectionLengths.Add(currentDirectionLength);

					continue;
				}

				currentDirectionLength += legLengths[i];
				currentDirectionChange += legChanges[i];
			}

			if (channelDirectionIdxs.Count == 0) {
				return;
			}

			SMA stdDevSMA = SMA(LegShort.LegLengthStandardDeviations, 10);

			for (int i = 0; i < channelDirections.Count; i++) {
				channelDirectionStdDevs.Add(LegShort.CalculateLegLengthStandardDeviation(channelDirectionIdxs[i], channelDirectionLengths[i]));
			}

			for (int i = 0; i < legIndexes.Count; i++) {
				int barIndex = legIndexes[i];

				for (int idx = 0; idx < channelDirectionIdxs.Count; idx++) {
					int channelIndex = channelDirectionIdxs[idx];

					if (i < channelIndex) {
						continue;
					}

					double stdDevReference = stdDevSMA[channelIndex];
					if (channelDirectionStdDevs[idx] > stdDevReference) {
						BroadChannels[barIndex] = Utls.DirectionFromInt(channelDirections[idx]);
					} else {
						TightChannels[barIndex] = Utls.DirectionFromInt(channelDirections[idx]);
					}

					break;
				}
			}
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
