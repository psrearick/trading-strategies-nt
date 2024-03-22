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
		private PriceActionUtils PA;

		protected override void OnStateChange()
		{
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
				AddPlot(Brushes.DarkCyan, "Legs Direction");
			}
			else if (State == State.Configure)
			{
				PA = PriceActionUtils();
			}
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBar < 15) {
				Value[0] = 0;
				return;
			}

			int bearish = 0;
			int bullish = 0;

			for (int i = 0; i < 15; i++) {
				if (PA.isBearishBar(i) && PA.isFallingBar(i)) {
					bearish++;
				}

				if (PA.isBullishBar(i) && PA.isRisingBar(i)) {
					bullish++;
				}
			}

			Value[0] = bullish > bearish ? 1 : bearish > bullish ? -1 : 0;
		}
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
