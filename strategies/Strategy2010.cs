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
	public class Strategy2010 : Strategy
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

		double stopLossDistance = 0;

		private bool longPatternMatched;
		private bool shortPatternMatched;

		private int lowerLows = 0;
		private int higherHighs = 0;
		private int maxLowerLows = 0;
		private int maxHigherHighs = 0;

		private ATR atr;
		private ATR atrLong;
		private SMA atrMa;
		private ADX adx;
		private EMA ema;
		private PriceActionUtils pa;
		private Strategy208Display signals;
		private SwingRange swing;


		private Strategy208Signals signals2;

		private DateTime lastDay = DateTime.MinValue;
		private int days = 0;
		private int trades = 0;

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
				Name											= "Strategy 2.0.10";
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
				IsInstantiatedOnEachOptimizationIteration		= true;
				IsUnmanaged										= false;


				LogTrades 										= false;

				LongPeriod = 1;
				SwingPeriod = 25;
			}
			#endregion

			#region State.Configure
			if (State == State.Configure)
			{
				AddDataSeries(Data.BarsPeriodType.Second, 15);
				AddDataSeries(BarsPeriodType.Minute, 20);
				AddDataSeries(BarsPeriodType.Minute, 40);
				AddDataSeries(BarsPeriodType.Minute, 60);
				AddDataSeries(BarsPeriodType.Minute, 80);
				AddDataSeries(BarsPeriodType.Minute, 100);
				AddDataSeries(BarsPeriodType.Minute, 120);
				AddDataSeries(BarsPeriodType.Minute, 140);
				AddDataSeries(BarsPeriodType.Minute, 160);
				AddDataSeries(BarsPeriodType.Minute, 180);
				AddDataSeries(BarsPeriodType.Minute, 200);

//				adx = ADX(14);
//				atr = ATR(8);
//				atrLong = ATR(52);
//				ema = EMA(10);
//				pa = PriceActionUtils();
//				signals = Strategy208Display(LongPeriod, 15, 85);
//				swing = SwingRange(SwingPeriod, 20);

//				signals2 = Strategy208Signals(BarsArray[LongPeriod + 1]);
			}
			#endregion

			#region State.DataLoaded
			if (State == State.DataLoaded) {
				adx = ADX(14);
				atr = ATR(8);
				atrLong = ATR(52);
				ema = EMA(10);
				pa = PriceActionUtils();
				signals = Strategy208Display(LongPeriod, 15, 85);
				swing = SwingRange(SwingPeriod, 20);

				signals2 = Strategy208Signals(BarsArray[LongPeriod + 1]);
				atrMa = SMA(atr, 10);
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
			}

			if (Time[0] > endDate)
			{
				endDate = Time[0];
			}

			if (CurrentBar < BarsRequiredToTrade || CurrentBars[0] < 1 || CurrentBars[1] < 1 || CurrentBars[2] < 1) {
				return;
            }

			ExitPositions();

			if (BarsInProgress != 0) {
				return;
			}

			if (SystemPerformance.AllTrades.Count > trades)
			{
				trades = SystemPerformance.AllTrades.Count;
				Trade lastTrade = SystemPerformance.AllTrades[SystemPerformance.AllTrades.Count - 1];
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

			SetIndicators();
			SetEntries();
		}
		#endregion

		#region ExitPositions()
		private void ExitPositions()
		{
//			if (Position.MarketPosition == MarketPosition.Long && signals[1] < 50 && signals[0] < 50)
//			{
////				Print(swing[0]);
//				ExitLong();
//			}

//			if (Position.MarketPosition == MarketPosition.Short && signals[1] > 50 && signals[0] > 50)
//			{
////				Print(swing[0]);
//				ExitShort();
//			}

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
			int consecutiveHigherHighs = pa.MaxNumberOfConsecutiveHigherHighs(0, 20);
			int consecutiveLowerLows = pa.MaxNumberOfConsecutiveLowerLows(0, 20);

			higherHighs = consecutiveHigherHighs;
			lowerLows = consecutiveLowerLows;
			maxHigherHighs = Math.Max(maxHigherHighs, higherHighs);
			maxLowerLows = Math.Max(maxLowerLows, lowerLows);






//			longPatternMatched = (signals2.Signals[0] == TrendDirection.Bullish) && (signals2.LongScores[0] > 10);
//			shortPatternMatched = (signals2.Signals[0] == TrendDirection.Bearish) && (signals2.ShortScores[0] > 10);



//			longPatternMatched = false;
//			shortPatternMatched = false;

//			if (swing.Choppiness[0] == ChopLevel.Low)
//			{
//				return;
//			}


//			if (swing.Choppiness[0] == ChopLevel.Mid)
//			{
//				return;
//			}

//			if (swing.Choppiness[0] == ChopLevel.High)
//			{
//				return;
//			}

//			longPatternMatched = (signals.Signals[0] == TrendDirection.Bullish) && (signals.LongScores[0] > 10);
//			shortPatternMatched = (signals.Signals[0] == TrendDirection.Bearish) && (signals.ShortScores[0] > 10);

//			longPatternMatched = (signals.Signals[0] == TrendDirection.Bullish) && (signals[0] > 95);
//			shortPatternMatched = (signals.Signals[0] == TrendDirection.Bearish) && (signals[0] < 5);

			longPatternMatched = signals.Signals[0] == TrendDirection.Bullish && signals.marketLong.Direction[0] == TrendDirection.Bullish;
			shortPatternMatched = signals.Signals[0] == TrendDirection.Bearish && signals.marketLong.Direction[0] == TrendDirection.Bearish;

//			longPatternMatched = signals.Signals[0] == TrendDirection.Bullish;
//			shortPatternMatched = signals.Signals[0] == TrendDirection.Bearish;

//			longPatternMatched = false;
//			shortPatternMatched = false;

//			if (swing.Choppiness[0] == swing.Choppiness[1])
//			{
//				return;
//			}

//			if (Close[0] > swing.Upper[0] || Close[0] < swing.Lower[0])
//			{
//				return;
//			}

//			if (swing.chop[0] > 50)
//			{
//				return;
//			}

//			if (signals[0] > 15 && signals[1] < 15)
//			{
//				shortPatternMatched = true;
//			}

//			if (signals[0] < 85 && signals[1] > 85)
//			{
//				longPatternMatched = true;
//			}

//			if (signals[0] > 60 && signals[1] < 60)
//			{
//				longPatternMatched = true;
//			}

//			if (signals[0] < 40 && signals[1] > 40)
//			{
//				shortPatternMatched = true;
//			}

//			if (signals[0] > 50 && signals[1] > 50 && signals[2] > 50)
//			{
//				longPatternMatched = true;
//			}

//			if (signals[0] < 50 && signals[1] < 50 && signals[2] < 50)
//			{
//				shortPatternMatched = true;
//			}


//			if (swing.Choppiness[0] == ChopLevel.Mid)
//			{
//				return;
//			}

//			if (swing.Choppiness[0] == ChopLevel.High)
//			{
//				return;
//			}

//			longPatternMatched = signals[0] > 50;
//			shortPatternMatched = signals[0] < 50;


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

//			if (adx[0] < 25)
//			{
//				return;
//			}

//			double atr = ATR(14)[0];
//			double atrMa = SMA(ATR(14), 20)[0];

//			double atrDistance = 2 - Math.Min(2, (atr / atrMa));
////			double atrDistanceFactor = atrDistance;

//			double slTarget = (2 - (Math.Abs((atr - atrMa) / atrMa) + (1 - (adx[0] / 100)))) * 5;

////			double target = slTarget * extremesMultiplier;

//			double stopLossDistancePoints = slTarget * 4;// (atrLong[0] * target);
//			stopLossDistance = stopLossDistancePoints / TickSize;

//			double profitDistance = stopLossDistance * (atrDistance);



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

			double stopLossDistancePoints = (atrLong[0] * target);
			stopLossDistance = stopLossDistancePoints / TickSize;

			double profitDistance = stopLossDistance * (atrDistanceFactor);

			double atrsFromEma = Math.Abs(Close[0] - ema[0]) / atr[0];

//			if (atrsFromEma > 1)
//			{
//				return;
//			}

//			if (Close[0] > swing.Upper[0] || Close[0] < swing.Lower[0])
//			{
//				return;
//			}

//			double above = ((swing.Upper[0] - Close[0]) / TickSize) * 0.5;
//			double below = ((Close[0] - swing.Lower[0]) / TickSize) * 0.5;

			SetStopLoss(CalculationMode.Ticks, stopLossDistance);
			SetProfitTarget(CalculationMode.Ticks, profitDistance);

//			Print(longPatternMatched);
//			Print(stopLossDistance);


			if (longPatternMatched)
			{
//				SetStopLoss(CalculationMode.Ticks, below);
//				SetProfitTarget(CalculationMode.Ticks, above);
//				SetStopLoss(CalculationMode.Ticks, stopLossDistance);
//				SetProfitTarget(CalculationMode.Ticks, profitDistance);

				EnterLong(1, "longEntry");

				return;
			}

			if (shortPatternMatched)
			{
//				SetStopLoss(CalculationMode.Ticks, above);
//				SetProfitTarget(CalculationMode.Ticks, below);
//				SetStopLoss(CalculationMode.Ticks, stopLossDistance);
//				SetProfitTarget(CalculationMode.Ticks, profitDistance);

				EnterShort(1, "shortEntry");
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
		[Display(Name="Long Period", Description="Long Period", Order=1, GroupName="Parameters")]
		public int LongPeriod
		{ get; set; }

		[NinjaScriptProperty]
		[Range(0, int.MaxValue)]
		[Display(Name="Swing Period", Description="Swing Period", Order=1, GroupName="Parameters")]
		public int SwingPeriod
		{ get; set; }

		#endregion
	}
}
