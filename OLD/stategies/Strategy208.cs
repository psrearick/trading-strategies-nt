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
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

//This namespace holds Strategies in this folder and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Strategies
{
	public class Strategy208 : Strategy
	{
		private Order longEntry;
        private Order shortEntry;
        private Order takeProfitOrder;
        private Order stopLossOrder;

		private DateTime OpenTime;
		private DateTime CloseTime;
		private DateTime LastTradeTime;

		private int zzLookback = 100;

		private bool longPatternMatched;
		private bool shortPatternMatched;

		private double profitDistance;
		private double stopLossDistance;

		private EMA ema;
		private ATR atr;
		private ZigZag zz;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Trend Following Price Action Strategy";
				Name										= "Strategy 2.08";
				Calculate									= Calculate.OnBarClose;
				EntriesPerDirection							= 1;
				EntryHandling								= EntryHandling.AllEntries;
				IsExitOnSessionCloseStrategy				= true;
				ExitOnSessionCloseSeconds					= 30;
				IsFillLimitOnTouch							= false;
				MaximumBarsLookBack							= MaximumBarsLookBack.TwoHundredFiftySix;
				OrderFillResolution							= OrderFillResolution.Standard;
				Slippage									= 0;
				StartBehavior								= StartBehavior.WaitUntilFlat;
				TimeInForce									= TimeInForce.Day;
				TraceOrders									= false;
				RealtimeErrorHandling						= RealtimeErrorHandling.StopCancelCloseIgnoreRejects;
				StopTargetHandling							= StopTargetHandling.PerEntryExecution;
				BarsRequiredToTrade							= 20;
				IsInstantiatedOnEachOptimizationIteration	= false;
				IsUnmanaged									= false;

				Risk										= 0;
				TradeQuantity								= 1;
				TimeShift									= 0;
				ZZLength									= 0.5;
				OpenTime									= DateTime.Parse("10:00", System.Globalization.CultureInfo.InvariantCulture);
				CloseTime									= DateTime.Parse("15:30", System.Globalization.CultureInfo.InvariantCulture);
				LastTradeTime								= DateTime.Parse("15:00", System.Globalization.CultureInfo.InvariantCulture);
			}
			else if (State == State.Configure)
			{
				AddDataSeries(Data.BarsPeriodType.Second, 1);
			}
			if (State == State.DataLoaded) {
				ema			= EMA(9);
				atr 		= ATR(14);
				zz			= ZigZag(DeviationType.Points, ZZLength, false);
			}
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBar < BarsRequiredToTrade || CurrentBars[0] < 1 || CurrentBars[1] < 1) {
				return;
            }

			exitPositions();

			if (BarsInProgress == 0) {
				setIndicators();
				setEntries();
			}
		}

		private void exitPositions()
		{
			bool isLong 	= Position.MarketPosition == MarketPosition.Long;
			bool isShort 	= Position.MarketPosition == MarketPosition.Short;
			bool toExit		= false;

			if (!isLong && !isShort) {
				return;
			}

			if (!isValidTradeTime()) {
				toExit = true;
			}

			if (isLong && zz.HighBar(0, 1, zzLookback) == 1) {
				toExit = true;
			}

			if (isShort && zz.LowBar(0, 1, zzLookback) == 1) {
				toExit = true;
			}

			if (!toExit) {
				return;
			}

			if (isLong) {
				ExitLong();
			}

			if (isShort) {
				ExitShort();
			}
        }

		private void setIndicators()
		{
			longPatternMatched 		= Close[0] > ema[0];
			shortPatternMatched		= Close[0] < ema[0];
		}

		private bool isValidEntryTime(int time = 0)
		{
			int shift	= ToTime(0, (int)Math.Floor(60 * TimeShift), 0);
			int now 	= time > 0 ? time : ToTime(Time[0]);

			if (now < ToTime(OpenTime) + shift) {
				return false;
			}

			if (now > ToTime(LastTradeTime) + shift) {
				return false;
			}

			return true;
		}

		private bool isValidTradeTime(int time = 0)
		{
			int shift	= ToTime(0, (int)Math.Floor(60 * TimeShift), 0);
			int now 	= time > 0 ? time : ToTime(Time[0]);

			if (now > ToTime(CloseTime) + shift) {
				return false;
			}

			if (now < ToTime(OpenTime) + shift) {
				return false;
			}

			return true;
		}

		private void resetTargetDistance()
		{
			profitDistance = 0;
			stopLossDistance = 0;
		}

		private void calculateTargetDistance()
		{
			resetTargetDistance();

			if (CurrentBar < zzLookback) {
				return;
			}

			if (zz.HighBar(0, 1, zzLookback) == -1) {
				return;
			}

			if (zz.LowBar(0, 1, zzLookback) == -1) {
				return;
			}

			if (!longPatternMatched && !shortPatternMatched) {
				return;
			}

			int[] pivots 			= new int[zzLookback * 2];
			int pivotPosition		= 0;

			for (int i = 1; i < zzLookback; i++)
			{
				if (zz.HighBar(0, i, zzLookback) > -1) {
					pivots[pivotPosition] = zz.HighBar(0, i, zzLookback);
					pivotPosition++;
				}

				if (zz.LowBar(0, i, zzLookback) > -1) {
					pivots[pivotPosition] = zz.LowBar(0, i, zzLookback);
					pivotPosition++;
				}
			}

			int[] pivotsOrdered = pivots.Where(x => x != 0).ToArray();
			Array.Sort(pivotsOrdered);

			evaluateLongTrend(pivotsOrdered);
			evaluateShortTrend(pivotsOrdered);
		}

		private void evaluateLongTrend(int[] pivots)
		{
			if (pivots.Length < 3) {
				return;
			}

			if (MAX(High, pivots[1])[0] < MAX(High, 10)[0]) {
				return;
			}

			if (!longPatternMatched) {
				return;
			}

			if (Close[pivots[1]] < Close[pivots[0]]) {
				return;
			}

			if (pivots[0] != 1) {
				return;
			}

			double trendStart = Close[pivots[2]];

			if (Close[pivots[0]] <= trendStart) {
				return;
			}

			double topPivot 		= Close[pivots[1]];
			double tracebackBottom	= Close[pivots[0]];
			double move				= topPivot - trendStart;
			double profitTarget		= tracebackBottom + move;
			double stopLoss			= trendStart - 0.25;
			double ptDistance		= profitTarget - Close[0];
			double slDistance		= Close[0] - stopLoss;
			double halfRetracement	= (trendStart + topPivot) / 2;

			if (Close[0] < halfRetracement) {
				return;
			}

			if (ptDistance < atr[0]) {
				return;
			}

			if (slDistance < atr[0]) {
				return;
			}

			if (Risk > 0 && (Risk * atr[0]) < slDistance) {
				slDistance = Risk * atr[0];
			}


			profitDistance 		= ptDistance * 4;
			stopLossDistance	= slDistance * 4;
		}

		private void evaluateShortTrend(int[] pivots)
		{
			if (pivots.Length < 3) {
				return;
			}

			if (MIN(Low, pivots[1])[0] > MIN(Low, 10)[0]) {
				return;
			}

			if (!shortPatternMatched) {
				return;
			}

			if (Close[pivots[1]] > Close[pivots[0]]) {
				return;
			}

			if (pivots[0] != 1) {
				return;
			}

			double trendStart = Close[pivots[2]];

			if (Close[pivots[0]] >= trendStart) {
				return;
			}

			double bottomPivot 		= Close[pivots[1]];
			double tracebackTop		= Close[pivots[0]];
			double move				= trendStart - bottomPivot;
			double profitTarget		= tracebackTop - move;
			double stopLoss			= trendStart + 0.25;
			double ptDistance		= Close[0] - profitTarget;
			double slDistance		= stopLoss - Close[0];
			double halfRetracement	= (trendStart + bottomPivot) / 2;

			if (Close[0] > halfRetracement) {
				return;
			}

			if (ptDistance < atr[0]) {
				return;
			}

			if (slDistance < atr[0]) {
				return;
			}

			if (Risk > 0 && (Risk * atr[0]) < slDistance) {
				slDistance = Risk * atr[0];
			}

			profitDistance 		= ptDistance * 4;
			stopLossDistance	= slDistance * 4;
		}

		private void setEntries()
		{
			if (!isValidEntryTime()) {
				return;
            }

			if (Position.MarketPosition != MarketPosition.Flat) {
				return;
			}

			calculateTargetDistance();

			if (stopLossDistance <= 0) {
				return;
			}

			if (profitDistance <= 0) {
				return;
			}

			SetStopLoss(CalculationMode.Ticks, stopLossDistance);
			//SetProfitTarget(CalculationMode.Ticks, profitDistance);

			if (longPatternMatched) {
				longEntry = EnterLong(TradeQuantity, "longEntry");
			}

			if (shortPatternMatched) {
				shortEntry = EnterShort(TradeQuantity, "shortEntry");
			}
		}

		#region Properties

		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name="Risk", Description="Risk", Order=1, GroupName="Parameters")]
		public int Risk
		{ get; set; }

		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name="Trade Quantity", Description="Trade Quantity", Order=2, GroupName="Parameters")]
		public int TradeQuantity
		{ get; set; }

		[NinjaScriptProperty]
		[Range(double.MinValue, double.MaxValue)]
		[Display(Name="ZZ Length", Description="ZZ Length", Order=3, GroupName="Parameters")]
		public double ZZLength
		{ get; set; }

		[NinjaScriptProperty]
		[Range(double.MinValue, double.MaxValue)]
		[Display(Name="Time Shift (Hours)", Description="Time Shift", Order=5, GroupName="Parameters")]
		public double TimeShift
		{ get; set; }

		#endregion
	}
}
