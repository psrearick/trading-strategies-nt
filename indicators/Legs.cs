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
	public class Legs : Indicator
	{
		#region Variables
		private PriceActionUtils PA;
		public Series<double> Starts;
		public Series<double> BarsAgoStarts;
		#endregion

		#region OnStateChange()
		protected override void OnStateChange()
		{
			#region State.SetDefaults
			if (State == State.SetDefaults)
			{
				Description									= @"Enter the description for your new custom Indicator here.";
				Name										= "Legs";
				Calculate									= Calculate.OnBarClose;
				IsOverlay									= false;
				DisplayInDataBox							= true;
				DrawOnPricePanel							= true;
				DrawHorizontalGridLines						= true;
				DrawVerticalGridLines						= true;
				PaintPriceMarkers							= true;
				ScaleJustification							= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				//Disable this property if your indicator requires custom values that cumulate with each new market data event.
				//See Help Guide for additional information.
				IsSuspendedWhileInactive					= true;
				AddPlot(Brushes.DarkCyan, "Signal");
				AddPlot(new Stroke(Brushes.Green, 3), PlotStyle.Line, "Leg");
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
				BarsAgoStarts	= new Series<double>(this, MaximumBarsLookBack.Infinite);
				Starts			= new Series<double>(this, MaximumBarsLookBack.Infinite);
			}
			#endregion
		}
		#endregion

		#region OnBarUpdate()
		protected override void OnBarUpdate()
		{
			if (CurrentBar < 12) {
				Values[0][0] = 0;
				Values[1][0] = 0;
				Starts[0] = 0;
				return;
			}

			int bearish = 0;
			int bullish = 0;
			int barsAgoHigh = 0;
			int barsAgoLow = 0;
			double high = High[0];
			double low = Low[0];

			for (int i = 0; i < 12; i++) {
				if (PA.IsBearishBar(i) && PA.IsFallingBar(i)) {
					bearish++;
				}

				if (PA.IsBullishBar(i) && PA.IsRisingBar(i)) {
					bullish++;
				}

				if (High[i] >= high) {
					high = High[i];
					barsAgoHigh = i;
				}

				if (Low[i] <= low) {
					low = Low[i];
					barsAgoLow = i;
				}
			}

			Values[0][0] = bullish > bearish ? 1 : bearish > bullish ? -1 : 0;

			BarsAgoStarts[0] = Values[0][0] == Values[0][1]
				? BarsAgoStarts[1] + 1
				: Values[0][0] == 1
					? barsAgoLow
					: Values[0][0] == -1
						? barsAgoHigh
						: Math.Max(barsAgoHigh, barsAgoLow);

			Starts[0] = CurrentBar - BarsAgoStarts[0];

			for (int i = 0; i < BarsAgoStarts[0]; i++) {
				Values[1][i] = Values[0][0];
			}
		}
		#endregion

		#region ValFromStart()
		public int ValFromStart(int barsAgo = 0)
		{
			return (int) Values[1][barsAgo];
		}
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private PR.Legs[] cacheLegs;
		public PR.Legs Legs()
		{
			return Legs(Input);
		}

		public PR.Legs Legs(ISeries<double> input)
		{
			if (cacheLegs != null)
				for (int idx = 0; idx < cacheLegs.Length; idx++)
					if (cacheLegs[idx] != null &&  cacheLegs[idx].EqualsInput(input))
						return cacheLegs[idx];
			return CacheIndicator<PR.Legs>(new PR.Legs(), input, ref cacheLegs);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.PR.Legs Legs()
		{
			return indicator.Legs(Input);
		}

		public Indicators.PR.Legs Legs(ISeries<double> input )
		{
			return indicator.Legs(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.PR.Legs Legs()
		{
			return indicator.Legs(Input);
		}

		public Indicators.PR.Legs Legs(ISeries<double> input )
		{
			return indicator.Legs(input);
		}
	}
}

#endregion
