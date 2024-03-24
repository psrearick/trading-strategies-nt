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
	public class HighLowPlot : Indicator
	{
		#region Parameters
		private HighsAndLowsCounter HLCount;
		#endregion

		#region OnStageChange()
		protected override void OnStateChange()
		{
			#region State.SetDefaults
			if (State == State.SetDefaults)
			{
				Description									= @"";
				Name										= "High Low Plot";
				Calculate									= Calculate.OnBarClose;
				IsOverlay									= false;
				DisplayInDataBox							= true;
				DrawOnPricePanel							= true;
				DrawHorizontalGridLines						= true;
				DrawVerticalGridLines						= true;
				PaintPriceMarkers							= true;
				ScaleJustification							= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				IsSuspendedWhileInactive					= true;

				AddPlot(new Stroke(Brushes.Green, 3), PlotStyle.Line, "High");
				AddPlot(new Stroke(Brushes.Red, 3), PlotStyle.Line, "Low");
				AddPlot(new Stroke(Brushes.DarkGreen, 3), PlotStyle.Line, "Swing High");
				AddPlot(new Stroke(Brushes.DarkRed, 3), PlotStyle.Line, "Swing Low");
			}
			#endregion

			#region State.Configure
			else if (State == State.Configure)
			{
				HLCount = HighsAndLowsCounter();
			}
			#endregion

			#region State.DataLoaded
			else if (State == State.DataLoaded)
			{
			}
			#endregion
		}
		#endregion

		#region OnBarUpdate()
		protected override void OnBarUpdate()
		{
			Values[0][0] = HLCount.Highs[0];
			Values[1][0] = HLCount.Lows[0];
			Values[2][0] = HLCount.SwingHighs[0];
			Values[3][0] = HLCount.SwingLows[0];
		}
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private PR.HighLowPlot[] cacheHighLowPlot;
		public PR.HighLowPlot HighLowPlot()
		{
			return HighLowPlot(Input);
		}

		public PR.HighLowPlot HighLowPlot(ISeries<double> input)
		{
			if (cacheHighLowPlot != null)
				for (int idx = 0; idx < cacheHighLowPlot.Length; idx++)
					if (cacheHighLowPlot[idx] != null &&  cacheHighLowPlot[idx].EqualsInput(input))
						return cacheHighLowPlot[idx];
			return CacheIndicator<PR.HighLowPlot>(new PR.HighLowPlot(), input, ref cacheHighLowPlot);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.PR.HighLowPlot HighLowPlot()
		{
			return indicator.HighLowPlot(Input);
		}

		public Indicators.PR.HighLowPlot HighLowPlot(ISeries<double> input )
		{
			return indicator.HighLowPlot(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.PR.HighLowPlot HighLowPlot()
		{
			return indicator.HighLowPlot(Input);
		}

		public Indicators.PR.HighLowPlot HighLowPlot(ISeries<double> input )
		{
			return indicator.HighLowPlot(input);
		}
	}
}

#endregion
