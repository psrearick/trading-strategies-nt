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
	public class Strategy208Ensemble : Strategy
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

		private List<Strategy208ESLogic> strategies = new List<Strategy208ESLogic>();

		public ADX adx;
		public ATR atr;
		public EMA emaShort;
		public PriceActionUtils pa;

		private ATR atrLong;
		private SMA atrMa;

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
				Name											= "Strategy 2.0.8 Ensemble";
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
				BarsRequiredToTrade								= 200;

				IncludeTradeHistoryInBacktest					= true;
				IsInstantiatedOnEachOptimizationIteration		= true;
				IsUnmanaged										= false;

				LogTrades = false;
				ConsensusThreshold = 6;
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
				atr 				= ATR(8);
				atrLong				= ATR(52);
				atrMa 				= SMA(atr, 10);
				pa				 	= PriceActionUtils();
				adx					= ADX(14);

				strategies.Add(new Strategy208ESLogic{ BaseStrategy = this, Signals = Strategy208Signals(BarsArray[2]) });
				strategies.Add(new Strategy208ESLogic{ BaseStrategy = this, Signals = Strategy208Signals(BarsArray[3]) });
				strategies.Add(new Strategy208ESLogic{ BaseStrategy = this, Signals = Strategy208Signals(BarsArray[4]) });
				strategies.Add(new Strategy208ESLogic{ BaseStrategy = this, Signals = Strategy208Signals(BarsArray[5]) });
				strategies.Add(new Strategy208ESLogic{ BaseStrategy = this, Signals = Strategy208Signals(BarsArray[6]) });
				strategies.Add(new Strategy208ESLogic{ BaseStrategy = this, Signals = Strategy208Signals(BarsArray[7]) });
				strategies.Add(new Strategy208ESLogic{ BaseStrategy = this, Signals = Strategy208Signals(BarsArray[8]) });
				strategies.Add(new Strategy208ESLogic{ BaseStrategy = this, Signals = Strategy208Signals(BarsArray[9]) });
				strategies.Add(new Strategy208ESLogic{ BaseStrategy = this, Signals = Strategy208Signals(BarsArray[10]) });
				strategies.Add(new Strategy208ESLogic{ BaseStrategy = this, Signals = Strategy208Signals(BarsArray[11]) });
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

			if (CurrentBar < BarsRequiredToTrade || CurrentBars[0] < 1 || CurrentBars[1] < 1 || CurrentBars[2] < 1)
				return;

			ExitPositions();

			if (BarsInProgress != 0)
				return;

			if (SystemPerformance.AllTrades.Count > trades)
			{
				trades = SystemPerformance.AllTrades.Count;
				Trade lastTrade = SystemPerformance.AllTrades[SystemPerformance.AllTrades.Count - 1];
			}

			if (Time[0].Date != lastDay.Date)
			{
				Days++;
				lastDay = Time[0];
			}

			if (!IsValidEntryTime()) {
				return;
            }

			if (Position.MarketPosition != MarketPosition.Flat) {
				return;
			}

			int longVotes = 0, shortVotes = 0;

			foreach (var strategy in strategies)
	        {
                if (strategy.ShouldEnterLong())
                    longVotes++;
				if (strategy.ShouldEnterShort())
                    shortVotes++;
	        }

	        if (longVotes >= ConsensusThreshold)
	            EnterLongTrade();
	        else if (shortVotes >= ConsensusThreshold)
	            EnterShortTrade();
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

		#region SetExits()
		private void SetExits(bool isLong)
		{
			double atrDistance = 2 - Math.Min(2, (atr[0] / atrMa[0]));
			double atrDistanceFactor = atrDistance;
			double slTarget = (2 - (Math.Abs((atr[0] - atrMa[0]) / atrMa[0]) + (1 - (adx[0] / 100)))) * 5;
			double target = slTarget * strategies[0].GetExtremesMultiplier(isLong);
			double stopLossDistance = (atrLong[0] * target) / TickSize;
			double profitDistance = stopLossDistance * (atrDistanceFactor);

			SetStopLoss(CalculationMode.Ticks, stopLossDistance);
			SetProfitTarget(CalculationMode.Ticks, profitDistance);
		}
		#endregion

		#region EnterLongTrade()
		private void EnterLongTrade()
		{
			SetExits(true);
			EnterLong(1, "longEntry");
		}
		#endregion

		#region EnterShortTrade()
		private void EnterShortTrade()
		{
			SetExits(false);
			EnterShort(1, "shortEntry");
		}
		#endregion

		#region Properties

		[NinjaScriptProperty]
		[Display(Name="Log Trades", Description="Log Trades", Order=0, GroupName="Parameters")]
		public bool LogTrades
		{ get; set; }

		[NinjaScriptProperty]
	    [Range(1, int.MaxValue)]
	    [Display(Name="Consensus Threshold", Description="Number of strategies that must agree to enter a trade", Order=1, GroupName="Parameters")]
	    public int ConsensusThreshold { get; set; }

		#endregion
	}

	public class Strategy208ESLogic
	{
		public Strategy208Ensemble BaseStrategy { get; set; }
		public Strategy208Signals Signals {get; set; }

		private List<int> higherHighs = new List<int>();
		private List<int> lowerLows = new List<int>();

		public double GetExtremesMultiplier(bool isLong)
		{
			int consecutiveHigherHighs = BaseStrategy.pa.MaxNumberOfConsecutiveHigherHighs(0, 20);
			int consecutiveLowerLows = BaseStrategy.pa.MaxNumberOfConsecutiveLowerLows(0, 20);

			higherHighs.Add(consecutiveHigherHighs);

			if (higherHighs.Count() > 10)
			{
				higherHighs.RemoveAt(0);
			}

			lowerLows.Add(consecutiveLowerLows);

			if (lowerLows.Count() > 10)
			{
				lowerLows.RemoveAt(0);
			}

			int maxHigherHighs = higherHighs.Max();
			int maxLowerLows = lowerLows.Max();

			double normalizedHigherHighs = Math.Max(0.1, (double) consecutiveHigherHighs / maxHigherHighs);
			double normalizedLowerLows = Math.Max(0.1, (double) consecutiveLowerLows / maxLowerLows);

			return isLong ? normalizedHigherHighs : normalizedLowerLows;
		}

		private bool ShouldEnter(bool isLong)
		{
//			if (GetExtremesMultiplier(isLong) < 0.4)
//				return false;

			if (BaseStrategy.adx[0] < 25)
				return false;

			return (Math.Abs(BaseStrategy.Close[0] - BaseStrategy.emaShort[0]) / BaseStrategy.atr[0]) < 1;
		}

		public bool ShouldEnterLong()
		{
			if (Signals.Signals[0] != TrendDirection.Bullish)
				return false;

			if (Signals.LongScores[0] < 11)
				return false;

			return ShouldEnter(true);
		}

		public bool ShouldEnterShort()
		{
			if (Signals.Signals[0] != TrendDirection.Bearish)
				return false;

			if (Signals.ShortScores[0] < 11)
				return false;
		
			return ShouldEnter(false);
		}
	}
}
