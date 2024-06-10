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
	public class Strategy208 : Strategy
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
		double stopLossDistance = 0;

		private bool longPatternMatched;
		private bool shortPatternMatched;

		private EMA emaShort;
		private EMA emaLong;
		private ATR atr;
		private ATR atrLong;
		private SMA atrMa;
		private SMA maLong;
		private SMA maShort;
		private SMA maRef;
		private StdDev atrStdDev;
		private ChoppinessIndex chop;
		private MarketCycle market;
		private MarketCycle marketLong;
//		private MarketCycle marketExtraLong;
		private PriceActionUtils pa;
		private ADX adx;
		private Strategy208Signals signals;

		private int lowerLows = 0;
		private int higherHighs = 0;
		private int maxLowerLows = 0;
		private int maxHigherHighs = 0;
		private int longSeriesMarketBar = 0;

		private DateTime lastDay = DateTime.MinValue;
		private int days = 0;
		private int trades = 0;
		private string marketCondition = "";
		private string indicatorString = "";
		private string indicatorHeader = "";

		private DateTime endDate = DateTime.MinValue;
        [Browsable(false)]
        public DateTime EndDate
        {
            get { return endDate; }
            set { endDate = value; }
        }

		private DateTime startDate = DateTime.MinValue;
        [Browsable(false)]
        public DateTime StartDate
        {
            get { return startDate; }
            set { startDate = value; }
        }


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
				Name											= "Strategy 2.0.8";
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
				BarsRequiredToTrade								= 52;

				IncludeTradeHistoryInBacktest					= true;
				IsInstantiatedOnEachOptimizationIteration		= false;
				IsUnmanaged										= false;

				Risk											= 0;
				TradeQuantity									= 1;
//				StopLossTarget									= 5;
//				HighATRMultiplier								= 3;
				LogTrades 										= false;
			}
			#endregion

			#region State.Configure
			if (State == State.Configure)
			{
				AddDataSeries(Data.BarsPeriodType.Second, 15);
				AddDataSeries(BarsPeriodType.Minute, 20);
//				AddDataSeries(BarsPeriodType.Minute, 25);
			}
			#endregion

			#region State.DataLoaded
			if (State == State.DataLoaded) {
				emaShort			= EMA(10);
				emaLong				= EMA(20);
				atr 				= ATR(8);
				atrLong				= ATR(52);
				atrMa 				= SMA(atr, 10);
				atrStdDev	 		= StdDev(atr, 20);
				maShort				= SMA(20);
				maLong				= SMA(50);
				maRef				= SMA(100);
				chop				= ChoppinessIndex(7);
				market				= MarketCycle();
				marketLong			= MarketCycle(BarsArray[2]);
				pa				 	= PriceActionUtils();
				adx					= ADX(14);
				signals				= Strategy208Signals();
			}
			#endregion
		}
		#endregion

		#region OnBarUpdate()
		protected override void OnBarUpdate()
		{
			if (startDate == DateTime.MinValue)
			{
				startDate = Time[0];

//				Print(Time[0]);
			}

			if (Time[0] > endDate)
			{
				endDate = Time[0];
			}

			if (CurrentBar < BarsRequiredToTrade || CurrentBars[0] < 1 || CurrentBars[1] < 1 || CurrentBars[2] < 1) {
				return;
            }

//			if (StopLossTarget <= HighATRMultiplier)
//			{
//				return;
//			}

			ExitPositions();

			if (BarsInProgress != 0) {
				return;
			}

			if (SystemPerformance.AllTrades.Count > trades)
			{
				trades = SystemPerformance.AllTrades.Count;
				Trade lastTrade = SystemPerformance.AllTrades[SystemPerformance.AllTrades.Count - 1];
//				Print($"{lastTrade.ProfitCurrency},{marketCondition}");
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

			CalculateQuantity();
			SetIndicators();
			SetEntries();

			if (longSeriesMarketBar < marketLong.CurrentBar)
			{
				longSeriesMarketBar = marketLong.CurrentBar;
			}
		}
		#endregion

		#region CalculateQuantity()
		private void CalculateQuantity()
		{
			quantity = TradeQuantity;

			if (Risk > 0) {
				double TickValue = Instrument.MasterInstrument.PointValue * TickSize;
				quantity = (int) Math.Max(1, Math.Floor(Risk / (stopLossDistance * TickValue)));

			}
		}
		#endregion

		#region ExitPositions()
		private void ExitPositions()
		{
			if (IsValidTradeTime()) {
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

		#region SetIndicators()
		private void SetIndicators()
		{
//			bool emaShortRising = pa.IsRising(emaShort, 0, 1);
//			bool emaShortFalling = pa.IsFalling(emaShort, 0, 1);
//			bool emaLongRising = pa.IsRising(emaLong, 0, 1);
//			bool emaLongFalling	= pa.IsFalling(emaLong, 0, 1);
//			bool maRising = emaShortRising && emaLongRising;
//			bool maFalling = emaShortFalling && emaLongFalling;

//			bool aboveMA = Close[0] > maRef[0];
//			bool belowMA = Close[0] < maRef[0];

//			bool lowChop = (chop[0] > 20) && (chop[0] < 40);
//			bool highChop = (chop[0] > 60) && (chop[0] < 80);
//			bool validChoppiness = lowChop || highChop;

//			bool rising	= pa.ConsecutiveBarsUp(3, 1);
//			bool falling = pa.ConsecutiveBarsDown(3, 1);

//			bool newHigh = High[0] >= pa.HighestHigh(1, 5);
//			bool newLow	= Low[0] <= pa.LowestLow(1, 5);

//			bool higherHigh	= pa.ConsecutiveHigherHighs(0, 3);
//			bool lowerLow = pa.ConsecutiveLowerLows(0, 3);

//			bool highestInTrend	= pa.HighestHigh(0, 2) >= pa.HighestHigh(0, 5);
//			bool lowestInTrend = pa.LowestLow(0, 2) <= pa.LowestLow(0, 5);

			int consecutiveHigherHighs = pa.MaxNumberOfConsecutiveHigherHighs(0, 20);
			int consecutiveLowerLows = pa.MaxNumberOfConsecutiveLowerLows(0, 20);

			higherHighs = consecutiveHigherHighs;
			lowerLows = consecutiveLowerLows;
			maxHigherHighs = Math.Max(maxHigherHighs, higherHighs);
			maxLowerLows = Math.Max(maxLowerLows, lowerLows);

//			MarketCycleStage stage = market.Stage[0];
//			MarketCycleStage stageLong = marketLong.Stage[0];

//			bool validLongMarket = stageLong == MarketCycleStage.BroadChannel || stageLong == MarketCycleStage.TightChannel;
//			bool validShortMarket = stage != MarketCycleStage.Breakout;
////			bool validShortMarket = stage != MarketCycleStage.TightChannel;

////			bool validMarket = validLongMarket && validShortMarket;

//			bool validMarketDirection = market.Direction[0] == marketLong.Direction[0];

//			if (marketLong.Direction[0] != marketLong.Direction[1] && longSeriesMarketBar != marketLong.CurrentBar)
//			{
////				Print($"{Time[0]},{marketLong.Direction[0]},{marketLong.Stage[0]}");
//			}

//			indicatorHeader = "emaShortRising,emaShortFalling,emaLongRising,emaLongFalling,lowChop,highChop,rising,falling,newHigh,newLow,higherHigh,lowerLow,highestInTrend,lowestInTrend";
//			indicatorString = $"{emaShortRising},{emaShortFalling},{emaLongRising},{emaLongFalling},{lowChop},{highChop},{rising},{falling},{newHigh},{newLow},{higherHigh},{lowerLow},{highestInTrend},{lowestInTrend}";

//			longPatternMatched = true
////				&& highChop
////				&& lowChop
//				&& validChoppiness
////				&& validLongMarket
////				&& validShortMarket
////				&& validMarket
////				&& validMarketDirection
////				&& emaLongRising
//				&& aboveMA
////				&& emaShortRising
//				&& maRising
//				&& rising
////				&& newHigh
////				&& higherHigh
//				&& highestInTrend
//				&& market.Direction[0] == TrendDirection.Bullish
//				&& marketLong.Direction[0] == TrendDirection.Bullish
//			;

//			shortPatternMatched = true
////				&& lowChop
////				&& highChop
//				&& validChoppiness
////				&& validLongMarket
////				&& validShortMarket
////				&& validMarket
////				&& validMarketDirection
////				&& emaLongFalling
//				&& belowMA
////				&& emaShortFalling
//				&& maFalling
//				&& falling
////				&& newLow
////				&& lowerLow
//				&& lowestInTrend
//				&& market.Direction[0] == TrendDirection.Bearish
//				&& marketLong.Direction[0] == TrendDirection.Bearish
//			;

			longPatternMatched = signals.Signals[0] == TrendDirection.Bullish;
			shortPatternMatched = signals.Signals[0] == TrendDirection.Bearish;
		}
		#endregion

		#region IsValidEntryTime()
		private bool IsValidEntryTime()
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

		#region IsValidTradeTime()
		private bool IsValidTradeTime()
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

		#region SetEntries()
		private void SetEntries()
		{
			if (!IsValidEntryTime()) {
				return;
            }

			if (Position.MarketPosition != MarketPosition.Flat) {
				return;
			}

			if (quantity == 0) {
				return;
			}

			double normalizedHigherHighs = Math.Max(0.1, (double) higherHighs / maxHigherHighs);
			double normalizedLowerLows = Math.Max(0.1, (double) lowerLows / maxLowerLows);
			double extremesMultiplier = longPatternMatched
				? normalizedHigherHighs
				: shortPatternMatched
					? normalizedLowerLows
					: (normalizedHigherHighs + normalizedLowerLows) * 0.5;

			if (extremesMultiplier < 0.4 || adx[0] < 25)
			{
				longPatternMatched = false;
				shortPatternMatched = false;
			}

			double atrDistance = 2 - Math.Min(2, (atr[0] / atrMa[0]));
			double atrDistanceFactor = atrDistance;

			double slTarget = (2 - (Math.Abs((atr[0] - atrMa[0]) / atrMa[0]) + (1 - (adx[0] / 100)))) * 5;

			double target = slTarget * extremesMultiplier;

			stopLossDistance = (atrLong[0] * target) / TickSize;

			double profitDistance = stopLossDistance * (atrDistanceFactor);

			SetStopLoss(CalculationMode.Ticks, stopLossDistance);
			SetProfitTarget(CalculationMode.Ticks, profitDistance);

			MarketCycleStage stage = market.Stage[0];
			MarketCycleStage stageLong = marketLong.Stage[0];

			TrendDirection direction = maLong[0] > maLong[1] ? TrendDirection.Bullish : TrendDirection.Bearish;

			if (longPatternMatched) {
				marketCondition = $"Long,{chop[0]},{stage},{stageLong},{direction},{indicatorString}";
				EnterLong(quantity, "longEntry");
			}

			if (shortPatternMatched) {
				marketCondition = $"Short,{chop[0]},{stage},{stageLong},{direction},{indicatorString}";
				EnterShort(quantity, "shortEntry");
			}
		}
		#endregion

		#region Properties


		[NinjaScriptProperty]
		[Display(Name="Log Trades", Description="Log Trades", Order=0, GroupName="Parameters")]
		public bool LogTrades
		{ get; set; }

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

//		[NinjaScriptProperty]
//		[Range(0, double.MaxValue)]
//		[Display(Name="Stop Loss Target", Description="Stop Loss Target", Order=4, GroupName="Parameters")]
//		public double StopLossTarget
//		{ get; set; }

//		[NinjaScriptProperty]
//		[Range(0, 100)]
//		[Display(Name="High ATR Multiplier", Description="High ATR Multiplier", Order=5, GroupName="Parameters")]
//		public double HighATRMultiplier
//		{ get; set; }

		#endregion
	}
}
