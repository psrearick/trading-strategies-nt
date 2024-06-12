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
	public class Strategy209 : Strategy
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

		private EMA emaShort;
		private ATR atr;
		private ATR atrLong;
		private SMA atrMa;
		private MarketCycle market;
		private MarketCycle marketLong;
		private PriceActionUtils pa;
		private ADX adx;
		private Strategy209Signals signals;

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

		private int[] validTimeframes = {9, 10, 12, 15};

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
				Name											= "Strategy 2.0.9";
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


				LogTrades 										= false;

				LongPeriod = 1;
				LowerThreshold = 10;
				UpperThreshold = 90;
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
			}
			#endregion

			#region State.DataLoaded
			if (State == State.DataLoaded) {
				emaShort			= EMA(10);
//				emaLong				= EMA(20);
				atr 				= ATR(8);
				atrLong				= ATR(52);
				atrMa 				= SMA(atr, 10);
				market				= MarketCycle();
				marketLong			= MarketCycle(BarsArray[2]);
				pa				 	= PriceActionUtils();
				adx					= ADX(14);
				signals				= Strategy209Signals(LongPeriod, LowerThreshold, UpperThreshold);
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

			SetIndicators();
			SetEntries();

			if (longSeriesMarketBar < marketLong.CurrentBar)
			{
				longSeriesMarketBar = marketLong.CurrentBar;
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
			int consecutiveHigherHighs = pa.MaxNumberOfConsecutiveHigherHighs(0, 20);
			int consecutiveLowerLows = pa.MaxNumberOfConsecutiveLowerLows(0, 20);

			higherHighs = consecutiveHigherHighs;
			lowerLows = consecutiveLowerLows;
			maxHigherHighs = Math.Max(maxHigherHighs, higherHighs);
			maxLowerLows = Math.Max(maxLowerLows, lowerLows);

			longPatternMatched = (signals.Signals[0] == TrendDirection.Bullish) && (signals.LongScores[0] > 10);
			shortPatternMatched = (signals.Signals[0] == TrendDirection.Bearish) && (signals.ShortScores[0] > 10);
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

			double atrsFromEma = Math.Abs(Close[0] - emaShort[0]) / atr[0];

			if (atrsFromEma > 1)
			{
				return;
			}

			SetStopLoss(CalculationMode.Ticks, stopLossDistance);
			SetProfitTarget(CalculationMode.Ticks, profitDistance);

			if (longPatternMatched) {
//				Print(signals[0]);
//				marketCondition = $"Long,{chop[0]},{stage},{stageLong},{direction},{indicatorString}";
				EnterLong(1, "longEntry");

				return;
			}

			if (shortPatternMatched) {
//				Print(signals[0]);
//				marketCondition = $"Short,{chop[0]},{stage},{stageLong},{direction},{indicatorString}";
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
		[Range(1, int.MaxValue)]
		[Display(Name="Long Period", Description="Long Period", Order=1, GroupName="Parameters")]
		public int LongPeriod
		{ get; set; }

		[Range(0, 100), NinjaScriptProperty]
		[Display(Name = "Lower Threshold", Description = "Lower Threshold", GroupName = "Parameters", Order = 2)]
		public double LowerThreshold
		{ get; set; }
	
		[Range(0, 100), NinjaScriptProperty]
		[Display(Name = "Upper Threshold", Description = "Upper Threshold", GroupName = "Parameters", Order = 3)]
		public double UpperThreshold
		{ get; set; }

		#endregion
	}
}
