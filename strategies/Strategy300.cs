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
	public class Strategy300 : Strategy
	{
		private Order longEntry;
        private Order shortEntry;
        private Order takeProfitOrder;
        private Order stopLossOrder;

		private DateTime OpenTime;
		private DateTime CloseTime;
		private DateTime LastTradeTime;
		private DateTime LastDataDay;

		private int quantity = 1;

		private bool longPatternMatched = false;
		private bool shortPatternMatched = false;
		private bool longExitCondition = false;
		private bool shortExitCondition = false;


		private PriceAction PA;
		private ATR Atr;
		private EMA Ema;
		private ChoppinessIndex Chop;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Trend Following Price Action Strategy";
				Name										= "Strategy 3.0.0";
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
				// Disable this property for performance gains in Strategy Analyzer optimizations
				// See the Help Guide for additional information
				IsInstantiatedOnEachOptimizationIteration	= false;
				IsUnmanaged									= false;

				StopLossTarget								= 4;

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
				PA 		= PriceAction();
				Atr		= ATR(14);
				Ema 	= EMA(21);
				Chop	= ChoppinessIndex(14);
			}
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBar < BarsRequiredToTrade || CurrentBars[0] < 1 || CurrentBars[1] < 1) {
				return;
            }

			exitPositions();

			if (BarsInProgress != 0) {
				return;
			}

			setIndicators();
			setEntries();
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
			bool barsDown 		= PA.LeastBarsDown(3, 4) && PA.BarIsDown(0) && PA.BarIsDown(1) && Low[0] < MIN(Low, 10)[1];
			bool barsUp 		= PA.LeastBarsUp(3, 4) && PA.BarIsUp(0) && PA.BarIsUp(1) && High[0] > MAX(High, 10)[1];
			bool barsLarge		= PA.BarIsBig(0) || PA.BarNearExtreme(0);// && (PA.BarRealBodySize(0) > Atr[0]);// || PA.BarRealBodySize(1) > Atr[1]);
			bool EMARising		= Ema[0] > Ema[3];
			bool EMALong		= EMARising && Close[0] > Ema[0];
			bool EMAFalling		= Ema[0] < Ema[3];
			bool EMAShort		= EMAFalling && Close[0] < Ema[0];

			bool bar0BelowEMA = Close[0] < Ema[0];
			bool bar1BelowEMA = Close[1] < Ema[1];
			bool barsBelowEMA = bar0BelowEMA && bar1BelowEMA;

			bool bar0AboveEMA = Close[0] > Ema[0];
			bool bar1AboveEMA = Close[1] > Ema[1];
			bool barsAboveEMA = bar0AboveEMA && bar1AboveEMA;

			bool bar0DownOrSmall = PA.BarIsDown(0) || PA.BarIsSmall(0);
			bool bar1DownOrSmall = PA.BarIsDown(1) || PA.BarIsSmall(1);
			bool bar2DownOrSmall = PA.BarIsDown(2) || PA.BarIsSmall(2);
			bool barsDownOrSmall = bar0DownOrSmall && bar1DownOrSmall && bar2DownOrSmall;

			bool bar0UpOrSmall = PA.BarIsUp(0) || PA.BarIsSmall(0);
			bool bar1UpOrSmall = PA.BarIsUp(1) || PA.BarIsSmall(1);
			bool bar2UpOrSmall = PA.BarIsUp(2) || PA.BarIsSmall(2);
			bool barsUpOrSmall = bar0UpOrSmall && bar1UpOrSmall && bar2UpOrSmall;

			longPatternMatched 	= barsUp && barsLarge && EMALong && Chop[0] > 35;
			longExitCondition 	= barsDownOrSmall || barsBelowEMA;// || Chop[0] < 35;
			//longExitCondition 	= (PA.ConsecutiveSmallerBars(3) && PA.ConsecutiveBarsDown(3) && PA.BarNearExtreme(0)) || PA.LeastBarsDown(4, 5) || PA.LeastSmallBars(3, 3);

			shortPatternMatched	= barsDown && EMAShort && barsLarge && Chop[0] > 35;
			shortExitCondition	= barsUpOrSmall || barsAboveEMA;// || Chop[0] < 35;
			//shortExitCondition 	= (PA.ConsecutiveSmallerBars(3) && PA.ConsecutiveBarsUp(3) && PA.BarNearExtreme(0)) || PA.LeastBarsUp(4, 5) || PA.LeastSmallBars(3, 3);
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

			// set double profitDistance
			//double stopLossDistance = 16;

			SetStopLoss(CalculationMode.Ticks, StopLossTarget);
//			SetProfitTarget(CalculationMode.Ticks, profitDistance);

			if (longPatternMatched) {
				longEntry = EnterLong(quantity, "longEntry");
			}

			if (shortPatternMatched) {
				shortEntry = EnterShort(quantity, "shortEntry");
			}
		}

		#region Properties

		[NinjaScriptProperty]
		[Range(double.MinValue, double.MaxValue)]
		[Display(Name="Time Shift (Hours)", Description="Time Shift", Order=1, GroupName="Parameters")]
		public double TimeShift
		{ get; set; }

		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name="Stop Loss Target", Description="Stop Loss Target", Order=2, GroupName="Parameters")]
		public int StopLossTarget
		{ get; set; }

		#endregion
	}
}
