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
	public class PriceRange2 : Indicator
	{
		private	Series<double> i_midpoint_distance;
		private SMA i_signal;
		private SMA i_ma;
		private double stdev;
		
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
				
				MovingAverageLength							= 14;
				SmoothingPeriod								= 2;
				LookbackPeriod								= 9;

				AddPlot(Brushes.LimeGreen, "MA");
				AddPlot(Brushes.Aqua, "Range Signal");
				AddPlot(Brushes.Red, "Upper Band 1");
				AddPlot(Brushes.Red, "Lower Band 1");
				AddPlot(Brushes.Coral, "Upper Band 2");
				AddPlot(Brushes.Coral, "Lower Band 2");
			}
			
			if (State == State.DataLoaded) {
                i_midpoint_distance = new Series<double>(this);
				i_signal			= SMA(i_midpoint_distance, SmoothingPeriod);
				i_ma				= SMA(i_midpoint_distance, MovingAverageLength);
			}
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBar < 1) {
				return;
			}

			i_midpoint_distance[0] 	= Math.Abs(Close[0] - ((MAX(High, LookbackPeriod)[0] + MIN(Low, LookbackPeriod)[0]) / 2));
			stdev 					= StdDev(i_midpoint_distance, MovingAverageLength)[0];
			
			MovingAverage[0]	= i_ma[0];
			Signal[0] 			= i_signal[0];
			UpperBand1[0] 		= i_ma[0] + stdev;
			LowerBand1[0] 		= i_ma[0] - stdev;
			UpperBand2[0] 		= i_ma[0] + stdev * 2;
			LowerBand2[0] 		= i_ma[0] - stdev * 2;
		}
		
		#region Properties
		
		[Browsable(false)]
		[XmlIgnore]
		public Series<double> MovingAverage
		{
			get { return Values[0]; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> Signal
		{
			get { return Values[1]; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> UpperBand1
		{
			get { return Values[2]; }
		}
		
		[Browsable(false)]
		[XmlIgnore]
		public Series<double> LowerBand1
		{
			get { return Values[3]; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> UpperBand2
		{
			get { return Values[4]; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> LowerBand2
		{
			get { return Values[5]; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> Midpoint
		{
			get { return i_midpoint_distance; }
		}
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Moving Average Length", Description="Moving Average Length", Order=1, GroupName="Parameters")]
		public int MovingAverageLength
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Smoothing Period", Description="Smoothing Period", Order=2, GroupName="Parameters")]
		public int SmoothingPeriod
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Lookback", Description="Lookback", Order=3, GroupName="Parameters")]
		public int LookbackPeriod
		{ get; set; }
		
		#endregion
	}
	
	
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private PriceRange2[] cachePriceRange2;
		public PriceRange2 PriceRange2(int movingAverageLength, int smoothingPeriod, int lookbackPeriod)
		{
			return PriceRange2(Input, movingAverageLength, smoothingPeriod, lookbackPeriod);
		}

		public PriceRange2 PriceRange2(ISeries<double> input, int movingAverageLength, int smoothingPeriod, int lookbackPeriod)
		{
			if (cachePriceRange2 != null)
				for (int idx = 0; idx < cachePriceRange2.Length; idx++)
					if (cachePriceRange2[idx] != null && cachePriceRange2[idx].MovingAverageLength == movingAverageLength && cachePriceRange2[idx].SmoothingPeriod == smoothingPeriod && cachePriceRange2[idx].LookbackPeriod == lookbackPeriod && cachePriceRange2[idx].EqualsInput(input))
						return cachePriceRange2[idx];
			return CacheIndicator<PriceRange2>(new PriceRange2(){ MovingAverageLength = movingAverageLength, SmoothingPeriod = smoothingPeriod, LookbackPeriod = lookbackPeriod }, input, ref cachePriceRange2);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.PriceRange2 PriceRange2(int movingAverageLength, int smoothingPeriod, int lookbackPeriod)
		{
			return indicator.PriceRange2(Input, movingAverageLength, smoothingPeriod, lookbackPeriod);
		}

		public Indicators.PriceRange2 PriceRange2(ISeries<double> input , int movingAverageLength, int smoothingPeriod, int lookbackPeriod)
		{
			return indicator.PriceRange2(input, movingAverageLength, smoothingPeriod, lookbackPeriod);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.PriceRange2 PriceRange2(int movingAverageLength, int smoothingPeriod, int lookbackPeriod)
		{
			return indicator.PriceRange2(Input, movingAverageLength, smoothingPeriod, lookbackPeriod);
		}

		public Indicators.PriceRange2 PriceRange2(ISeries<double> input , int movingAverageLength, int smoothingPeriod, int lookbackPeriod)
		{
			return indicator.PriceRange2(input, movingAverageLength, smoothingPeriod, lookbackPeriod);
		}
	}
}

#endregion
