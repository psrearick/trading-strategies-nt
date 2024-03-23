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
//		private EMA Ema;
		private TrendTypes TrendValues;
//		private Legs LegIdentifier;
//		private Swings SwingIdentifier;
//		private Trends TrendIdentifier;
//		private TrendDirection tradeDirection = TrendDirection.Flat;

		private double entryHigh = 0;
		private double entryLow = 0;
		private double stopLoss = 0;

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
				Description									= @"Enter the description for your new custom Strategy here.";
				Name										= "Strategy 3.0.1";
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
				RealtimeErrorHandling						= RealtimeErrorHandling.StopCancelClose;
				StopTargetHandling							= StopTargetHandling.PerEntryExecution;
				BarsRequiredToTrade							= 20;
				// Disable this property for performance gains in Strategy Analyzer optimizations
				// See the Help Guide for additional information
				IsInstantiatedOnEachOptimizationIteration	= true;
				TimeShift									= -6;
				CycleThreshold								= 5;
				CycleExitThreshold							= 3;
			}
			#endregion

			#region State.Configure
			else if (State == State.Configure)
			{
//				AddDataSeries(Data.BarsPeriodType.Second, 60);
			}
			#endregion

			#region State.DataLoaded
			if (State == State.DataLoaded) {
				PA 			= PriceActionUtils();
				TrendValues	= TrendTypes();
//				Ema = EMA(21);
//				MC = MarketCycle();
//				LegIdentifier = Legs();
//				SwingIdentifier = Swings();
//				TrendIdentifier = Trends();
			}
			#endregion
		}
		#endregion

		#region OnBarUpdate()
		protected override void OnBarUpdate()
		{
			if (CurrentBar < BarsRequiredToTrade || CurrentBars[0] < 1) {// || CurrentBars[1] < 1) {
				return;
            }

//			if (BarsInProgress != 0) {
//				return;
//			}

			TrendValues.Evaluate();

			exitPositions();

			setEntries();
		}
		#endregion

		#region shouldExit()
		private bool shouldExit() {
			if (Position.MarketPosition == MarketPosition.Long) {
//				if (TrendValues.LegValues[0] < 0) {
//					return true;
//				}

				if (TrendValues.SwingValues[0] < 0) {
					return true;
				}
			}

			if (Position.MarketPosition == MarketPosition.Short) {
//				if (TrendValues.LegValues[0] > 0) {
//					return true;
//				}

				if (TrendValues.SwingValues[0] > 0) {
					return true;
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

			if (longMatch) {
				double swingLow = MIN(Low, TrendValues.BreakoutStarted)[0];
				stopLoss = swingLow;
				double stopLossDistance = 4 * (Close[0] - stopLoss) + 4;

//				if (stopLossDistance > 100) {
//					return;
//				}

				if (swingLow < Low[0]) {
					SetStopLoss(CalculationMode.Ticks, stopLossDistance);
					EnterLong(1, "longEntry");
				}
			}

			if (shortMatch) {
				double swingHigh = MAX(High, TrendValues.BreakoutStarted)[0];
				stopLoss = swingHigh;
				double stopLossDistance = 4 * (stopLoss - Close[0]) + 4;

//				if (stopLossDistance > 100) {
//					return;
//				}

				if (swingHigh > High[0]) {
					SetStopLoss(CalculationMode.Ticks, stopLossDistance);
					EnterShort(1, "shortEntry");
				}
			}
		}
		#endregion

		#region longPatternMatched()
		private bool longPatternMatched()
		{
			if (TrendValues.BreakoutStarted == 0) {
				return false;
			}

			if (TrendValues.BreakoutValues[0] < 1) {
				return false;
			}

			if (TrendValues.LegValues[0] < 1) {
				return false;
			}

			if (TrendValues.SwingValues[0] < 0) {
				return false;
			}

			if (TrendValues.TrendValues[0] < 0) {
				return false;
			}

			return true;
		}
		#endregion

		#region shortPatternMatched()
		private bool shortPatternMatched() {

			if (TrendValues.BreakoutStarted == 0) {
				return false;
			}

			if (TrendValues.BreakoutValues[0] > -1) {
				return false;
			}

			if (TrendValues.LegValues[0] > -1) {
				return false;
			}

			if (TrendValues.SwingValues[0] > 0) {
				return false;
			}

			if (TrendValues.TrendValues[0] > 0) {
				return false;
			}

			return true;
		}
		#endregion

		#region Properties

		[NinjaScriptProperty]
		[Range(1, 9)]
		[Display(Name="Cycle", Description="Cycle", Order=1, GroupName="Parameters")]
		public double CycleThreshold
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, 9)]
		[Display(Name="Cycle Exit", Description="Cycle Exit", Order=2, GroupName="Parameters")]
		public int CycleExitThreshold
		{ get; set; }

		[NinjaScriptProperty]
		[Range(int.MinValue, int.MaxValue)]
		[Display(Name="Time Shift (Hours)", Description="Time Shift", Order=3, GroupName="Parameters")]
		public int TimeShift
		{ get; set; }

		#endregion
	}
}
