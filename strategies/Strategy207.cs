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

		private double stopLossDistance = 1;
		private double stopLossMultiplier = 1;
		private double profitTargetDistance = 1;
		private double profitTargetMultiplier = 1;

		private SMA atrMA;
		private EMA emaShort;
		private EMA emaLong;

		private ATR atr;

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
				IsInstantiatedOnEachOptimizationIteration		= true;
				IsUnmanaged										= false;

				Risk = 0;
				TradeQuantity = 1;
				StopLossTarget = 5;
				LogTrades = false;
			}
			#endregion

			#region State.Configure
			if (State == State.Configure)
			{
				AddDataSeries(Data.BarsPeriodType.Second, 30);
				emaShort			= EMA(10);
				emaLong				= EMA(20);
				atr 				= ATR(8);
				atrMA				= SMA(atr, 3);
				chop				= ChoppinessIndex(7);
			}
			#endregion

			#region State.DataLoaded
			if (State == State.DataLoaded) {
			}
			#endregion
		}
		#endregion

		protected override void OnBarUpdate()
		{
			if (CurrentBar < BarsRequiredToTrade || CurrentBars[0] < 1 || CurrentBars[1] < 1)
			{
				return;
            }

			exitPositions();

			if (BarsInProgress != 0)
			{
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

			calculateMultiples();
			calculateQuantity();
			setIndicators();
			setEntries();
		}

		#region calculateQuantity()
		private void calculateQuantity()
		{
			quantity = TradeQuantity;

			if (Risk > 0) {
				double TickValue = Instrument.MasterInstrument.PointValue * TickSize;

				quantity = (int) Math.Max(1, Math.Floor(Risk / (stopLossDistance * TickValue)));

			}
		}
		#endregion

		#region calculateMultiples()
		private void calculateMultiples()
		{
			double atrValue = atr[0];
			double atrMultiple = (atrValue > atrMA[0]) ? 1.5 : 1.0;

   			double shortEmaSlope = (emaShort[0] - emaShort[2]) / 2;
    		double longEmaSlope = (emaLong[0] - emaLong[2]) / 2;

			double profitTarget = Math.Min(10, Math.Max(1, (1 - (StopLossTarget / 30)) * 10));

			if (longPatternMatched)
		    {
		        stopLossMultiplier = (shortEmaSlope > longEmaSlope) ? StopLossTarget : (StopLossTarget * 1.5);
		        profitTargetMultiplier = (shortEmaSlope > longEmaSlope) ? (profitTarget * 1.5) : profitTarget;
		    }
		    else if (shortPatternMatched)
		    {
		        stopLossMultiplier = (shortEmaSlope < longEmaSlope) ? StopLossTarget : (StopLossTarget * 1.5);
		        profitTargetMultiplier = (shortEmaSlope < longEmaSlope) ? (profitTarget * 1.5) : profitTarget;
		    }

			stopLossDistance = atrValue * stopLossMultiplier * atrMultiple;
    		profitTargetDistance = atrValue * profitTargetMultiplier * atrMultiple;
		}
		#endregion

		#region exitPositions()
		private void exitPositions()
		{
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
		#endregion

		#region
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
		#endregion

		#region isValidEntryTime()
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
		#endregion

		#region  isValidTradeTime()
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

			if (quantity == 0) {
				return;
			}

			SetStopLoss(CalculationMode.Ticks, stopLossDistance);
			SetProfitTarget(CalculationMode.Ticks, profitTargetDistance);

			if (longPatternMatched) {
				EnterLong(quantity, "longEntry");
			}

			if (shortPatternMatched) {
				EnterShort(quantity, "shortEntry");
			}
		}
		#endregion

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
		[Display(Name="Stop Loss Target", Description="Stop Loss Target", Order=2, GroupName="Parameters")]
		public double StopLossTarget
		{ get; set; }

		[NinjaScriptProperty]
		[Display(Name="Log Trades", Description="Log Trades", Order=3, GroupName="Parameters")]
		public bool LogTrades
		{ get; set; }

		#endregion
	}
}
