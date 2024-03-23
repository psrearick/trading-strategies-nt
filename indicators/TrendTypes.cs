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
		public Series<double> MovementValues;
		public Series<double> BreakoutValues;
		private double MinScalpSize;
		private double MinSwingSize;

		private TrendDirection Swing;
		private int SwingBarCount;
		private int BarsBeforeTradingRange;
		private TrendDirection DirectionBeforeTradingRange;
		private int TradingRangeCount;

		private TrendDirection Trend;
		private int TrendBarCount;
		private int BarsBeforeTrendTradingRange;
		private TrendDirection DirectionBeforeTrendTradingRange;
		private int TrendTradingRangeCount;
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
				MovementValues	= new Series<double>(this);
				BreakoutValues	= new Series<double>(this);
			}
			#endregion
		}
		#endregion

		#region OnBarUpdate()
		protected override void OnBarUpdate()
		{
			if (CurrentBar < 12) {
				LegValues[0] 		= 0;
				SwingValues[0]		= 0;
				TrendValues[0] 		= 0;
				MovementValues[0]	= 0;
				BreakoutValues[0]	= 0;

				SetPlotValues(0, 1);
				return;
			}

			SetSwingSize();

			MovementValues[0] = Movements.ValFromStart(0);

			EvaluateLeg();
			EvaluateBreakout();
			EvaluateSwing();
			EvaluateTrend();
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

			if (LegValues[0] != 0 && length <= 2 && number <= 1 && Movements.BarsAgoStarts[0] >= 5) {
				for (int i = 0; i <= Movements.BarsAgoStarts[0]; i++) {
					BreakoutValues[i] = LegValues[0];
				}
			}

			SetPlotValues(0, Movements.BarsAgoStarts[0]);
		}
		#endregion

		#region EvaluateSwing()
		private void EvaluateSwing()
		{
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

			for (int i = 0; i <= SwingBarCount; i++) {
				SwingValues[i] = Movements.ValFromStart(0);
			}

			SetPlotValues(0, SwingBarCount);
		}
		#endregion

		#region EvaluateTrend()
		private void EvaluateTrend()
		{
			if (LegValues[0] == 0) {
				if (Trend != TrendDirection.Flat) {
					DirectionBeforeTradingRange = Trend;
					BarsBeforeTradingRange = TrendBarCount;
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
				if (Trend == TrendDirection.Flat && DirectionBeforeTrendTradingRange == direction && TrendTradingRangeCount < 20) {
					TrendBarCount = TrendBarCount + BarsBeforeTrendTradingRange + TrendTradingRangeCount;
				}
			}

			Trend 								= direction;
			DirectionBeforeTrendTradingRange	= TrendDirection.Flat;

			TrendTradingRangeCount = 0;
			TrendBarCount++;

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
