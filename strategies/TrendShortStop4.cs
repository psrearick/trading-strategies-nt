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
	public class TrendShort2 : Strategy
	{
        private Order entryOrder;
        private Order longStopEntry;
        private Order shortStopEntry;
        private Order stopLossOrder;
		private int tradeDirection	    = 0;
        private string ocoString;

        private OrderFlowVWAP i_vwap;
        private ATR i_atr;
		private PriceRange5 i_price_range;
		private PriceRange5 i_price_range_short;
		private PriceAction i_price_action;
		private MABand i_ma_band;
		private MABand i_hourly_ma_band;
		private EMA i_atr_ma;
		private SMA i_long_sma;
		private MIN i_open_min;
		private MAX i_open_max;
		private MIN i_close_min;
		private MAX i_close_max;

        private bool maStackRising = false;
        private bool maStackFalling = false;
        private bool allMaRising = false;
        private bool allMaFalling = false;
		private bool smaRising = false;
		private bool priceAboveSma = false;
		private bool slowAboveSma = false;
		private bool priceBelowSma = false;
		private bool slowBelowSma = false;
        private bool closeAboveHourlyMA = false;
		private bool slowRising = false;
        private bool closeAboveSlow = false;
        private bool closeAboveFast = false;
		private bool closeAboveHourlyFast = false;
        private bool closeAboveHourlyMid = false;
        private bool closeAboveHourlySlow = false;
		private bool hourlySlowAboveHourlyFast = false;
		private bool hourlySlowAboveHourlyMid = false;
		
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
		private bool barsUp79 = false;
		private bool barsUp57 = false;
		
		
		private double patternHigh = 0.0;
		private double patternLow = 0.0;
        private bool shortPatternMatched = false;
        private bool longPatternMatched = false;
        private bool longCondition = false;
        private bool shortCondition = false;
        private int quantity = 0;
		private int barsSincePatternMatched = 0;
		
		private double prReference = 0.0;
		private bool prBelowLower = false;
		private bool prBelowMid = false;
		private bool prBelowUpper = false;
		private bool prMiddle = false;
		private bool prAboveUpper = false;
		private bool prAboveMid = false;
		private bool prAboveLower = false;
		private bool prAboveBand2Lower = false;
		private bool prBelowBand2Lower = false;
		private bool prAboveBand2Upper = false;
		private bool prBelowBand2Upper = false;
		
		private double ATRma = 0.0;
		private bool belowAverageATR = false;
		private bool aboveAverageATR = false;
        private bool averageATR = false;

        bool useFirstCondition = true;
		bool isLong = false;
		
        private string tradeLog = "";

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults) {
				Description	                                = "Short Trend 2";
                Name		                                = "Short Trend 2";
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

				StopLoss									= 8;
				OpenTime									= DateTime.Parse("09:30", System.Globalization.CultureInfo.InvariantCulture);
				CloseTime									= DateTime.Parse("15:55", System.Globalization.CultureInfo.InvariantCulture);
				MAFastPeriod								= 9;
				MAMidPeriod									= 21;
				MASlowPeriod								= 50;
				MAHourlySlowPeriod							= 120;
				MAHourlyMidPeriod							= 50;
				MAHourlyPeriod								= 30;
				ATRThreshold								= 25;
				PRLength									= 5;
				PRFast										= 10;
				PRSlow										= 20;
				IsUnmanaged									= true;
			}

			if (State == State.Configure) {
				AddDataSeries(Data.BarsPeriodType.Tick, 1);
				AddDataSeries(BarsPeriodType.Minute, 60);
			}
			
            if (State == State.DataLoaded) {
                quantity            = Convert.ToInt32(DefaultQuantity);
				
                i_vwap              = OrderFlowVWAP(VWAPResolution.Standard, TradingHours.String2TradingHours("CME US Index Futures RTH"), VWAPStandardDeviations.Three, 1, 2, 3);
                i_atr               = ATR(4);
				i_price_range		= PriceRange5(PRLength, PRFast, PRSlow);
				i_price_action		= PriceAction();
				i_price_range_short	= PriceRange5(Convert.ToInt32(Math.Round(Convert.ToDouble(PRLength / 2), 0)), PRFast, PRSlow);
				i_ma_band			= MABand(MAFastPeriod, MAMidPeriod, MASlowPeriod);
				i_atr_ma			= EMA(i_atr, 9);
				i_long_sma			= SMA(200);
				i_hourly_ma_band	= MABand(MAHourlyPeriod, MAHourlyMidPeriod, MAHourlySlowPeriod);
				i_open_min			= MIN(Opens[0], 2);
				i_open_max			= MAX(Opens[0], 2);
				i_close_min			= MIN(Closes[0], 2);
				i_close_max			= MAX(Closes[0], 2);
				
				AddChartIndicator(i_price_range);
				AddChartIndicator(i_ma_band);
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

			setIndicatorStatuses();
			
            evaluateConditions();
			
			setTargetEntries();
			
			executeTrades();
			
		}

        protected override void OnOrderUpdate(Cbi.Order order, double limitPrice, double stopPrice,
			int quantity, int filled, double averageFillPrice,
			Cbi.OrderState orderState, DateTime time, Cbi.ErrorCode error, string comment)
		{
			AssignOrderToVariable(ref order);
		}

        protected override void OnExecutionUpdate(Cbi.Execution execution, string executionId, double price, int quantity,
			Cbi.MarketPosition marketPosition, string orderId, DateTime time)
		{
			executionAtr					= ATR(14)[0];
			double adjustedStopLoss		    = (executionAtr * StopLoss) * TickSize;
			
			if (longStopEntry != null && execution.Order == longStopEntry) {	
				tradeDirection = 1;
				ocoString       = string.Format("unmanageexitdoco{0}", DateTime.Now.ToString("hhmmssffff"));
				stopLossOrder   = SubmitOrderUnmanaged(1, OrderAction.Sell, OrderType.StopMarket, quantity, 0, price - adjustedStopLoss, ocoString, "longStopLoss");
                logEntry();
			} else if (shortStopEntry != null && execution.Order == shortStopEntry) {
				tradeDirection = -1;
				ocoString       = string.Format("unmanageexitdoco{0}", DateTime.Now.ToString("hhmmssffff"));
				stopLossOrder   = SubmitOrderUnmanaged(1, OrderAction.BuyToCover, OrderType.StopMarket, quantity, 0, price + adjustedStopLoss, ocoString, "shortStopLoss");
                logEntry();
			} else if (execution.Name == "Exit on session close" || execution.Name == "longProfitTarget" || execution.Name == "longStopLoss" || execution.Name == "shortProfitTarget" || execution.Name == "shortStopLoss") {
				double difference = price - entryOrder.AverageFillPrice;
				
				if (marketPosition == MarketPosition.Short) {
					difference = difference * -1;
				}
				
				longStopEntry	= null;
				shortStopEntry	= null;
                stopLossOrder   = null;
				entryOrder		= null;
                logExit(execution.Name, difference);
				tradeDirection = 0;
			}
		}
		
		private void setIndicatorStatuses()
		{
			//###########################################################################
			// MOVING AVERAGES
			//#####################
			maStackRising		= i_ma_band.maStackRising[0];
			maStackFalling		= i_ma_band.maStackFalling[0];
			allMaRising		    = i_ma_band.allMaRising[0];
			allMaFalling		= i_ma_band.allMaFalling[0];
			smaRising			= IsRising(i_long_sma);
			priceAboveSma		= Close[0] > i_long_sma[0];
			slowAboveSma		= i_ma_band.Slow[0] > i_long_sma[0];
            slowRising          = IsRising(i_ma_band.Slow);
			priceBelowSma		= Close[0] < i_long_sma[0];
			slowBelowSma		= i_ma_band.Slow[0] < i_long_sma[0];
            closeAboveSlow      = Close[0] > i_ma_band.Slow[0];
            closeAboveFast      = Close[0] > i_ma_band.Fast[0];
			
            closeAboveHourlyFast  		= Close[0] > i_hourly_ma_band.Fast[0];
            closeAboveHourlyMid 		= Close[0] > i_hourly_ma_band.Mid[0];
            closeAboveHourlySlow  		= Close[0] > i_hourly_ma_band.Slow[0];
            hourlySlowAboveHourlyMid  	=   i_hourly_ma_band.Slow[0] > i_hourly_ma_band.Mid[0];
            hourlySlowAboveHourlyFast  	=   i_hourly_ma_band.Slow[0] > i_hourly_ma_band.Fast[0];

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
            atr 				= i_atr[0];
            atrBelowThreshold 	= ATRThreshold > atr;
			ATRma				= i_atr_ma[0];
			belowAverageATR		= atr < (ATRma * 0.8);
			aboveAverageATR		= atr > (ATRma * 1.2);
            averageATR          = !belowAverageATR && !aboveAverageATR;

			//#####################
			// Price Range
            //#####################
//			prReference			= i_price_range.Signal[0];
//			prBelowUpper		= prReference < i_price_range.UpperBand1[0];
//			prBelowMid 	    	= prReference < i_price_range.MovingAverage[0];
//			prBelowLower		= prReference < i_price_range.LowerBand1[0];
//			prAboveUpper 		= prReference > i_price_range.UpperBand1[0];
//			prAboveMid			= prReference > i_price_range.MovingAverage[0];
//			prAboveLower		= prReference > i_price_range.LowerBand1[0];
//			prMiddle 			= prAboveLower && prBelowUpper;
//			prAboveBand2Upper	= prReference > i_price_range.UpperBand2[0];
//			prBelowBand2Upper	= prReference < i_price_range.UpperBand2[0];
//			prAboveBand2Lower	= prReference > i_price_range.LowerBand2[0];
//			prBelowBand2Lower	= prReference < i_price_range.LowerBand2[0];
			
			//#####################
			// Price Action
            //#####################
			upBars				= i_price_action.BarIsUp(0) && i_price_action.LeastBarsUp(1, 2, 1);
			downBars			= i_price_action.BarIsDown(0) && i_price_action.LeastBarsDown(1, 2, 1);
			smallerBars 		= i_price_action.BarIsSmaller(0) && i_price_action.LeastSmallerBars(1, 2, 1);
			biggerBars 			= i_price_action.BarIsBigger(0) && i_price_action.LeastBiggerBars(1, 2, 1);
			barsUp79 			= i_price_action.LeastBarsUp(7, 9);
			barsUp57			= i_price_action.LeastBarsUp(5, 7);
		}
		
		private void setTargetEntries()
		{	
			if (ToTime(Time[0]) < ToTime(OpenTime) || ToTime(Time[0]) > ToTime(CloseTime)) {
				patternHigh = 0.0;
				patternLow = 0.0;
				longPatternMatched = false;
				shortPatternMatched = false;
				
				return;
            }
			
			if (barsSincePatternMatched > 20) {
				barsSincePatternMatched = 0;
				patternHigh = 0.0;
				patternLow = 0.0;
				longPatternMatched = false;
				shortPatternMatched = false;
				
				return;
			}
			
			if (patternHigh == 0.0 && patternLow == 0.0 && (shortPatternMatched || longPatternMatched)) {
				patternLow = Math.Min(i_close_min[0], i_open_min[0]);
				patternHigh	= Math.Max(i_close_max[0], i_open_max[0]);
				barsSincePatternMatched = -1;
			}
			
			barsSincePatternMatched = barsSincePatternMatched + 1;
			
			setShortTargetEntries();

			setLongTargetEntries();
		}

		private void setShortTargetEntries()
		{
			if (shortPatternMatched && patternHigh == 0.0 && patternLow == 0.0) {
				longPatternMatched = false;
			}
			
			if (Close[0] > patternHigh && patternHigh > 0 && shortPatternMatched) {
				shortPatternMatched = false;
				patternHigh = 0.0;
				patternLow = 0.0;
			}
			
			if (Close[0] < patternLow && shortPatternMatched) {
				shortPatternMatched = false;
				patternHigh = 0.0;
				patternLow = 0.0;
				
				shortCondition = true;
			}
		}

		private void setLongTargetEntries()
		{
			if (longPatternMatched && patternHigh == 0.0 && patternLow == 0.0) {
				shortPatternMatched = false;
			}
			
			if (Close[0] < patternLow && longPatternMatched) {
				longPatternMatched = false;
				patternHigh = 0.0;
				patternLow = 0.0;
			}
			
			if (Close[0] > patternHigh && patternHigh > 0 && longPatternMatched) {
				longPatternMatched = false;
				patternHigh = 0.0;
				patternLow = 0.0;
				
				longCondition = true;
			}
		}
		
		private void executeTrades()
		{
			if (tradeDirection != 0) {
                exitPositions();

				return;
			}
			
			if (longCondition) {
				ocoString		= string.Format("unmanagedlongentryoco{0}", DateTime.Now.ToString("hhmmssffff"));
				longStopEntry	= SubmitOrderUnmanaged(1, OrderAction.Buy, OrderType.Market, quantity, 0, 0, ocoString, "longStopEntry");
				entryOrder		= longStopEntry;
			}

			if (shortCondition) {
				ocoString		= string.Format("unmanagedshortentryoco{0}", DateTime.Now.ToString("hhmmssffff"));
				shortStopEntry	= SubmitOrderUnmanaged(1, OrderAction.SellShort, OrderType.Market, quantity, 0, 0, ocoString, "shortStopEntry");
				entryOrder		= shortStopEntry;
			}
		}

        private void AssignOrderToVariable(ref Order order)
		{
			if (order.Name == "longStopEntry" && longStopEntry != order)
				longStopEntry = order;

			if (order.Name == "shortStopEntry" && shortStopEntry != order)
				shortStopEntry = order;
		}

        private void exitPositions() {
			if (tradeDirection == 0) {
				return;
			}
			
			PriceRange5 priceRange = getPR();
			
//			bool crossAbove = CrossAbove(priceRange.Signal, 20, 1);
//			bool crossBelow = CrossBelow(priceRange.Signal, 80, 1);
			
			bool crossAbove = CrossAbove(priceRange.Fast, priceRange.Slow, 1);
			bool crossBelow = CrossBelow(priceRange.Fast, priceRange.Slow, 1);
			
			if (crossAbove || crossBelow) {
				string exitOcoString = string.Format("unmanageexitcross{0}", DateTime.Now.ToString("hhmmssffff"));
//			if (CrossAbove(priceRange.Signal, priceRange.UpperBand1, 1) || CrossBelow(priceRange.Signal, priceRange.LowerBand1, 1)) {
				if (tradeDirection == 1) {
					SubmitOrderUnmanaged(1, OrderAction.Sell, OrderType.Market, quantity, 0, 0, exitOcoString, "longProfitTarget");
				} else {
					SubmitOrderUnmanaged(1, OrderAction.BuyToCover, OrderType.Market, quantity, 0, 0, exitOcoString, "shortProfitTarget");
				}
				
				CancelOrder(stopLossOrder);
			}
        }
		
		private PriceRange5 getPR()
		{
			bool conditions = true
				&& false
//				&& allMaFalling && maStackFalling
//				&& allMaRising && maStackRising
//				&& hourlySlowAboveHourlyMid
//				|| hourlySlowAboveHourlyFast
//				&& !averageATR
//				&& closeAboveHourlyMid
//				&& !closeAboveHourlyFast
			;
			
//			            closeAboveHourlyFast  		= Close[0] > i_hourly_ma_band.Fast[0];
//            closeAboveHourlyMid 		= Close[0] > i_hourly_ma_band.Mid[0];
//            closeAboveHourlySlow  		= Close[0] > i_hourly_ma_band.Slow[0];
//            hourlySlowAboveHourlyMid  	=   i_hourly_ma_band.Slow[0] > i_hourly_ma_band.Mid[0];
//            hourlySlowAboveHourlyFast  	=   i_hourly_ma_band.Slow[0] > i_hourly_ma_band.Fast[0];
			
			
			return conditions ? i_price_range_short : i_price_range;
		}
		
		private void evaluateConditions()
		{
			longCondition		= false;
			shortCondition		= false;
			
			longPatternMatched 	= evaluateLongConditions();
			shortPatternMatched = longPatternMatched ? false : evaluateShortConditions();		
		}
		
		private bool evaluateLongConditions()
		{
			PriceRange5 priceRange = getPR();
			
			return CrossAbove(priceRange.Fast, priceRange.Slow, 1);
//			return CrossAbove(priceRange.Signal, 30, 1);
		}
		
		private bool evaluateShortConditions()
		{
			PriceRange5 priceRange = getPR();
			
			return CrossBelow(priceRange.Fast, priceRange.Slow, 1);
//			return CrossBelow(priceRange.Signal, 70, 1);
		}
		
		private void logEntry()
		{	
			tradeLog =
//				prBelowLower +
//				"|" + prBelowMid +
//				"|" + prBelowUpper +
//				"|" + prMiddle +
//				"|" + prAboveUpper +
//				"|" + prAboveMid +
//				"|" + prAboveLower +
				belowAverageATR +
				"|" + aboveAverageATR +
				"|" + aboveVwapDown +
				"|" + belowVwapUp +
				"|" + aboveVwap +
				"|" + belowVwap +
				"|" + averageATR +
				"|" + smaRising +
				"|" + priceAboveSma +
				"|" + slowAboveSma +
				"|" + biggerBars +
				"|" + smallerBars +
				"|" + downBars +
				"|" + upBars +
				"|" + priceBelowSma +
				"|" + slowBelowSma +
				"|" + allMaFalling +
				"|" + maStackFalling +
				"|" + closeAboveHourlyFast +
				"|" + closeAboveHourlyMid +
				"|" + closeAboveHourlySlow +
				"|" + hourlySlowAboveHourlyMid +
				"|" + hourlySlowAboveHourlyFast
			;
		}
		
		private void logExit(string orderName, double change)
		{
            string orderType    = tradeDirection == 1 ? "Long" : "Short";
            string winLoss      = change > 0 ? "L" : "W";

            string log = orderType + "|" + winLoss + "|" + tradeLog;

//             Print(log);
			
            tradeLog    = "";
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
		[Display(Name="Stop Loss", Description="Stop Loss", Order=3, GroupName="Parameters")]
		public double StopLoss
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, double.MaxValue)]
		[Display(Name="ATR Threshold", Description="ATR Threshold", Order=4, GroupName="Parameters")]
		public double ATRThreshold
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Price Range Length", Description="Price Range Length", Order=5, GroupName="Parameters")]
		public int PRLength
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Price Range Fast Length", Description="Price Range Fast Length", Order=6, GroupName="Parameters")]
		public int PRFast
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Price Range Slow Length", Description="Price Range Slow Length", Order=7, GroupName="Parameters")]
		public int PRSlow
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Moving Average Fast Period", Description="Moving Average Fast Period", Order=1, GroupName="Moving Averages")]
		public int MAFastPeriod
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Moving Average Mid Period", Description="Moving Average Mid Period", Order=2, GroupName="Moving Averages")]
		public int MAMidPeriod
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Moving Average Slow Period", Description="Moving Average Slow Period", Order=3, GroupName="Moving Averages")]
		public int MASlowPeriod
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Moving Average Hourly Fast Period", Description="Moving Average Hourly Fast Period", Order=4, GroupName="Moving Averages")]
		public int MAHourlyPeriod
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Moving Average Hourly Mid Period", Description="Moving Average Hourly Mid Period", Order=5, GroupName="Moving Averages")]
		public int MAHourlyMidPeriod
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Moving Average Hourly Slow Period", Description="Moving Average Hourly Slow Period", Order=6, GroupName="Moving Averages")]
		public int MAHourlySlowPeriod
		{ get; set; }
		
		#endregion
	}
}
