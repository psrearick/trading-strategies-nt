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
		public bool firstBarUp = false;
		public bool firstBarDown = false;
		public Series<double> BuySell;
		public Series<double> BuySellEMA;
		public bool isInBullTrend = false;
		public bool isInBearTrend = false;
		public bool inBullPullback = false;
		public bool inBearPullback = false;
		public double trendHigh = 0;
		public double trendLow = 0;

        private bool twoSmallerBars = false;
        private bool twoBiggerBars = false;
        private bool threeSmallerBars = false;
        private bool threeBiggerBars = false;
        private bool smallerBars = false;
        private bool biggerBars = false;
		private double buyingPressure = 0.5;
		private double sellingPressure = 0.5;
		private double pullbackSwingHigh = 0;
		private double pullbackSwingLow = 0;
		private bool hasBuyingPressure = false;
		private bool hasSellingPressure = false;
		private bool increasingBuyingPressure = false;
		private bool increasingSellingPressure = false;
		private bool brokeFiftyPercentPullback = false;

		private int openGaps = 0;
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
			}
			#endregion
			#region State.DataLoaded
			else if (State == State.DataLoaded)
			{
				BuySell 	= new Series<double>(this);
				BuySellEMA	= new Series<double>(this);
			}
			#endregion
		}
		#endregion

		#region OnBarUpdate()
		protected override void OnBarUpdate()
		{
			if (CurrentBar < 40) {
				BuySell[0] 			= 50;
				BuySellEMA[0] 		= 50;
				trendHigh 			= High[0];
				trendLow 			= Low[0];
				pullbackSwingHigh 	= High[0];
				pullbackSwingLow 	= Low[0];
				return;
			}

			firstBarUp 					= BarIsUp(0);
			firstBarDown				= BarIsDown(0);

			double pressure 			= getBuySellPressure(0, 20);

			BuySell[0] 					= pressure;
			BuySellEMA[0] 				= EMA(BuySell, 9)[0];

			increasingBuyingPressure 	= BuySell[0] > BuySell[1];
			increasingSellingPressure 	= BuySell[1] >= BuySell[0];

			double previousTrendLow		= trendLow;
			double previousTrendHigh	= trendHigh;

			bool previousInBullTrend	= BuySellEMA[1] > 50;
			bool previousInBearTrend	= BuySellEMA[1] < 50;
			isInBullTrend 				= BuySellEMA[0] > 50;
			isInBearTrend 				= BuySellEMA[0] < 50;

			if (isInBullTrend && previousInBearTrend) {
				trendHigh 					= High[0];
				pullbackSwingHigh			= High[0];
				brokeFiftyPercentPullback	= false;
			}

			if (isInBearTrend && previousInBullTrend) {
				trendLow 					= Low[0];
				pullbackSwingLow			= Low[0];
				brokeFiftyPercentPullback	= false;
			}

			if (isInBullTrend) {
				trendHigh = Math.Max(trendHigh, High[0]);
			}

			if (isInBearTrend) {
				trendLow = Math.Min(trendLow, Low[0]);
			}

			double fiftyPercentPullback = (trendHigh + trendLow) / 2;

			if (isInBearTrend && High[0] > fiftyPercentPullback) {
				brokeFiftyPercentPullback	= true;
				pullbackSwingHigh			= High[0];
			}

			if (isInBullTrend && Low[0] < fiftyPercentPullback) {
				brokeFiftyPercentPullback 	= true;
				pullbackSwingLow			= Low[0];
			}

			if (isInBearTrend && brokeFiftyPercentPullback && Low[0] < previousTrendLow) {
				trendHigh = pullbackSwingHigh;
				brokeFiftyPercentPullback = false;
			}

			if (isInBullTrend && brokeFiftyPercentPullback && High[0] > previousTrendHigh) {
				trendLow = pullbackSwingLow;
				brokeFiftyPercentPullback = false;
			}

//			if (trendHigh > 0) {
//				Values[0][0] = trendHigh;
//			}

//			if (trendLow > 0) {
//				Values[1][0] = trendLow;
//			}
		}
		#endregion

		#region BarRealBody()
		public double BarRealBody(int index)
		{
			return Close[index] - Open[index];
		}
		#endregion

		#region BarRealBodySize()
		public double BarRealBodySize(int index)
		{
			return Math.Abs(BarRealBody(index));
		}
		#endregion

		#region BarIsBig()
		public bool BarIsBig(int index)
		{
			return (BarRealBodySize(index) / (High[index] - Low[index])) > 0.75;
		}
		#endregion

		#region BarIsSmall()
		public bool BarIsSmall(int index)
		{
			return (BarRealBodySize(index) / (High[index] - Low[index])) < 0.25;
		}
		#endregion

		#region BarIsStrong()
		public bool BarIsStrong(int index)
		{
			return BarIsBig(index) && BarNearExtreme(index) && BarIsBigger(index);
		}
		#endregion

		#region BarIsBigger()
		public bool BarIsBigger(int index)
		{
			return BarRealBodySize(index) > BarRealBodySize(index + 1);
		}
		#endregion

		#region BarNearExtreme()
		public bool BarNearExtreme(int index)
		{
			if (Close[index] > Open[index]) {
				return ((Close[index] - Low[index]) / (High[index] - Low[index])) > 0.75;
			}

			return ((High[index] - Close[index]) / (High[index] - Low[index])) > 0.75;
		}
		#endregion

		#region BarIsSmaller()
		public bool BarIsSmaller(int index)
		{
			return BarRealBodySize(index) < BarRealBodySize(index + 1);
		}
		#endregion

		#region BarIsDown()
		public bool BarIsDown(int index)
		{
			return Close[index] < Close[index + 1];
		}
		#endregion

		#region BarIsUp()
		public bool BarIsUp(int index)
		{
			return Close[index] > Close[index + 1];
		}
		#endregion

		#region ConsecutiveBiggerBars()
		public bool ConsecutiveBiggerBars(int period, int index = 0)
		{
			bool biggerBars = true;

			for (int i = 0; i < period; i++) {
				if (!BarIsBigger(i + index)) {
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
				if (!BarIsSmaller(i + index)) {
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
				if (!BarIsUp(i + index)) {
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
				if (!BarIsDown(i + index)) {
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
				if (BarIsDown(i + index)) {
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
				if (isBearishBar(i)) {
					bearBarCount++;
					bearBarSize = bearBarSize + BarRealBodySize(i);
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
				if (isBullishBar(i)) {
					bullBarCount++;
					bullBarSize = bullBarSize + BarRealBodySize(i);
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
				if (isBearishBar(i) && isBearishBar(i + 1)) {
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
				if (isBullishBar(i) && isBullishBar(i + 1)) {
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
				if (isBearishBar(i)) {
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
				if (isBullishBar(i)) {
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
				if (isBullishBar(barsAgo)) {
					if (inPortionOfBarRange(Open, i, 33, 66)) {
						lowerTail++;
					}
				}

				if (isBearishBar(barsAgo)) {
					if (inPortionOfBarRange(Close, i, 33, 66)) {
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
				if (isBullishBar(barsAgo)) {
					if (inPortionOfBarRange(Close, i, 33, 66)) {
						upperTail++;
					}
				}

				if (isBearishBar(barsAgo)) {
					if (inPortionOfBarRange(Open, i, 33, 66)) {
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
				if (inPortionOfBarRange(i, 50, 100)) {
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
				if (inPortionOfBarRange(i, 0, 50)) {
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
				if (isHigherHigh(i, 1)) {
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
			return AverageBarsInTrendPullback(barsAgo, period, High, isHigherHigh);
		}
		#endregion

		#region AverageBarsInBullTrendPullback()
		public int AverageBarsInBullTrendPullback(int barsAgo, int period) {
			return AverageBarsInTrendPullback(barsAgo, period, Low, isLowerLow);
		}
		#endregion

		#region NumberOfBullPullbacks()
		public int NumberOfBullPullbacks(int barsAgo, int period)
		{
			int bearBars = 0;
			int rangeMax = Math.Min((period + barsAgo), Close.Count);

			for (int i = barsAgo; i < rangeMax; i++) {
				if (isLowerLow(i, 1)) {
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
				if (BarIsUp(i + index)) {
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
				if (BarIsSmall(i + index)) {
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
				if (BarIsBig(i + index)) {
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
				if (BarIsSmaller(i + index)) {
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
				if (BarIsBigger(i + index)) {
					barsBigger = barsBigger + 1;
				}
			}

			return barsBigger >= count;
		}
		#endregion

		#region isRisingBar()
		public bool isRisingBar(int barsAgo = 0)
		{
			return isHigherHigh(barsAgo) && !isLowerLow(barsAgo);
		}
		#endregion

		#region isFallingBar()
		public bool isFallingBar(int barsAgo = 0)
		{
			return !isHigherHigh(barsAgo) && isLowerLow(barsAgo);
		}
		#endregion

		#region isBullishBar()
		// determine if the bar at `barsAgo` is a bull bar
		public bool isBullishBar(int barsAgo = 0)
		{
			return Close[barsAgo] > Open[barsAgo];
		}
		#endregion

		#region isBearishBar()
		// determine if the bar at `barsAgo` is a bear bar
		public bool isBearishBar(int barsAgo = 0)
		{
			return Close[barsAgo] < Open[barsAgo];
		}
		#endregion

		#region isFalling()
		// is source at `barsAgo` is less than `length` bars earlier
		// source = Series to check
		// barsAgo = the latest bar
		// length = the number of bars before `barsAgo` to evaluate
		public bool isFalling(ISeries<double> source, int barsAgo = 0, int length = 1)
		{
			if (source == null) {
				source = Close;
			}

			return source[barsAgo] < source[barsAgo + length];
		}

		public bool isFalling(int barsAgo = 0, int length = 1)
		{
			return isFalling(Close, barsAgo, length);
		}

		public bool isFalling(int barsAgo = 0)
		{
			return isFalling(Close, barsAgo, 1);
		}
		#endregion

		#region isRising()
		// is source at `barsAgo` is greater than `length` bars earlier
		// source = Series to check
		// barsAgo = the latest bar
		// length = the number of bars before `barsAgo` to evaluate
		public bool isRising(ISeries<double> source, int barsAgo = 0, int length = 1)
		{
			if (source == null) {
				source = Close;
			}

			return source[barsAgo] > source[barsAgo + length];
		}

		public bool isRising( int barsAgo = 0, int length = 1)
		{
			return isRising(Close, barsAgo, length);
		}

		public bool isRising( int barsAgo = 0)
		{
			return isRising(Close, barsAgo, 1);
		}
		#endregion

		#region isHigherHigh()
		// Is High at `barsAgo` greater than `length` bars earlier
		public bool isHigherHigh(int barsAgo = 0, int length = 1)
		{
			return isRising(High, barsAgo, length);
		}
		#endregion

		#region isLowerLow()
		// Is Low at `barsAgo` less than `length` bars earlier
		public bool isLowerLow(int barsAgo = 0, int length = 1)
		{
			return isFalling(Low, barsAgo, length);
		}
		#endregion

		#region isLowerHigh()
		// Is High at `barsAgo` less than `length` bars earlier
		public bool isLowerHigh(int barsAgo = 0, int length = 1)
		{
			return isFalling(High, barsAgo, length);
		}
		#endregion

		#region isHigherLow()
		// Is Low at `barsAgo` greater than `length` bars earlier
		public bool isHigherLow(int barsAgo = 0, int length = 1)
		{
			return isRising(Low, barsAgo, length);
		}
		#endregion

		#region inRangeOfBars()
		// the number of bars that have `val` within the bars open and close, starting at `barsAgo`, looking `length` bars back
		public int inRangeOfBars(int barsAgo, double val, int length)
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

		#region inPortionOfBarRange()
		// determine how far a value is between the Low and High of a bar at `barsAgo`. The range is 0 - 100 with 0 being the Low and 100 being the High.
		public bool inPortionOfBarRange(ISeries<double> source, int barsAgo, double lowestValue, double highestValue)
		{
			double reference = source[barsAgo] - Low[barsAgo];
			double range = High[barsAgo] - Low[barsAgo];
			double position = (reference / range) * 100;

			return position >= lowestValue && position <= highestValue;
		}
		#endregion

		#region inPortionOfBarRange()
		public bool inPortionOfBarRange(int barsAgo, double lowestValue, double highestValue)
		{
			return inPortionOfBarRange(Close, barsAgo, lowestValue, highestValue);
		}
		#endregion

		#region getTrendDirection()
		// get the trend direction over the last `length` bars, starting `barsAgo` bars before the current bar
		public TrendDirection getTrendDirection(int barsAgo = 0, int length = 10)
		{
			if (Close.Count < length + barsAgo) return TrendDirection.Flat;

			int bullishBars = 0;
			int bearishBars = 0;

			for (int i = barsAgo; i < barsAgo + length; i++) {

				if (isBullishBar(i) && isRising(i)) {
					bullishBars++;
				}

				if (isBearishBar(i) && isFalling(i)) {
					bearishBars++;
				}
			}

			Print(bullishBars);
			Print(bearishBars);
			Print("======");

			if (bullishBars > bearishBars) {
				return TrendDirection.Bullish;
			}

			if (bullishBars < bearishBars) {
				return TrendDirection.Bearish;
			}


			return TrendDirection.Flat;
		}
		#endregion

		#region isTrendBar()
		public bool isTrendBar(int barsAgo)
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

		#region isTradingRangeBar()
		public bool isTradingRangeBar(int barsAgo)
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

		#region isWeakSignalBar()
		public bool isWeakSignalBar(int barsAgo, TrendDirection direction = TrendDirection.Flat)
		{
			if (barsAgo > 0 && !isWeakFollowThroughBar(barsAgo - 1)) {
				return false;
			}

			double change = Close[barsAgo] - Open[barsAgo + 3];

			if (direction == TrendDirection.Flat) {
				direction = getTrendDirection(1, 3);
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

		#region isStrongSellSignalBar()
		public bool isStrongSellSignalBar(int barsAgo, TrendDirection direction = TrendDirection.Flat)
		{
			if (isWeakSignalBar(barsAgo, direction)) {
				return false;
			}

			if (!isBearishSellReversalBar(barsAgo)) {
				return false;
			}

			if (Close[0] >= Close[1]) {
				return false;
			}

			if (Close[0] < Close[3]) {
				return false;
			}

			if (inRangeOfBars(0, Close[0], 5) > 2) {
				return false;
			}

			if (Open[0] < Close[1]) {
				return false;
			}

			if (inPortionOfBarRange(0, 20, 100)) {
				return false;
			}

			if (!inPortionOfBarRange(Open, 0, 50, 67)) {
				return false;
			}

			return true;
		}
		#endregion

		#region buySignalBarStrength()
		public double buySignalBarStrength(int barsAgo, TrendDirection direction = TrendDirection.Flat)
		{
			double countStrength = 0;
			double strengthIndicators = 8;

			if (!isWeakSignalBar(barsAgo, direction)) {
				countStrength++;
			}

			if (isBullishBuyReversalBar(barsAgo)) {
				countStrength++;
			}

			if (Close[0] > Close[1]) {
				countStrength++;
			}

			if (Close[0] <= Close[3]) {
				countStrength++;
			}

			if (inRangeOfBars(0, Close[0], 5) <= 2) {
				countStrength++;
			}

			if (Open[0] <= Close[1]) {
				countStrength++;
			}

			if (inPortionOfBarRange(0, 80, 100)) {
				countStrength++;
			}

			if (inPortionOfBarRange(Open, 0, 33, 50)) {
				countStrength++;
			}

			return countStrength / strengthIndicators;
		}
		#endregion

		#region sellSignalBarStrength()
		public double sellSignalBarStrength(int barsAgo, TrendDirection direction = TrendDirection.Flat)
		{
			double countStrength = 0;
			double strengthIndicators = 8;

			if (!isWeakSignalBar(barsAgo, direction)) {
				countStrength++;
			}

			if (isBearishSellReversalBar(barsAgo)) {
				countStrength++;
			}

			if (Close[0] < Close[1]) {
				countStrength++;
			}

			if (Close[0] >= Close[3]) {
				countStrength++;
			}

			if (inRangeOfBars(0, Close[0], 5) <= 2) {
				countStrength++;
			}

			if (Open[0] >= Close[1]) {
				countStrength++;
			}

			if (!inPortionOfBarRange(0, 20, 100)) {
				countStrength++;
			}

			if (inPortionOfBarRange(Open, 0, 50, 67)) {
				countStrength++;
			}

			return countStrength / strengthIndicators;
		}
		#endregion

		#region isStrongBuySignalBar()
		public bool isStrongBuySignalBar(int barsAgo, TrendDirection direction = TrendDirection.Flat)
		{
			if (isWeakSignalBar(barsAgo, direction)) {
				return false;
			}

			if (!isBullishBuyReversalBar(barsAgo)) {
				return false;
			}

			if (Close[0] <= Close[1]) {
				return false;
			}

			if (Close[0] > Close[3]) {
				return false;
			}

			if (inRangeOfBars(0, Close[0], 5) > 2) {
				return false;
			}

			if (Open[0] > Close[1]) {
				return false;
			}

			if (inPortionOfBarRange(0, 0, 80)) {
				return false;
			}

			if (!inPortionOfBarRange(Open, 0, 33, 50)) {
				return false;
			}

			return true;
		}
		#endregion

		#region isWeakFollowThroughBar()
		public bool isWeakFollowThroughBar(int barsAgo)
		{
			bool isReversalBar = Close[barsAgo + 1] > Open[barsAgo + 1]
				? isBuyReversalBar(barsAgo)
				: isSellReversalBar(barsAgo);

			return isDoji(barsAgo)
				|| isInsideBar(barsAgo)
				|| isReversalBar;
		}
		#endregion

		#region isStrongFollowThroughBar()
		public bool isStrongFollowThroughBar(int barsAgo)
		{
			if (isWeakFollowThroughBar(barsAgo)) {
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

			return isTrendBar(barsAgo);
		}
		#endregion

		#region isDoji()
		public bool isDoji(int barsAgo)
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

		#region isInsideBar()
		public bool isInsideBar(int barsAgo)
		{
			return !isHigherHigh(barsAgo, 1) && !isLowerLow(barsAgo, 1);
		}
		#endregion

		#region isOutsideBar()
		public bool isOutsideBar(int barsAgo)
		{
			return isHigherHigh(barsAgo, 1) && isLowerLow(barsAgo, 1);
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

				if (barType == 'i' && !isInsideBar(i + barsAgo))
				{
					return false;
				}

				if (barType == 'o' && !isOutsideBar(i + barsAgo))
				{
					return false;
				}
			}

			return true;
		}
		#endregion

		#region isBuyReversalBar()
		public bool isBuyReversalBar(int barsAgo)
		{
			double midpoint = (High[barsAgo] + Low[barsAgo]) / 2;

			return Close[barsAgo] > midpoint;
		}
		#endregion

		#region isBullishBuyReversalBar()
		public bool isBullishBuyReversalBar(int barsAgo)
		{
			return isBuyReversalBar(barsAgo) && isBullishBar(barsAgo);
		}
		#endregion

		#region isBearishBuyReversalBar()
		public bool isBearishBuyReversalBar(int barsAgo)
		{
			return isBuyReversalBar(barsAgo) && isBearishBar(barsAgo);
		}
		#endregion

		#region isSellReversalBar()
		public bool isSellReversalBar(int barsAgo)
		{
			double midpoint = (High[barsAgo] + Low[barsAgo]) / 2;

			return Close[barsAgo] < midpoint;
		}
		#endregion

		#region isBullishSellReversalBar()
		public bool isBullishSellReversalBar(int barsAgo)
		{
			return isSellReversalBar(barsAgo) && isBullishBar(barsAgo);
		}
		#endregion

		#region isBearishSellReversalBar()
		public bool isBearishSellReversalBar(int barsAgo)
		{
			return isSellReversalBar(barsAgo) && isBearishBar(barsAgo);
		}
		#endregion

		#region isBreakout()
		public bool isBreakout(int barsAgo, double priceOfInterest)
		{
			if (Close[barsAgo + 1] > priceOfInterest) {
				return Low[barsAgo] < priceOfInterest;
			}

			return High[barsAgo] > priceOfInterest;
		}
		#endregion

		#region isStrongBreakout()
		public bool isStrongBreakout(int barsAgo, double priceOfInterest)
		{
			int prev = barsAgo + 1;

			if (Close[prev] > priceOfInterest) {
				return Close[barsAgo] < Low[prev];
			}

			return Close[barsAgo] > High[prev];
		}
		#endregion

		#region getBuyingPressure()
		public double getBuyingPressure(int barsAgo, int period)
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

		#region getSellingPressure()
		public double getSellingPressure(int barsAgo, int period)
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

		#region getBuySellPressure()
		public double getBuySellPressure(int barsAgo, int period)
		{
			double balance = 50;

			double bullValue = getBuyingPressure(barsAgo, period) * 50;
			double bearValue = getSellingPressure(barsAgo, period) * 50;

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
			if (getTrendDirection(barsAgo, period) == TrendDirection.Bullish) {
				return NumberOfBullPullbacks(barsAgo, period);
			}

			if (getTrendDirection(barsAgo, period) == TrendDirection.Bearish) {
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
			if (getTrendDirection(barsAgo, period) == TrendDirection.Bullish) {
				return AverageBarsInBullTrendPullback(barsAgo, period);
			}

			if (getTrendDirection(barsAgo, period) == TrendDirection.Bearish) {
				return AverageBarsInBearTrendPullback(barsAgo, period);
			}

			return 0;
		}
		#endregion

		#region isBreakoutTrend()
		public bool isBreakoutTrend(int barsAgo, int period, TrendDirection direction)
		{
			return NumberOfPullbacksInTrend(barsAgo, period, direction) <= 1 && AveragePullbackLength(barsAgo, period, direction) <= 2 && period >= 4;
		}
		#endregion

		#region isBreakoutTrend()
		public bool isBreakoutTrend(int barsAgo, int period)
		{
			return NumberOfPullbacksInTrend(barsAgo, period) <= 1 && AveragePullbackLength(barsAgo, period) <= 2 && period >= 4;
		}
		#endregion

		#region largestPullbackInTrend()
		public double largestPullbackInBullTrend(int barsAgo, int period)
		{
			double low = Low[barsAgo + period];
			double high = High[barsAgo + period];;

			for (int i = barsAgo + period; i > barsAgo; i--) {
				if (isHigherHigh(i, 1)) {
					high = High[i];
					low = Low[i];
				}

				if (isLowerLow(i, 1)) {
					high = Math.Max(high, High[i]);
					low = Math.Min(low, Low[i]);
				}
			}

			return high - low;
		}

		public double largestPullbackInBearTrend(int barsAgo, int period)
		{
			double low = Low[barsAgo + period];
			double high = High[barsAgo + period];

			for (int i = barsAgo + period; i > barsAgo; i--) {
				if (isLowerLow(i, 1)) {
					high = High[i];
					low = Low[i];
				}

				if (isHigherHigh(i, 1)) {
					high = Math.Max(high, High[i]);
					low = Math.Min(low, Low[i]);
				}
			}

			return high - low;
		}

		public double largestPullbackInTrend(int barsAgo, int period, TrendDirection direction) {
			if (period == 0) {
				return 0;
			}

			double totalRange = MAX(High, period)[barsAgo] - MIN(Low, period)[barsAgo];

			if (direction == TrendDirection.Bullish) {
				return largestPullbackInBullTrend(barsAgo, period) / totalRange;
			}

			if (direction == TrendDirection.Bearish) {
				return largestPullbackInBearTrend(barsAgo, period) / totalRange;
			}

			return 0;
		}

		public double largestPullbackInTrend(int barsAgo, int period) {
			if (period == 0) {
				return 0;
			}

			if (isRising(barsAgo, period)) {
				return largestPullbackInTrend(barsAgo, period, TrendDirection.Bullish);
			}

			if (isFalling(barsAgo, period)) {
				return largestPullbackInTrend(barsAgo, period, TrendDirection.Bearish);
			}

			return 0;
		}
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
