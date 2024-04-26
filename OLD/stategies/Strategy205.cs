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
	public class Strategy205 : Strategy
	{
		private Order longEntry;
        private Order shortEntry;
        private Order takeProfitOrder;
        private Order stopLossOrder;

		private DateTime OpenTime;
		private DateTime CloseTime;
		private DateTime LastTradeTime;

		private int tradeDirection;
		private int quantity;

		private bool longPatternMatched;
		private bool shortPatternMatched;

		private EMA emaShort;
		private EMA emaLong;
		private ATR atr;
		private ATR atrDaily;
		private ChoppinessIndex chop;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Trend Following Price Action Strategy";
				Name										= "Strategy 2.05";
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
				TimeInForce									= TimeInForce.Gtc;
				TraceOrders									= false;
				RealtimeErrorHandling						= RealtimeErrorHandling.StopCancelCloseIgnoreRejects;
				StopTargetHandling							= StopTargetHandling.PerEntryExecution;
				BarsRequiredToTrade							= 20;
				// Disable this property for performance gains in Strategy Analyzer optimizations
				// See the Help Guide for additional information
				IsInstantiatedOnEachOptimizationIteration	= false;
				IsUnmanaged									= true;

				Risk										= 0;
				TradeQuantity								= 1;
				TargetMultiplier							= 2;
				ProfitTarget								= 0.2;
				StopLossTarget								= 0.8;
				ChoppinessThresholdLow						= 45;
				HighATRMultiplier							= 1.5;
				OpenTime									= DateTime.Parse("10:00", System.Globalization.CultureInfo.InvariantCulture);
				CloseTime									= DateTime.Parse("15:30", System.Globalization.CultureInfo.InvariantCulture);
				LastTradeTime								= DateTime.Parse("15:00", System.Globalization.CultureInfo.InvariantCulture);
			}
			else if (State == State.Configure)
			{
				//AddDataSeries(Data.BarsPeriodType.Tick, 1);
				AddDataSeries(Data.BarsPeriodType.Second, 1);
				AddDataSeries(Data.BarsPeriodType.Minute, 30);
				//AddDataSeries(Data.BarsPeriodType.Day, 1);
			}
			if (State == State.DataLoaded) {
				emaShort			= EMA(9);
				emaLong				= EMA(21);
				atr 				= ATR(14);
				atrDaily			= ATR(BarsArray[2], 14);
				chop				= ChoppinessIndex(14);
			}
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBar < BarsRequiredToTrade || CurrentBars[0] < 1 || CurrentBars[1] < 1 || CurrentBars[2] < 1) {
				return;
            }

			if (BarsInProgress != 0) {
				return;
			}

			calculateQuantity();
			setIndicators();
			exitPositions();
			setEntries();
		}

		protected override void OnExecutionUpdate(Cbi.Execution execution, string executionId, double price, int orderQuantity,
			Cbi.MarketPosition marketPosition, string orderId, DateTime time)
		{
			string ocoString 				= Guid.NewGuid().ToString();
//			double profitDistance			= Math.Round(atrDaily[0] * TargetMultiplier * ProfitTarget) * 0.25;
//			double stopLossDistance			= Math.Round(atrDaily[0] * TargetMultiplier * StopLossTarget) * 0.25;

//			if (atr[0] > (atrDaily[0] * 0.125)) {
//				profitDistance = Math.Round(profitDistance * HighATRMultiplier) * 0.25;
//			}

			double profitDistance			= atrDaily[0] * TargetMultiplier * ProfitTarget;
			double stopLossDistance			= atrDaily[0] * TargetMultiplier * StopLossTarget;

			if (atr[0] > (atrDaily[0] * 0.125)) {
				profitDistance = profitDistance * HighATRMultiplier;
			}

			if (longEntry != null && execution.Order == longEntry) {
				tradeDirection   = 1;
				stopLossOrder    = SubmitOrderUnmanaged(1, OrderAction.Sell, OrderType.StopMarket, longEntry.Filled, 0, execution.Order.AverageFillPrice - stopLossDistance, ocoString, "longStopLoss");
				takeProfitOrder  = SubmitOrderUnmanaged(1, OrderAction.Sell, OrderType.Limit, longEntry.Filled, execution.Order.AverageFillPrice + profitDistance, 0, ocoString, "longProfitTarget");
			}

			if (shortEntry != null && execution.Order == shortEntry) {
				tradeDirection   = -1;
				stopLossOrder    = SubmitOrderUnmanaged(1, OrderAction.BuyToCover, OrderType.StopMarket, shortEntry.Filled, 0, execution.Order.AverageFillPrice + stopLossDistance, ocoString, "shortStopLoss");
				takeProfitOrder  = SubmitOrderUnmanaged(1, OrderAction.BuyToCover, OrderType.Limit, shortEntry.Filled, execution.Order.AverageFillPrice - profitDistance, 0, ocoString, "shortProfitTarget");
			}

			if (execution.Name == "Exit on session close" || execution.Name == "longProfitTarget" || execution.Name == "longStopLoss" || execution.Name == "shortExitCondition" || execution.Name == "longExitCondition" || execution.Name == "shortProfitTarget" || execution.Name == "shortStopLoss") {
				resetOrders();
			}
		}

		private void calculateQuantity()
		{
			quantity = TradeQuantity;

			if (Risk > 0) {
				double stopLossDistance	= Math.Round(atrDaily[0] * TargetMultiplier * StopLossTarget) * 50;
				quantity = (int) Math.Floor(Math.Max(quantity, Risk / stopLossDistance));
			}
		}

		private void exitPositions() {
			if (Position.MarketPosition == MarketPosition.Flat) {
				return;
			}

			if (isValidTradeTime()) {
				return;
			}

			int orderQuantity	= tradeDirection == 1 ? longEntry.Filled : shortEntry.Filled;

			if (tradeDirection == 1) {
				SubmitOrderUnmanaged(1, OrderAction.Sell, OrderType.Market, orderQuantity, 0, 0, string.Empty, "longExitCondition");
			}

			if (tradeDirection == -1) {
				SubmitOrderUnmanaged(1, OrderAction.BuyToCover, OrderType.Market, orderQuantity, 0, 0, string.Empty, "shortExitCondition");
			}

			CancelOrder(stopLossOrder);
			CancelOrder(takeProfitOrder);
        }

		private void resetOrders()
		{
			if (longEntry != null) {
				CancelOrder(longEntry);
				longEntry = null;
			}

			if (shortEntry != null) {
				CancelOrder(shortEntry);
				shortEntry = null;
			}

			if (stopLossOrder != null) {
				CancelOrder(stopLossOrder);
				stopLossOrder = null;
			}

			if (takeProfitOrder != null) {
				CancelOrder(takeProfitOrder);
				takeProfitOrder = null;
			}

			tradeDirection = 0;
		}

		private void setIndicators()
		{
			bool emaShortRising 	= emaShort[0] > emaShort[1];
			bool emaShortFalling	= emaShort[0] < emaShort[1];
			bool emaLongRising		= emaLong[0] > emaLong[1];
			bool emaLongFalling		= emaLong[0] < emaLong[1];
			bool maRising			= emaShortRising && emaLongRising;
			bool maFalling			= emaShortFalling && emaLongFalling;

			bool lowChop			= chop[0] < ChoppinessThresholdLow;

			bool rising1			= Close[0] > Close[1];
			bool rising2			= Close[1] > Close[2];
			bool rising3			= Close[2] > Close[3];
			bool rising				= rising1 && rising2 && rising3;

			bool falling1			= Close[0] < Close[1];
			bool falling2			= Close[1] < Close[2];
			bool falling3			= Close[2] < Close[3];
			bool falling			= falling1 && falling2 && falling3;

			bool newHigh			= Close[0] > MAX(High, 10)[1];
			bool newLow				= Close[0] < MIN(Low, 10)[1];

			bool higherHigh1		= High[0] > High[1];
			bool higherHigh2		= High[1] > High[2];
			bool higherHigh3		= High[2] > High[3];
			bool higherHigh			= higherHigh1 && higherHigh2 && higherHigh3;

			bool lowerLow1			= Low[0] < Low[1];
			bool lowerLow2			= Low[1] < Low[2];
			bool lowerLow3			= Low[2] < Low[3];
			bool lowerLow			= lowerLow1 && lowerLow2 && lowerLow3;

			bool highestInTrend		= MAX(High, 3)[0] >= MAX(High, 10)[0];
			bool lowestInTrend		= MIN(Low, 3)[0] <= MIN(Low, 10)[0];

			bool upTrend 			= higherHigh && highestInTrend;
			bool downTrend			= lowerLow && lowestInTrend;

			longPatternMatched 		= maRising && rising && newHigh && !lowChop && upTrend;
			shortPatternMatched		= maFalling && falling && newLow && !lowChop && downTrend;
		}

		private bool isValidEntryTime()
		{
			int now = ToTime(Time[0]);

			if (now < ToTime(OpenTime)) {
				return false;
			}

			if (now > ToTime(LastTradeTime)) {
				return false;
			}

			return true;
		}

		private bool isValidTradeTime()
		{
			int now = ToTime(Time[0]);

			if (now > ToTime(CloseTime)) {
				return false;
			}

			if (now < ToTime(OpenTime)) {
				return false;
			}

			return true;
		}

		private void setEntries()
		{
			if (!isValidEntryTime()) {
				return;
            }

			if (tradeDirection != 0) {
				return;
			}

			if (Position.MarketPosition != MarketPosition.Flat) {
				return;
			}

			if (quantity == 0) {
				return;
			}

			if (longPatternMatched) {
				closeOpenEntryOrders();
				longEntry	= SubmitOrderUnmanaged(1, OrderAction.Buy, OrderType.Market, quantity, 0, 0, string.Empty, "longEntry");
				return;
			}

			if (shortPatternMatched) {
				closeOpenEntryOrders();
				shortEntry	= SubmitOrderUnmanaged(1, OrderAction.SellShort, OrderType.Market, quantity, 0, 0, string.Empty, "shortEntry");
			}
		}

		private void closeOpenEntryOrders()
		{
			if (longEntry != null) {
				if (longEntry.Filled == 0) {
					CancelOrder(longEntry);
					longEntry = null;
				}
			}

			if (shortEntry != null) {
				if (shortEntry.Filled == 0) {
					CancelOrder(shortEntry);
					shortEntry = null;
				}
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
		[Range(0, int.MaxValue)]
		[Display(Name="Target Multiplier", Description="Target Multiplier", Order=3, GroupName="Parameters")]
		public int TargetMultiplier
		{ get; set; }

		[NinjaScriptProperty]
		[Range(0, double.MaxValue)]
		[Display(Name="Profit Target", Description="Profit Target", Order=4, GroupName="Parameters")]
		public double ProfitTarget
		{ get; set; }


		[NinjaScriptProperty]
		[Range(0, double.MaxValue)]
		[Display(Name="Stop Loss Target", Description="Stop Loss Target", Order=5, GroupName="Parameters")]
		public double StopLossTarget
		{ get; set; }

		[NinjaScriptProperty]
		[Range(0, 100)]
		[Display(Name="Choppiness Threshold Low", Description="Choppiness Threshold Low", Order=6, GroupName="Parameters")]
		public double ChoppinessThresholdLow
		{ get; set; }

		[NinjaScriptProperty]
		[Range(0, 100)]
		[Display(Name="High ATR Multiplier", Description="High ATR Multiplier", Order=7, GroupName="Parameters")]
		public double HighATRMultiplier
		{ get; set; }

		#endregion
	}
}
