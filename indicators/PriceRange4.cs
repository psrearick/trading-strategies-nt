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
	public class PriceRange4 : Indicator
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
				
				MovingAverageLength							= 10;
				Length										= 2;
				LookbackPeriod								= 4;

				AddPlot(Brushes.LimeGreen, "MA");
				AddPlot(Brushes.Aqua, "Range Signal");
				AddPlot(Brushes.Red, "Upper Band 1");
				AddPlot(Brushes.Red, "Lower Band 1");
				AddPlot(Brushes.Coral, "Upper Band 2");
				AddPlot(Brushes.Coral, "Lower Band 2");
			}
			
			if (State == State.DataLoaded) {
                i_midpoint_distance = new Series<double>(this);
				i_signal			= SMA(i_midpoint_distance, Length);
				i_ma				= SMA(i_midpoint_distance, MovingAverageLength);
			}
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBar < 1) {
				return;
			}
			
			double highestHigh	= MAX(High, LookbackPeriod)[0];
			double highestLow	= MAX(Low, LookbackPeriod)[0];
			double highestOpen	= MAX(Open, LookbackPeriod)[0];
			double highestClose = MAX(Close, LookbackPeriod)[0];
			double lowestHigh	= MIN(High, LookbackPeriod)[0];
			double lowestLow	= MIN(Low, LookbackPeriod)[0];
			double lowestOpen	= MIN(Open, LookbackPeriod)[0];
			double lowestClose  = MIN(Close, LookbackPeriod)[0];

			double high = RealBody ? Math.Max(highestClose, highestOpen) : highestHigh;
			double low 	= RealBody ? Math.Min(lowestClose, lowestOpen) : lowestLow;
			
			i_midpoint_distance[0] 	= Close[0] - ((high + low) / 2);
			stdev 					= StdDev(i_midpoint_distance, MovingAverageLength)[0];
			
			MovingAverage[0]	= i_ma[0];
			Signal[0] 			= i_signal[0];
			UpperBand1[0] 		= i_ma[0] + stdev * 1;
			LowerBand1[0] 		= i_ma[0] - stdev * 1;
			UpperBand2[0] 		= i_ma[0] + stdev * 1.5;
			LowerBand2[0] 		= i_ma[0] - stdev * 1.5;
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
		[Display(Name="Length", Description="Length", Order=1, GroupName="Parameters")]
		public int Length
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="Real Body", Description="Real Body", Order=2, GroupName="Parameters")]
		public bool RealBody
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Moving Average Length", Description="Moving Average Length", Order=3, GroupName="Parameters")]
		public int MovingAverageLength
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Lookback", Description="Lookback", Order=4, GroupName="Parameters")]
		public int LookbackPeriod
		{ get; set; }
		
		#endregion
	}
	
}

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		public PriceRange4 PriceRange4(int Length)
		{
			return PriceRange4(Length, true);
		}
		
		public PriceRange4 PriceRange4(int Length, bool RealBody)
		{
			int maLength = Length * 5;
			int lookback = Length * 2;
			
			return PriceRange4(Length, RealBody, maLength, lookback);
		}
		
		public PriceRange4 PriceRange4(int Length, int MALength, int Lookback)
		{
			return PriceRange4(Length, true, MALength, Lookback);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.PriceRange4 PriceRange4(int Length)
		{
			return PriceRange4(Length, true);
		}
		
		public Indicators.PriceRange4 PriceRange4(int Length, bool RealBody)
		{
			int maLength = Length * 5;
			int lookback = Length * 2;
			
			return PriceRange4(Length, RealBody, maLength, lookback);
		}
		
		public Indicators.PriceRange4 PriceRange4(int Length, int MALength, int Lookback)
		{
			return PriceRange4(Length, true, MALength, Lookback);
		}
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private PriceRange4[] cachePriceRange4;
		public PriceRange4 PriceRange4(int length, bool realBody, int movingAverageLength, int lookbackPeriod)
		{
			return PriceRange4(Input, length, realBody, movingAverageLength, lookbackPeriod);
		}

		public PriceRange4 PriceRange4(ISeries<double> input, int length, bool realBody, int movingAverageLength, int lookbackPeriod)
		{
			if (cachePriceRange4 != null)
				for (int idx = 0; idx < cachePriceRange4.Length; idx++)
					if (cachePriceRange4[idx] != null && cachePriceRange4[idx].Length == length && cachePriceRange4[idx].RealBody == realBody && cachePriceRange4[idx].MovingAverageLength == movingAverageLength && cachePriceRange4[idx].LookbackPeriod == lookbackPeriod && cachePriceRange4[idx].EqualsInput(input))
						return cachePriceRange4[idx];
			return CacheIndicator<PriceRange4>(new PriceRange4(){ Length = length, RealBody = realBody, MovingAverageLength = movingAverageLength, LookbackPeriod = lookbackPeriod }, input, ref cachePriceRange4);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.PriceRange4 PriceRange4(int length, bool realBody, int movingAverageLength, int lookbackPeriod)
		{
			return indicator.PriceRange4(Input, length, realBody, movingAverageLength, lookbackPeriod);
		}

		public Indicators.PriceRange4 PriceRange4(ISeries<double> input , int length, bool realBody, int movingAverageLength, int lookbackPeriod)
		{
			return indicator.PriceRange4(input, length, realBody, movingAverageLength, lookbackPeriod);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.PriceRange4 PriceRange4(int length, bool realBody, int movingAverageLength, int lookbackPeriod)
		{
			return indicator.PriceRange4(Input, length, realBody, movingAverageLength, lookbackPeriod);
		}

		public Indicators.PriceRange4 PriceRange4(ISeries<double> input , int length, bool realBody, int movingAverageLength, int lookbackPeriod)
		{
			return indicator.PriceRange4(input, length, realBody, movingAverageLength, lookbackPeriod);
		}
	}
}

#endregion
