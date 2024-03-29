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
		private bool isTrendSet = false;
		private int WindowSize = 81;

		private List<TrendDirection> legDirections		= new List<TrendDirection>();
		private List<int> legIndexes					= new List<int>(); // Absolute References
		private List<int> legLengths 					= new List<int>();
		private	List<double> legChanges 				= new List<double>();

		private List<TrendDirection> channelDirections 	= new List<TrendDirection>();
		private List<double> channelDirectionStdDevs 	= new List<double>();
		private List<int> channelDirectionIdxs 			= new List<int>();
		private List<int> channelDirectionLengths 		= new List<int>();

		private int numberOfTrendDirectionalLegsInChannel 				= 0;
		private int numberOfCounterDirectionalLegsInChannel 			= 0;
		private	int numberOfDirectionalLegsInChannel					= 0;
		private int numberOfConsecutiveCounterDirectionalLegsInChannel	= 0;

		private TrendDirection previousDirectionalLegDirection 	= TrendDirection.Flat;
		private double previousDirectionalLegChange 			= 0;
		private int previousDirectionalLegLength 				= 0;

		private TrendDirection previousLegDirection 	= TrendDirection.Flat;
		private int previousLegLength 					= 0;
		private double previousLegChange 				= 0;
		private	TrendDirection currentChannelDirection 	= TrendDirection.Flat;
		private	int currentChannelLength 				= 0;
		private	double currentChannelChange 			= 0;
		private	TrendDirection currentLegDirection 		= TrendDirection.Flat;
		private	int currentLegLength 					= 0;
		private	double currentLegChange 				= 0;
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

			int currentTrendLength		= LegShort.BarsAgoStarts[0];

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

			if (!isTrendSet)
			{
				SetTrend();
			}

			BackBrush = null;
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

		#region SetTrend()
		// initialize the trend tracking variables based on the data in the latest window
		private void SetTrend()
		{
			// evaluate in descending order - chronological
			// the identified legs will be stored in lists in ascending order
			for (int i = WindowSize - 1; i >= 0; i--) {
				EvaluateChangesInLegDirection(i);
			}

			InitializeChannelList(WindowSize - 1);

			// evaluate identified legs in ascending order - chronological
			for (int i = 0; i < legDirections.Count; i++) {
				EvaluateLegInContextOfTheCurrentChannel(i);
			}

			if (channelDirections.Count == 0) {
				return;
			}

			// Populate values for channel standard deviation list
			for (int i = 0; i < channelDirections.Count; i++) {
				AddChannelStdDev(i);
			}

			// sort channels into tight and broad channels
			for (int i = WindowSize - 1; i >= 0; i--) {
				SetChannelType(i);
			}

			isTrendSet = true;
		}
		#endregion

		#region EvaluateChangesInLegDirection()
		// Add information for new legs identified at `barsAgo` before before the `CurrentBar`
		private void EvaluateChangesInLegDirection(int barsAgo = 0)
		{
			if (legIndexes.Count > 0 && legIndexes.Last() == barsAgo) {
				return;
			}

			int idx 						= barsAgo + 1;
			TrendDirection barsAgoDirection	= Utls.DirectionFromInt((int) LegShort[barsAgo]);
			TrendDirection legDirection 	= Utls.DirectionFromInt((int) LegShort[idx]);
			TrendDirection longLegDirection = Utls.DirectionFromInt((int) LegLong[idx]);

			if (longLegDirection != TrendDirection.Flat && longLegDirection != legDirection) {
				legDirection = TrendDirection.Flat;
			}

			if (barsAgoDirection == legDirection) {
				return;
			}

			int length 		= LegShort.BarsAgoStarts[idx];
			double change	= legDirection == TrendDirection.Bullish ? High[idx] - Low[length]
								: legDirection == TrendDirection.Bearish ? Low[idx] - High[length]
								: (double) Close[idx] - Close[length];

			legDirections.Add(legDirection);
			legIndexes.Add(CurrentBar - barsAgo); // Absolute Reference
			legLengths.Add(length);
			legChanges.Add(change);
		}
		#endregion

		#region InitializeChannelList()
		// Set initial channel direction based on first leg available
		private void InitializeChannelList(int legIndex)
		{
			currentLegDirection 	= legDirections[legIndex];
			currentLegLength 		= legLengths[legIndex];
			currentLegChange		= legChanges[legIndex];
			currentChannelDirection = legDirections[legIndex];
			currentChannelLength	= legLengths[legIndex];
			currentChannelChange	= legChanges[legIndex];

			if (legDirections[legIndex] != TrendDirection.Flat) {
				numberOfDirectionalLegsInChannel 		= 1;
				numberOfTrendDirectionalLegsInChannel 	= 1;
			}

			channelDirections.Add(currentChannelDirection);
			channelDirectionIdxs.Add(legIndexes[legIndex]);
			channelDirectionLengths.Add(currentChannelLength);
		}
		#endregion

		#region EvaluateLegInContextOfTheCurrentChannel()
		private void EvaluateLegInContextOfTheCurrentChannel(int legIndex)
		{
			// if trading range is 20 or more bars, the channel becomes trading range, so previous direction does not matter
			if (legDirections[legIndex] == TrendDirection.Flat && legLengths[legIndex] >= 20) {
				SetChannelToTradingRange(legIndex);
				return;
			}

			IncrementChannelList(legIndex);

			// if in trading range - channel stays the same
			if (legDirections[legIndex] == TrendDirection.Flat) {
				AddTradingRangeLeg(legIndex);
				return;
			}

			// if trend-direction leg - channel stays the same
			if (legDirections[legIndex] == currentChannelDirection) {
				AddTrendDirectionLeg(legIndex);
				return;
			}

			AddCounterTrendDirectionLeg(legIndex);
		}
		#endregion

		#region SetChannelToTradingRange()
		// add trading range entry to channel lists
		private void SetChannelToTradingRange(int legIndex)
		{
			currentLegDirection 	= TrendDirection.Flat;
			currentLegLength		= legLengths[legIndex];
			currentChannelDirection	= TrendDirection.Flat;
			currentChannelLength	= legLengths[legIndex];
			previousLegDirection	= TrendDirection.Flat;
			previousLegLength		= 0;

			previousDirectionalLegDirection = TrendDirection.Flat;
			previousDirectionalLegChange 	= 0;
			previousDirectionalLegLength 	= 0;

			numberOfDirectionalLegsInChannel 					= 0;
			numberOfCounterDirectionalLegsInChannel 			= 0;
			numberOfConsecutiveCounterDirectionalLegsInChannel	= 0;
			numberOfTrendDirectionalLegsInChannel				= 0;

			channelDirections.Add(0);
			channelDirectionIdxs.Add(legIndexes[legIndex]);
			channelDirectionLengths.Add(currentChannelLength);
		}
		#endregion

		#region IncrementChannelList()
		// update "previous" list values and current channel values
		private void IncrementChannelList(int legIndex)
		{
			previousLegDirection 	= currentLegDirection;
			previousLegLength 		= currentLegLength;
			previousLegChange 		= currentLegChange;

			if (previousLegDirection != TrendDirection.Flat) {
				previousDirectionalLegDirection = previousLegDirection;
				previousDirectionalLegChange	= previousLegChange;
				previousDirectionalLegLength	= previousLegLength;
			}

			currentLegDirection 	= legDirections[legIndex];
			currentLegLength 		= legLengths[legIndex];
			currentLegChange		= legChanges[legIndex];
		}
		#endregion

		#region AddTradingRangeLeg()
		// update values for a tranding range leg
		private void AddTradingRangeLeg(int legIndex)
		{
			currentChannelLength += legLengths[legIndex];
			currentChannelChange += legChanges[legIndex];

			channelDirections.Add(legDirections[legIndex]);
			channelDirectionIdxs.Add(legIndexes[legIndex]);
			channelDirectionLengths.Add(currentChannelLength);
		}
		#endregion

		#region AddTrendDirectionLeg()
		// update values for a leg in to direction of the current trend
		private void AddTrendDirectionLeg(int legIndex)
		{
			currentChannelLength += legLengths[legIndex];
			currentChannelChange += legChanges[legIndex];

			numberOfDirectionalLegsInChannel					+= 1;
			numberOfConsecutiveCounterDirectionalLegsInChannel 	= 0;
			numberOfTrendDirectionalLegsInChannel				+= 1;

			channelDirections.Add(legDirections[legIndex]);
			channelDirectionIdxs.Add(legIndexes[legIndex]);
			channelDirectionLengths.Add(currentChannelLength);
		}
		#endregion

		#region AddCounterTrendDirectionLeg()
		// update values for a Counter-trend leg
		private void AddCounterTrendDirectionLeg(int legIndex)
		{
			numberOfDirectionalLegsInChannel 		+= 1;
			numberOfCounterDirectionalLegsInChannel += 1;

			if (currentLegDirection == previousDirectionalLegDirection) {
				numberOfConsecutiveCounterDirectionalLegsInChannel++;
			} else {
				numberOfConsecutiveCounterDirectionalLegsInChannel = 1;
			}

			if (DoesLegReverseChannel()) {
				currentChannelLength = numberOfConsecutiveCounterDirectionalLegsInChannel;
				currentChannelChange = legChanges[legIndex];

				numberOfDirectionalLegsInChannel					= currentChannelLength;
				numberOfTrendDirectionalLegsInChannel				= currentChannelLength;
				numberOfConsecutiveCounterDirectionalLegsInChannel	= 0;
				numberOfCounterDirectionalLegsInChannel				= 0;

				channelDirections.Add(legDirections[legIndex]);
				channelDirectionIdxs.Add(legIndexes[legIndex]);
				channelDirectionLengths.Add(currentChannelLength);

				return;
			}

			currentChannelLength += legLengths[legIndex];
			currentChannelChange += legChanges[legIndex];
		}
		#endregion

		#region DoesLegReverseChannel()
		private bool DoesLegReverseChannel()
		{
			// if has more legs than most recent direction - channel reverses
			if (numberOfCounterDirectionalLegsInChannel > numberOfTrendDirectionalLegsInChannel) {
				return true;
			}

			// if reverses most recent direction (greater change) - channel reverses
			if (currentLegChange > previousDirectionalLegChange && previousDirectionalLegDirection != currentLegDirection) {
				return true;
			}

			// if reverses direction (greater change) - channel reverses
			if (currentLegChange > currentChannelChange) {
				return true;
			}

			// if has more bars that most recent direction - channel reverses
			if (currentLegLength > previousDirectionalLegLength && previousDirectionalLegDirection != currentLegDirection) {
				return true;
			}

			// if two consecutive countertrend legs - channel reverses
			if (previousDirectionalLegDirection == currentLegDirection) {
				return true;
			}

			return false;
		}
		#endregion

		#region AddChannelStdDev()
		// add values for standard deviation for channels
		private void AddChannelStdDev(int channelIndex)
		{
			int barsAgo 	= CurrentBar - channelDirectionIdxs[channelIndex];
			int length 		= channelDirectionLengths[channelIndex];
			double stdDev	= LegShort.CalculateLegLengthStandardDeviation(barsAgo, length);
			channelDirectionStdDevs.Add(stdDev);
		}
		#endregion

		#region SetChannelType()
		// distinguish between tight and broad channels
		private void SetChannelType(int index)
		{
			SMA stdDevSMA 	= SMA(LegShort.LegLengthStandardDeviations, 10);
			int legIndex 	= CurrentBar - index;

			for (int idx = 0; idx < channelDirectionIdxs.Count; idx++) {
				int channelIndex = channelDirectionIdxs[idx];

				if (legIndex < channelIndex) {
					continue;
				}

				double stdDevReference = stdDevSMA[channelIndex];

				if (channelDirectionStdDevs[idx] > stdDevReference) {
					BroadChannels[index] = channelDirections[idx];
				} else {
					TightChannels[index] = channelDirections[idx];
				}

				break;
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
