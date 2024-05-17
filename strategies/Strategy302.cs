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
	public class Strategy302 : Strategy
	{
		#region Variables
		private SignalGenerator signalGenerator;
		private TrendDirection direction;
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
		private int TimeShift = 0;
		private double stopLoss = double.MaxValue;
		private double profitTarget = 1000;
		#endregion

		#region OnStateChange()
		protected override void OnStateChange()
		{
			#region State.SetDefaults
			if (State == State.SetDefaults)
			{
				Description									= @"";
				Name										= "Strategy 3.0.2";
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
			}
			#endregion

			#region State.Configure
			else if (State == State.Configure)
			{
				AddDataSeries(Data.BarsPeriodType.Second, 15);
			}
			#endregion

			#region State.DataLoaded
			else if (State == State.DataLoaded)
			{
//				signalGenerator = SignalGenerator(BarsArray[1], Data.BarsPeriodType.Minute, 5);
//				signalGenerator = SignalGenerator(BarsArray[0], Data.BarsPeriodType.Second, 15);
				signalGenerator = SignalGenerator();
			}
			#endregion
		}
		#endregion

		#region OnBarUpdate()
		protected override void OnBarUpdate()
		{


//			if (BarsInProgress == 1) {
//				signalGenerator.Update();
//			}
			signalGenerator.Update();

            if (CurrentBar < BarsRequiredToTrade || CurrentBars[0] < 1 || CurrentBars[1] < 1)
            {
                return;
            }

            exitPositions();

			if (BarsInProgress != 0) {
				return;
			}

            setEntries();
		}
		#endregion

		#region setEntries()
		private void setEntries()
		{
			if (!isValidEntryTime())
            {
                return;
            }

            if (Position.MarketPosition != MarketPosition.Flat)
            {
                return;
            }

			int entryCount = signalGenerator.CurrentEntries.Count();

			if (entryCount == 0) {
				return;
			}

			Signal entry = signalGenerator.CurrentEntries[entryCount - 1];

			if (entry.Bar != CurrentBar)
			{
				return;
			}

			double stop = Close[0];
			double confidence = entry.Combination.ConfidenceScore;

//			Print(confidence);

//			int quantity = (int) Math.Max(1, Math.Floor(5 * confidence));

			if (confidence < 0.5)
			{
				return;
			}

			int quantity = 1;
			double TickValue = Instrument.MasterInstrument.PointValue * TickSize;
			double TicksPerPoint = Instrument.MasterInstrument.PointValue / TickValue;

			direction = entry.Direction;
			if (direction == TrendDirection.Bullish)
			{
				stop = Low[signalGenerator.md.LegLong.BarsAgoStarts[0]] - 1;

				if (Close[0] <= stop)
				{
					return;
				}

				stopLoss = (Close[0] - stop) * TicksPerPoint;

				SetStopLoss(CalculationMode.Ticks, stopLoss);
				SetProfitTarget(CalculationMode.Ticks, profitTarget);

				EnterLong(quantity);

				return;
			}

			stop = High[signalGenerator.md.LegLong.BarsAgoStarts[0]] + 1;

			if (Close[0] >= stop)
			{
				return;
			}

			stopLoss = (stop - Close[0]) * TicksPerPoint;

			SetStopLoss(CalculationMode.Ticks, stopLoss);
			SetProfitTarget(CalculationMode.Ticks, profitTarget);

			EnterShort(quantity);
		}
		#endregion

		#region exitPositions()
        private void exitPositions()
        {
            if (isValidTradeTime() && !shouldExit())
            {
                return;
            }

            if (Position.MarketPosition == MarketPosition.Long)
            {
                ExitLong();
            }

            if (Position.MarketPosition == MarketPosition.Short)
            {
                ExitShort();
            }
        }
        #endregion

		#region shouldExit()
        private bool shouldExit()
        {
			double tickValue = Instrument.MasterInstrument.PointValue * TickSize;
			double ticksPerPoint = Instrument.MasterInstrument.PointValue / tickValue;

            if (Position.MarketPosition == MarketPosition.Flat)
            {
				return false;
			}

			int exitCount = signalGenerator.CurrentExits.Count();

			if (exitCount == 0) {
				return false;
			}

			double profitTargetSignal = signalGenerator.GetProfitTarget();
			double profitTargetDistance = (profitTargetSignal < double.MaxValue && profitTargetSignal > 0)
					? profitTargetSignal * ticksPerPoint : profitTarget;
			SetProfitTarget(CalculationMode.Ticks, profitTargetDistance);

			double stopLossSignal = signalGenerator.GetStopLoss();
			double stopLossDistance = (stopLossSignal < double.MaxValue && stopLossSignal > 0)
					? stopLossSignal * ticksPerPoint : stopLoss;
			SetStopLoss(CalculationMode.Ticks, stopLossDistance);

//			if (signalGenerator.AreExitConditionsMet(direction))
//			{
//				return true;
//			}

			if (signalGenerator.CurrentExits[exitCount - 1].Bar != CurrentBar)
			{
				return false;
			}

			return direction == signalGenerator.CurrentExits[exitCount - 1].Direction;
        }
        #endregion

        #region isValidEntryTime()
        private bool isValidEntryTime()
        {
            int now = ToTime(Time[0]);

            if (now < ToTime(OpenTime))
            {
                return false;
            }

            if (now > ToTime(LastTradeTime))
            {
                return false;
            }

            return true;
        }
        #endregion

        #region isValidTradeTime()
        private bool isValidTradeTime()
        {
            int now = ToTime(Time[0]);

            if (now > ToTime(CloseTime))
            {
                return false;
            }

            if (now < ToTime(OpenTime))
            {
                return false;
            }

            return true;
        }
        #endregion
	}
}
