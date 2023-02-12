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
		private int tradeDirection	    = 0;
        private string ocoString;

        private OrderFlowVWAP i_vwap;
        private ATR i_atr;
		private PriceRange3 i_price_range;
		private PriceAction i_price_action;
		private MABand i_ma_band;
		private EMA i_atr_ma;
		private SMA i_long_sma;
		private SMA i_hourly_sma;
        private SMA i_hourly_mid_sma;
        private SMA i_hourly_slow_sma;

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
		
		private int tradeCount = 0;
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

				StopLoss									= 1;
				OpenTime									= DateTime.Parse("09:30", System.Globalization.CultureInfo.InvariantCulture);
				CloseTime									= DateTime.Parse("15:55", System.Globalization.CultureInfo.InvariantCulture);
				ProfitTarget								= 16;
				ShortProfitTarget							= 16;
				MAFastPeriod								= 9;
				MAMidPeriod									= 21;
				MASlowPeriod								= 50;
				MAHourlySlowPeriod							= 120;
				MAHourlyMidPeriod							= 50;
				MAHourlyPeriod								= 30;
				ATRThreshold								= 25;
				PRMA										= 14;
				PRSmoothing									= 2;
				PRLookback									= 9;
				LongCondition1Threshold 					= 4;
				LongCondition2Threshold                     = 3.5;
				ShortCondition1Threshold					= 2;
				ShortCondition2Threshold					= 3;
				AllowLongTrades                             = true;
				AllowShortTrades                            = true;
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
				i_price_range		= PriceRange3(PRMA, PRSmoothing, PRLookback);
				i_price_action		= PriceAction();
				i_ma_band			= MABand(MAFastPeriod, MAMidPeriod, MASlowPeriod);
				i_atr_ma			= EMA(i_atr, 9);
				i_long_sma			= SMA(200);
				i_hourly_sma		= SMA(BarsArray[2], MAHourlyPeriod);
                i_hourly_slow_sma   = SMA(BarsArray[2], MAHourlySlowPeriod);
				i_hourly_mid_sma	= SMA(BarsArray[2], MAHourlyMidPeriod);
				
				AddChartIndicator(i_price_range);
//				AddChartIndicator(i_ma_band);
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

            //###########################################################################
			// Indicators
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

            closeAboveHourlyMA  		= Close[0] > i_hourly_sma[0];
            hourlySlowAboveHourlyMid  	=   i_hourly_slow_sma[0] > i_hourly_mid_sma[0];
            hourlySlowAboveHourlyFast  	=   i_hourly_slow_sma[0] > i_hourly_sma[0];

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
			prReference			= i_price_range.Signal[0];
			prBelowUpper		= prReference < i_price_range.UpperBand1[0];
			prBelowMid 	    	= prReference < i_price_range.MovingAverage[0];
			prBelowLower		= prReference < i_price_range.LowerBand1[0];
			prAboveUpper 		= prReference > i_price_range.UpperBand1[0];
			prAboveMid			= prReference > i_price_range.MovingAverage[0];
			prAboveLower		= prReference > i_price_range.LowerBand1[0];
			prMiddle 			= prAboveLower && prBelowUpper;
			prAboveBand2Upper	= prReference > i_price_range.UpperBand2[0];
			prBelowBand2Upper	= prReference < i_price_range.UpperBand2[0];
			prAboveBand2Lower	= prReference > i_price_range.LowerBand2[0];
			prBelowBand2Lower	= prReference < i_price_range.LowerBand2[0];
			
			//#####################
			// Price Action
            //#####################
			upBars				= i_price_action.BarIsUp(0) && i_price_action.LeastBarsUp(1, 2, 1);
			downBars			= i_price_action.BarIsDown(0) && i_price_action.LeastBarsDown(1, 2, 1);
			smallerBars 		= i_price_action.BarIsSmaller(0) && i_price_action.LeastSmallerBars(1, 2, 1);
			biggerBars 			= i_price_action.BarIsBigger(0) && i_price_action.LeastBiggerBars(1, 2, 1);
			barsUp79 			= i_price_action.LeastBarsUp(7, 9);
			barsUp57			= i_price_action.LeastBarsUp(5, 7);

            //###########################################################################
			// Conditions
			//###########################################################################
            evaluateConditions();

			if (shortPatternMatched && patternHigh == 0 && patternLow == 0) {
				longPatternMatched = false;
				patternLow = Math.Min(MIN(Close, 5)[0], MIN(Open, 5)[0]);
				patternHigh	= Math.Max(MAX(Open, 5)[0], MAX(Close, 5)[0]);
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
			
			if (longPatternMatched && patternHigh == 0 && patternLow == 0) {
				shortPatternMatched = false;
				patternHigh = MAX(Close, 5)[0];
				patternLow	= Math.Min(MIN(Open, 5)[0], MIN(Close, 5)[0]);
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
			double baseProfitTarget			= ProfitTarget;
			double profitTargetLow			= baseProfitTarget * 0.2;
			double profitTargetHalf			= (baseProfitTarget + profitTargetLow) / 2;
			double usableProfitTarget		= baseProfitTarget;
			double profitTargetDouble		= baseProfitTarget * 2;
			
			if (execution.Order == shortStopEntry) {
				usableProfitTarget = ShortProfitTarget;
			}
			
			executionAtr					= ATR(14)[0];
			double adjustedProfitTarget     = (executionAtr * usableProfitTarget) * TickSize;
			double adjustedStopLoss		    = (executionAtr * StopLoss) * TickSize;
			
			if (longStopEntry != null && execution.Order == longStopEntry) {	
				ocoString = string.Format("unmanageexitdoco{0}", DateTime.Now.ToString("hhmmssffff"));
				SubmitOrderUnmanaged(1, OrderAction.Sell, OrderType.Limit, quantity, price + adjustedProfitTarget, 0, ocoString, "longProfitTarget");
				SubmitOrderUnmanaged(1, OrderAction.Sell, OrderType.StopMarket, quantity, 0, price - adjustedStopLoss, ocoString, "longStopLoss");
                logEntry();
			} else if (shortStopEntry != null && execution.Order == shortStopEntry) {
				ocoString = string.Format("unmanageexitdoco{0}", DateTime.Now.ToString("hhmmssffff"));
				SubmitOrderUnmanaged(1, OrderAction.BuyToCover, OrderType.Limit, quantity, price - adjustedProfitTarget, 0, ocoString, "shortProfitTarget");
				SubmitOrderUnmanaged(1, OrderAction.BuyToCover, OrderType.StopMarket, quantity, 0, price + adjustedStopLoss, ocoString, "shortStopLoss");
                logEntry();
			} else if (execution.Name == "Exit on session close" || execution.Name == "longProfitTarget" || execution.Name == "longStopLoss" || execution.Name == "shortProfitTarget" || execution.Name == "shortStopLoss") {
				longStopEntry	= null;
				shortStopEntry	= null;
                logExit(execution.Name);
			}
		}

        private void AssignOrderToVariable(ref Order order)
		{
			if (order.Name == "longStopEntry" && longStopEntry != order)
				longStopEntry = order;

			if (order.Name == "shortStopEntry" && shortStopEntry != order)
				shortStopEntry = order;
		}
		
		private void evaluateConditions()
		{
			longCondition	= false;
			shortCondition	= false;
			longPatternMatched = false;
			shortPatternMatched = false;
			
			isLong = allMaRising || maStackRising;
//			isLong = allMaRising && maStackRising;
			
//			if (CrossBelow(i_price_range.Signal,i_price_range.LowerBand1, 1)) {
////				shortPatternMatched = !isLong;
//				longPatternMatched = isLong;
//			}		
			
			if (CrossBelow(i_price_range.Signal,i_price_range.UpperBand1, 1)) {
//				shortPatternMatched = !isLong;
//				longPatternMatched = isLong;
				longPatternMatched = true;
			}
			
			if (CrossAbove(i_price_range.Signal,i_price_range.LowerBand1, 1)) {
//				shortPatternMatched = !isLong;
////				longPatternMatched = isLong;
				shortPatternMatched = true;
			}
		}

        private void evaluateConditions2()
        {
			longCondition	= false;
			shortCondition	= false;
			longPatternMatched = false;
			shortPatternMatched = false;
			
//            useFirstCondition = hourlySlowAboveHourlyFast && hourlySlowAboveHourlyMid;
            useFirstCondition = hourlySlowAboveHourlyFast;
//            useFirstCondition = hourlySlowAboveHourlyMid;
//			useFirstCondition = slowBelowSma && slowRising;
			
//			useFirstCondition = slowAboveSma || slowRising;
//			useFirstCondition = closeAboveHourlyMA;
			
//			bool allowShort = !allMaRising && !maStackRising;
			bool allowShort = true;
			
//			isLong = allMaRising || maStackRising;
//			isLong = allMaRising && maStackRising;
//            isLong = closeAboveSlow || slowRising;
//            isLong = closeAboveSlow && slowRising;
			isLong = Close[0] > i_ma_band.Fast[0];

            double conditionOneShortMatches = 0.0;
            double conditionTwoShortMatches = 0.0;
            double conditionOneLongMatches = 0.0;
            double conditionTwoLongMatches = 0.0;

            //###########################################################################
			// Short 1 Conditions
			//###########################################################################
            if (smallerBars) {
//                conditionOneShortMatches = conditionOneShortMatches + 1;
            }
            
            if (slowAboveSma) {
                conditionOneShortMatches = conditionOneShortMatches + 1;
            }
            
            if (prMiddle) {
                conditionOneShortMatches = conditionOneShortMatches + 1;
            }
            
            if (prAboveMid) {
                conditionOneShortMatches = conditionOneShortMatches + 1;
            }

            //###########################################################################
			// Short 2 Conditions
			//###########################################################################
            if (belowVwap) {
//                conditionTwoShortMatches = conditionTwoShortMatches + 1;
            }
            
            if (prBelowMid) {
                conditionTwoShortMatches = conditionTwoShortMatches + 1;
            }
            
            if (prMiddle) {
                conditionTwoShortMatches = conditionTwoShortMatches + 0.5;
            }
            
            if (averageATR) {
                conditionTwoShortMatches = conditionTwoShortMatches + 0.5;
            }
            
            if (slowBelowSma) {
                conditionTwoShortMatches = conditionTwoShortMatches + 1;
            }
            
            if (upBars) {
//                conditionTwoShortMatches = conditionTwoShortMatches + 0.5;
            }

            //###########################################################################
			// Long 1 Conditions
			//###########################################################################
            if (closeAboveFast) {
//                conditionOneLongMatches = conditionOneLongMatches + 1;
            }
            
            if (prBelowLower) {
                conditionOneLongMatches = conditionOneLongMatches + 0.5;
            }
			
            
            if (prAboveUpper) {
                conditionOneLongMatches = conditionOneLongMatches + 0.5;
            }
            
//            if (prBelowUpper) {
//                conditionOneLongMatches = conditionOneLongMatches + 0.5;
//            }
            
            if (belowVwapUp) {
//                conditionOneLongMatches = conditionOneLongMatches + 0.5;
            }
            
            if (aboveVwap) {
//                conditionOneLongMatches = conditionOneLongMatches + 1;
            }
            
            if (averageATR) {
                conditionOneLongMatches = conditionOneLongMatches + 1;
            }
            
            if (slowAboveSma) {
                conditionOneLongMatches = conditionOneLongMatches + 1;
            }

            //###########################################################################
			// Long 2 Conditions
			//###########################################################################
            if (prBelowLower) {
                conditionTwoLongMatches = conditionTwoLongMatches + 1;
            }
            
            if (prBelowMid) {
                conditionTwoLongMatches = conditionTwoLongMatches + 0.5;
            }
            
            if (belowAverageATR) {
                conditionTwoLongMatches = conditionTwoLongMatches + 0.5;
            }
            
            if (belowVwapUp) {
//                conditionTwoLongMatches = conditionTwoLongMatches + 0.5;
            }
            
            if (averageATR) {
//                conditionTwoLongMatches = conditionTwoLongMatches + 1;
            }
            
            if (priceAboveSma) {
                conditionTwoLongMatches = conditionTwoLongMatches + 0.5;
            }
            
            if (biggerBars) {
//                conditionTwoLongMatches = conditionTwoLongMatches + 1;
            }
            
            if (upBars) {
//                conditionTwoLongMatches = conditionTwoLongMatches + 0.5;
            }
            
            if (slowBelowSma) {
                conditionTwoLongMatches = conditionTwoLongMatches + 0.5;
            }

			if (isLong) {
                if (useFirstCondition) {
                    longPatternMatched = conditionOneLongMatches >= LongCondition1Threshold;
                } else {
                    longPatternMatched = conditionTwoLongMatches >= LongCondition2Threshold;
                }
			} else {
				if (useFirstCondition) {
					shortPatternMatched = conditionOneShortMatches >= ShortCondition1Threshold;
				} else {
					shortPatternMatched = conditionTwoShortMatches >= ShortCondition2Threshold;
				}
            }
			
//			if (atr < ATRThreshold) {
//				shortPatternMatched = false;
//				longPatternMatched  = false;
//			}

			//////////////////////////////////////////
			/// Disable Short Trades
			shortPatternMatched = AllowLongTrades ? shortPatternMatched : false;
			//////////////////////////////////////////

			//////////////////////////////////////////
			/// Disable Long Trades
			longPatternMatched = AllowLongTrades ? longPatternMatched : false;
			//////////////////////////////////////////
        }
		
		private void logEntry()
		{	
			tradeLog =
				prBelowLower +
				"|" + prAboveBand2Upper +
				"|" + prBelowBand2Upper +
				"|" + prAboveBand2Lower +
				"|" + prBelowBand2Lower +
				"|" + prBelowMid +
				"|" + prBelowUpper +
				"|" + prMiddle +
				"|" + prAboveUpper +
				"|" + prAboveMid +
				"|" + prAboveLower +
				"|" + belowAverageATR +
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
				"|" + maStackFalling;
		}
		
		private void logExit(string orderName)
		{
            string orderType    = "";
            string winLoss      = "";

            if (orderName == "shortProfitTarget") {
                orderType   = "Short";
                winLoss     = "W";
            }

            if (orderName == "longProfitTarget" || orderName == "Exit on session close") {
                orderType   = "Long";
                winLoss     = "W";
            }

            if (orderName == "longStopLoss") {
                orderType   = "Long";
                winLoss     = "L";
            }

            if (orderName == "shortStopLoss") {
                orderType   = "Short";
                winLoss     = "L";
            }

            string log = tradeCount + "|" + orderType + "|" + winLoss + "|" + tradeLog;

//             Print(log);

			tradeCount  = tradeCount + 1;
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
		[Display(Name="Profit Target", Description="Profit Target", Order=3, GroupName="Parameters")]
		public double ProfitTarget
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(0.25, double.MaxValue)]
		[Display(Name="Short Profit Target", Description="Short Profit Target", Order=4, GroupName="Parameters")]
		public double ShortProfitTarget
		{ get; set; }

		[NinjaScriptProperty]
		[Range(0.25, double.MaxValue)]
		[Display(Name="Stop Loss", Description="Stop Loss", Order=5, GroupName="Parameters")]
		public double StopLoss
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, double.MaxValue)]
		[Display(Name="ATR Threshold", Description="ATR Threshold", Order=6, GroupName="Parameters")]
		public double ATRThreshold
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
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="MA Length", Description="Moving Average Length", Order=1, GroupName="Price Range")]
		public int PRMA
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Smoothing", Description="Smoothing Period", Order=2, GroupName="Price Range")]
		public int PRSmoothing
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Lookback", Description="Lookback Period", Order=3, GroupName="Price Range")]
		public int PRLookback
		{ get; set; }

		[NinjaScriptProperty]
		[Range(0, double.MaxValue)]
		[Display(Name="Long Condition 1 Threshold", Description="Long Condition 1 Threshold", Order=1, GroupName="Conditions")]
		public double LongCondition1Threshold
		{ get; set; }

		[NinjaScriptProperty]
		[Range(0, double.MaxValue)]
		[Display(Name="Long Condition 2 Threshold", Description="Long Condition 2 Threshold", Order=2, GroupName="Conditions")]
		public double LongCondition2Threshold
		{ get; set; }

		[NinjaScriptProperty]
		[Range(0, double.MaxValue)]
		[Display(Name="Short Condition 1 Threshold", Description="Short Condition 1 Threshold", Order=3, GroupName="Conditions")]
		public double ShortCondition1Threshold
		{ get; set; }

		[NinjaScriptProperty]
		[Range(0, double.MaxValue)]
		[Display(Name="Short Condition 2 Threshold", Description="Short Condition 2 Threshold", Order=4, GroupName="Conditions")]
		public double ShortCondition2Threshold
		{ get; set; }

		[NinjaScriptProperty]
		[Display(Name="Allow Long Trades", Description="Allow Long Trades", Order=5, GroupName="Conditions")]
		public bool AllowLongTrades
		{ get; set; }

		[NinjaScriptProperty]
		[Display(Name="Allow Short Trades", Description="Allow Short Trades", Order=6, GroupName="Conditions")]
		public bool AllowShortTrades
		{ get; set; }
		
		#endregion
		
		
//				longPatternMatched = true
//					&& (Close[0] > i_ma_band.Fast[0]) // 0
//					&& prBelowLower // 1
//					&& prAboveBand2Upper // 2
//					&& prBelowBand2Upper // 3
//					&& prAboveBand2Lower // 4
//					&& prBelowBand2Lower // 5
//					&& prBelowMid // 6
//					&& prBelowUpper // 7
//					&& prMiddle // 8
//					&& prAboveUpper // 9
//					&& prAboveMid // 10
//					&& prAboveLower // 11
//					&& belowAverageATR // 12
//					&& aboveAverageATR // 13
//					&& aboveVwapDown // 14
//					&& belowVwapUp // 15
//					&& aboveVwap // 16
//					&& belowVwap // 17
//					&& averageATR // 18
//					&& smaRising // 19
//					&& priceAboveSma // 20
//					&& slowAboveSma // 21
//					&& biggerBars // 22
//					&& smallerBars // 23
//					&& downBars // 24
//					&& upBars // 25
//					&& priceBelowSma // 26
//					&& slowBelowSma // 27
//					&& allMaFalling // 28
//					&& maStackFalling // 29
//				;		

				
//				if (Control == 0) {
//					longPatternMatched = longPatternMatched && (Close[0] > i_ma_band.Fast[0]);
//				}
//				if (Control == 1) {
//					longPatternMatched = longPatternMatched && prBelowLower;
//				}
//				if (Control == 2) {
//					longPatternMatched = longPatternMatched && prAboveBand2Upper;
//				}
//				if (Control == 3) {
//					longPatternMatched = longPatternMatched && prBelowBand2Upper;
//				}
//				if (Control == 4) {
//					longPatternMatched = longPatternMatched && prAboveBand2Lower;
//				}
//				if (Control == 5) {
//					longPatternMatched = longPatternMatched && prBelowBand2Lower;
//				}
//				if (Control == 6) {
//					longPatternMatched = longPatternMatched && prBelowMid;
//				}
//				if (Control == 7) {
//					longPatternMatched = longPatternMatched && prBelowUpper;
//				}
//				if (Control == 8) {
//					longPatternMatched = longPatternMatched && prMiddle;
//				}
//				if (Control == 9) {
//					longPatternMatched = longPatternMatched && prAboveUpper;
//				}
//				if (Control == 10) {
//					longPatternMatched = longPatternMatched && prAboveMid;
//				}
//				if (Control == 11) {
//					longPatternMatched = longPatternMatched && prAboveLower;
//				}
//				if (Control == 12) {
//					longPatternMatched = longPatternMatched && belowAverageATR;
//				}
//				if (Control == 13) {
//					longPatternMatched = longPatternMatched && aboveAverageATR;
//				}
//				if (Control == 14) {
//					longPatternMatched = longPatternMatched && aboveVwapDown;
//				}
//				if (Control == 15) {
//					longPatternMatched = longPatternMatched && belowVwapUp;
//				}
//				if (Control == 16) {
//					longPatternMatched = longPatternMatched && aboveVwap;
//				}
//				if (Control == 17) {
//					longPatternMatched = longPatternMatched && belowVwap;
//				}
//				if (Control == 18) {
//					longPatternMatched = longPatternMatched && averageATR;
//				}
//				if (Control == 19) {
//					longPatternMatched = longPatternMatched && smaRising;
//				}
//				if (Control == 20) {
//					longPatternMatched = longPatternMatched && priceAboveSma;
//				}
//				if (Control == 21) {
//					longPatternMatched = longPatternMatched && slowAboveSma;
//				}
//				if (Control == 22) {
//					longPatternMatched = longPatternMatched && biggerBars;
//				}
//				if (Control == 23) {
//					longPatternMatched = longPatternMatched && smallerBars;
//				}
//				if (Control == 24) {
//					longPatternMatched = longPatternMatched && downBars;
//				}
//				if (Control == 25) {
//					longPatternMatched = longPatternMatched && upBars;
//				}
//				if (Control == 26) {
//					longPatternMatched = longPatternMatched && priceBelowSma;
//				}
//				if (Control == 27) {
//					longPatternMatched = longPatternMatched && slowBelowSma;
//				}
//				if (Control == 28) {
//					longPatternMatched = longPatternMatched && allMaFalling;
//				}
//				if (Control == 29) {
//					longPatternMatched = longPatternMatched && maStackFalling;
//				}
	}
}
