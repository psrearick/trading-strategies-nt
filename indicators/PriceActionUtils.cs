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
		public EMA EmaFast;
		public EMA EmaSlow;
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
				Atr 	= ATR(14);
				EmaFast	= EMA(9);
				EmaSlow	= EMA(21);
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

		#region Chart Patterns

		#region IsBigger()
		public bool IsBigger(int index)
		{
			return RealBodySize(index) > RealBodySize(index + 1);
		}
		#endregion

		#region IsSmaller()
		public bool IsSmaller(int index)
		{
			return RealBodySize(index) < RealBodySize(index + 1);
		}
		#endregion

		#region IsConsecutiveBullBars()
		public bool IsConsecutiveBullBars(int barsAgo)
		{
			return IsBullishBar(barsAgo) && IsBullishBar(barsAgo + 1);
		}
		#endregion

		#region IsConsecutiveBearBars()
		public bool IsConsecutiveBearBars(int barsAgo)
		{
			return IsBearishBar(barsAgo) && IsBearishBar(barsAgo + 1);
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

		#region IsBearishFallingBar()
		public bool IsBearishFallingBar(int barsAgo)
		{
			return IsBearishBar(barsAgo) && IsFallingBar(barsAgo);
		}
		#endregion

		#region IsBullishRisingBar()
		public bool IsBullishRisingBar(int barsAgo)
		{
			return IsBullishBar(barsAgo) && IsRisingBar(barsAgo);
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

		#region BarsAgoHigh()
		public int BarsAgoHigh(int barsAgo = 0, int period = 12)
		{
			int barsAgoHigh = 0;
			double high = High[0];

			for (int i = barsAgo; i < barsAgo + period; i++) {
				if (High[i] >= high) {
					high = High[i];
					barsAgoHigh = i;
				}
			}

			return barsAgoHigh;
		}
		#endregion

		#region BarsAgoLow()
		public int BarsAgoLow(int barsAgo = 0, int period = 12)
		{
			int barsAgoLow = 0;
			double low = Low[0];

			for (int i = barsAgo; i < barsAgo + period; i++) {
				if (Low[i] <= low) {
					low = Low[i];
					barsAgoLow = i;
				}
			}

			return barsAgoLow;
		}
		#endregion

		#endregion

		#region Trend Recognition

		#region GetTrendDirection()
		// get the trend direction over the last `length` bars, starting `barsAgo` bars before the current bar
		public TrendDirection GetTrendDirection(int barsAgo = 0, int period = 10)
		{
			if (Close.Count < period + barsAgo) return TrendDirection.Flat;

			int bullishBars = NumberOfBullishRisingBars(barsAgo, period);
			int bearishBars = NumberOfBearishFallingBars(barsAgo, period);

			if (bullishBars > bearishBars) {
				return TrendDirection.Bullish;
			}

			if (bullishBars < bearishBars) {
				return TrendDirection.Bearish;
			}

			return TrendDirection.Flat;
		}
		#endregion

		#region GetWeakTrendDirection()
		public TrendDirection GetWeakTrendDirection(int barsAgo, int period)
		{
			return IsWeakBullishTrend(barsAgo, period)
				? TrendDirection.Bullish
				: IsWeakBearishTrend(barsAgo, period)
					? TrendDirection.Bearish
					: TrendDirection.Flat;
		}
		#endregion

		#region IsWeakBullishTrend()
		public bool IsWeakBullishTrend(int barsAgo, int period)
		{
			return IsBullishBarSizes(barsAgo, period)
				&& IsEMABullish(barsAgo)
				&& GetTrendDirection(barsAgo, period) == TrendDirection.Bullish;
		}
		#endregion

		#region IsWeakBearishTrend()
		public bool IsWeakBearishTrend(int barsAgo, int period)
		{
			return IsBearishBarSizes(barsAgo, period)
				&& IsEMABearish(barsAgo)
				&& GetTrendDirection(barsAgo, period) == TrendDirection.Bearish;
		}
		#endregion

		#region GetStrongTrendDirection()
		public TrendDirection GetStrongTrendDirection(int barsAgo, int period)
		{
			return IsStrongBullishTrend(barsAgo, period)
				? TrendDirection.Bullish
				: IsStrongBearishTrend(barsAgo, period)
					? TrendDirection.Bearish
					: TrendDirection.Flat;
		}
		#endregion

		#region IsStrongBullishTrend()
		public bool IsStrongBullishTrend(int barsAgo, int period)
		{
			return IsBullishBarSizes(barsAgo, period)
				&& IsEMABullishDivergence(barsAgo, period)
				&& GetTrendDirection(barsAgo, period) == TrendDirection.Bullish;
		}
		#endregion

		#region IsStrongBearishTrend()
		public bool IsStrongBearishTrend(int barsAgo, int period)
		{
			return IsBearishBarSizes(barsAgo, period)
				&& IsEMABearishDivergence(barsAgo, period)
				&& GetTrendDirection(barsAgo, period) == TrendDirection.Bearish;
		}
		#endregion

		#region IsBullishBarSizes()
		public bool IsBullishBarSizes(int barsAgo, int period)
		{
			return AverageBullishBarSizes(barsAgo, period) > AverageBearishBarSizes(barsAgo, period);
		}
		#endregion

		#region IsBearishBarSizes()
		public bool IsBearishBarSizes(int barsAgo, int period)
		{
			return AverageBullishBarSizes(barsAgo, period) < AverageBearishBarSizes(barsAgo, period);
		}
		#endregion

		#region AverageBullishBarSizes()
		public double AverageBullishBarSizes(int barsAgo, int period)
		{
			int bullishBars 	= 0;
			double bullishATRs	= 0;

			for (int i = barsAgo; i < (barsAgo + period); i++) {
				if (!IsBullishBar(i)) {
					continue;
				}

				bullishBars++;
				bullishATRs += Atr[barsAgo];
			}

			if (bullishBars == 0) {
				return 0;
			}

			return bullishATRs / bullishBars;
		}
		#endregion

		#region AverageBearishBarSizes()
		public double AverageBearishBarSizes(int barsAgo, int period)
		{
			int bearishBars		= 0;
			double bearishATRs	= 0;

			for (int i = barsAgo; i < (barsAgo + period); i++) {
				if (!IsBearishBar(i)) {
					continue;
				}

				bearishBars++;
				bearishATRs += Atr[barsAgo];
			}

			if (bearishBars == 0) {
				return 0;
			}

			return bearishATRs / bearishBars;
		}
		#endregion

		#region IsEMADiverging()
		public bool IsEMADiverging(int barsAgo, int period = 1)
		{
			return GetDistanceBetweenEmas(barsAgo) > GetDistanceBetweenEmas(barsAgo + period);
		}
		#endregion

		#region IsEMAConverging()
		public bool IsEMAConverging(int barsAgo, int period = 1)
		{
			return GetDistanceBetweenEmas(barsAgo) < GetDistanceBetweenEmas(barsAgo + period);
		}
		#endregion

		#region IsEMABullishDivergence()
		public bool IsEMABullishDivergence(int barsAgo, int period = 1)
		{
			return IsEMABullish(barsAgo) && IsEMADiverging(barsAgo, period);
		}
		#endregion

		#region IsEMABearishDivergence()
		public bool IsEMABearishDivergence(int barsAgo, int period = 1)
		{
			return IsEMABearish(barsAgo) && IsEMADiverging(barsAgo, period);
		}
		#endregion

		#region IsEMABullishConvergence()
		public bool IsEMABullishConvergence(int barsAgo, int period = 1)
		{
			return IsEMABullish(barsAgo) && IsEMAConverging(barsAgo, period);
		}
		#endregion

		#region IsEMABearishConvergence()
		public bool IsEMABearishConvergence(int barsAgo, int period = 1)
		{
			return IsEMABearish(barsAgo) && IsEMAConverging(barsAgo, period);
		}
		#endregion

		#region IsEMABullish()
		public bool IsEMABullish(int barsAgo)
		{
			return EmaFast[barsAgo] > EmaSlow[barsAgo];
		}
		#endregion

		#region IsEMABearish()
		public bool IsEMABearish(int barsAgo)
		{
			return EmaFast[barsAgo] < EmaSlow[barsAgo];
		}
		#endregion

		#region GetEMADirection()
		public TrendDirection GetEMADirection(int barsAgo)
		{
			return IsEMABearish(barsAgo) ? TrendDirection.Bearish : IsEMABullish(barsAgo) ? TrendDirection.Bullish : TrendDirection.Flat;
		}
		#endregion

		#region GetDistanceBetweenEmas()
		public double GetDistanceBetweenEmas(int barsAgo)
		{
			return Math.Abs(EmaFast[barsAgo] - EmaSlow[barsAgo]);
		}
		#endregion
		#endregion

		#region Signals

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

		#endregion

		#region Counting Bar Patterns

		#region Average Value of Bars Matching Pattern

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

		#endregion

		#region Consecutive Bars Matching Pattern

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

		#endregion

		#region Number of Bars Matching Pattern
		#region NumberOfConsecutiveBearBars()
		public int NumberOfConsecutiveBearBars(int barsAgo, int period)
		{
			return NumberOfOccurrencesInPeriod(barsAgo, period, IsConsecutiveBearBars);
		}
		#endregion

		#region NumberOfConsecutiveBullBars()
		public int NumberOfConsecutiveBullBars(int barsAgo, int period)
		{
			return NumberOfOccurrencesInPeriod(barsAgo, period, IsConsecutiveBullBars);
		}
		#endregion

		#region NumberOfBearBars()
		public int NumberOfBearBars(int barsAgo, int period)
		{
			return NumberOfOccurrencesInPeriod(barsAgo, period, IsBearishBar);
		}
		#endregion

		#region NumberOfBullBars()
		public int NumberOfBullBars(int barsAgo, int period)
		{
			return NumberOfOccurrencesInPeriod(barsAgo, period, IsBullishBar);
		}
		#endregion

		#region NumberOfBearishFallingBars()
		public int NumberOfBearishFallingBars(int barsAgo, int period)
		{
			return NumberOfOccurrencesInPeriod(barsAgo, period, IsBearishFallingBar);
		}
		#endregion

		#region NumberOfBullishRisingBars()
		public int NumberOfBullishRisingBars(int barsAgo, int period)
		{
			return NumberOfOccurrencesInPeriod(barsAgo, period, IsBullishRisingBar);
		}
		#endregion

		#endregion

		#region Least Bars Matching Pattern
		#region LeastBarsUp()
		public bool LeastBarsUp(int count, int period, int barsAgo = 0)
		{
			return NumberOfOccurrencesInPeriod(barsAgo, period, IsUp) >= count;
		}
		#endregion

		#region LeastBarsDown()
		public bool LeastBarsDown(int count, int period, int barsAgo = 0)
		{
			return NumberOfOccurrencesInPeriod(barsAgo, period, IsDown) >= count;
		}
		#endregion

		#region LeastSmallBars()
		public bool LeastSmallBars(int count, int period, int barsAgo = 0)
		{
			return NumberOfOccurrencesInPeriod(barsAgo, period, IsSmall) >= count;
		}
		#endregion

		#region LeastBigBars()
		public bool LeastBigBars(int count, int period, int barsAgo = 0)
		{
			return NumberOfOccurrencesInPeriod(barsAgo, period, IsBig) >= count;
		}
		#endregion

		#region LeastSmallerBars()
		public bool LeastSmallerBars(int count, int period, int barsAgo = 0)
		{
			return NumberOfOccurrencesInPeriod(barsAgo, period, IsSmaller) >= count;
		}
		#endregion

		#region LeastBiggerBars()
		public bool LeastBiggerBars(int count, int period, int barsAgo = 0)
		{
			return NumberOfOccurrencesInPeriod(barsAgo, period, IsBigger) >= count;
		}
		#endregion
		#endregion

		#endregion

		#region Bar Patterns

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

		#region ClosedNearExtreme()
		public bool ClosedNearExtreme(int index)
		{
			if (Close[index] > Open[index]) {
				return ((Close[index] - Low[index]) / (High[index] - Low[index])) > 0.75;
			}

			return ((High[index] - Close[index]) / (High[index] - Low[index])) > 0.75;
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

		#endregion

		#region Reversal Bar

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

		#endregion

		#region Breakouts

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

		#endregion

		#region Bar Construction

		#region InPortionOfBarRange()
		// determine how far a value is between the Low and High of a bar at `barsAgo`. The range is 0 - 100 with 0 being the Low and 100 being the High.
		public bool InPortionOfBarRange(ISeries<double> source, int barsAgo, double lowestValue, double highestValue)
		{
			double reference = source[barsAgo] - Low[barsAgo];
			double range = High[barsAgo] - Low[barsAgo];
			double position = (reference / range) * 100;

			return position >= lowestValue && position <= highestValue;
		}

		public bool InPortionOfBarRange(int barsAgo, double lowestValue, double highestValue)
		{
			return InPortionOfBarRange(Close, barsAgo, lowestValue, highestValue);
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
			return NumberOfOccurrencesInPeriod(barsAgo, period, InPortionOfBarRange, 50, 100);
		}
		#endregion

		#region NumberOfBarsClosingNearLow()
		public int NumberOfBarsClosingNearLow(int barsAgo, int period)
		{
			return NumberOfOccurrencesInPeriod(barsAgo, period, InPortionOfBarRange, 0, 50);
		}
		#endregion

		#endregion

		#region Pullbacks

		#region NumberOfBearPullbacks()
		public int NumberOfBearPullbacks(int barsAgo, int period)
		{
			return NumberOfOccurrencesInPeriod(barsAgo, period, IsHigherHigh);
		}
		#endregion

		#region NumberOfBullPullbacks()
		public int NumberOfBullPullbacks(int barsAgo, int period)
		{
			return NumberOfOccurrencesInPeriod(barsAgo, period, IsLowerLow);
		}
		#endregion

		#region AverageBarsInTrendPullback()
		private int AverageBarsInTrendPullback(int barsAgo, int period, ISeries <double> series, Func <int, int, bool> callback)
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

		#endregion

		#region BuyingPressure
		#region GetBuyingPressure()
		// gets buying pressure as a number between 0 and 1, the higher the number, the greater the pressure
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
		// gets selling pressure as a number between 0 and 1, the higher the number, the greater the pressure
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
		// gets a numeric value representing the buying power
		// the value is between 0 and 100, 50 is even, above 50 is bullish pressure, below 50 is bearish pressure
		public double GetBuySellPressure(int barsAgo, int period)
		{
			double balance = 50;

			double bullValue = GetBuyingPressure(barsAgo, period) * 50;
			double bearValue = GetSellingPressure(barsAgo, period) * 50;

			return 50 + bullValue - bearValue;
		}
		#endregion
		#endregion

		#region TEMPLATE METHODS
		#region Delegates
		public delegate bool IBCallbackDelegate(int arg1);
		public delegate bool IIBCallbackDelegate(int arg1, int arg2);
		public delegate bool IDDBCallbackDelegate(int arg1, double arg2, double arg3);
		#endregion

		#region NumberOfOccurrencesInPeriod()

		private int NumberOfOccurrencesInPeriod(int barsAgo, int period, IBCallbackDelegate criteria)
		{
			int barsThatMeetCriteria = 0;
			int maxRange = Math.Min((period + barsAgo), Close.Count);

			for (int i = barsAgo; i < maxRange; i++) {
				if (criteria(i)) {
					barsThatMeetCriteria++;
				}
			}

			return barsThatMeetCriteria;
		}

		private int NumberOfOccurrencesInPeriod(int barsAgo, int period, IIBCallbackDelegate criteria, int criteriaPeriod = 1)
		{
			int barsThatMeetCriteria = 0;
			int maxRange = Math.Min((period + barsAgo), Close.Count);

			for (int i = barsAgo; i < maxRange; i++) {
				if (criteria(i, criteriaPeriod)) {
					barsThatMeetCriteria++;
				}
			}

			return barsThatMeetCriteria;
		}

		private int NumberOfOccurrencesInPeriod(int barsAgo, int period, IDDBCallbackDelegate criteria, int arg1, int arg2)
		{
			int barsThatMeetCriteria = 0;
			int maxRange = Math.Min((period + barsAgo), Close.Count);

			for (int i = barsAgo; i < maxRange; i++) {
				if (criteria(i, arg1, arg2)) {
					barsThatMeetCriteria++;
				}
			}

			return barsThatMeetCriteria;
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
