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
	public class MarketDirection : Indicator
	{
		#region Variables
//		private HighsAndLowsCounter HL;
//		private HighsAndLowsCounter HLSlow;
		private int BarsSinceTrendBegan;
		private Brush brushUp1;
		private Brush brushDown1;
		private Series<TrendDirection> Direction;

		private double TrendHigh;
		private int NewHighAttempts;
		private int NewHighs;
		private int BarsSinceLastHigh;

		private double TrendLow;
		private int NewLowAttempts;
		private int NewLows;
		private int BarsSinceLastLow;
		#endregion

		#region OnStateChange()
		protected override void OnStateChange()
		{
			#region State.SetDefaults
			if (State == State.SetDefaults)
			{
				Description									= @"";
				Name										= "Market Direction";
				Calculate									= Calculate.OnBarClose;
				IsOverlay									= true;
				DisplayInDataBox							= true;
				DrawOnPricePanel							= true;
				DrawHorizontalGridLines						= true;
				DrawVerticalGridLines						= true;
				PaintPriceMarkers							= true;
				ScaleJustification							= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				//Disable this property if your indicator requires custom values that cumulate with each new market data event.
				//See Help Guide for additional information.
				IsSuspendedWhileInactive					= true;

				brushUp1 = Brushes.Green.Clone();
				brushUp1.Opacity = 0.210;
				brushUp1.Freeze();

				brushDown1 = Brushes.Red.Clone();
				brushDown1.Opacity = 0.210;
				brushDown1.Freeze();

				AddPlot(new Stroke(Brushes.DarkGreen, 2), PlotStyle.Line, "Swing High");
				AddPlot(new Stroke(Brushes.DarkRed, 2), PlotStyle.Line, "Swing Low");
			}
			#endregion

			#region State.Configure
			else if (State == State.Configure)
			{
				HL = HighsAndLowsCounter(-1);
				HLSlow	= HighsAndLowsCounter();
			}
			#endregion

			#region State.DataLoaded
			else if (State == State.DataLoaded)
			{
				Direction = new Series<TrendDirection>(this, MaximumBarsLookBack.Infinite);
			}
			#endregion
		}
		#endregion

		#region OnBarUpdate()
		protected override void OnBarUpdate()
		{
			if (CurrentBar < 2) {
				Direction[0] = TrendDirection.Flat;
				return;
			}

			if (High[0] > High[1]) {
				if (High[0] > TrendHigh) {
					TrendHigh = High[0];
					NewHighs++;
					BarsSinceLastHigh = 0;
				} else {
					NewHighAttempts++;
					BarsSinceLastHigh++;
				}
			}

			if (High[0] < High[1] && NewHighAttempts == 4) {
				Direction[0] = TrendDirection.Flat;
				NewHighs = 0;
				NewHighAttempts = 0;
			}

			if (Direction[0] == TrendDirection.Flat && NewHighs > 1 && NewHighs > NewLows) {
				Direction[0] = TrendDirection.Bullish;
			}

			if (Low[0] < Low[1]) {
				if (Low[0] < TrendLow) {
					TrendLow = Low[0];
					NewLows++;
					BarsSinceLastLow = 0;
				} else {
					NewLowAttempt++;
					BarsSinceLastLow = 0;
				}
			}

			if (Low[0] > Low[1] && NewLowAttempts == 4) {
				Direction[0] = TrendDirection.Flat;
				NewLows = 0;
				NewLowAttempts = 0;
			}

			if (Direction[0] == TrendDirection.Flat) {
				TrendHigh = High[0];
				TrendLow = Low[0];
			}




//			Direction[0] = Direction[1];

//			Values[0][0] = HL.SwingHighs[0];
//			Values[1][0] = HL.SwingLows[0];

//			if (Values[0][0] == Values[0][1]) {
//				BarsSinceLastHigh++;
//			} else {
//				BarsSinceLastHigh = 0;
//			}

//			if (Values[1][0] == Values[1][1]) {
//				BarsSinceLastLow++;
//			} else {
//				BarsSinceLastLow = 0;
//			}



//			if (BarsSinceLastHigh > BarsSinceLastLow) {
//				Direction[0] = TrendDirection.Bearish;
//			}

//			if (BarsSinceLastHigh < BarsSinceLastLow) {
//				Direction[0] = TrendDirection.Bullish;
//			}

//			if (Direction[0] == Direction[1]) {
//				BarsSinceTrendBegan++;
//			} else {
//				BarsSinceTrendBegan = 0;
//			}


//			if (Direction[0] != Direction[1]) {
//				BarsSinceTrendBegan = 0;

//				if (Direction[0] == TrendDirection.Bullish && Direction[1] == TrendDirection.Bearish) {
//					BarsSinceTrendBegan = BarsSinceLastLow;
//					Draw.Line(this, "BullishTrend"+CurrentBar.ToString(), BarsSinceTrendBegan, High[BarsSinceTrendBegan], 0, High[0], Brushes.DarkGreen);
//				}

//				if (Direction[0] == TrendDirection.Bearish && Direction[1] == TrendDirection.Bullish) {
//					BarsSinceTrendBegan = BarsSinceLastHigh;
//					Draw.Line(this, "BearishTrend"+CurrentBar.ToString(), BarsSinceTrendBegan, Low[BarsSinceTrendBegan], 0, Low[0], Brushes.DarkRed);
//				}

////				Print(Time[0]);
////				Print(Direction[0]);
////				Print(BarsSinceTrendBegan);
////				Print(BarsSinceLastLow);
////				Print(BarsSinceLastHigh);
////				Print("==========");

////				if (BarsSinceTrendBegan > 0) {
////					HL.ResetToBar(BarsSinceTrendBegan);
////				}
//			}

//			if (BarsSinceTrendBegan >= 20) {
//				Direction[0] = TrendDirection.Flat;
//			}


			PaintBackground();
		}
		#endregion

		#region PaintBackground()
		private void PaintBackground()
		{
			BackBrush = null;

			if (Direction[0] == TrendDirection.Bearish) {
				BackBrush = brushDown1;
			}

			if (Direction[0] == TrendDirection.Bullish) {
				BackBrush = brushUp1;
			}
		}
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private PR.MarketDirection[] cacheMarketDirection;
		public PR.MarketDirection MarketDirection()
		{
			return MarketDirection(Input);
		}

		public PR.MarketDirection MarketDirection(ISeries<double> input)
		{
			if (cacheMarketDirection != null)
				for (int idx = 0; idx < cacheMarketDirection.Length; idx++)
					if (cacheMarketDirection[idx] != null &&  cacheMarketDirection[idx].EqualsInput(input))
						return cacheMarketDirection[idx];
			return CacheIndicator<PR.MarketDirection>(new PR.MarketDirection(), input, ref cacheMarketDirection);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.PR.MarketDirection MarketDirection()
		{
			return indicator.MarketDirection(Input);
		}

		public Indicators.PR.MarketDirection MarketDirection(ISeries<double> input )
		{
			return indicator.MarketDirection(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.PR.MarketDirection MarketDirection()
		{
			return indicator.MarketDirection(Input);
		}

		public Indicators.PR.MarketDirection MarketDirection(ISeries<double> input )
		{
			return indicator.MarketDirection(input);
		}
	}
}

#endregion
