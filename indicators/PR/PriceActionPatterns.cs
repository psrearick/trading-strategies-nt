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
	public class PriceActionPatterns : Indicator
	{
		private ATR atr;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"";
				Name										= "PriceActionPatterns";
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
			}
			else if (State == State.Configure)
			{
				atr = ATR(14);
			}
		}

		protected override void OnBarUpdate()
		{
			//Add your custom indicator logic here.
		}

		public bool IsDoubleTop(int barsAgo, int lookbackPeriod, double declineATR)
		{
		    if (barsAgo + lookbackPeriod >= CurrentBar)
		        return false;

		    int highestHighIndex = Enumerable.Range(barsAgo, lookbackPeriod)
		        .OrderByDescending(i => High[i])
		        .First();

		    int firstTopIndex = Enumerable.Range(highestHighIndex + 1, lookbackPeriod - (highestHighIndex - barsAgo))
		        .FirstOrDefault(i => Close[i] < High[highestHighIndex] - (atr[0] * declineATR));

		    if (firstTopIndex == 0)
		        return false;

		    int secondTopIndex = Enumerable.Range(firstTopIndex + 1, lookbackPeriod - (firstTopIndex - barsAgo))
		        .FirstOrDefault(i => High[i] >= High[firstTopIndex] - (atr[0] * declineATR) && Close[i] < High[firstTopIndex]);

		    if (secondTopIndex == 0)
		        return false;

		    double highestHigh = High[highestHighIndex];
		    double firstTopHigh = High[firstTopIndex];
		    double secondTopHigh = High[secondTopIndex];

		    return secondTopHigh <= firstTopHigh &&
		           firstTopHigh <= highestHigh &&
		           secondTopHigh >= firstTopHigh - (atr[0] * declineATR) &&
		           Close[secondTopIndex] < firstTopHigh;
		}

		public bool IsDoubleBottom(int barsAgo, int lookbackPeriod, double inclineATR)
		{
		    if (barsAgo + lookbackPeriod >= CurrentBar)
		        return false;

		    int lowestLowIndex = Enumerable.Range(barsAgo, lookbackPeriod)
		        .OrderBy(i => Low[i])
		        .First();

		    int firstBottomIndex = Enumerable.Range(lowestLowIndex + 1, lookbackPeriod - (lowestLowIndex - barsAgo))
		        .FirstOrDefault(i => Close[i] > Low[lowestLowIndex] + (atr[0] * inclineATR));

		    if (firstBottomIndex == 0)
		        return false;

		    int secondBottomIndex = Enumerable.Range(firstBottomIndex + 1, lookbackPeriod - (firstBottomIndex - barsAgo))
		        .FirstOrDefault(i => Low[i] <= Low[firstBottomIndex] + (atr[0] * inclineATR) && Close[i] > Low[firstBottomIndex]);

		    if (secondBottomIndex == 0)
		        return false;

		    double lowestLow = Low[lowestLowIndex];
		    double firstBottomLow = Low[firstBottomIndex];
		    double secondBottomLow = Low[secondBottomIndex];

		    return secondBottomLow >= firstBottomLow &&
		           firstBottomLow >= lowestLow &&
		           secondBottomLow <= firstBottomLow + (atr[0] * inclineATR) &&
		           Close[secondBottomIndex] > firstBottomLow;
		}
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private PR.PriceActionPatterns[] cachePriceActionPatterns;
		public PR.PriceActionPatterns PriceActionPatterns()
		{
			return PriceActionPatterns(Input);
		}

		public PR.PriceActionPatterns PriceActionPatterns(ISeries<double> input)
		{
			if (cachePriceActionPatterns != null)
				for (int idx = 0; idx < cachePriceActionPatterns.Length; idx++)
					if (cachePriceActionPatterns[idx] != null &&  cachePriceActionPatterns[idx].EqualsInput(input))
						return cachePriceActionPatterns[idx];
			return CacheIndicator<PR.PriceActionPatterns>(new PR.PriceActionPatterns(), input, ref cachePriceActionPatterns);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.PR.PriceActionPatterns PriceActionPatterns()
		{
			return indicator.PriceActionPatterns(Input);
		}

		public Indicators.PR.PriceActionPatterns PriceActionPatterns(ISeries<double> input )
		{
			return indicator.PriceActionPatterns(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.PR.PriceActionPatterns PriceActionPatterns()
		{
			return indicator.PriceActionPatterns(Input);
		}

		public Indicators.PR.PriceActionPatterns PriceActionPatterns(ISeries<double> input )
		{
			return indicator.PriceActionPatterns(input);
		}
	}
}

#endregion
