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
	public class PriceActionUtils : Indicator
	{
		#region Variables
		public ATR Atr;
		#endregion

		#region OnStateChange()
		protected override void OnStateChange()
		{
			#region State.SetDefaults
			if (State == State.SetDefaults)
			{
				Description									= @"Price Action indicators";
				Name										= "Price Action Utilities";
				Calculate									= Calculate.OnBarClose;
				IsOverlay									= true;
				DisplayInDataBox							= true;
				DrawOnPricePanel							= true;
				DrawHorizontalGridLines						= true;
				DrawVerticalGridLines						= true;
				PaintPriceMarkers							= true;
				ScaleJustification							= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				//Disable this property if your indicator requires custom values that cumulate with each new market data event.
				//See Help Guide for additional information.
				IsSuspendedWhileInactive					= true;

//				AddPlot(Brushes.DodgerBlue,	"Swing High");
//				AddPlot(Brushes.Red,		"Swing Low");
			}
			#endregion
			#region State.Configure
			else if (State == State.Configure)
			{
				Atr = ATR(14);
			}
			#endregion
			#region State.DataLoaded
			else if (State == State.DataLoaded)
			{
			}
			#endregion
		}
		#endregion

		#region OnBarUpdate()
		protected override void OnBarUpdate()
		{
		}
		#endregion

		#region BodySize()
		public double BodySize(int index)
		{
			return Close[index] - Open[index];
		}
		#endregion

		#region RealBodySize()
		public double RealBodySize(int index)
		{
			return Math.Abs(BodySize(index));
		}
		#endregion

		#region IsBig()
		public bool IsBig(int index)
		{
			return (RealBodySize(index) / (High[index] - Low[index])) > 0.75;
		}
		#endregion

		#region IsSmall()
		public bool IsSmall(int index)
		{
			return (RealBodySize(index) / (High[index] - Low[index])) < 0.25;
		}
		#endregion

		#region IsStrong()
		public bool IsStrong(int index)
		{
			return IsBig(index) && ClosedNearExtreme(index) && IsBigger(index);
		}
		#endregion

		#region IsBigger()
		public bool IsBigger(int index)
		{
			return RealBodySize(index) > RealBodySize(index + 1);
		}
		#endregion

		#region ClosedNearExtreme()
		public bool ClosedNearExtreme(int index)
		{
			if (Close[index] > Open[index]) {
				return ((Close[index] - Low[index]) / (High[index] - Low[index])) > 0.75;
			}

			return ((High[index] - Close[index]) / (High[index] - Low[index])) > 0.75;
		}
		#endregion

		#region IsSmaller()
		public bool IsSmaller(int index)
		{
			return RealBodySize(index) < RealBodySize(index + 1);
		}
		#endregion

		#region IsDown()
		public bool IsDown(int barsAgo)
		{
			return IsBearishBar(barsAgo);
		}
		#endregion

		#region IsUp()
		public bool IsUp(int barsAgo)
		{
			return IsBullishBar(barsAgo);
		}
		#endregion

		#region IsLargerThanAverage()
		public bool IsLargerThanAverage(int barsAgo)
		{
			return RealBodySize(barsAgo) > Atr[barsAgo];
		}
		#endregion

		#region IsSmallerThanAverage()
		public bool IsSmallerThanAverage(int barsAgo)
		{
			return RealBodySize(barsAgo) < Atr[barsAgo];
		}
		#endregion

		#region ConsecutiveBiggerBars()
		public bool ConsecutiveBiggerBars(int period, int index = 0)
		{
			bool biggerBars = true;

			for (int i = 0; i < period; i++) {
				if (!IsBigger(i + index)) {
					biggerBars = false;

					break;
				}
			}

			return biggerBars;
		}
		#endregion

		#region ConsecutiveSmallerBars()
		public bool ConsecutiveSmallerBars(int period, int index = 0)
		{
			bool smallerBars = true;

			for (int i = 0; i < period; i++) {
				if (!IsSmaller(i + index)) {
					smallerBars = false;

					break;
				}
			}

			return smallerBars;
		}
		#endregion

		#region ConsecutiveBarsUp()
		public bool ConsecutiveBarsUp(int period, int index = 0)
		{
			bool barsUp = true;

			for (int i = 0; i < period; i++) {
				if (!IsUp(i + index)) {
					barsUp = false;

					break;
				}
			}

			return barsUp;
		}
		#endregion

		#region ConsecutiveBarsDown()
		public bool ConsecutiveBarsDown(int period, int index = 0)
		{
			bool barsDown = true;

			for (int i = 0; i < period; i++) {
				if (!IsDown(i + index)) {
					barsDown = false;

					break;
				}
			}

			return barsDown;
		}
		#endregion

		#region LeastBarsDown()
		public bool LeastBarsDown(int count, int period, int index = 0)
		{
			int barsDown = 0;

			for (int i = 0; i < period; i++) {
				if (IsDown(i + index)) {
					barsDown = barsDown + 1;
				}
			}

			return barsDown >= count;
		}
		#endregion

		#region AverageBearBarSize()
		public double AverageBearBarSize(int barsAgo, int period)
		{
			double bearBarSize = 0;
			double bearBarCount = 0;
			int rangeMax = Math.Min((period + barsAgo), Close.Count);

			for (int i = barsAgo; i < rangeMax; i++) {
				if (IsBearishBar(i)) {
					bearBarCount++;
					bearBarSize = bearBarSize + RealBodySize(i);
				}
			}

			if (bearBarCount == 0) {
				return 0;
			}

			return bearBarSize / bearBarCount;
		}
		#endregion

		#region AverageBullBarSize()
		public double AverageBullBarSize(int barsAgo, int period)
		{
			double bullBarSize = 0;
			double bullBarCount = 0;
			int rangeMax = Math.Min((period + barsAgo), Close.Count);

			for (int i = barsAgo; i < rangeMax; i++) {
				if (IsBullishBar(i)) {
					bullBarCount++;
					bullBarSize = bullBarSize + RealBodySize(i);
				}
			}

			if (bullBarCount == 0) {
				return 0;
			}

			return bullBarSize / bullBarCount;
		}
		#endregion

		#region NumberOfConsecutiveBearBars()
		public int NumberOfConsecutiveBearBars(int barsAgo, int period)
		{
			int bearBars = 0;
			int rangeMax = Math.Min((period + barsAgo), Close.Count);

			for (int i = barsAgo; i < rangeMax; i++) {
				if (IsBearishBar(i) && IsBearishBar(i + 1)) {
					bearBars++;
				}
			}

			return bearBars;
		}
		#endregion

		#region NumberOfConsecutiveBullBars()
		public int NumberOfConsecutiveBullBars(int barsAgo, int period)
		{
			int bullBars = 0;
			int rangeMax = Math.Min((period + barsAgo), Close.Count);

			for (int i = barsAgo; i < rangeMax; i++) {
				if (IsBullishBar(i) && IsBullishBar(i + 1)) {
					bullBars++;
				}
			}

			return bullBars;
		}
		#endregion

		#region NumberOfBearBars()
		public int NumberOfBearBars(int barsAgo, int period)
		{
			int bearBars = 0;
			int rangeMax = Math.Min((period + barsAgo), Close.Count);

			for (int i = barsAgo; i < rangeMax; i++) {
				if (IsBearishBar(i)) {
					bearBars++;
				}
			}

			return bearBars;
		}
		#endregion

		#region NumberOfBullBars()
		public int NumberOfBullBars(int barsAgo, int period)
		{
			int bullBars = 0;
			int rangeMax = Math.Min((period + barsAgo), Close.Count);

			for (int i = barsAgo; i < rangeMax; i++) {
				if (IsBullishBar(i)) {
					bullBars++;
				}
			}

			return bullBars;
		}
		#endregion

		#region NumberOfBarsWithLowerTail()
		public int NumberOfBarsWithLowerTail(int barsAgo, int period)
		{
			int lowerTail = 0;
			int rangeMax = Math.Min((period + barsAgo), Close.Count);

			for (int i = barsAgo; i < rangeMax; i++) {
				if (IsBullishBar(barsAgo)) {
					if (InPortionOfBarRange(Open, i, 33, 66)) {
						lowerTail++;
					}
				}

				if (IsBearishBar(barsAgo)) {
					if (InPortionOfBarRange(Close, i, 33, 66)) {
						lowerTail++;
					}
				}
			}

			return lowerTail;
		}
		#endregion

		#region NumberOfBarsWithUpperTail()
		public int NumberOfBarsWithUpperTail(int barsAgo, int period)
		{
			int upperTail = 0;
			int rangeMax = Math.Min((period + barsAgo), Close.Count);

			for (int i = barsAgo; i < rangeMax; i++) {
				if (IsBullishBar(barsAgo)) {
					if (InPortionOfBarRange(Close, i, 33, 66)) {
						upperTail++;
					}
				}

				if (IsBearishBar(barsAgo)) {
					if (InPortionOfBarRange(Open, i, 33, 66)) {
						upperTail++;
					}
				}
			}

			return upperTail;
		}
		#endregion

		#region NumberOfBarsClosingNearHigh()
		public int NumberOfBarsClosingNearHigh(int barsAgo, int period)
		{
			int bullBars = 0;
			int rangeMax = Math.Min((period + barsAgo), Close.Count);

			for (int i = barsAgo; i < rangeMax; i++) {
				if (InPortionOfBarRange(i, 50, 100)) {
					bullBars++;
				}
			}

			return bullBars;
		}
		#endregion

		#region NumberOfBarsClosingNearLow()
		public int NumberOfBarsClosingNearLow(int barsAgo, int period)
		{
			int bearBars = 0;
			int rangeMax = Math.Min((period + barsAgo), Close.Count);

			for (int i = barsAgo; i < rangeMax; i++) {
				if (InPortionOfBarRange(i, 0, 50)) {
					bearBars++;
				}
			}

			return bearBars;
		}
		#endregion

		#region NumberOfBearPullbacks()
		public int NumberOfBearPullbacks(int barsAgo, int period)
		{
			int bullBars = 0;
			int rangeMax = Math.Min((period + barsAgo), Close.Count);

			for (int i = barsAgo; i < rangeMax; i++) {
				if (IsHigherHigh(i, 1)) {
					bullBars++;
				}
			}

			return bullBars;
		}
		#endregion

		#region AverageBarsInTrendPullback()
		public int AverageBarsInTrendPullback(int barsAgo, int period, ISeries <double> series, Func <int, int, bool> callback)
		{
			int barsInPullback = 0;
			int pullbacks = 0;
			double pullbackStart = 0;

			int rangeMax = Math.Min((period + barsAgo), Close.Count);

			for (int i = barsAgo; i < rangeMax; i++) {
				if (pullbackStart == 0) {
					if (callback(i, 1)) {
						pullbackStart = series[1];
						pullbacks++;
						barsInPullback++;
					}
				} else {
					barsInPullback++;

					if (Low[0] < pullbackStart) {
						pullbackStart = 0;
					}
				}
			}

			if (pullbacks == 0) {
				return 0;
			}

			return (int) Math.Floor((double)(barsInPullback / pullbacks));
		}
		#endregion

		#region AverageBarsInBearTrendPullback()
		public int AverageBarsInBearTrendPullback(int barsAgo, int period) {
			return AverageBarsInTrendPullback(barsAgo, period, High, IsHigherHigh);
		}
		#endregion

		#region AverageBarsInBullTrendPullback()
		public int AverageBarsInBullTrendPullback(int barsAgo, int period) {
			return AverageBarsInTrendPullback(barsAgo, period, Low, IsLowerLow);
		}
		#endregion

		#region NumberOfBullPullbacks()
		public int NumberOfBullPullbacks(int barsAgo, int period)
		{
			int bearBars = 0;
			int rangeMax = Math.Min((period + barsAgo), Close.Count);

			for (int i = barsAgo; i < rangeMax; i++) {
				if (IsLowerLow(i, 1)) {
					bearBars++;
				}
			}

			return bearBars;
		}
		#endregion

		#region LeastBarsUp()
		public bool LeastBarsUp(int count, int period, int index = 0)
		{
			int barsUp = 0;

			for (int i = 0; i < period; i++) {
				if (IsUp(i + index)) {
					barsUp = barsUp + 1;
				}
			}

			return barsUp >= count;
		}
		#endregion

		#region LeastSmallBars()
		public bool LeastSmallBars(int count, int period, int index = 0)
		{
			int barsSmall = 0;

			for (int i = 0; i < period; i++) {
				if (IsSmall(i + index)) {
					barsSmall = barsSmall + 1;
				}
			}

			return barsSmall >= count;
		}
		#endregion

		#region LeastBigBars()
		public bool LeastBigBars(int count, int period, int index = 0)
		{
			int barsBig = 0;

			for (int i = 0; i < period; i++) {
				if (IsBig(i + index)) {
					barsBig = barsBig + 1;
				}
			}

			return barsBig >= count;
		}
		#endregion

		#region LeastSmallerBars()
		public bool LeastSmallerBars(int count, int period, int index = 0)
		{
			int barsSmaller = 0;

			for (int i = 0; i < period; i++) {
				if (IsSmaller(i + index)) {
					barsSmaller = barsSmaller + 1;
				}
			}

			return barsSmaller >= count;
		}
		#endregion

		#region LeastBiggerBars()
		public bool LeastBiggerBars(int count, int period, int index = 0)
		{
			int barsBigger = 0;

			for (int i = 0; i < period; i++) {
				if (IsBigger(i + index)) {
					barsBigger = barsBigger + 1;
				}
			}

			return barsBigger >= count;
		}
		#endregion

		#region IsRisingBar()
		public bool IsRisingBar(int barsAgo = 0)
		{
			return IsHigherHigh(barsAgo) && !IsLowerLow(barsAgo);
		}
		#endregion

		#region IsFallingBar()
		public bool IsFallingBar(int barsAgo = 0)
		{
			return !IsHigherHigh(barsAgo) && IsLowerLow(barsAgo);
		}
		#endregion

		#region IsBullishBar()
		// determine if the bar at `barsAgo` is a bull bar
		public bool IsBullishBar(int barsAgo = 0)
		{
			return Close[barsAgo] > Open[barsAgo];
		}
		#endregion

		#region IsBearishBar()
		// determine if the bar at `barsAgo` is a bear bar
		public bool IsBearishBar(int barsAgo = 0)
		{
			return Close[barsAgo] < Open[barsAgo];
		}
		#endregion

		#region IsFalling()
		// is source at `barsAgo` is less than `length` bars earlier
		// source = Series to check
		// barsAgo = the latest bar
		// length = the number of bars before `barsAgo` to evaluate
		public bool IsFalling(ISeries<double> source, int barsAgo = 0, int length = 1)
		{
			if (source == null) {
				source = Close;
			}

			return source[barsAgo] < source[barsAgo + length];
		}

		public bool IsFalling(int barsAgo = 0, int length = 1)
		{
			return IsFalling(Close, barsAgo, length);
		}

		public bool IsFalling(int barsAgo = 0)
		{
			return IsFalling(Close, barsAgo, 1);
		}
		#endregion

		#region IsRising()
		// is source at `barsAgo` is greater than `length` bars earlier
		// source = Series to check
		// barsAgo = the latest bar
		// length = the number of bars before `barsAgo` to evaluate
		public bool IsRising(ISeries<double> source, int barsAgo = 0, int length = 1)
		{
			if (source == null) {
				source = Close;
			}

			return source[barsAgo] > source[barsAgo + length];
		}

		public bool IsRising( int barsAgo = 0, int length = 1)
		{
			return IsRising(Close, barsAgo, length);
		}

		public bool IsRising( int barsAgo = 0)
		{
			return IsRising(Close, barsAgo, 1);
		}
		#endregion

		#region IsHigherHigh()
		// Is High at `barsAgo` greater than `length` bars earlier
		public bool IsHigherHigh(int barsAgo = 0, int length = 1)
		{
			return IsRising(High, barsAgo, length);
		}
		#endregion

		#region IsLowerLow()
		// Is Low at `barsAgo` less than `length` bars earlier
		public bool IsLowerLow(int barsAgo = 0, int length = 1)
		{
			return IsFalling(Low, barsAgo, length);
		}
		#endregion

		#region IsLowerHigh()
		// Is High at `barsAgo` less than `length` bars earlier
		public bool IsLowerHigh(int barsAgo = 0, int length = 1)
		{
			return IsFalling(High, barsAgo, length);
		}
		#endregion

		#region IsHigherLow()
		// Is Low at `barsAgo` greater than `length` bars earlier
		public bool IsHigherLow(int barsAgo = 0, int length = 1)
		{
			return IsRising(Low, barsAgo, length);
		}
		#endregion

		#region IsLowerClose()
		// Is Close at `barsAgo` less than `length` bars earlier
		public bool IsLowerClose(int barsAgo = 0, int length = 1)
		{
			return IsFalling(Close, barsAgo, length);
		}
		#endregion

		#region IsHigherClose()
		// Is Close at `barsAgo` greater than `length` bars earlier
		public bool IsHigherClose(int barsAgo = 0, int length = 1)
		{
			return IsRising(Close, barsAgo, length);
		}
		#endregion

		#region InRangeOfBars()
		// the number of bars that have `val` within the bars open and close, starting at `barsAgo`, looking `length` bars back
		public int InRangeOfBars(int barsAgo, double val, int length)
		{
			int bars = 0;
			double barLow = 0;
			double barHigh = 0;

			for (int i = barsAgo; i < barsAgo + length; i++) {
				barLow = Math.Min(Open[i], Close[i]);
				barHigh = Math.Max(Open[i], Close[i]);

				if (val >= barLow && val <= barHigh) {
					bars++;
				}
			}

			return bars;
		}
		#endregion

		#region InPortionOfBarRange()
		// determine how far a value is between the Low and High of a bar at `barsAgo`. The range is 0 - 100 with 0 being the Low and 100 being the High.
		public bool InPortionOfBarRange(ISeries<double> source, int barsAgo, double lowestValue, double highestValue)
		{
			double reference = source[barsAgo] - Low[barsAgo];
			double range = High[barsAgo] - Low[barsAgo];
			double position = (reference / range) * 100;

			return position >= lowestValue && position <= highestValue;
		}
		#endregion

		#region InPortionOfBarRange()
		public bool InPortionOfBarRange(int barsAgo, double lowestValue, double highestValue)
		{
			return InPortionOfBarRange(Close, barsAgo, lowestValue, highestValue);
		}
		#endregion

		#region GetTrendDirection()
		// get the trend direction over the last `length` bars, starting `barsAgo` bars before the current bar
		public TrendDirection GetTrendDirection(int barsAgo = 0, int length = 10)
		{
			if (Close.Count < length + barsAgo) return TrendDirection.Flat;

			int bullishBars = 0;
			int bearishBars = 0;

			for (int i = barsAgo; i < barsAgo + length; i++) {

				if (IsBullishBar(i) && IsRising(i)) {
					bullishBars++;
				}

				if (IsBearishBar(i) && IsFalling(i)) {
					bearishBars++;
				}
			}

			if (bullishBars > bearishBars) {
				return TrendDirection.Bullish;
			}

			if (bullishBars < bearishBars) {
				return TrendDirection.Bearish;
			}


			return TrendDirection.Flat;
		}
		#endregion

		#region IsTrendBar()
		public bool IsTrendBar(int barsAgo)
		{
			double thresholdPercentage = 0.5;
			double openCloseDifference = Math.Abs(Open[barsAgo] - Close[barsAgo]);
			double barRange = High[barsAgo] - Low[barsAgo];

			if (barRange == 0) {
				return false;
			}

			return (openCloseDifference / barRange) >= thresholdPercentage;
		}
		#endregion

		#region IsTradingRangeBar()
		public bool IsTradingRangeBar(int barsAgo)
		{
			double thresholdPercentage = 0.5;
			double openCloseDifference = Math.Abs(Open[barsAgo] - Close[barsAgo]);
			double barRange = High[barsAgo] - Low[barsAgo];

			if (barRange == 0) {
				return false;
			}

			return (openCloseDifference / barRange) < thresholdPercentage;
		}
		#endregion

		#region IsWeakSignalBar()
		public bool IsWeakSignalBar(int barsAgo, TrendDirection direction = TrendDirection.Flat)
		{
			if (barsAgo > 0 && !IsWeakFollowThroughBar(barsAgo - 1)) {
				return false;
			}

			double change = Close[barsAgo] - Open[barsAgo + 3];

			if (direction == TrendDirection.Flat) {
				direction = GetTrendDirection(1, 3);
			}

			if (direction == TrendDirection.Bullish && change < 0) {
				return false;
			}

			if (direction == TrendDirection.Bearish && change > 0) {
				return false;
			}

			return true;
		}
		#endregion

		#region IsStrongSellSignalBar()
		public bool IsStrongSellSignalBar(int barsAgo, TrendDirection direction = TrendDirection.Flat)
		{
			if (IsWeakSignalBar(barsAgo, direction)) {
				return false;
			}

			if (!IsBearishSellReversalBar(barsAgo)) {
				return false;
			}

			if (Close[0] >= Close[1]) {
				return false;
			}

			if (Close[0] < Close[3]) {
				return false;
			}

			if (InRangeOfBars(0, Close[0], 5) > 2) {
				return false;
			}

			if (Open[0] < Close[1]) {
				return false;
			}

			if (InPortionOfBarRange(0, 20, 100)) {
				return false;
			}

			if (!InPortionOfBarRange(Open, 0, 50, 67)) {
				return false;
			}

			return true;
		}
		#endregion

		#region BuySignalBarStrength()
		public double BuySignalBarStrength(int barsAgo, TrendDirection direction = TrendDirection.Flat)
		{
			double countStrength = 0;
			double strengthIndicators = 8;

			if (!IsWeakSignalBar(barsAgo, direction)) {
				countStrength++;
			}

			if (IsBullishBuyReversalBar(barsAgo)) {
				countStrength++;
			}

			if (Close[0] > Close[1]) {
				countStrength++;
			}

			if (Close[0] <= Close[3]) {
				countStrength++;
			}

			if (InRangeOfBars(0, Close[0], 5) <= 2) {
				countStrength++;
			}

			if (Open[0] <= Close[1]) {
				countStrength++;
			}

			if (InPortionOfBarRange(0, 80, 100)) {
				countStrength++;
			}

			if (InPortionOfBarRange(Open, 0, 33, 50)) {
				countStrength++;
			}

			return countStrength / strengthIndicators;
		}
		#endregion

		#region SellSignalBarStrength()
		public double SellSignalBarStrength(int barsAgo, TrendDirection direction = TrendDirection.Flat)
		{
			double countStrength = 0;
			double strengthIndicators = 8;

			if (!IsWeakSignalBar(barsAgo, direction)) {
				countStrength++;
			}

			if (IsBearishSellReversalBar(barsAgo)) {
				countStrength++;
			}

			if (Close[0] < Close[1]) {
				countStrength++;
			}

			if (Close[0] >= Close[3]) {
				countStrength++;
			}

			if (InRangeOfBars(0, Close[0], 5) <= 2) {
				countStrength++;
			}

			if (Open[0] >= Close[1]) {
				countStrength++;
			}

			if (!InPortionOfBarRange(0, 20, 100)) {
				countStrength++;
			}

			if (InPortionOfBarRange(Open, 0, 50, 67)) {
				countStrength++;
			}

			return countStrength / strengthIndicators;
		}
		#endregion

		#region IsStrongBuySignalBar()
		public bool IsStrongBuySignalBar(int barsAgo, TrendDirection direction = TrendDirection.Flat)
		{
			if (IsWeakSignalBar(barsAgo, direction)) {
				return false;
			}

			if (!IsBullishBuyReversalBar(barsAgo)) {
				return false;
			}

			if (Close[0] <= Close[1]) {
				return false;
			}

			if (Close[0] > Close[3]) {
				return false;
			}

			if (InRangeOfBars(0, Close[0], 5) > 2) {
				return false;
			}

			if (Open[0] > Close[1]) {
				return false;
			}

			if (InPortionOfBarRange(0, 0, 80)) {
				return false;
			}

			if (!InPortionOfBarRange(Open, 0, 33, 50)) {
				return false;
			}

			return true;
		}
		#endregion

		#region IsWeakFollowThroughBar()
		public bool IsWeakFollowThroughBar(int barsAgo)
		{
			bool isReversalBar = Close[barsAgo + 1] > Open[barsAgo + 1]
				? IsBuyReversalBar(barsAgo)
				: IsSellReversalBar(barsAgo);

			return IsDoji(barsAgo)
				|| IsInsideBar(barsAgo)
				|| isReversalBar;
		}
		#endregion

		#region IsStrongFollowThroughBar()
		public bool IsStrongFollowThroughBar(int barsAgo)
		{
			if (IsWeakFollowThroughBar(barsAgo)) {
				return false;
			}

			TrendDirection previousDirection = Close[barsAgo + 1] > Open[barsAgo + 1]
				? TrendDirection.Bullish
				: TrendDirection.Bearish;

			TrendDirection direction = Close[barsAgo] > Open[barsAgo]
				? TrendDirection.Bullish
				: TrendDirection.Bearish;

			if (previousDirection != direction) {
				return false;
			}

			return IsTrendBar(barsAgo);
		}
		#endregion

		#region IsDoji()
		public bool IsDoji(int barsAgo)
		{
			double thresholdPercentage = 0.1;
			double openCloseDifference = Math.Abs(Open[barsAgo] - Close[barsAgo]);
			double barRange = High[barsAgo] - Low[barsAgo];

			if (barRange == 0) {
				return false;
			}

			return (openCloseDifference / barRange) <=thresholdPercentage;
		}
		#endregion

		#region IsInsideBar()
		public bool IsInsideBar(int barsAgo)
		{
			return !IsHigherHigh(barsAgo, 1) && !IsLowerLow(barsAgo, 1);
		}
		#endregion

		#region IsOutsideBar()
		public bool IsOutsideBar(int barsAgo)
		{
			return IsHigherHigh(barsAgo, 1) && IsLowerLow(barsAgo, 1);
		}
		#endregion

		#region DoesInsideOutsideMatch()
		public bool DoesInsideOutsideMatch(string pattern, int barsAgo)
		{
			for (int i = 0; i < pattern.Length; i++)
			{
				char barType = pattern[pattern.Length - 1 - i];

				if (barType != 'i' && barType != 'o')
				{
					return false;
				}

				if (barType == 'i' && !IsInsideBar(i + barsAgo))
				{
					return false;
				}

				if (barType == 'o' && !IsOutsideBar(i + barsAgo))
				{
					return false;
				}
			}

			return true;
		}
		#endregion

		#region IsBuyReversalBar()
		public bool IsBuyReversalBar(int barsAgo)
		{
			double midpoint = (High[barsAgo] + Low[barsAgo]) / 2;

			return Close[barsAgo] > midpoint;
		}
		#endregion

		#region IsBullishBuyReversalBar()
		public bool IsBullishBuyReversalBar(int barsAgo)
		{
			return IsBuyReversalBar(barsAgo) && IsBullishBar(barsAgo);
		}
		#endregion

		#region IsBearishBuyReversalBar()
		public bool IsBearishBuyReversalBar(int barsAgo)
		{
			return IsBuyReversalBar(barsAgo) && IsBearishBar(barsAgo);
		}
		#endregion

		#region IsSellReversalBar()
		public bool IsSellReversalBar(int barsAgo)
		{
			double midpoint = (High[barsAgo] + Low[barsAgo]) / 2;

			return Close[barsAgo] < midpoint;
		}
		#endregion

		#region IsBullishSellReversalBar()
		public bool IsBullishSellReversalBar(int barsAgo)
		{
			return IsSellReversalBar(barsAgo) && IsBullishBar(barsAgo);
		}
		#endregion

		#region IsBearishSellReversalBar()
		public bool IsBearishSellReversalBar(int barsAgo)
		{
			return IsSellReversalBar(barsAgo) && IsBearishBar(barsAgo);
		}
		#endregion

		#region IsBreakoutBeyondLevel()
		public bool IsBreakoutBeyondLevel(int barsAgo, double priceOfInterest)
		{
			if (Close[barsAgo + 1] > priceOfInterest) {
				return Low[barsAgo] < priceOfInterest;
			}

			return High[barsAgo] > priceOfInterest;
		}
		#endregion

		#region IsStrongBreakoutBeyondLevel()
		public bool IsStrongBreakoutBeyondLevel(int barsAgo, double priceOfInterest)
		{
			int prev = barsAgo + 1;

			if (Close[prev] > priceOfInterest) {
				return Close[barsAgo] < Low[prev];
			}

			return Close[barsAgo] > High[prev];
		}
		#endregion

		#region GetBuyingPressure()
		public double GetBuyingPressure(int barsAgo, int period)
		{
			double buyingCount = 0;
			double indicatorCount = 10;

			int bearBarCount = NumberOfBearBars(barsAgo, period);
			int bullBarCount = NumberOfBullBars(barsAgo, period);

			if (bearBarCount < bullBarCount) {
				buyingCount++;
			}

			int consecutiveBearCount 			= NumberOfConsecutiveBearBars(barsAgo, period);
			int consecutiveBullCount 			= NumberOfConsecutiveBullBars(barsAgo, period);
			int consecutiveBullCountPrevious 	= NumberOfConsecutiveBullBars(barsAgo + period, period);
			int consecutiveBearCountPrevious	= NumberOfConsecutiveBearBars(barsAgo + period, period);

			if (consecutiveBearCount < consecutiveBullCount) {
				buyingCount++;
			}

			if (consecutiveBullCountPrevious < consecutiveBullCount) {
				buyingCount++;
			}

			if (consecutiveBearCount < consecutiveBearCountPrevious) {
				buyingCount++;
			}

			double averageBearBar 			= AverageBearBarSize(barsAgo, period);
			double averageBullBar 			= AverageBullBarSize(barsAgo, period);
			double averageBullBarPrevious	= AverageBullBarSize(barsAgo + period, period);
			double averageBearBarPrevious	= AverageBearBarSize(barsAgo + period, period);

			if (averageBearBar < averageBullBar) {
				buyingCount++;
			}

			if (averageBullBarPrevious < averageBullBar) {
				buyingCount++;
			}

			if (averageBearBarPrevious > averageBearBar) {
				buyingCount++;
			}

			int barsClosingNearHigh = NumberOfBarsClosingNearHigh(barsAgo, period);
			int barsClosingNearLow  = NumberOfBarsClosingNearLow(barsAgo, period);

			if (barsClosingNearLow < barsClosingNearHigh) {
				buyingCount++;
			}

			int bearPullbacks = NumberOfBearPullbacks(barsAgo, period);
			int bullPullbacks  = NumberOfBullPullbacks(barsAgo, period);

			if (bullPullbacks < bearPullbacks) {
				buyingCount++;
			}

			int barsWithLowerTail = NumberOfBarsWithLowerTail(barsAgo, period);
			int barsWithUpperTail  = NumberOfBarsWithUpperTail(barsAgo, period);

			if (barsWithUpperTail < barsWithLowerTail) {
				buyingCount++;
			}

			return buyingCount / indicatorCount;
		}
		#endregion

		#region GetSellingPressure()
		public double GetSellingPressure(int barsAgo, int period)
		{
			double sellingCount = 0;
			double indicatorCount = 10;

			int bearBarCount = NumberOfBearBars(barsAgo, period);
			int bullBarCount = NumberOfBullBars(barsAgo, period);

			if (bearBarCount > bullBarCount) {
				sellingCount++;
			}

			int consecutiveBearCount 			= NumberOfConsecutiveBearBars(barsAgo, period);
			int consecutiveBullCount 			= NumberOfConsecutiveBullBars(barsAgo, period);
			int consecutiveBullCountPrevious 	= NumberOfConsecutiveBullBars(barsAgo + period, period);
			int consecutiveBearCountPrevious	= NumberOfConsecutiveBearBars(barsAgo + period, period);

			if (consecutiveBearCount > consecutiveBullCount) {
				sellingCount++;
			}

			if (consecutiveBullCountPrevious > consecutiveBullCount) {
				sellingCount++;
			}

			if (consecutiveBearCount > consecutiveBearCountPrevious) {
				sellingCount++;
			}

			double averageBearBar 			= AverageBearBarSize(barsAgo, period);
			double averageBullBar 			= AverageBullBarSize(barsAgo, period);
			double averageBullBarPrevious	= AverageBullBarSize(barsAgo + period, period);
			double averageBearBarPrevious	= AverageBearBarSize(barsAgo + period, period);

			if (averageBearBar > averageBullBar) {
				sellingCount++;
			}

			if (averageBullBarPrevious > averageBullBar) {
				sellingCount++;
			}

			if (averageBearBarPrevious < averageBearBar) {
				sellingCount++;
			}

			int barsClosingNearHigh = NumberOfBarsClosingNearHigh(barsAgo, period);
			int barsClosingNearLow  = NumberOfBarsClosingNearLow(barsAgo, period);

			if (barsClosingNearLow > barsClosingNearHigh) {
				sellingCount++;
			}

			int bearPullbacks = NumberOfBearPullbacks(barsAgo, period);
			int bullPullbacks  = NumberOfBullPullbacks(barsAgo, period);

			if (bullPullbacks > bearPullbacks) {
				sellingCount++;
			}

			int barsWithLowerTail = NumberOfBarsWithLowerTail(barsAgo, period);
			int barsWithUpperTail  = NumberOfBarsWithUpperTail(barsAgo, period);

			if (barsWithUpperTail > barsWithLowerTail) {
				sellingCount++;
			}

			return sellingCount / indicatorCount;
		}
		#endregion

		#region GetBuySellPressure()
		public double GetBuySellPressure(int barsAgo, int period)
		{
			double balance = 50;

			double bullValue = GetBuyingPressure(barsAgo, period) * 50;
			double bearValue = GetSellingPressure(barsAgo, period) * 50;

			return 50 + bullValue - bearValue;
		}
		#endregion

		#region NumberOfPullbacksInTrend()
		public int NumberOfPullbacksInTrend(int barsAgo, int period, TrendDirection direction)
		{
			if (direction == TrendDirection.Bullish) {
				return NumberOfBullPullbacks(barsAgo, period);
			}

			if (direction == TrendDirection.Bearish) {
				return NumberOfBearPullbacks(barsAgo, period);
			}

			return 0;
		}

		public int NumberOfPullbacksInTrend(int barsAgo, int period)
		{
			if (GetTrendDirection(barsAgo, period) == TrendDirection.Bullish) {
				return NumberOfBullPullbacks(barsAgo, period);
			}

			if (GetTrendDirection(barsAgo, period) == TrendDirection.Bearish) {
				return NumberOfBearPullbacks(barsAgo, period);
			}

			return 0;
		}
		#endregion

		#region AveragePullbackLength()
		public double AveragePullbackLength(int barsAgo, int period, TrendDirection direction)
		{
			if (direction == TrendDirection.Bullish) {
				return AverageBarsInBullTrendPullback(barsAgo, period);
			}

			if (direction == TrendDirection.Bearish) {
				return AverageBarsInBearTrendPullback(barsAgo, period);
			}

			return 0;
		}

		public double AveragePullbackLength(int barsAgo, int period)
		{
			if (GetTrendDirection(barsAgo, period) == TrendDirection.Bullish) {
				return AverageBarsInBullTrendPullback(barsAgo, period);
			}

			if (GetTrendDirection(barsAgo, period) == TrendDirection.Bearish) {
				return AverageBarsInBearTrendPullback(barsAgo, period);
			}

			return 0;
		}
		#endregion

		#region IsBreakoutTrend()
		public bool IsBreakoutTrend(int barsAgo, int period, TrendDirection direction)
		{
			return NumberOfPullbacksInTrend(barsAgo, period, direction) <= 1 && AveragePullbackLength(barsAgo, period, direction) <= 2 && period >= 5;
		}

		public bool IsBreakoutTrend(int barsAgo, int period)
		{
			return NumberOfPullbacksInTrend(barsAgo, period) <= 1 && AveragePullbackLength(barsAgo, period) <= 2 && period >= 5;
		}
		#endregion

		#region LargestPullbackInTrend()
		#region LargestPullbackInBullTrend()
		public double LargestPullbackInBullTrend(int barsAgo, int period)
		{
			double low = Low[barsAgo + period];
			double high = High[barsAgo + period];;

			for (int i = barsAgo + period; i > barsAgo; i--) {
				if (IsHigherHigh(i, 1)) {
					high = High[i];
					low = Low[i];
				}

				if (IsLowerLow(i, 1)) {
					high = Math.Max(high, High[i]);
					low = Math.Min(low, Low[i]);
				}
			}

			return high - low;
		}
		#endregion

		#region LargestPullbackInBearTrend()
		public double LargestPullbackInBearTrend(int barsAgo, int period)
		{
			double low = Low[barsAgo + period];
			double high = High[barsAgo + period];

			for (int i = barsAgo + period; i > barsAgo; i--) {
				if (IsLowerLow(i, 1)) {
					high = High[i];
					low = Low[i];
				}

				if (IsHigherHigh(i, 1)) {
					high = Math.Max(high, High[i]);
					low = Math.Min(low, Low[i]);
				}
			}

			return high - low;
		}
		#endregion

		#region LargestPullbackInTrend()
		public double LargestPullbackInTrend(int barsAgo, int period, TrendDirection direction) {
			if (period == 0) {
				return 0;
			}

			double totalRange = MAX(High, period)[barsAgo] - MIN(Low, period)[barsAgo];

			if (direction == TrendDirection.Bullish) {
				return LargestPullbackInBullTrend(barsAgo, period) / totalRange;
			}

			if (direction == TrendDirection.Bearish) {
				return LargestPullbackInBearTrend(barsAgo, period) / totalRange;
			}

			return 0;
		}
		#endregion

		#region LargestPullbackInTrend()
		public double LargestPullbackInTrend(int barsAgo, int period) {
			if (period == 0) {
				return 0;
			}

			if (IsRising(barsAgo, period)) {
				return LargestPullbackInTrend(barsAgo, period, TrendDirection.Bullish);
			}

			if (IsFalling(barsAgo, period)) {
				return LargestPullbackInTrend(barsAgo, period, TrendDirection.Bearish);
			}

			return 0;
		}
		#endregion
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private PR.PriceActionUtils[] cachePriceActionUtils;
		public PR.PriceActionUtils PriceActionUtils()
		{
			return PriceActionUtils(Input);
		}

		public PR.PriceActionUtils PriceActionUtils(ISeries<double> input)
		{
			if (cachePriceActionUtils != null)
				for (int idx = 0; idx < cachePriceActionUtils.Length; idx++)
					if (cachePriceActionUtils[idx] != null &&  cachePriceActionUtils[idx].EqualsInput(input))
						return cachePriceActionUtils[idx];
			return CacheIndicator<PR.PriceActionUtils>(new PR.PriceActionUtils(), input, ref cachePriceActionUtils);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.PR.PriceActionUtils PriceActionUtils()
		{
			return indicator.PriceActionUtils(Input);
		}

		public Indicators.PR.PriceActionUtils PriceActionUtils(ISeries<double> input )
		{
			return indicator.PriceActionUtils(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.PR.PriceActionUtils PriceActionUtils()
		{
			return indicator.PriceActionUtils(Input);
		}

		public Indicators.PR.PriceActionUtils PriceActionUtils(ISeries<double> input )
		{
			return indicator.PriceActionUtils(input);
		}
	}
}

#endregion
