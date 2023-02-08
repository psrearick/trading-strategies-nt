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
	public class PriceRange : Indicator
	{
		private	Series<double> i_midpoint_distance;
		private SMA i_fast;
		private SMA i_slow;
		private ATR i_atr;
		private double mid;
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Enter the description for your new custom Indicator here.";
				Name										= "_Price Range";
				Calculate									= Calculate.OnBarClose;
				IsOverlay									= false;
				DisplayInDataBox							= true;
				DrawOnPricePanel							= false;
				DrawHorizontalGridLines						= true;
				DrawVerticalGridLines						= true;
				PaintPriceMarkers							= true;
				ScaleJustification							= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				IsSuspendedWhileInactive					= true;
				
				FastMovingAverage							= 40;
				SlowMovingAverage							= 50;
				LookbackPeriod								= 12;
				ATRLength									= 14;
				
				AddPlot(Brushes.Aqua, "Fast");
				AddPlot(Brushes.Red, "Slow");
				AddPlot(Brushes.Goldenrod, "ATR");
			}
			
			if (State == State.DataLoaded) {
                i_midpoint_distance = new Series<double>(this);
				i_slow				= SMA(i_midpoint_distance, SlowMovingAverage);
				i_fast				= SMA(i_midpoint_distance, FastMovingAverage);
				i_atr				= ATR(ATRLength);
			}
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBar < 1) {
				return;
			}

			mid		 				= (MAX(High, LookbackPeriod)[0] + MIN(Low, LookbackPeriod)[0]) / 2;
			i_midpoint_distance[0] 	= Math.Abs(Close[0] - mid);
			
			Fast[0] = i_fast[0];
			Slow[0] = i_slow[0];
			ATR[0] = i_atr[0];
		}
		
		#region Properties
		
		[Browsable(false)]
		[XmlIgnore]
		public Series<double> Fast
		{
			get { return Values[0]; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> Slow
		{
			get { return Values[1]; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> ATR
		{
			get { return Values[2]; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> Midpoint
		{
			get { return i_midpoint_distance; }
		}
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="MA Fast", Description="Moving Average Fast", Order=1, GroupName="Parameters")]
		public int FastMovingAverage
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="MA Slow", Description="Moving Average Slow", Order=2, GroupName="Parameters")]
		public int SlowMovingAverage
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Lookback", Description="Lookback", Order=3, GroupName="Parameters")]
		public int LookbackPeriod
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="ATR Length", Description="ATR Length", Order=4, GroupName="Parameters")]
		public int ATRLength
		{ get; set; }
		
		#endregion
	}
	
	
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private PriceRange[] cachePriceRange;
		public PriceRange PriceRange(int fastMovingAverage, int slowMovingAverage, int lookbackPeriod, int aTRLength)
		{
			return PriceRange(Input, fastMovingAverage, slowMovingAverage, lookbackPeriod, aTRLength);
		}

		public PriceRange PriceRange(ISeries<double> input, int fastMovingAverage, int slowMovingAverage, int lookbackPeriod, int aTRLength)
		{
			if (cachePriceRange != null)
				for (int idx = 0; idx < cachePriceRange.Length; idx++)
					if (cachePriceRange[idx] != null && cachePriceRange[idx].FastMovingAverage == fastMovingAverage && cachePriceRange[idx].SlowMovingAverage == slowMovingAverage && cachePriceRange[idx].LookbackPeriod == lookbackPeriod && cachePriceRange[idx].ATRLength == aTRLength && cachePriceRange[idx].EqualsInput(input))
						return cachePriceRange[idx];
			return CacheIndicator<PriceRange>(new PriceRange(){ FastMovingAverage = fastMovingAverage, SlowMovingAverage = slowMovingAverage, LookbackPeriod = lookbackPeriod, ATRLength = aTRLength }, input, ref cachePriceRange);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.PriceRange PriceRange(int fastMovingAverage, int slowMovingAverage, int lookbackPeriod, int aTRLength)
		{
			return indicator.PriceRange(Input, fastMovingAverage, slowMovingAverage, lookbackPeriod, aTRLength);
		}

		public Indicators.PriceRange PriceRange(ISeries<double> input , int fastMovingAverage, int slowMovingAverage, int lookbackPeriod, int aTRLength)
		{
			return indicator.PriceRange(input, fastMovingAverage, slowMovingAverage, lookbackPeriod, aTRLength);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.PriceRange PriceRange(int fastMovingAverage, int slowMovingAverage, int lookbackPeriod, int aTRLength)
		{
			return indicator.PriceRange(Input, fastMovingAverage, slowMovingAverage, lookbackPeriod, aTRLength);
		}

		public Indicators.PriceRange PriceRange(ISeries<double> input , int fastMovingAverage, int slowMovingAverage, int lookbackPeriod, int aTRLength)
		{
			return indicator.PriceRange(input, fastMovingAverage, slowMovingAverage, lookbackPeriod, aTRLength);
		}
	}
}

#endregion
