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
namespace NinjaTrader.NinjaScript.Indicators
{
	public class PriceAction : Indicator
	{
		public bool firstBarUp = false;
		public bool firstBarDown = false;
		
//        private double range0 = 0.0;
//        private double range1 = 0.0;
//        private double range2 = 0.0;
//        private double range3 = 0.0;
//        private bool range0Bigger = false;
//        private bool range1Bigger = false;
//        private bool range2Bigger = false;
//        private bool range0Smaller = false;
//        private bool range1Smaller = false;
//        private bool range2Smaller = false;
        private bool twoSmallerBars = false;
        private bool twoBiggerBars = false;
        private bool threeSmallerBars = false;
        private bool threeBiggerBars = false;
        private bool smallerBars = false;
        private bool biggerBars = false;
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Price Action indicators.";
				Name										= "_Price Action";
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
			}
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBar < 1) {
				return;
			}
			
			firstBarUp 		= BarIsUp(0);
			firstBarDown	= BarIsDown(0);
			
//			range0		    = Math.Abs(Close[0] - Open[0]);
//			range1		    = Math.Abs(Close[1] - Open[1]);
//			range2		    = Math.Abs(Close[2] - Open[2]);
//			range3		    = Math.Abs(Close[3] - Open[3]);
			
//			range0Bigger	= range0 > range1;
//			range1Bigger	= range1 > range2;
//			range2Bigger	= range2 > range3;
//			range0Smaller	= !range0Bigger;
//			range1Smaller	= !range1Bigger;
//			range2Smaller 	= !range2Bigger;

//			twoSmallerBars		= range0Smaller && range1Smaller;
//			twoBiggerBars		= range0Bigger && range1Bigger;
//			threeSmallerBars	= twoSmallerBars && range2Smaller;
//			threeBiggerBars	    = twoBiggerBars && range2Bigger;
//			smallerBars		    = range0Smaller && (range1Smaller || range2Smaller);
//			biggerBars			= range0Bigger && (range1Bigger || range2Bigger);
		}
		
		public double BarRealBody(int index)
		{
			return Close[index] - Open[index];
		}
		
		public double BarRealBodySize(int index)
		{
			return Math.Abs(BarRealBody(index));
		}
		
		public bool BarIsBigger(int index)
		{
			return BarRealBodySize(index) > BarRealBodySize(index + 1);
		}
		
		public bool BarIsSmaller(int index)
		{
			return BarRealBodySize(index) < BarRealBodySize(index + 1);
		}
		
		public bool BarIsDown(int index)
		{
			return Close[index] < Close[index + 1];
		}
		
		public bool BarIsUp(int index)
		{
			return Close[index] > Close[index + 1];
		}
		
		public bool ConsecutiveBiggerBars(int period, int index = 0)
		{
			bool biggerBars = true;
			
			for (int i = 0; i < period; i++) {
				if (!BarIsBigger(i + index)) {
					biggerBars = false;
					
					break;
				}
			}
			
			return biggerBars;
		}
		
		
		public bool ConsecutiveSmallerBars(int period, int index = 0)
		{
			bool smallerBars = true;
			
			for (int i = 0; i < period; i++) {
				if (!BarIsSmaller(i + index)) {
					smallerBars = false;
					
					break;
				}
			}
			
			return smallerBars;
		}
		
		public bool ConsecutiveBarsUp(int period, int index = 0)
		{
			bool barsUp = true;
			
			for (int i = 0; i < period; i++) {
				if (!BarIsUp(i + index)) {
					barsUp = false;
					
					break;
				}
			}
			
			return barsUp;
		}
		
		public bool ConsecutiveBarsDown(int period, int index = 0)
		{
			bool barsDown = true;
			
			for (int i = 0; i < period; i++) {
				if (!BarIsDown(i + index)) {
					barsDown = false;
					
					break;
				}
			}
			
			return barsDown;
		}
		
		public bool LeastBarsDown(int count, int period, int index = 0)
		{
			int barsUp = 0;
			
			for (int i = 0; i < period; i++) {
				if (BarIsDown(i + index)) {
					barsUp = barsUp + 1;
				}
			}
			
			return barsUp >= count;
		}
		
		public bool LeastBarsUp(int count, int period, int index = 0)
		{
			int barsDown = 0;
			
			for (int i = 0; i < period; i++) {
				if (BarIsUp(i + index)) {
					barsDown = barsDown + 1;
				}
			}
			
			return barsDown >= count;
		}
		
		
		
		public bool LeastSmallerBars(int count, int period, int index = 0)
		{
			int barsSmaller = 0;
			
			for (int i = 0; i < period; i++) {
				if (BarIsSmaller(i + index)) {
					barsSmaller = barsSmaller + 1;
				}
			}
			
			return barsSmaller >= count;
		}
		
		public bool LeastBiggerBars(int count, int period, int index = 0)
		{
			int barsBigger = 0;
			
			for (int i = 0; i < period; i++) {
				if (BarIsBigger(i + index)) {
					barsBigger = barsBigger + 1;
				}
			}
			
			return barsBigger >= count;
		}
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private PriceAction[] cachePriceAction;
		public PriceAction PriceAction()
		{
			return PriceAction(Input);
		}

		public PriceAction PriceAction(ISeries<double> input)
		{
			if (cachePriceAction != null)
				for (int idx = 0; idx < cachePriceAction.Length; idx++)
					if (cachePriceAction[idx] != null &&  cachePriceAction[idx].EqualsInput(input))
						return cachePriceAction[idx];
			return CacheIndicator<PriceAction>(new PriceAction(), input, ref cachePriceAction);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.PriceAction PriceAction()
		{
			return indicator.PriceAction(Input);
		}

		public Indicators.PriceAction PriceAction(ISeries<double> input )
		{
			return indicator.PriceAction(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.PriceAction PriceAction()
		{
			return indicator.PriceAction(Input);
		}

		public Indicators.PriceAction PriceAction(ISeries<double> input )
		{
			return indicator.PriceAction(input);
		}
	}
}

#endregion
