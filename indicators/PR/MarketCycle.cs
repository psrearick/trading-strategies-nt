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
	public class MarketCycle : Indicator
	{
		#region Variables
		private PriceActionUtils PA;
		private Trends TrendIdentifier;

		private Brush brush0;
		private Brush brushUp1;
		private Brush brushUp2;
		private Brush brushUp3;
		private Brush brushUp4;
		private Brush brushUp5;
		private Brush brushUp6;
		private Brush brushUp7;
		private Brush brushUp8;
		private Brush brushUp9;
		private Brush brushDown1;
		private Brush brushDown2;
		private Brush brushDown3;
		private Brush brushDown4;
		private Brush brushDown5;
		private Brush brushDown6;
		private Brush brushDown7;
		private Brush brushDown8;
		private Brush brushDown9;
		#endregion

		#region OnStateChange()
		protected override void OnStateChange()
		{
			#region State.SetDefaults
			if (State == State.SetDefaults)
			{
				#region properties
				Description									= @"Identify the current market cycle.";
				Name										= "MarketCycle";
				Calculate									= Calculate.OnBarClose;
				ScaleJustification							= NinjaTrader.Gui.Chart.ScaleJustification.Right;

				DisplayInDataBox							= true;
				DrawOnPricePanel							= true;
				PaintPriceMarkers							= false;
				IsSuspendedWhileInactive					= false;
				IsOverlay									= false;
				IsAutoScale									= true;

				AddPlot(Brushes.DarkCyan, "Market Direction");

				brush0 = null;
				brushUp1 = Brushes.Green.Clone();
				brushUp2 = Brushes.Green.Clone();
				brushUp3 = Brushes.Green.Clone();
				brushUp4 = Brushes.Green.Clone();
				brushUp5 = Brushes.Green.Clone();
				brushUp6 = Brushes.Green.Clone();
				brushUp7 = Brushes.Green.Clone();
				brushUp8 = Brushes.Green.Clone();
				brushUp9 = Brushes.Green.Clone();
				brushUp1.Opacity = 0.030;
				brushUp2.Opacity = 0.060;
				brushUp3.Opacity = 0.090;
				brushUp4.Opacity = 0.120;
				brushUp5.Opacity = 0.150;
				brushUp6.Opacity = 0.180;
				brushUp7.Opacity = 0.210;
				brushUp8.Opacity = 0.240;
				brushUp9.Opacity = 0.270;
				brushUp1.Freeze();
				brushUp2.Freeze();
				brushUp3.Freeze();
				brushUp4.Freeze();
				brushUp5.Freeze();
				brushUp6.Freeze();
				brushUp7.Freeze();
				brushUp8.Freeze();
				brushUp9.Freeze();
				brushDown1 = Brushes.Red.Clone();
				brushDown2 = Brushes.Red.Clone();
				brushDown3 = Brushes.Red.Clone();
				brushDown4 = Brushes.Red.Clone();
				brushDown5 = Brushes.Red.Clone();
				brushDown6 = Brushes.Red.Clone();
				brushDown7 = Brushes.Red.Clone();
				brushDown8 = Brushes.Red.Clone();
				brushDown9 = Brushes.Red.Clone();
				brushDown1.Opacity = 0.030;
				brushDown2.Opacity = 0.060;
				brushDown3.Opacity = 0.090;
				brushDown4.Opacity = 0.120;
				brushDown5.Opacity = 0.150;
				brushDown6.Opacity = 0.180;
				brushDown7.Opacity = 0.210;
				brushDown8.Opacity = 0.240;
				brushDown9.Opacity = 0.270;
				brushDown1.Freeze();
				brushDown2.Freeze();
				brushDown3.Freeze();
				brushDown4.Freeze();
				brushDown5.Freeze();
				brushDown6.Freeze();
				brushDown7.Freeze();
				brushDown8.Freeze();
				brushDown9.Freeze();
				#endregion
			}
			#endregion
			#region State.Configure
			else if (State == State.Configure)
			{
				PA = PriceActionUtils();
				TrendIdentifier = Trends();
			}
			#endregion
			#region State.Terminated
			else if(State == State.Terminated)
			{
				#region change background back to normal
				if(BackBrushAll 		   != null)
					BackBrushAll  			= null;
				#endregion
          	}
			#endregion
		}
		#endregion

		#region OnBarUpdate()
		protected override void OnBarUpdate()
		{
			colorBackground();
		}
		#endregion



		#region colorBackground()
		private void colorBackground()
		{
			int trend = (int) TrendIdentifier[0];
			Value[0] = TrendIdentifier[0];

			BackBrush =
				trend == -9 ? brushDown9 :
				trend == -8 ? brushDown8 :
				trend == -7 ? brushDown7 :
				trend == -6 ? brushDown6 :
				trend == -5 ? brushDown5 :
				trend == -4 ? brushDown4 :
				trend == -3 ? brushDown3 :
				trend == -2 ? brushDown2 :
				trend == -1 ? brushDown1 :
				trend == 0 ? brush0 :
				trend == 1 ? brushUp1 :
				trend == 2 ? brushUp2 :
				trend == 3 ? brushUp3 :
				trend == 4 ? brushUp4 :
				trend == 5 ? brushUp5 :
				trend == 6 ? brushUp6 :
				trend == 7 ? brushUp7 :
				trend == 8 ? brushUp8 :
				trend == 9 ? brushUp9 :
				null;
		}
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private PR.MarketCycle[] cacheMarketCycle;
		public PR.MarketCycle MarketCycle()
		{
			return MarketCycle(Input);
		}

		public PR.MarketCycle MarketCycle(ISeries<double> input)
		{
			if (cacheMarketCycle != null)
				for (int idx = 0; idx < cacheMarketCycle.Length; idx++)
					if (cacheMarketCycle[idx] != null &&  cacheMarketCycle[idx].EqualsInput(input))
						return cacheMarketCycle[idx];
			return CacheIndicator<PR.MarketCycle>(new PR.MarketCycle(), input, ref cacheMarketCycle);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.PR.MarketCycle MarketCycle()
		{
			return indicator.MarketCycle(Input);
		}

		public Indicators.PR.MarketCycle MarketCycle(ISeries<double> input )
		{
			return indicator.MarketCycle(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.PR.MarketCycle MarketCycle()
		{
			return indicator.MarketCycle(Input);
		}

		public Indicators.PR.MarketCycle MarketCycle(ISeries<double> input )
		{
			return indicator.MarketCycle(input);
		}
	}
}

#endregion
