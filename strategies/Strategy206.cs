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
	public class Strategy206 : Strategy
	{
		private Order longEntry;
        private Order shortEntry;
        private Order takeProfitOrder;
        private Order stopLossOrder;

		private DateTime OpenTime;
		private DateTime CloseTime;
		private DateTime LastTradeTime;
		private DateTime LastDataDay;

		private int quantity;

		private bool longPatternMatched;
		private bool shortPatternMatched;

		private EMA emaShort;
		private EMA emaLong;
		private ATR atr;
		private ATR atrDaily;
		private ChoppinessIndex chop;

		private TradesExporter tradesExporter;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description										= @"Trend Following Price Action Strategy";
				Name											= "Strategy 2.0.6";
				Calculate										= Calculate.OnBarClose;
				EntriesPerDirection								= 1;
				EntryHandling									= EntryHandling.AllEntries;
				IsExitOnSessionCloseStrategy					= true;
				ExitOnSessionCloseSeconds						= 30;
				IsFillLimitOnTouch								= false;
				MaximumBarsLookBack								= MaximumBarsLookBack.TwoHundredFiftySix;
				OrderFillResolution								= OrderFillResolution.Standard;
				Slippage										= 0;
				StartBehavior									= StartBehavior.WaitUntilFlat;
				TimeInForce										= TimeInForce.Day;
				TraceOrders										= false;
				RealtimeErrorHandling							= RealtimeErrorHandling.StopCancelCloseIgnoreRejects;
				StopTargetHandling								= StopTargetHandling.PerEntryExecution;
				BarsRequiredToTrade								= 20;

				IncludeTradeHistoryInBacktest					= true;
				IsInstantiatedOnEachOptimizationIteration		= false;
				IsUnmanaged										= false;

				Risk											= 0;
				TradeQuantity									= 1;
				ProfitTarget									= 3;
				StopLossTarget									= 5;
				ChoppinessThresholdLow							= 30;
				ChoppinessThresholdHigh							= 60;
				HighATRMultiplier								= 3;
				TimeShift										= 0;
				LastDataDay										= new DateTime(2023, 03, 17);
				OpenTime										= DateTime.Parse("10:00", System.Globalization.CultureInfo.InvariantCulture);
				CloseTime										= DateTime.Parse("15:30", System.Globalization.CultureInfo.InvariantCulture);
				LastTradeTime									= DateTime.Parse("15:00", System.Globalization.CultureInfo.InvariantCulture);
				TradesExporterActivated							= false;
				InvertChoppiness								= false;
			}
			else if (State == State.Configure)
			{
				AddDataSeries(Data.BarsPeriodType.Second, 1);
				AddDataSeries(Data.BarsPeriodType.Minute, 30);
			}

			if (State == State.DataLoaded) {
				emaShort			= EMA(9);
				emaLong				= EMA(21);
				atr 				= ATR(14);
				atrDaily			= ATR(BarsArray[2], 14);
				chop				= ChoppinessIndex(14);
				tradesExporter		= TradesExporter(Name, Instrument.MasterInstrument.Name);
			}
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBar < BarsRequiredToTrade || CurrentBars[0] < 1 || CurrentBars[1] < 1 || CurrentBars[2] < 1) {
				return;
            }

			exitPositions();

			if (BarsInProgress != 0) {
				return;
			}

			calculateQuantity();
			setIndicators();
			setEntries();
		}

		protected override void OnExecutionUpdate(Execution execution, string executionId, double price, int quantity, MarketPosition marketPosition, string orderId, DateTime time)
		{
			if (TradesExporterActivated && SystemPerformance.AllTrades.Count > 0)
			{
				tradesExporter.OnNewTrade(SystemPerformance.AllTrades[SystemPerformance.AllTrades.Count - 1]);
			}
		}

		private void calculateQuantity()
		{
			quantity = TradeQuantity;

			if (Risk > 0) {
				double stopLossDistance	= Math.Round((atrDaily[0] * StopLossTarget) / 4);
				quantity = (int) Math.Floor(Math.Max(quantity, Risk / (stopLossDistance * 50)));
			}
		}

		private void exitPositions() {
			if (isValidTradeTime()) {
				return;
			}

			if (Position.MarketPosition == MarketPosition.Long) {
				ExitLong();
			}

			if (Position.MarketPosition == MarketPosition.Short) {
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

			bool lowChop			= chop[0] < ChoppinessThresholdLow;
			bool highChop			= chop[0] > ChoppinessThresholdHigh;
			bool validChoppiness 	= lowChop || highChop;

			if (InvertChoppiness) {
				validChoppiness 	= !lowChop && !highChop;
			}


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

			longPatternMatched 		= maRising && rising && newHigh && validChoppiness && upTrend;
			shortPatternMatched		= maFalling && falling && newLow && validChoppiness && downTrend;
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

			if (quantity == 0) {
				return;
			}

			double profitDistance			= atrDaily[0] * ProfitTarget;
			double stopLossDistance			= atrDaily[0] * StopLossTarget;

			if (atr[0] > (atrDaily[0] * 0.5)) {
				profitDistance = profitDistance * HighATRMultiplier;
			}

			SetStopLoss(CalculationMode.Ticks, stopLossDistance);
			SetProfitTarget(CalculationMode.Ticks, profitDistance);

			if (longPatternMatched) {
				longEntry = EnterLong(quantity, "longEntry");
			}

			if (shortPatternMatched) {
				shortEntry = EnterShort(quantity, "shortEntry");
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
		[Range(0, double.MaxValue)]
		[Display(Name="Profit Target", Description="Profit Target", Order=3, GroupName="Parameters")]
		public double ProfitTarget
		{ get; set; }

		[NinjaScriptProperty]
		[Range(0, double.MaxValue)]
		[Display(Name="Stop Loss Target", Description="Stop Loss Target", Order=4, GroupName="Parameters")]
		public double StopLossTarget
		{ get; set; }

		[NinjaScriptProperty]
		[Range(0, 100)]
		[Display(Name="Choppiness Threshold High", Description="Choppiness Threshold High", Order=5, GroupName="Parameters")]
		public double ChoppinessThresholdHigh
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


		[NinjaScriptProperty]
		[Display(Name="Invert Choppiness Filter", Description="Invert Choppiness Filter", Order=8, GroupName="Parameters")]
		public bool InvertChoppiness
		{ get; set; }


		[NinjaScriptProperty]
		[Range(double.MinValue, double.MaxValue)]
		[Display(Name="Time Shift (Hours)", Description="Time Shift", Order=9, GroupName="Parameters")]
		public double TimeShift
		{ get; set; }


		[NinjaScriptProperty]
		[Display(Name="Export Trades", Description="Export Trades", Order=10, GroupName="Parameters")]
		public bool TradesExporterActivated
		{ get; set; }

		#endregion
	}
}
