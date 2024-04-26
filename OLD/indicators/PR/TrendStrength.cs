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
	public class TrendStrength : Indicator
	{
		#region Variables
		private PriceActionUtils PA;
		private Brush brushUp1;
		private Brush brushUp2;
		private Brush brushUp3;
		private Brush brushUp4;
		private Brush brushDown1;
		private Brush brushDown2;
		private Brush brushDown3;
		private Brush brushDown4;
		public Series<TrendDirection> Direction;
		public Series<int> StrengthOfTrend;
		private int Period = 16;
		#endregion

		#region OnStateChange()
		protected override void OnStateChange()
		{
			#region State.SetDefaults
			if (State == State.SetDefaults)
			{
				Description									= @"";
				Name										= "TrendStrength";
				Calculate									= Calculate.OnBarClose;
				IsOverlay									= true;
				DisplayInDataBox							= true;
				DrawOnPricePanel							= true;
				DrawHorizontalGridLines						= true;
				DrawVerticalGridLines						= true;
				PaintPriceMarkers							= true;
				ScaleJustification							= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				IsSuspendedWhileInactive					= true;

				brushUp1 = Brushes.Green.Clone();
				brushUp1.Opacity = 0.500;
				brushUp1.Freeze();

				brushUp2 = Brushes.Green.Clone();
				brushUp2.Opacity = 0.400;
				brushUp2.Freeze();

				brushUp3 = Brushes.Green.Clone();
				brushUp3.Opacity = 0.300;
				brushUp3.Freeze();

				brushUp4 = Brushes.Green.Clone();
				brushUp4.Opacity = 0.200;
				brushUp4.Freeze();

				brushDown1 = Brushes.Red.Clone();
				brushDown1.Opacity = 0.500;
				brushDown1.Freeze();

				brushDown2 = Brushes.Red.Clone();
				brushDown2.Opacity = 0.400;
				brushDown2.Freeze();

				brushDown3 = Brushes.Red.Clone();
				brushDown3.Opacity = 0.300;
				brushDown3.Freeze();

				brushDown4 = Brushes.Red.Clone();
				brushDown4.Opacity = 0.200;
				brushDown4.Freeze();
			}
			#endregion

			#region State.Configure
			else if (State == State.Configure)
			{
				PA = PriceActionUtils();
			}
			#endregion

			#region State.DataLoaded
			else if (State == State.DataLoaded)
			{
				Direction 		= new Series<TrendDirection>(this, MaximumBarsLookBack.Infinite);
				StrengthOfTrend	= new Series<int>(this, MaximumBarsLookBack.Infinite);
			}
			#endregion
		}
		#endregion

		#region OnBarUpdate()
		protected override void OnBarUpdate()
		{
			if (CurrentBar < Period) {
				Direction[0] 	= TrendDirection.Flat;
				StrengthOfTrend[0]	= 0;
				return;
			}

			TrendDirection strongTrendDirection 	= PA.GetStrongTrendDirection(0, Period);
			TrendDirection weakTrendDirection 		= PA.GetWeakTrendDirection(0, Period);



			if (strongTrendDirection != TrendDirection.Flat) {
				Direction[0] = strongTrendDirection;
				StrengthOfTrend[0] = 1;
			} else if (weakTrendDirection != TrendDirection.Flat) {
				Direction[0] = weakTrendDirection;
				StrengthOfTrend[0] = 2;
			} else if (PA.IsEMABullish(0) && PA.GetTrendDirection(0, Period) == TrendDirection.Bullish) {
				Direction[0] = TrendDirection.Bullish;
				StrengthOfTrend[0] = 3;
			} else if (PA.IsEMABearish(0) && PA.GetTrendDirection(0, Period) == TrendDirection.Bearish) {
				Direction[0] = TrendDirection.Bearish;
				StrengthOfTrend[0] = 3;
			} else if (PA.GetTrendDirection(0, Period) != TrendDirection.Flat) {
				Direction[0] = PA.GetTrendDirection(0, Period);
				StrengthOfTrend[0] = 4;
			} else {
				Direction[0] = TrendDirection.Flat;
			}

			PaintBackground(0);
		}
		#endregion

		#region PaintBackground()
		private void PaintBackground(int barsAgo = 0)
		{
			BackBrush = null;

			if (Direction[barsAgo] == TrendDirection.Bearish) {
				BackBrushes[barsAgo] = StrengthOfTrend[barsAgo] == 1 ? brushDown1
									: StrengthOfTrend[barsAgo] == 2 ? brushDown2
									: StrengthOfTrend[barsAgo] == 3 ? brushDown3
									: StrengthOfTrend[barsAgo] == 4 ? brushDown4
									: null;
			}

			if (Direction[barsAgo] == TrendDirection.Bullish) {
				BackBrushes[barsAgo] = StrengthOfTrend[barsAgo] == 1 ? brushUp1
									: StrengthOfTrend[barsAgo] == 2 ? brushUp2
									: StrengthOfTrend[barsAgo] == 3 ? brushUp3
									: StrengthOfTrend[barsAgo] == 4 ? brushUp4
									: null;
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
		private PR.TrendStrength[] cacheTrendStrength;
		public PR.TrendStrength TrendStrength()
		{
			return TrendStrength(Input);
		}

		public PR.TrendStrength TrendStrength(ISeries<double> input)
		{
			if (cacheTrendStrength != null)
				for (int idx = 0; idx < cacheTrendStrength.Length; idx++)
					if (cacheTrendStrength[idx] != null &&  cacheTrendStrength[idx].EqualsInput(input))
						return cacheTrendStrength[idx];
			return CacheIndicator<PR.TrendStrength>(new PR.TrendStrength(), input, ref cacheTrendStrength);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.PR.TrendStrength TrendStrength()
		{
			return indicator.TrendStrength(Input);
		}

		public Indicators.PR.TrendStrength TrendStrength(ISeries<double> input )
		{
			return indicator.TrendStrength(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.PR.TrendStrength TrendStrength()
		{
			return indicator.TrendStrength(Input);
		}

		public Indicators.PR.TrendStrength TrendStrength(ISeries<double> input )
		{
			return indicator.TrendStrength(input);
		}
	}
}

#endregion
