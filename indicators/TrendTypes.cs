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
using NinjaTrader.NinjaScript.DrawingTools;
#endregion

//This namespace holds Indicators in this folder and is required. Do not change it.
namespace NinjaTrader.NinjaScript.Indicators.PR
{
	public class TrendTypes : Indicator
	{
		#region Variables
		private Legs Movements;
		private ATR BarRange;
		private PriceActionUtils PA;
		public Series<double> LegValues;
		public Series<double> SwingValues;
		public Series<double> TrendValues;
		public Series<double> TrendStarts;
		public Series<double> MovementValues;
		public Series<double> BreakoutValues;

		private Series<double> SwingEvaluations;
		private Series<double> TrendEvaluations;
		private Series<bool> SwingAccuracy;
		private Series<bool> TrendAccuracy;
		private int CorrectSwingValues;
		private int CheckedSwingValues;
		private int CorrectTrendValues;
		private int CheckedTrendValues;
		private int lastSwingChecked;
		private int lastTrendChecked;

		public int LegStarted;
		public int SwingStarted;
		public int TrendStarted;
		public int BreakoutStarted;
		private double MinScalpSize;
		private double MinSwingSize;

		private TrendDirection Swing;
		private int SwingBarCount;
		private int BarsBeforeTradingRange;
		private TrendDirection DirectionBeforeTradingRange;
		public int TradingRangeCount;

		private TrendDirection Trend;
		private int TrendBarCount;
		private int BarsBeforeTrendTradingRange;
		private TrendDirection DirectionBeforeTrendTradingRange;
		public int TrendTradingRangeCount;
		#endregion

		#region OnStateChange()
		protected override void OnStateChange()
		{
			#region State.SetDefaults
			if (State == State.SetDefaults)
			{
				Description									= @"Plots Legs, Swings, and Trends";
				Name										= "Trend Types";
				Calculate									= Calculate.OnBarClose;
				IsOverlay									= false;
				DisplayInDataBox							= true;
				DrawOnPricePanel							= true;
				PaintPriceMarkers							= false;
				ScaleJustification							= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				IsSuspendedWhileInactive					= true;

				AddPlot(new Stroke(Brushes.Blue, 2), PlotStyle.Line, "Legs");
				AddPlot(new Stroke(Brushes.Green, 3), PlotStyle.Line, "Swings");
				AddPlot(new Stroke(Brushes.Orange, 4), PlotStyle.Line, "Trends");
				AddPlot(new Stroke(Brushes.Red, 2), PlotStyle.Line, "Breakouts");
			}
			#endregion

			#region State.Configure
			else if (State == State.Configure)
			{
				BarRange	= ATR(10);
				Movements	= Legs();
				PA			= PriceActionUtils();
			}
			#endregion

			#region State.DataLoaded
			else if (State == State.DataLoaded)
			{
				LegValues 		= new Series<double>(this);
				SwingValues 	= new Series<double>(this);
				TrendValues		= new Series<double>(this);
				TrendStarts		= new Series<double>(this);
				MovementValues	= new Series<double>(this);
				BreakoutValues	= new Series<double>(this);

				SwingEvaluations	= new Series<double>(this, MaximumBarsLookBack.Infinite);
				TrendEvaluations	= new Series<double>(this, MaximumBarsLookBack.Infinite);
				SwingAccuracy		= new Series<bool>(this, MaximumBarsLookBack.Infinite);
				TrendAccuracy		= new Series<bool>(this, MaximumBarsLookBack.Infinite);
			}
			#endregion
		}
		#endregion

		#region OnBarUpdate()
		protected override void OnBarUpdate()
		{
			Evaluate();
		}
		#endregion

		#region Evaluate()
		public void Evaluate()
		{
			if (CurrentBar < 25) {
				LegValues[0] 		= 0;
				SwingValues[0]		= 0;
				TrendValues[0] 		= 0;
				TrendStarts[0]		= 0;
				MovementValues[0]	= 0;
				BreakoutValues[0]	= 0;
				SwingEvaluations[0]	= 0;
				TrendEvaluations[0]	= 0;

				SetPlotValues(0, 1);
				return;
			}

			SetSwingSize();

			MovementValues[0] = Movements.ValFromStart(0);

			EvaluateLeg();
			EvaluateBreakout();
			EvaluateSwing();
			EvaluateTrend();

			SwingEvaluations[0] = SwingValues[0];
			TrendEvaluations[0] = TrendValues[0];

			CheckSwingAccuracy();
			CheckTrendAccuracy();
		}
		#endregion

		#region EvaluateLeg()
		private void EvaluateLeg()
		{
			TrendDirection direction = Movements.ValFromStart(0) > 0
				? TrendDirection.Bullish
				: Movements.ValFromStart(0) < 0
					? TrendDirection.Bearish
					: TrendDirection.Flat;

			double pullback = PA.largestPullbackInTrend(0, (int) Movements.BarsAgoStarts[0], direction);

			bool deepPullback = pullback > 0.5;

			LegStarted = deepPullback ? 0 : (int) Movements.BarsAgoStarts[0];

			for (int i = 0; i <= Movements.BarsAgoStarts[0]; i++) {
				LegValues[i] = deepPullback ? 0 : Movements.ValFromStart(0);
			}

			SetPlotValues(0, Movements.BarsAgoStarts[0]);
		}
		#endregion

		#region EvaluateBreakout()
		private void EvaluateBreakout()
		{
			TrendDirection direction = LegValues[0] > 0
				? TrendDirection.Bullish
				: LegValues[0] < 0
					? TrendDirection.Bearish
					: TrendDirection.Flat;

			BreakoutValues[0] = 0;

			double length = PA.AveragePullbackLength(0, (int) Movements.BarsAgoStarts[0], direction);
			double number = PA.NumberOfPullbacksInTrend(0, (int) Movements.BarsAgoStarts[0], direction);

			if (LegValues[0] != 0 && length <= 1 && number <= 1 && Movements.BarsAgoStarts[0] >= 5) {
				BreakoutStarted = (int) Movements.BarsAgoStarts[0];
				for (int i = 0; i <= BreakoutStarted; i++) {
					BreakoutValues[i] = LegValues[0];
				}
			}

			SetPlotValues(0, Movements.BarsAgoStarts[0]);
		}
		#endregion

		#region EvaluateSwing()
		private void EvaluateSwing()
		{
			SwingStarted = 0;
			if (LegValues[0] == 0) {
				if (Swing != TrendDirection.Flat) {
					DirectionBeforeTradingRange = Swing;
					BarsBeforeTradingRange = SwingBarCount;
					InitializeSwing();
				}

				TradingRangeCount++;

				for (int i = 0; i < TradingRangeCount; i++) {
					SwingValues[i] = 0;
				}

				return;
			}

			if (LegValues[0] != SwingValues[1]) {
				SwingBarCount = 0;
			}

			TrendDirection direction = LegValues[0] > 0 ? TrendDirection.Bullish : LegValues[0] < 0 ? TrendDirection.Bearish : TrendDirection.Flat;

			if (SwingBarCount == 0) {
				SwingBarCount = (int) Movements.BarsAgoStarts[0];
				if (Swing == TrendDirection.Flat && DirectionBeforeTradingRange == direction && TradingRangeCount < 20) {
					SwingBarCount = SwingBarCount + BarsBeforeTradingRange + TradingRangeCount;
				}
			}

			Swing 						= direction;
			DirectionBeforeTradingRange	= TrendDirection.Flat;

			TradingRangeCount = 0;
			SwingBarCount++;

			if (SwingBarCount > 25) {
				SwingBarCount = (int) Movements.BarsAgoStarts[0];
			}

			double high = MAX(High, SwingBarCount)[0];
			double low = MIN(Low, SwingBarCount)[0];
			double range = high - low;

			double pullback = PA.largestPullbackInTrend(0, (int) SwingBarCount, direction);

			bool deepPullback = pullback >= 1;

			if (range < MinSwingSize || deepPullback) {
				for (int i = 0; i < SwingBarCount; i++) {
					SwingValues[i] = 0;
				}

				return;
			}

			SwingStarted = SwingBarCount;
			for (int i = 0; i <= SwingBarCount; i++) {
				SwingValues[i] = Movements.ValFromStart(0);
			}

			SetPlotValues(0, SwingBarCount);
		}
		#endregion

		#region EvaluateTrend()
		private void EvaluateTrend()
		{
			TrendStarted = 0;
			if (LegValues[0] == 0) {
				if (Trend != TrendDirection.Flat) {
					DirectionBeforeTrendTradingRange = Trend;
					BarsBeforeTrendTradingRange = TrendBarCount;
					InitializeTrend();
				}

				TrendTradingRangeCount++;

				for (int i = 0; i < TrendTradingRangeCount; i++) {
					TrendValues[i] = 0;
				}

				return;
			}

			if (LegValues[0] != TrendValues[1]) {
				TrendBarCount = 0;
			}

			TrendDirection direction = LegValues[0] > 0 ? TrendDirection.Bullish : LegValues[0] < 0 ? TrendDirection.Bearish : TrendDirection.Flat;

			if (TrendBarCount == 0) {
				TrendBarCount = (int) Movements.BarsAgoStarts[0];
				int trCount = BarsBeforeTrendTradingRange + TrendTradingRangeCount;
				if (Trend == TrendDirection.Flat
					&& DirectionBeforeTrendTradingRange == direction
					&& TrendTradingRangeCount < 20
					&& trCount > 0) {
					TrendBarCount = trCount;
				}
			} else {
				TrendBarCount++;
			}

			Trend 								= direction;
			DirectionBeforeTrendTradingRange	= TrendDirection.Flat;
			TrendTradingRangeCount 				= 0;

			double high = MAX(High, TrendBarCount)[0];
			double low = MIN(Low, TrendBarCount)[0];
			double range = high - low;

			if (range < MinSwingSize) {
				for (int i = 0; i < TrendBarCount; i++) {
					TrendValues[i] = 0;
				}

				return;
			}

			if (TrendBarCount < 20) {
				for (int i = 0; i < TrendBarCount; i++) {
					TrendValues[i] = TrendValues[TrendBarCount + 1];
				}

				return;
			}

			TrendStarted = TrendBarCount;
			for (int i = 0; i <= TrendBarCount; i++) {
				TrendValues[i] = Movements.ValFromStart(0);
			}

			SetPlotValues(0, TrendBarCount);
		}
		#endregion

		#region InitializeSwing()
		private void InitializeSwing()
		{
			Swing				= TrendDirection.Flat;
			SwingBarCount		= 0;
		}
		#endregion

		#region InitializeTrend()
		private void InitializeTrend()
		{
			Trend				= TrendDirection.Flat;
			TrendBarCount		= 0;
		}
		#endregion

		#region CheckSwingAccuracy()
		private void CheckSwingAccuracy()
		{
			int lastAvailableValue = CurrentBar - SwingStarted - 1;

			if (lastAvailableValue == lastSwingChecked) {
				return;
			}

			if (lastAvailableValue > lastSwingChecked) {
				for (int i = lastSwingChecked; i < lastAvailableValue - 1; i++) {
					int index = i;

					SwingAccuracy[index] = (SwingValues[index] == SwingEvaluations[index]);
				}
			}

			if (lastSwingChecked > lastAvailableValue) {
				for (int i = lastAvailableValue; i < lastSwingChecked; i++) {
					int index = i;

					SwingAccuracy[index] = false;
				}
			}
		}
		#endregion

		#region CheckTrendAccuracy()
		private void CheckTrendAccuracy()
		{
			int lastAvailableValue = CurrentBar - TrendStarted - 1;

			if (lastAvailableValue == lastTrendChecked) {
				return;
			}

			if (lastAvailableValue > lastTrendChecked) {
				for (int i = lastTrendChecked; i < lastAvailableValue - 1; i++) {
					int index = i;

					TrendAccuracy[index] = (TrendValues[index] == TrendEvaluations[index]);
				}
			}

			if (lastTrendChecked > lastAvailableValue) {
				for (int i = lastAvailableValue; i < lastTrendChecked; i++) {
					int index = i;

					TrendAccuracy[index] = false;
				}
			}
		}
		#endregion

		#region ComputeAccuracy()
		public double ComputeAccuracy(int period)
		{
			int length = Math.Min(period, SwingAccuracy.Count);

			int swingCorrect = 0;
			int trendCorrect = 0;

			for (int i = 0; i < length; i++) {
				int swingIndex = CurrentBar - lastSwingChecked;
				swingCorrect += (SwingAccuracy[swingIndex] ? 1 : 0);

				int trendIndex = CurrentBar - lastTrendChecked;
				trendCorrect += (TrendAccuracy[trendIndex] ? 1 : 0);
			}

			double swingPercent = swingCorrect / length;
			double trendPercent = trendCorrect / length;

			return (swingPercent + trendPercent) / 2;
		}
		#endregion

		#region SetSwingSize()
		private void SetSwingSize()
		{
			MinScalpSize = 0.7 * BarRange[0];
			MinSwingSize = MinScalpSize * 3.75;
		}
		#endregion

		#region SetPlotValues()
		private void SetPlotValues(int barsAgo = 0, int period = 1)
		{
			for (int i = barsAgo; i < period; i++) {
				Values[0][i] 	= LegValues[i] * 2;
				Values[1][i] 	= SwingValues[i] * 3;
				Values[2][i] 	= TrendValues[i] * 4;
				Values[3][i] 	= BreakoutValues[i];
			}
		}

		private void SetPlotValues(int barsAgo = 0, double period = 1)
		{
			SetPlotValues(barsAgo, (int) period);
		}
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private PR.TrendTypes[] cacheTrendTypes;
		public PR.TrendTypes TrendTypes()
		{
			return TrendTypes(Input);
		}

		public PR.TrendTypes TrendTypes(ISeries<double> input)
		{
			if (cacheTrendTypes != null)
				for (int idx = 0; idx < cacheTrendTypes.Length; idx++)
					if (cacheTrendTypes[idx] != null &&  cacheTrendTypes[idx].EqualsInput(input))
						return cacheTrendTypes[idx];
			return CacheIndicator<PR.TrendTypes>(new PR.TrendTypes(), input, ref cacheTrendTypes);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.PR.TrendTypes TrendTypes()
		{
			return indicator.TrendTypes(Input);
		}

		public Indicators.PR.TrendTypes TrendTypes(ISeries<double> input )
		{
			return indicator.TrendTypes(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.PR.TrendTypes TrendTypes()
		{
			return indicator.TrendTypes(Input);
		}

		public Indicators.PR.TrendTypes TrendTypes(ISeries<double> input )
		{
			return indicator.TrendTypes(input);
		}
	}
}

#endregion
