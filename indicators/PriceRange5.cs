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
	public class PriceRange5 : Indicator
	{
		private	Series<double> i_price_distance;
		private	Series<double> i_atr_distance;
		private	Series<double> i_price_atr_average;
		private ATR i_atr;
		private SMA i_atr_ma;
		private SMA i_price_ma;
		private EMA i_price_signal;
		private EMA i_ma_fast;
		private EMA i_ma_slow;
		private	Series<double> signal;
		
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
				
				AddPlot(Brushes.LimeGreen, "Signal");
				AddPlot(Brushes.Aqua, "Fast");
				AddPlot(Brushes.Red, "Slow");
			}
			
			if (State == State.DataLoaded) {
				i_price_distance 	= new Series<double>(this);
				i_atr_distance 		= new Series<double>(this);
				i_price_atr_average	= new Series<double>(this);
				signal				= new Series<double>(this);
				i_atr				= ATR(LookbackPeriod);
				i_atr_ma			= SMA(i_atr, MovingAverageLength);
				i_price_signal		= EMA(Length);
				i_price_ma			= SMA(MovingAverageLength);
				i_ma_fast			= EMA(signal, FastLength);
				i_ma_slow			= EMA(signal, SlowLength);
			}
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBar < 1) {
				return;
			}
			
			i_price_distance[0] 			= i_price_signal[0] - i_price_ma[0];
			i_atr_distance[0]				= i_atr[0] - i_atr_ma[0];
			
			double priceDistanceMax			= MAX(i_price_distance, MovingAverageLength)[0];
			double priceDistanceMin			= MIN(i_price_distance, MovingAverageLength)[0];
			double priceDistanceRange		= priceDistanceMax - priceDistanceMin;
			double priceDistanceNorm		= (((i_price_distance[0] - priceDistanceMin) * 200) / priceDistanceRange) - 100;
			
			double atrDistanceMax			= MAX(i_atr_distance, MovingAverageLength)[0];
			double atrDistanceMin			= MIN(i_atr_distance, MovingAverageLength)[0];
			double atrDistanceRange			= atrDistanceMax - atrDistanceMin;
			double atrDistanceNorm			= ((i_atr_distance[0] - atrDistanceMin) * 100) / atrDistanceRange;
			
			double atrNormToAverage 		= priceDistanceNorm > 50 ? atrDistanceNorm : atrDistanceNorm * -1;
			i_price_atr_average[0]  		= (priceDistanceNorm + atrNormToAverage) / 2;
			double averageDistanceMax		= MAX(i_price_atr_average, MovingAverageLength)[0];
			double averageDistanceMin		= MIN(i_price_atr_average, MovingAverageLength)[0];
			double averageDistanceRange		= averageDistanceMax - averageDistanceMin;
			double averageDistanceNorm		= ((i_price_atr_average[0] - averageDistanceMin) * 100) / averageDistanceRange;
			
			signal[0] 	= averageDistanceNorm;
			Signal[0]	= signal[0];
			Fast[0]		= i_ma_fast[0];
			Slow[0]		= i_ma_slow[0];
			
//			Price[0] 	= priceDistanceNorm;
//			Range[0]	= atrDistanceNorm;
//			Values[2][0] = averageDistanceNorm;
		}
		
		#region Properties
		
		[Browsable(false)]
		[XmlIgnore]
		public Series<double> Signal
		{
			get { return Values[0]; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> Fast
		{
			get { return Values[1]; }
		}
		
		[Browsable(false)]
		[XmlIgnore]
		public Series<double> Slow
		{
			get { return Values[2]; }
		}
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Length", Description="Length", Order=1, GroupName="Parameters")]
		public int Length
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Moving Average Length", Description="Moving Average Length", Order=2, GroupName="Parameters")]
		public int MovingAverageLength
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Lookback", Description="Lookback", Order=3, GroupName="Parameters")]
		public int LookbackPeriod
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Fast Length", Description="Fast Length", Order=4, GroupName="Parameters")]
		public int FastLength
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Slow Length", Description="Slow Length", Order=5, GroupName="Parameters")]
		public int SlowLength
		{ get; set; }
		
		#endregion
	}
	
}

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
//		public PriceRange5 PriceRange5(int Length)
//		{
//			return PriceRange5(Length, true);
//		}
		
		public PriceRange5 PriceRange5(int Length, int Fast, int Slow)
		{
			int maLength = Length * 5;
			int lookback = Length * 2;
			
			return PriceRange5(Length, maLength, lookback, Fast, Slow);
		}
		
//		public PriceRange5 PriceRange5(int Length, int MALength, int Lookback)
//		{
//			return PriceRange5(Length, MALength, Lookback);
//		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
//		public Indicators.PriceRange5 PriceRange5(int Length)
//		{
//			return PriceRange5(Length, true);
//		}
		
		public Indicators.PriceRange5 PriceRange5(int Length, int Fast, int Slow)
		{
			int maLength = Length * 5;
			int lookback = Length * 2;
			
			return PriceRange5(Length, maLength, lookback, Fast, Slow);
		}
		
//		public Indicators.PriceRange5 PriceRange5(int Length, int MALength, int Lookback)
//		{
//			return PriceRange5(Length, true, MALength, Lookback);
//		}
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private PriceRange5[] cachePriceRange5;
		public PriceRange5 PriceRange5(int length, int movingAverageLength, int lookbackPeriod, int fastLength, int slowLength)
		{
			return PriceRange5(Input, length, movingAverageLength, lookbackPeriod, fastLength, slowLength);
		}

		public PriceRange5 PriceRange5(ISeries<double> input, int length, int movingAverageLength, int lookbackPeriod, int fastLength, int slowLength)
		{
			if (cachePriceRange5 != null)
				for (int idx = 0; idx < cachePriceRange5.Length; idx++)
					if (cachePriceRange5[idx] != null && cachePriceRange5[idx].Length == length && cachePriceRange5[idx].MovingAverageLength == movingAverageLength && cachePriceRange5[idx].LookbackPeriod == lookbackPeriod && cachePriceRange5[idx].FastLength == fastLength && cachePriceRange5[idx].SlowLength == slowLength && cachePriceRange5[idx].EqualsInput(input))
						return cachePriceRange5[idx];
			return CacheIndicator<PriceRange5>(new PriceRange5(){ Length = length, MovingAverageLength = movingAverageLength, LookbackPeriod = lookbackPeriod, FastLength = fastLength, SlowLength = slowLength }, input, ref cachePriceRange5);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.PriceRange5 PriceRange5(int length, int movingAverageLength, int lookbackPeriod, int fastLength, int slowLength)
		{
			return indicator.PriceRange5(Input, length, movingAverageLength, lookbackPeriod, fastLength, slowLength);
		}

		public Indicators.PriceRange5 PriceRange5(ISeries<double> input , int length, int movingAverageLength, int lookbackPeriod, int fastLength, int slowLength)
		{
			return indicator.PriceRange5(input, length, movingAverageLength, lookbackPeriod, fastLength, slowLength);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.PriceRange5 PriceRange5(int length, int movingAverageLength, int lookbackPeriod, int fastLength, int slowLength)
		{
			return indicator.PriceRange5(Input, length, movingAverageLength, lookbackPeriod, fastLength, slowLength);
		}

		public Indicators.PriceRange5 PriceRange5(ISeries<double> input , int length, int movingAverageLength, int lookbackPeriod, int fastLength, int slowLength)
		{
			return indicator.PriceRange5(input, length, movingAverageLength, lookbackPeriod, fastLength, slowLength);
		}
	}
}

#endregion
