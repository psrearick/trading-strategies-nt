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
	public class Strategy207 : Strategy
	{
		#region Variables
		private DateTime OpenTime = DateTime.Parse(
            "10:00",
            System.Globalization.CultureInfo.InvariantCulture
        );
        private DateTime CloseTime = DateTime.Parse(
            "15:30",
            System.Globalization.CultureInfo.InvariantCulture
        );
        private DateTime LastTradeTime = DateTime.Parse(
            "15:00",
            System.Globalization.CultureInfo.InvariantCulture
        );

		private int quantity;

		private bool longPatternMatched;
		private bool shortPatternMatched;

		private EMA emaShort;
		private EMA emaLong;

		private ATR atr;
		private ATR atrDaily;

		private ChoppinessIndex chop;
		private double ChoppinessThresholdLow = 38.2;
		private double ChoppinessThresholdHigh = 61.8;

		private DateTime lastDay = DateTime.MinValue;
		private int days = 0;

		[Browsable(false)]
		public int Days
		{
		    get { return days; }
		    set { days = value; }
		}
		#endregion

		#region OnStateChange()
		protected override void OnStateChange()
		{
			#region State.SetDefaults
			if (State == State.SetDefaults)
			{
				Description										= @"Trend Following Price Action Strategy";
				Name											= "Strategy 2.0.7";
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

				Risk = 0;
				TradeQuantity = 1;
				ProfitTarget = 3;
				StopLossTarget = 5;
				HighATRMultiplier = 3;
				LogTrades = false;
			}
			#endregion

			#region State.Configure
			if (State == State.Configure)
			{
				AddDataSeries(Data.BarsPeriodType.Second, 15);
				AddDataSeries(Data.BarsPeriodType.Minute, 30);
			}
			#endregion

			#region State.DataLoaded
			if (State == State.DataLoaded) {
				emaShort			= EMA(9);
				emaLong				= EMA(21);
				atr 				= ATR(14);
				atrDaily			= ATR(BarsArray[2], 14);
				chop				= ChoppinessIndex(5);
//				chop				= ChoppinessIndex(10);
//				chop				= ChoppinessIndex(14);
			}
			#endregion
		}
		#endregion

		protected override void OnBarUpdate()
		{
			if (CurrentBar < BarsRequiredToTrade || CurrentBars[0] < 1 || CurrentBars[1] < 1 || CurrentBars[2] < 1) {
				return;
            }

			exitPositions();

			if (BarsInProgress != 0) {
				return;
			}

			if (CurrentBar % 20 == 0)
			{
//				Print($"Bar: {CurrentBar} Time: {Time[0]}");
			}

			if (Time[0].Date != lastDay.Date)
			{
				Days++;
				lastDay = Time[0];
			}

			calculateQuantity();
			setIndicators();
			setEntries();
		}

		private void calculateQuantity()
		{
			quantity = TradeQuantity;

			if (Risk > 0) {
				double stopLossDistance	= Math.Round((atrDaily[0] * StopLossTarget) / TickSize);
				double TickValue = Instrument.MasterInstrument.PointValue * TickSize;
				quantity = (int) Math.Max(1, Math.Floor(Risk / (stopLossDistance * TickValue)));

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
				EnterLong(quantity, "longEntry");
			}

			if (shortPatternMatched) {
				EnterShort(quantity, "shortEntry");
			}
		}

		#region Properties

		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name="Risk", Description="Risk", Order=0, GroupName="Parameters")]
		public int Risk
		{ get; set; }

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
		[Range(0, 100)]
		[Display(Name="High ATR Multiplier", Description="High ATR Multiplier", Order=4, GroupName="Parameters")]
		public double HighATRMultiplier
		{ get; set; }

		[NinjaScriptProperty]
		[Display(Name="Log Trades", Description="Log Trades", Order=5, GroupName="Parameters")]
		public bool LogTrades
		{ get; set; }

		#endregion
	}
}
