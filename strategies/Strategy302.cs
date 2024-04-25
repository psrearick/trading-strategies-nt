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
		private DateTime LastDataDay = new DateTime(2023, 03, 17);
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
				signalGenerator = SignalGenerator();
			}
			#endregion
		}
		#endregion

		#region OnBarUpdate()
		protected override void OnBarUpdate()
		{
			signalGenerator.Update();
//			if (signalGenerator.optimalEntryCombinations.Count > 0) {
//				Print(signalGenerator.optimalEntryCombinations.Count);
//			}

//            tradeDirection = Position.MarketPosition;

            if (CurrentBar < BarsRequiredToTrade || CurrentBars[0] < 1)
            {
                return;
            }

            exitPositions();

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

			if (confidence < 0.5)
			{
				return;
			}

//			int quantity = confidence > 0.9 ? 2
//				: confidence > 0.8 ? 2
//				: confidence > 0.7 ? 1
//				: confidence > 0.6 ? 1
//				: 1;

			int quantity = 1;

			direction = entry.Direction;
			if (direction == TrendDirection.Bullish)
			{
				stop = Low[signalGenerator.md.LegLong.BarsAgoStarts[0]] - 1;

				if (Close[0] <= stop)
				{
					return;
				}

				SetStopLoss(CalculationMode.Price, stop);

				EnterLong(quantity);

				return;
			}

			stop = High[signalGenerator.md.LegLong.BarsAgoStarts[0]] + 1;

			if (Close[0] >= stop)
			{
				return;
			}

			SetStopLoss(CalculationMode.Price, stop);

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
            if (Position.MarketPosition == MarketPosition.Flat)
            {
				return false;
			}


			int exitCount = signalGenerator.CurrentExits.Count();

			if (exitCount == 0) {
				return false;
			}

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

            double shift = Time[0] > LastDataDay ? 0.0 : TimeShift;

            if (now < ToTime(OpenTime.AddHours(shift)))
            {
                return false;
            }

            if (now > ToTime(LastTradeTime.AddHours(shift)))
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

            double shift = Time[0] > LastDataDay ? 0.0 : TimeShift;

            if (now > ToTime(CloseTime.AddHours(shift)))
            {
                return false;
            }

            if (now < ToTime(OpenTime.AddHours(shift)))
            {
                return false;
            }

            return true;
        }
        #endregion
	}
}
