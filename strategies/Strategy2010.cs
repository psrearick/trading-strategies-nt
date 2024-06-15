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

		private ADX adx;
		private Strategy208Display signals;
		private SwingRange swing;

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
			}
			#endregion

			#region State.DataLoaded
			if (State == State.DataLoaded) {
				adx			= ADX(14);
				signals		= Strategy208Display(LongPeriod, 15, 85);
				swing		= SwingRange(SwingPeriod, 20);
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
			longPatternMatched = false;
			shortPatternMatched = false;

			if (swing.Choppiness[0] == swing.Choppiness[1])
			{
				return;
			}

			if (swing.Choppiness[0] == ChopLevel.Mid)
			{
				return;
			}

			longPatternMatched = signals[0] > 55;
			shortPatternMatched = signals[0] < 45;
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

			if (adx[0] < 25)
			{
				return;
			}

			if (Close[0] > swing.Upper[0] || Close[0] < swing.Lower[0])
			{
				return;
			}

			double above = ((swing.Upper[0] - Close[0]) / TickSize);
			double below = ((Close[0] - swing.Lower[0]) / TickSize);

			if (longPatternMatched)
			{
				SetStopLoss(CalculationMode.Ticks, below);
				SetProfitTarget(CalculationMode.Ticks, above);

				EnterLong(1, "longEntry");

				return;
			}

			if (shortPatternMatched)
			{
				SetStopLoss(CalculationMode.Ticks, above);
				SetProfitTarget(CalculationMode.Ticks, below);

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
