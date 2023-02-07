//
// Copyright (C) 2022, NinjaTrader LLC <www.ninjatrader.com>.
// NinjaTrader reserves the right to modify or overwrite this NinjaScript component with each release.
//
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
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.Indicators;
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

//This namespace holds strategies in this folder and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Strategies
{
	public class TrendDynamic : Strategy
	{
        private Order entryOrder;
        private Order longStopEntry;
        private Order shortStopEntry;
		private int tradeDirection	    = 0;
        private string ocoString;

        private OrderFlowVWAP i_vwap;
        private ATR i_atr;
		private PriceRange i_price_range;
		private PriceAction i_price_action;
		private MABand i_ma_band;

        private bool maStackRising = false;
        private bool maStackFalling = false;
        private bool allMaRising = false;
        private bool allMaFalling = false;
        private double VwapUp1 = 0.0;
        private double VwapDown1 = 0.0;
        private double vwap = 0.0;
        private bool aboveVwapDown = false;
        private bool belowVwapUp = false;
        private bool aboveVwap = false;
        private bool belowVwap = false;
        private double atr = 0.0;
        private double executionAtr = 0.0;
		private bool atrBelowThreshold = false;
        private bool upBars = false;
        private bool downBars = false;
        private bool smallerBars = false;
        private bool biggerBars = false;
		private bool highDistance = false;
		private bool lowDistance = false;
        
        private bool longCondition = false;
        private bool shortCondition = false;
        private int quantity = 0;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults) {
				Description	                                = "Dynamic Trend-following strategy with tick resolution for accuracy";
                Name		                                = "Dynamic Trend Tick Resolution";
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
				IsInstantiatedOnEachOptimizationIteration   = false;

				StopLoss									= 6;
				OpenTime									= DateTime.Parse("09:30", System.Globalization.CultureInfo.InvariantCulture);
				CloseTime									= DateTime.Parse("15:55", System.Globalization.CultureInfo.InvariantCulture);
				ProfitTarget								= 8;
				MAFastPeriod								= 9;
				MAMidPeriod									= 21;
				MASlowPeriod								= 50;
				ATRThreshold								= 25;
				MDFast										= 28;
				MDSlow										= 90;
				MDLookback									= 12;
				MDATR										= 14;
				IsUnmanaged									= true;
			}

			if (State == State.Configure) {
				AddDataSeries(Data.BarsPeriodType.Tick, 1);
			}
			
            if (State == State.DataLoaded) {
                quantity            = Convert.ToInt32(DefaultQuantity);
				
                i_vwap              = OrderFlowVWAP(VWAPResolution.Standard, TradingHours.String2TradingHours("CME US Index Futures RTH"), VWAPStandardDeviations.Three, 1, 2, 3);
                i_atr               = ATR(14);
				i_price_range		= PriceRange(MDFast, MDSlow, MDLookback, MDATR);
				i_price_action		= PriceAction();
				i_ma_band			= MABand(MAFastPeriod, MAMidPeriod, MASlowPeriod);
				
				AddChartIndicator(i_price_range);
			}
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBar < BarsRequiredToTrade || CurrentBars[0] < 1 || CurrentBars[1] < 1) {
				return;
            }
		
			if (BarsInProgress != 0) {
				return;		
            }

            //###########################################################################
			// Indicators
			//###########################################################################
			// MOVING AVERAGES
			//#####################
			maStackRising		= i_ma_band.maStackRising[0];
			maStackFalling		= i_ma_band.maStackFalling[0];
			allMaRising		    = i_ma_band.allMaRising[0];
			allMaFalling		= i_ma_band.allMaFalling[0];
			

			//#####################
			// VWAP
            //#####################
            VwapUp1 		= i_vwap.StdDev1Upper[0];
			VwapDown1 		= i_vwap.StdDev1Lower[0];
			vwap			= i_vwap.VWAP[0];
			aboveVwapDown   = Close[0] > VwapDown1;
			belowVwapUp		= Close[0] < VwapUp1;
			aboveVwap		= Close[0] > vwap;
			belowVwap		= Close[0] < vwap;

			//#####################
			// ATR
            //#####################
            atr = i_atr[0];
            atrBelowThreshold = ATRThreshold > atr;

			//#####################
			// Price Range
            //#####################
			highDistance = i_price_range.Fast[0] > i_price_range.ATR[0];
			lowDistance = i_price_range.Fast[0] < i_price_range.ATR[0];
			
			//#####################
			// Price Action
            //#####################
			upBars				= i_price_action.BarIsUp(0) && i_price_action.LeastBarsUp(1, 2, 1);
			downBars			= i_price_action.BarIsDown(0) && i_price_action.LeastBarsDown(1, 2, 1);
			smallerBars 		= i_price_action.BarIsSmaller(0) && i_price_action.LeastSmallerBars(1, 2, 1);
			biggerBars 			= i_price_action.BarIsBigger(0) && i_price_action.LeastBiggerBars(1, 2, 1);

            //###########################################################################
			// Conditions
			//###########################################################################
			longCondition	= true
				&& maStackRising
				&& allMaRising
				&& aboveVwap
				&& atrBelowThreshold
				&& highDistance
			;
			
			shortCondition = true
				&& maStackFalling
				&& allMaFalling
				&& belowVwap
				&& atrBelowThreshold
				&& highDistance
			;

			//###########################################################################
			// Execute Trades
			//###########################################################################
			tradeDirection = 0;
			
			if (Position.MarketPosition != MarketPosition.Flat) {
				tradeDirection = Position.MarketPosition == MarketPosition.Long ? 1 : -1;
			}
			
			if (entryOrder != null) {
				if (entryOrder.OrderState == OrderState.Accepted) {
					CancelOrder(entryOrder);
				} else {
					return;
				}
			}
			
			if (tradeDirection != 0) {
				return;
			}
			
			if (ToTime(Time[0]) < ToTime(OpenTime)) {
				return;
            }
			
			if (ToTime(Time[0]) > ToTime(CloseTime)) {
				return;
            }
			
			if (longCondition) {
				ocoString		= string.Format("unmanagedlongentryoco{0}", DateTime.Now.ToString("hhmmssffff"));
				longStopEntry	= SubmitOrderUnmanaged(1, OrderAction.Buy, OrderType.Market, quantity, 0, 0, ocoString, "longStopEntry");
			}

			if (shortCondition) {
				ocoString		= string.Format("unmanagedshortentryoco{0}", DateTime.Now.ToString("hhmmssffff"));
				shortStopEntry	= SubmitOrderUnmanaged(1, OrderAction.SellShort, OrderType.Market, quantity, 0, 0, ocoString, "shortStopEntry");
			}
		}

        protected override void OnOrderUpdate(Cbi.Order order, double limitPrice, double stopPrice,
			int quantity, int filled, double averageFillPrice,
			Cbi.OrderState orderState, DateTime time, Cbi.ErrorCode error, string comment)
		{
			AssignOrderToVariable(ref order);
			if ((longStopEntry != null && longStopEntry.OrderState == OrderState.Cancelled && shortStopEntry != null && shortStopEntry.OrderState == OrderState.Cancelled) || (order.Name == "Exit on session close" && order.OrderState == OrderState.Filled))
			{
				longStopEntry	= null;
				shortStopEntry	= null;
				tradeDirection	= 0;
			}
			
			bool isOrderEntry = order.Name == "LongStopEntry" || order.Name == "ShortStopEntry";
			
			if (isOrderEntry) {
				tradeDirection 	= 0;
				entryOrder 		= order;
				
				if (orderState == OrderState.Filled) {
					tradeDirection	= order.Name == "LongStopEntry" ? 1 : -1;
					entryOrder 		= null;
				}
				
				if (orderState == OrderState.Cancelled) {
					entryOrder = null;
				}
			}
		}

        protected override void OnExecutionUpdate(Cbi.Execution execution, string executionId, double price, int quantity,
			Cbi.MarketPosition marketPosition, string orderId, DateTime time)
		{
			executionAtr					= ATR(14)[0];
			double adjustedProfitTarget     = (executionAtr * ProfitTarget) * TickSize;
			double adjustedStopLoss		    = (executionAtr * StopLoss) * TickSize;
			
			if (longStopEntry != null && execution.Order == longStopEntry) {	
				ocoString = string.Format("unmanageexitdoco{0}", DateTime.Now.ToString("hhmmssffff"));
				SubmitOrderUnmanaged(1, OrderAction.Sell, OrderType.Limit, quantity, price + adjustedProfitTarget, 0, ocoString, "longProfitTarget");
				SubmitOrderUnmanaged(1, OrderAction.Sell, OrderType.StopMarket, quantity, 0, price - adjustedStopLoss, ocoString, "longStopLoss");
			} else if (shortStopEntry != null && execution.Order == shortStopEntry) {
				ocoString = string.Format("unmanageexitdoco{0}", DateTime.Now.ToString("hhmmssffff"));
				SubmitOrderUnmanaged(1, OrderAction.BuyToCover, OrderType.Limit, quantity, price - adjustedProfitTarget, 0, ocoString, "shortProfitTarget");
				SubmitOrderUnmanaged(1, OrderAction.BuyToCover, OrderType.StopMarket, quantity, 0, price + adjustedStopLoss, ocoString, "shortStopLoss");
			} else if (execution.Name == "longProfitTarget" || execution.Name == "longStopLoss" || execution.Name == "shortProfitTarget" || execution.Name == "shortStopLoss") {
				longStopEntry	= null;
				shortStopEntry	= null;
			}
		}

        private void AssignOrderToVariable(ref Order order)
		{
			if (order.Name == "longStopEntry" && longStopEntry != order)
				longStopEntry = order;

			if (order.Name == "shortStopEntry" && shortStopEntry != order)
				shortStopEntry = order;
		}

		#region Properties
		
		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name="Open Time", Order=1, GroupName="Parameters")]
		public DateTime OpenTime
		{ get; set; }

		[NinjaScriptProperty]
		[PropertyEditor("NinjaTrader.Gui.Tools.TimeEditorKey")]
		[Display(Name="Close Time", Order=2, GroupName="Parameters")]
		public DateTime CloseTime
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0.25, double.MaxValue)]
		[Display(Name="Profit Target", Description="Profit Target", Order=3, GroupName="Parameters")]
		public double ProfitTarget
		{ get; set; }

		[NinjaScriptProperty]
		[Range(0.25, double.MaxValue)]
		[Display(Name="Stop Loss", Description="Stop Loss", Order=4, GroupName="Parameters")]
		public double StopLoss
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Moving Average Fast Period", Description="Moving Average Fast Period", Order=5, GroupName="Moving Averages")]
		public int MAFastPeriod
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Moving Average Mid Period", Description="Moving Average Mid Period", Order=6, GroupName="Moving Averages")]
		public int MAMidPeriod
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Moving Average Slow Period", Description="Moving Average Slow Period", Order=7, GroupName="Moving Averages")]
		public int MASlowPeriod
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, double.MaxValue)]
		[Display(Name="ATR Threshold", Description="ATR Threshold", Order=8, GroupName="Parameters")]
		public double ATRThreshold
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="MA Fast", Description="Moving Average Fast", Order=9, GroupName="Price Range")]
		public int MDFast
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="MA Slow", Description="Moving Average Slow", Order=10, GroupName="Price Range")]
		public int MDSlow
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Lookback Period", Description="Lookback Period", Order=11, GroupName="Price Range")]
		public int MDLookback
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="ATR Length", Description="ATR Length", Order=12, GroupName="Price Range")]
		public int MDATR
		{ get; set; }
		
		#endregion
	}
}
