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
	public class Strategy209 : Strategy
	{
		private Order longEntry;
        private Order shortEntry;
        private Order takeProfitOrder;
        private Order stopLossOrder;

		private DateTime OpenTime;
		private DateTime CloseTime;
		private DateTime LastTradeTime;
		private DateTime LastDataDay;

		private bool longPatternMatched = false;
		private bool shortPatternMatched = false;
		private bool longExitCondition = false;
		private bool shortExitCondition = false;

		private PriceAction PA;
		private EMA emaShort;
		private EMA emaLong;
		private ATR atr;
		private EMA atrEMA;
		private ChoppinessIndex chop;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Trend Following Price Action Strategy";
				Name										= "Strategy 2.0.9";
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

				TradeQuantity								= 1;
				ProfitTarget								= 0.25;
				StopLossTarget								= 0.75;
				TimeShift									= 0;
				LastDataDay									= new DateTime(2023, 03, 17);
				OpenTime									= DateTime.Parse("10:00", System.Globalization.CultureInfo.InvariantCulture);
				CloseTime									= DateTime.Parse("15:30", System.Globalization.CultureInfo.InvariantCulture);
				LastTradeTime								= DateTime.Parse("15:00", System.Globalization.CultureInfo.InvariantCulture);
			}
			else if (State == State.Configure)
			{
				//AddDataSeries(Data.BarsPeriodType.Tick, 1);
				AddDataSeries(Data.BarsPeriodType.Second, 1);
			}
			if (State == State.DataLoaded) {
				PA 					= PriceAction();
				emaShort			= EMA(9);
				emaLong				= EMA(21);
				atr 				= ATR(14);
				atrEMA				= EMA(atr, 21);
				chop				= ChoppinessIndex(14);
			}
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBar < BarsRequiredToTrade || CurrentBars[0] < 1 || CurrentBars[1] < 1) {
				return;
            }

			if (BarsInProgress == 0) {
				setIndicators();
				exitPositions();
				setEntries();

				return;
			}

			exitPositions();

		}

		private void exitPositions() {
			bool exitLong = !isValidTradeTime() || longExitCondition;
			bool exitShort = !isValidTradeTime() || shortExitCondition;

			if (exitLong && Position.MarketPosition == MarketPosition.Long) {
				ExitLong();
			}

			if (exitShort && Position.MarketPosition == MarketPosition.Short) {
				ExitShort();
			}
        }

		private void setIndicators()
		{
			bool emaShortRising 	= emaShort[0] > emaShort[1];
			bool emaShortFalling	= emaShort[0] < emaShort[1];
			bool emaLongRising		= emaLong[0] > emaLong[1];
			bool emaLongFalling		= emaLong[0] < emaLong[1];
			bool maRising			= emaShortRising && emaLongRising;
			bool maFalling			= emaShortFalling && emaLongFalling;

			bool inChopRange		= chop[0] > 38.2 && chop[0] < 62.8;

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

			bool closeAboveEma		= Close[0] > emaShort[0];
			bool closeBelowEma		= Close[0] < emaShort[0];

			bool barsDown 		= PA.LeastBarsDown(3, 4) && PA.BarIsDown(0) && PA.BarIsDown(1) && Low[0] < MIN(Low, 10)[1];
			bool barsUp 		= PA.LeastBarsUp(3, 4) && PA.BarIsUp(0) && PA.BarIsUp(1) && High[0] > MAX(High, 10)[1];
			bool barsLarge		= PA.BarIsBig(0) || PA.BarNearExtreme(0);

			//bool longPattern		= maRising && rising && newHigh && upTrend && closeAboveEma && barsUp && barsLarge;
			//bool shortPattern		= maFalling && falling && newLow && downTrend && closeBelowEma && barsDown && barsLarge;

			//bool longPattern		= maRising && barsUp && barsLarge;
			//bool shortPattern		= maFalling && barsDown && barsLarge;

			longPatternMatched 		= maRising && rising && newHigh && !inChopRange && upTrend;
			shortPatternMatched		= maFalling && falling && newLow && !inChopRange && downTrend;

			//longPatternMatched 		= longPattern && !inChopRange;
			//shortPatternMatched		= shortPattern && !inChopRange;
			//longExitCondition		= downTrend || falling || maFalling || PA.LeastBarsDown(3, 5) || PA.ConsecutiveSmallerBars(5);
			//shortExitCondition		= upTrend || rising || maRising || PA.LeastBarsUp(3, 5) || PA.ConsecutiveSmallerBars(5);
		}

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

		private void setEntries()
		{
			if (!isValidEntryTime()) {
				return;
            }

			if (Position.MarketPosition != MarketPosition.Flat) {
				return;
			}

			double profitDistance			= atr[0] * ProfitTarget * ((atr[0] > atrEMA[0]) ? 2 : 1);
			double stopLossDistance			= atr[0] * StopLossTarget;

			SetStopLoss(CalculationMode.Ticks, stopLossDistance);
			SetProfitTarget(CalculationMode.Ticks, profitDistance);

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
		[Display(Name="Trade Quantity", Description="Trade Quantity", Order=1, GroupName="Parameters")]
		public int TradeQuantity
		{ get; set; }

		[NinjaScriptProperty]
		[Range(0, double.MaxValue)]
		[Display(Name="Profit Target", Description="Profit Target", Order=2, GroupName="Parameters")]
		public double ProfitTarget
		{ get; set; }

		[NinjaScriptProperty]
		[Range(0, double.MaxValue)]
		[Display(Name="Stop Loss Target", Description="Stop Loss Target", Order=3, GroupName="Parameters")]
		public double StopLossTarget
		{ get; set; }


		[NinjaScriptProperty]
		[Range(double.MinValue, double.MaxValue)]
		[Display(Name="Time Shift (Hours)", Description="Time Shift", Order=4, GroupName="Parameters")]
		public double TimeShift
		{ get; set; }

		#endregion
	}
}
x
