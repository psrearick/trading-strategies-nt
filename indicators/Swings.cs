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
	public class Swings : Indicator
	{
		#region Variables
		private Legs legIdentifier;
		public Series<double> SwingStart;
		private TrendDirection swingDirection = TrendDirection.Flat;
		private TrendDirection legDirection = TrendDirection.Flat;
		#endregion

		#region OnStateChange()
		protected override void OnStateChange()
		{
			#region State.SetDefaults
			if (State == State.SetDefaults)
			{
				Description									= @"Enter the description for your new custom Indicator here.";
				Name										= "Swings";
				Calculate									= Calculate.OnBarClose;
				IsOverlay									= false;
				DisplayInDataBox							= true;
				DrawOnPricePanel							= true;
				DrawHorizontalGridLines						= true;
				DrawVerticalGridLines						= true;
				PaintPriceMarkers							= true;
				ScaleJustification							= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				IsSuspendedWhileInactive					= true;

				AddPlot(Brushes.DarkCyan, "Swing Direction");
			}
			#endregion
			#region  State.Configure
			else if (State == State.Configure)
			{
				legIdentifier = Legs();
			}
			#endregion

			#region State.DataLoaded
			else if (State == State.DataLoaded)
			{
				SwingStart 	= new Series<double>(this);
			}
			#endregion
		}
		#endregion

		protected override void OnBarUpdate()
		{
			if (CurrentBar < 25) {
				Value[0] = 0;
				return;
			}

			int lookback = Math.Min(CurrentBar, 81);
			int trendMin = (int) Math.Floor((double) lookback / 3);

			int legBarsUp = 0;
			int legBarsDown = 0;

			for (int i = 0; i < 25; i++) {
				if (legIdentifier[i] == 1) {
					legBarsUp++;
				}

				if (legIdentifier[i] == -1) {
					legBarsDown++;
				}
			}

			TrendDirection previousDirection = swingDirection;
			swingDirection = legBarsUp > legBarsDown ? TrendDirection.Bullish : legBarsUp < legBarsDown ? TrendDirection.Bearish : TrendDirection.Flat;

			SwingStart[0] = SwingStart[1];
			if (swingDirection != previousDirection) {
				if (swingDirection == TrendDirection.Flat) {
					SwingStart[0] = (previousDirection == TrendDirection.Bullish) ? Low[0] : High[0];
				} else {
					SwingStart[0] = (swingDirection == TrendDirection.Bullish) ? Low[0] : High[0];
				}
			}

			legDirection = legIdentifier[0] == 1 ? TrendDirection.Bullish : legIdentifier[0] == -1 ? TrendDirection.Bearish : TrendDirection.Flat;

			Value[0] = 0;

			if (swingDirection == TrendDirection.Bullish) {
				Value[0] = 2;

				if (legDirection == TrendDirection.Bullish) {
					Value[0] = 3;
				}

				if (legDirection == TrendDirection.Bearish) {
					Value[0] = 1;
				}
			}

			if (swingDirection == TrendDirection.Bearish) {
				Value[0] = -2;

				if (legDirection == TrendDirection.Bearish) {
					Value[0] = -3;
				}

				if (legDirection == TrendDirection.Bullish) {
					Value[0] = -1;
				}
			}
		}
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private PR.Swings[] cacheSwings;
		public PR.Swings Swings()
		{
			return Swings(Input);
		}

		public PR.Swings Swings(ISeries<double> input)
		{
			if (cacheSwings != null)
				for (int idx = 0; idx < cacheSwings.Length; idx++)
					if (cacheSwings[idx] != null &&  cacheSwings[idx].EqualsInput(input))
						return cacheSwings[idx];
			return CacheIndicator<PR.Swings>(new PR.Swings(), input, ref cacheSwings);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.PR.Swings Swings()
		{
			return indicator.Swings(Input);
		}

		public Indicators.PR.Swings Swings(ISeries<double> input )
		{
			return indicator.Swings(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.PR.Swings Swings()
		{
			return indicator.Swings(Input);
		}

		public Indicators.PR.Swings Swings(ISeries<double> input )
		{
			return indicator.Swings(input);
		}
	}
}

#endregion
