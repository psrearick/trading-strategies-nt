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
		private Legs legs;
		private MarketDirection marketDirection;
		private EntryEvaluator entryEvaluator;
		private TradesExporter tradesExporter;

		private EntrySignal entry;

		private DateTime LastDataDay		= new DateTime(2023, 03, 17);
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
				Description									= @"";
				Name											= "Strategy 3.0.1";
				Calculate									= Calculate.OnBarClose;
				EntriesPerDirection							= 1;
				EntryHandling								= EntryHandling.UniqueEntries;
				IsExitOnSessionCloseStrategy					= true;
				ExitOnSessionCloseSeconds					= 30;
				IsFillLimitOnTouch							= false;
				MaximumBarsLookBack							= MaximumBarsLookBack.TwoHundredFiftySix;
				OrderFillResolution							= OrderFillResolution.Standard;
				Slippage										= 0;
				StartBehavior								= StartBehavior.WaitUntilFlat;
				TimeInForce									= TimeInForce.Gtc;
				TraceOrders									= false;
				RealtimeErrorHandling						= RealtimeErrorHandling.StopCancelClose;
				StopTargetHandling							= StopTargetHandling.PerEntryExecution;
				BarsRequiredToTrade							= 200;
				// Disable this property for performance gains in Strategy Analyzer optimizations
				// See the Help Guide for additional information
				IsInstantiatedOnEachOptimizationIteration	= true;
				TimeShift									= -6;
				Period 										= 10;
				Quantity 									= 1;
//				TargetMultiplier								= 1;
//				QuantityMultiplier							= 2;
				Window										= 20;
			}
			#endregion

			#region State.Configure
			else if (State == State.Configure)
			{
				entryEvaluator			= EntryEvaluator(Period, Window);
				entry					= EntrySignal(1);
			}
			#endregion

			#region State.DataLoaded
			if (State == State.DataLoaded) {
//				tradesExporter			= TradesExporter(Name, Instrument.MasterInstrument.Name);
				marketDirection 			= entryEvaluator.md;
				legs 					= marketDirection.LegLong;
			}
			#endregion
		}
		#endregion

		#region OnBarUpdate()
		protected override void OnBarUpdate()
		{
			if (Position.MarketPosition != MarketPosition.Flat) {
				entry.Update();
			}

			if (CurrentBar < BarsRequiredToTrade || CurrentBars[0] < 1) {
				return;
            }

			exitPositions();

			setEntries();
		}
		#endregion

//		#region OnExecutionUpdate()
//		protected override void OnExecutionUpdate(Execution execution, string executionId, double price, int quantity, MarketPosition marketPosition, string orderId, DateTime time)
//		{
//			if (TradesExporterActivated && SystemPerformance.AllTrades.Count > 0)
//			{
//				tradesExporter.OnNewTrade(SystemPerformance.AllTrades[SystemPerformance.AllTrades.Count - 1]);
//			}
//		}
//		#endregion

		#region shouldExit()
		private bool shouldExit() {
			if (Position.MarketPosition != MarketPosition.Flat) {
				if (entryEvaluator.EvaluateExitCriteria(entry) > entryEvaluator.successRate) {
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

			if (!entryPatternMatched()) {
				return;
			}

			entryEvaluator.InitializeEntry(entry);


			int quantity = Math.Max(1, (int) Math.Round(entryEvaluator.successRate * entryEvaluator.matched[0] * (double)Quantity, 0));

			if (marketDirection.Direction[0] == TrendDirection.Bullish) {
				double swingLow = legs.BarsAgoStarts[0] > 0 ? Math.Min(MIN(Low, legs.BarsAgoStarts[0])[0], MIN(Low, 4)[0]) : Low[0];
				double stopLossDistance = 4 * (Close[0] - swingLow) + 1;

				if (swingLow < Low[0]) {
					SetStopLoss(CalculationMode.Ticks, stopLossDistance);
					if (entryEvaluator.successRate < 0.6) {
						SetProfitTarget("LongEntry1", CalculationMode.Ticks, stopLossDistance * 0.5);
					}

					EnterLong(quantity, "LongEntry1");
				}
			}

			if (marketDirection.Direction[0] == TrendDirection.Bullish) {
				double swingHigh = legs.BarsAgoStarts[0] > 0 ? Math.Max(MAX(High, legs.BarsAgoStarts[0])[0], MAX(High, 4)[0]) : High[0];
				double stopLossDistance = 4 * (swingHigh - Close[0]) + 1;

				if (swingHigh > High[0]) {
					SetStopLoss(CalculationMode.Ticks, stopLossDistance);
					if (entryEvaluator.successRate < 0.6) {
						SetProfitTarget("ShortEntry1", CalculationMode.Ticks, stopLossDistance * 0.5);
					}
					EnterShort(quantity, "ShortEntry1");
				}
			}
		}
		#endregion

		#region entryPatternMatched()
		private bool entryPatternMatched()
		{
			if (marketDirection.Direction[0] == TrendDirection.Flat) {
				return false;
			}

			if (entryEvaluator.matched[0] < (1 - entryEvaluator.successRate)) {
				return false;
			}

			if (legs.BarsAgoStarts[0] < 4) {
				return false;
			}

			if (legs.BarsAgoStarts[0] > 8) {
				return false;
			}

			return true;
		}
		#endregion

		#region Properties

		[NinjaScriptProperty]
		[Range(2, int.MaxValue)]
		[Display(Name="Period", Description="Period", Order=0, GroupName="Parameters")]
		public int Period
		{ get; set; }

		[NinjaScriptProperty]
		[Range(int.MinValue, int.MaxValue)]
		[Display(Name="Time Shift (Hours)", Description="Time Shift", Order=1, GroupName="Parameters")]
		public int TimeShift
		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Quantity", Description="Quantity", Order=2, GroupName="Parameters")]
		public int Quantity
		{ get; set; }

//		[NinjaScriptProperty]
//		[Range(1, int.MaxValue)]
//		[Display(Name="Quantity Multiplier", Description="Quantity Multiplier", Order=3, GroupName="Parameters")]
//		public int QuantityMultiplier
//		{ get; set; }

//		[NinjaScriptProperty]
//		[Range(double.MinValue, double.MaxValue)]
//		[Display(Name="Target Multiplier", Description="Target Multiplier", Order=4, GroupName="Parameters")]
//		public double TargetMultiplier
//		{ get; set; }

		[NinjaScriptProperty]
		[Range(1, 200)]
		[Display(Name="Window", Description="Window", Order=5, GroupName="Parameters")]
		public int Window
		{ get; set; }

//		[NinjaScriptProperty]
//		[Range(0.25, double.MaxValue)]
//		[Display(Name="Low Target Multiplier", Description="Low Target Multiplier", Order=4, GroupName="Parameters")]
//		public double LowATRMultiplier
//		{ get; set; }

//		[NinjaScriptProperty]
//		[Range(0.25, double.MaxValue)]
//		[Display(Name="High Target Multiplier", Description="High Target Multiplier", Order=5, GroupName="Parameters")]
//		public double HighATRMultiplier
//		{ get; set; }

		[NinjaScriptProperty]
		[Display(Name="Export Trades", Description="Export Trades", Order=6, GroupName="Parameters")]
		public bool TradesExporterActivated
		{ get; set; }

		#endregion
	}
}
