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
				signalGenerator = SignalGenerator();
			}
			#endregion

			#region State.DataLoaded
			else if (State == State.DataLoaded)
			{
				SetParameterTypes();
				SetConditions();
			}
			#endregion
		}
		#endregion

		#region SetParameterTypes()
		private void SetParameterTypes()
		{
			signalGenerator.SetParameterType(typeof(StopLossCondition), "StopLossMultiplier", 5, 1, 1);
			signalGenerator.SetParameterType(typeof(ProfitTargetCondition), "ProfitTargetMultiplier", 8, 1, 1);
			signalGenerator.SetParameterType(typeof(ProfitTargetCondition), "HighATRMultiplier", 5, 0, 1);
			signalGenerator.SetParameterType(typeof(SLTPCondition), "StopLossMultiplier", 5, 1, 1);
			signalGenerator.SetParameterType(typeof(SLTPCondition), "ProfitTargetMultiplier", 8, 1, 1);
			signalGenerator.SetParameterType(typeof(SLTPCondition), "HighATRMultiplier", 5, 0, 1);
			signalGenerator.SetParameterType(typeof(NoNewExtremeCondition), "StopLossMultiplier", 20, 6, 2);
		}
		#endregion

		#region SetConditions()
		private void SetConditions()
		{
			#region Entry Conditions

//			entryConditions.Add(new MARisingFallingCondition());
			signalGenerator.AddEntryCondition(new TrendRisingFallingCondition());
//			entryConditions.Add(new NewHighLowCondition());
			signalGenerator.AddEntryCondition(new ValidChoppinessCondition());
			signalGenerator.AddEntryCondition(new UpDownTrendCondition());

//			entryConditions.Add(new EMAConvergingCondition());
//			entryConditions.Add(new WithTrendEMACondition());
//			entryConditions.Add(new BelowAverageATRCondition());
//			entryConditions.Add(new AboveAverageATRByAStdDevCondition());
//			entryConditions.Add(new BreakoutCondition());
			signalGenerator.AddEntryCondition(new BroadChannelCondition());
//			entryConditions.Add(new WeakBarCondition());
//			entryConditions.Add(new RSIRangeCondition());
//			entryConditions.Add(new AboveAverageATRCondition());
//			entryConditions.Add(new WithTrendTrendBarCondition());
//			entryConditions.Add(new WithTrendPressureCondition());
//			entryConditions.Add(new EMADivergingCondition());
//			entryConditions.Add(new FastEMADirectionCondition());
//			entryConditions.Add(new SlowEMADirectionCondition());

//			entryConditions.Add(new TrendFollowingStrategy206Condition()); // Produces very few trades -- REMOVE
//			entryConditions.Add(new StrongWithTrendPressureCondition()); // Produces few trades -- REMOVE
//			entryConditions.Add(new TightChannelCondition()); // Produces very few trades -- REMOVE
//			entryConditions.Add(new WeakTrendCondition()); // Produces very few trades -- REMOVE
//			entryConditions.Add(new StrongTrendCondition()); // Produces very few trades -- REMOVE
//			entryConditions.Add(new BreakoutBarPatternCondition()); // Produces very few trades -- REMOVE
//			entryConditions.Add(new StrongFollowThroughCondition()); // Produces very few trades -- REMOVE
//			entryConditions.Add(new LeadsFastEMAByMoreThanATRCondition()); // Produces very few trades -- REMOVE

			#endregion

			#region Exit Conditions



//			singleExitConditions.Add(new TrendDirectionChangedCondition());
//			// singleExitConditions.Add(new CounterTrendTightChannelCondition()); // Rarely Triggers
//			// singleExitConditions.Add(new CounterTrendBroadChannelCondition()); // Rarely Triggers
//			// singleExitConditions.Add(new CounterTrendBreakoutsCondition()); // Rarely Triggers
//			// singleExitConditions.Add(new CounterTrendBreakoutTrendCondition()); // Rarely Triggers
//			singleExitConditions.Add(new CounterTrendLegLongCondition());
//			singleExitConditions.Add(new CounterTrendLegShortCondition());
//			// singleExitConditions.Add(new DoubleTopBottomCondition()); // Rarely Triggers
//			// singleExitConditions.Add(new CounterTrendLegAfterDoubleTopBottomCondition()); // Rarely Triggers
//			singleExitConditions.Add(new TrailingStopBeyondPreviousExtremeCondition());
//			singleExitConditions.Add(new MovingAverageCrossoverCondition());
//			singleExitConditions.Add(new NoNewExtremeCondition());
			// singleExitConditions.Add(new CounterTrendPressureCondition()); // Rarely Triggers
//			// singleExitConditions.Add(new CounterTrendWeakTrendCondition()); // Rarely Triggers
//			// singleExitConditions.Add(new CounterTrendStrongTrendCondition()); // Rarely Triggers
//			// singleExitConditions.Add(new RSIOutOfRangeCondition()); // Rarely Triggers
//			singleExitConditions.Add(new AboveAverageATRExitCondition());
//			singleExitConditions.Add(new BelowAverageATRExitCondition());
//			singleExitConditions.Add(new AboveAverageATRByAStdDevExitCondition());
//			singleExitConditions.Add(new BelowAverageATRByAStdDevExitCondition());
//			singleExitConditions.Add(new StrongCounterTrendFollowThroughCondition());


//			singleExitConditions.Add(new ProfitTargetCondition());
//			singleExitConditions.Add(new StopLossCondition());
			signalGenerator.AddSingleExitCondition(new SLTPCondition());
			#endregion

			signalGenerator.SetConditions();
		}
		#endregion

		#region OnBarUpdate()
		protected override void OnBarUpdate()
		{
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
