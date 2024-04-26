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
	public class TrendSnap : Indicator
	{
		#region Properties
		private TrendTypes TT;
		public Series<double> LegValues;
		public Series<double> SwingValues;
		public Series<double> TrendValues;
		public Series<double> BreakoutValues;
		public int LegStarted		= 0;
		public int SwingStarted		= 0;
		public int TrendStarted		= 0;
		public int BreakoutStarted	= 0;
		#endregion

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"";
				Name										= "TrendSnap";
				Calculate									= Calculate.OnBarClose;
				IsOverlay									= false;
				DisplayInDataBox							= true;
				DrawOnPricePanel							= false;
				DrawHorizontalGridLines						= true;
				DrawVerticalGridLines						= true;
				PaintPriceMarkers							= true;
				ScaleJustification							= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				//Disable this property if your indicator requires custom values that cumulate with each new market data event.
				//See Help Guide for additional information.
				IsSuspendedWhileInactive					= true;



				AddPlot(new Stroke(Brushes.Blue, 2), PlotStyle.Line, "Legs");
				AddPlot(new Stroke(Brushes.Green, 3), PlotStyle.Line, "Swings");
				AddPlot(new Stroke(Brushes.Orange, 4), PlotStyle.Line, "Trends");
				AddPlot(new Stroke(Brushes.Red, 2), PlotStyle.Line, "Breakouts");
				AddPlot(Brushes.Black, "Zero Line");
			}
			#region State.Configure
			else if (State == State.Configure)
			{
				TT		= TrendTypes();
			}
			#endregion

			#region State.DataLoaded
			else if (State == State.DataLoaded)
			{
				LegValues 		= new Series<double>(this);
				SwingValues 	= new Series<double>(this);
				TrendValues		= new Series<double>(this);
				BreakoutValues	= new Series<double>(this);
			}
			#endregion
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBar < 25) {
				return;
			}

			Values[0][0] 	= TT.Values[0][0];
			Values[1][0] 	= TT.Values[1][0];
			Values[2][0] 	= TT.Values[2][0];
			Values[3][0] 	= TT.Values[3][0];
			Values[4][0]	= 0;

			LegValues[0] 		= TT.Values[0][0];
			SwingValues[0]		= TT.Values[1][0];
			TrendValues[0] 		= TT.Values[2][0];
			BreakoutValues[0] 	= TT.Values[3][0];

			LegStarted		= LegValues[0] != 0 ? (LegValues[0] == LegValues[1] ? LegStarted++ : 1) : 0;
			SwingStarted	= SwingValues[0] != 0 ? (SwingValues[0] == SwingValues[1] ? SwingStarted++ : 1) : 0;
			TrendStarted	= TrendValues[0] != 0 ? (TrendValues[0] == TrendValues[1] ? TrendStarted++ : 1) : 0;
			BreakoutStarted	= BreakoutValues[0] != 0 ? (BreakoutValues[0] == BreakoutValues[1] ? BreakoutStarted++ : 1) : 0;
		}
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private PR.TrendSnap[] cacheTrendSnap;
		public PR.TrendSnap TrendSnap()
		{
			return TrendSnap(Input);
		}

		public PR.TrendSnap TrendSnap(ISeries<double> input)
		{
			if (cacheTrendSnap != null)
				for (int idx = 0; idx < cacheTrendSnap.Length; idx++)
					if (cacheTrendSnap[idx] != null &&  cacheTrendSnap[idx].EqualsInput(input))
						return cacheTrendSnap[idx];
			return CacheIndicator<PR.TrendSnap>(new PR.TrendSnap(), input, ref cacheTrendSnap);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.PR.TrendSnap TrendSnap()
		{
			return indicator.TrendSnap(Input);
		}

		public Indicators.PR.TrendSnap TrendSnap(ISeries<double> input )
		{
			return indicator.TrendSnap(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.PR.TrendSnap TrendSnap()
		{
			return indicator.TrendSnap(Input);
		}

		public Indicators.PR.TrendSnap TrendSnap(ISeries<double> input )
		{
			return indicator.TrendSnap(input);
		}
	}
}

#endregion
