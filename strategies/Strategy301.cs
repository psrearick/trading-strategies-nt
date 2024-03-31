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
using NinjaTrader.NinjaScript.Indicators.PR;
#endregion

//This namespace holds Strategies in this folder and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Strategies
{
	public class Strategy301 : Strategy
	{
		#region Variables
		private PriceActionUtils PA;
		private Legs legs;
		private MarketDirection marketDirection;
		private MarketDirection marketDirectionShort;
//		private MarketDirection marketDirectionMedium;
		private MarketDirection marketDirectionLong;
		private ATR atr;
		private StdDev stdDevAtr;
		private SMA avgAtr;

		private double stopLoss 			= 0;
		private int choppinessThresholdLow	= 40;
		private int choppinessThresholdHigh	= 60;
		private List<double> chopHistory = new List<double>();
		private bool reset = false;
		private int day = 0;
		private int LongPeriodLow = 12;
		private int LongPeriodHigh = 26;

		private DateTime LastDataDay	= new DateTime(2023, 03, 17);
		private DateTime OpenTime		= DateTime.Parse("10:00", System.Globalization.CultureInfo.InvariantCulture);
		private DateTime CloseTime		= DateTime.Parse("15:30", System.Globalization.CultureInfo.InvariantCulture);
		private DateTime LastTradeTime	= DateTime.Parse("15:00", System.Globalization.CultureInfo.InvariantCulture);
		#endregion

		#region OnStateChange()
		protected override void OnStateChange()
		{
			#region State.SetDefaults
			if (State == State.SetDefaults)
			{
				Description									= @"";
				Name										= "Strategy 3.0.1";
				Calculate									= Calculate.OnBarClose;
				EntriesPerDirection							= 1;
				EntryHandling								= EntryHandling.UniqueEntries;
				IsExitOnSessionCloseStrategy				= true;
				ExitOnSessionCloseSeconds					= 30;
				IsFillLimitOnTouch							= false;
				MaximumBarsLookBack							= MaximumBarsLookBack.TwoHundredFiftySix;
				OrderFillResolution							= OrderFillResolution.Standard;
				Slippage									= 0;
				StartBehavior								= StartBehavior.WaitUntilFlat;
				TimeInForce									= TimeInForce.Gtc;
				TraceOrders									= false;
				RealtimeErrorHandling						= RealtimeErrorHandling.StopCancelClose;
				StopTargetHandling							= StopTargetHandling.PerEntryExecution;
				BarsRequiredToTrade							= 200;
				// Disable this property for performance gains in Strategy Analyzer optimizations
				// See the Help Guide for additional information
				IsInstantiatedOnEachOptimizationIteration	= true;
				TimeShift									= -6;
				ShortPeriod = 12;
				LongPeriod = 18;
				Quantity = 2;
			}
			#endregion

			#region State.Configure
			else if (State == State.Configure)
			{
			}
			#endregion

			#region State.DataLoaded
			if (State == State.DataLoaded) {
				PA 						= PriceActionUtils();
				marketDirectionShort	= MarketDirection(ShortPeriod, LongPeriodLow);
				marketDirectionLong		= MarketDirection(ShortPeriod, LongPeriodHigh);
//				marketDirectionLong		= MarketDirection(ShortPeriod, LongPeriodHigh);
//				marketDirectionLong	= MarketDirection((int)Math.Floor((double) ShortPeriod * 1.5), LongPeriod + (int) Math.Floor((double) ShortPeriod / 2));
//				marketDirection		= MarketDirection(ShortPeriod, LongPeriod);
//				legs				= marketDirection.LegLong;
				atr						= ATR(14);
				stdDevAtr				= StdDev(atr, 20);
				avgAtr					= SMA(atr, 20);
			}
			#endregion
		}
		#endregion

		#region OnBarUpdate()
		protected override void OnBarUpdate()
		{
			if (CurrentBar < BarsRequiredToTrade || CurrentBars[0] < 1) {
				return;
            }

			marketDirection = marketDirectionShort;
			if ((avgAtr[0] - stdDevAtr[0]) > atr[0]) {
				marketDirection = marketDirectionLong;
			}

			marketDirection.Update();
			legs = marketDirection.LegLong;

			exitPositions();

			setEntries();
		}
		#endregion

		#region shouldExit()
		private bool shouldExit() {
			double currentStopLoss = stopLoss;
			if (Position.MarketPosition == MarketPosition.Long) {
				if (marketDirection.Direction[0] == TrendDirection.Bearish) {
					return true;
				}

				if (PA.IsBreakoutTrend(0, legs.BarsAgoStarts[0], TrendDirection.Bearish)) {
					return true;
				}

				double swingLow = MIN(Low, legs.BarsAgoStarts[0])[0];

				if (swingLow > stopLoss && legs[0] > 0) {
					stopLoss = swingLow;
					SetStopLoss(CalculationMode.Price, stopLoss);
				}
			}

			if (Position.MarketPosition == MarketPosition.Short) {
				if (marketDirection.Direction[0] == TrendDirection.Bullish) {
					return true;
				}

				if (PA.IsBreakoutTrend(0, legs.BarsAgoStarts[0], TrendDirection.Bullish)) {
					return true;
				}

				double swingHigh = MAX(High, legs.BarsAgoStarts[0])[0];

				if (swingHigh < stopLoss && legs[0] < 0) {
					stopLoss = swingHigh;
					SetStopLoss(CalculationMode.Price, stopLoss);
				}
			}

			return false;
		}
		#endregion

		#region exitPositions()
		private void exitPositions() {
			if (isValidTradeTime() && !shouldExit()) {
				return;
			}

			if (Position.MarketPosition == MarketPosition.Long) {
				ExitLong();
			}

			if (Position.MarketPosition == MarketPosition.Short) {
				ExitShort();
			}
        }
		#endregion

		#region isValidEntryTime()
		private bool isValidEntryTime()
		{
			int now = ToTime(Time[0]);

			double shift = Time[0] > LastDataDay ? 0.0 : TimeShift;

			if (now < ToTime(OpenTime.AddHours(shift))) {
				return false;
			}

			if (now > ToTime(LastTradeTime.AddHours(shift))) {
				return false;
			}

			return true;
		}
		#endregion

		#region isValidTradeTime()
		private bool isValidTradeTime()
		{
			int now = ToTime(Time[0]);

			double shift = Time[0] > LastDataDay ? 0.0 : TimeShift;

			if (now > ToTime(CloseTime.AddHours(shift))) {
				return false;
			}

			if (now < ToTime(OpenTime.AddHours(shift))) {
				return false;
			}

			return true;
		}
		#endregion

		#region setEntries()
		private void setEntries()
		{
			if (!isValidEntryTime()) {
				return;
            }

			if (Position.MarketPosition != MarketPosition.Flat) {
				return;
			}

			bool longMatch 	= longPatternMatched();
			bool shortMatch	= shortPatternMatched();

			int quantity2 = (int) Math.Floor((double) Quantity / 2);
			int quantity1 = Quantity - quantity2;

			double stopLossMultiple = 0.5;
//			if ((avgAtr[0] - stdDevAtr[0]) > atr[0]) {
//				stopLossMultiple = 0.25;
//			}

			if (longMatch) {
				double swingLow = Math.Min(MIN(Low, legs.BarsAgoStarts[0])[0], MIN(Low, 4)[0]);
				stopLoss = swingLow;
				double stopLossDistance = 4 * (Close[0] - stopLoss);

				if (swingLow < Low[0]) {
					SetStopLoss(CalculationMode.Ticks, stopLossDistance);

					EnterLong(quantity1, "LongEntry1");

					if (quantity2 > 0) {
						SetProfitTarget("LongEntry2", CalculationMode.Ticks, stopLossDistance * stopLossMultiple);
						EnterLong(quantity2, "LongEntry2");
					}
				}
			}

			if (shortMatch) {
				double swingHigh = Math.Max(MAX(High, legs.BarsAgoStarts[0])[0], MAX(High, 4)[0]);
				stopLoss = swingHigh;
				double stopLossDistance = 4 * (stopLoss - Close[0]);

				if (swingHigh > High[0]) {
					SetStopLoss(CalculationMode.Ticks, stopLossDistance);
					EnterShort(quantity1, "ShortEntry1");

					if (quantity2 > 0) {
						SetProfitTarget("ShortEntry2", CalculationMode.Ticks, stopLossDistance * stopLossMultiple);
						EnterShort(quantity2, "ShortEntry2");
					}
				}
			}
		}
		#endregion

		#region longPatternMatched()
		private bool longPatternMatched()
		{
			if (marketDirection.Direction[0] != TrendDirection.Bullish) {
				return false;
			}

			if (PA.GetBuySellPressure(0, legs.BarsAgoStarts[0]) < 80) {
				return false;
			}

			if (PA.GetStrongTrendDirection(0, 20) != TrendDirection.Bullish) {
                return false;
            }

//			if (!PA.IsBreakoutTrend(0, legs.BarsAgoStarts[0], TrendDirection.Bullish)) {
//				return false;
//			}

//			if ((avgAtr[0] - stdDevAtr[0]) > atr[0]) {
//				return false;
//			}

			return true;
		}
		#endregion

		#region shortPatternMatched()
		private bool shortPatternMatched()
		{
			if (marketDirection.Direction[0] != TrendDirection.Bearish) {
				return false;
			}

			if (PA.GetBuySellPressure(0, legs.BarsAgoStarts[0]) > 20) {
				return false;
			}

			if (PA.GetStrongTrendDirection(0, 20) != TrendDirection.Bearish) {
                return false;
            }

//			if (!PA.IsBreakoutTrend(0, legs.BarsAgoStarts[0], TrendDirection.Bearish)) {
//				return false;
//			}

//			if ((avgAtr[0] - stdDevAtr[0]) > atr[0]) {
//				return false;
//			}

			return true;
		}
		#endregion

		#region Properties

		[NinjaScriptProperty]
		[Range(6, int.MaxValue)]
		[Display(Name="Short Period", Description="Short Period", Order=0, GroupName="Parameters")]
		public int ShortPeriod
		{ get; set; }

		[NinjaScriptProperty]
		[Range(6, int.MaxValue)]
		[Display(Name="Long Period", Description="Long Period", Order=1, GroupName="Parameters")]
		public int LongPeriod
		{ get; set; }

		[NinjaScriptProperty]
		[Range(int.MinValue, int.MaxValue)]
		[Display(Name="Time Shift (Hours)", Description="Time Shift", Order=2, GroupName="Parameters")]
		public int TimeShift
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Quantity", Description="Quantity", Order=3, GroupName="Parameters")]
		public int Quantity
		{ get; set; }

		#endregion
	}
}
