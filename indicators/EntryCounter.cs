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
	public class EntryCounter : Indicator
	{
		private int entryCount = 0;
		private double entryPoint;
		private double high;
		private double low;
		private bool inUptrend;
		private EMA ema;
		private NinjaTrader.Gui.Tools.SimpleFont indicatorFont = new NinjaTrader.Gui.Tools.SimpleFont("Arial", 12) { Size = 12, Bold = true };
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Counter of legs in a swing.";
				Name										= "_Entry Counter";
				Calculate									= Calculate.OnBarClose;
				IsOverlay									= true;
				DisplayInDataBox							= true;
				DrawOnPricePanel							= true;
				DrawHorizontalGridLines						= true;
				DrawVerticalGridLines						= true;
				PaintPriceMarkers							= true;
				ScaleJustification							= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				//Disable this property if your indicator requires custom values that cumulate with each new market data event. 
				//See Help Guide for additional information.
				IsSuspendedWhileInactive					= true;
				EMALength									= 21;
				
//				AddPlot(Brushes.Blue, "Highest High");
//    			AddPlot(Brushes.Red, "Lowest Low");
				
				
			}
			else if (State == State.DataLoaded)
			{
				ema = EMA(EMALength);
			}
			else if (State == State.Configure)
			{
			}
		}

		protected override void OnBarUpdate()
		{
			bool rising = IsRising(ema);
			bool trendChange = false;
			bool newHigh = false;
			bool newLow = false;
			bool higherHigh = IsRising(High);
			bool lowerLow = IsFalling(Low);
			bool higherLow = IsRising(Low);
			bool lowerHigh = IsFalling(High);
			bool doubleTop = !higherHigh && !lowerHigh;
			bool doubleBottom = !lowerLow && !higherLow;
			bool entry = false;
			
			if (rising != inUptrend)
			{
				inUptrend = rising;
				trendChange = true;
			}
			
			if (trendChange)
			{
				high = High[0];
				low = Low[0];
				entryCount = 0;
			}
			
			if (High[0] > high) {
				high = High[0];
				newHigh = true;
				entryCount = 0;
			}
			
			
			if (Low[0] < low) {
				low = Low[0];
				newLow = true;
				entryCount = 0;
			}
			
//			if (inUptrend && doubleTop)
//			{
//				entryCount = 0;
//			}
			
//			if (!inUptrend && doubleBottom)
//			{
//				entryCount = 0;
//			}
			
			if (inUptrend && High[0] > entryPoint) {
				entryCount = 0;
			}
			
			if (!inUptrend && Low[0] < entryPoint) {
				entryCount = 0;
			}
			
			
			if (inUptrend && higherHigh && !newHigh) {
				entry = true;
				entryPoint = High[0];
			}
			
			if (!inUptrend && lowerLow && !newLow) {
				entry = true;
				entryPoint = Low[0];
			}
			
			if (entry && inUptrend) {
				entryCount++;
				Draw.Text(this, "tagUp" + CurrentBar, false, entryCount.ToString(), 0, High[0] + TickSize * 5, 0, Brushes.Green, indicatorFont, TextAlignment.Center, Brushes.Black, null, 1);
			}
			
			if (entry && !inUptrend) {
				entryCount++;
				Draw.Text(this, "tagDown" + CurrentBar, false, entryCount.ToString(), 0, Low[0] - TickSize * 5, 0, Brushes.Red, indicatorFont, TextAlignment.Center, Brushes.Black, null, 1);
			}
			
			
			
			
			
			
			
//			Values[0][0] = high;
//			Values[1][0] = low;
			
			
			
			
//			if (inUptrend == true)
//			{
//				Draw.Text(this, "tagUp" + CurrentBar, false, "1", 0, High[0] + TickSize * 5, 0, Brushes.Green, indicatorFont, TextAlignment.Center, Brushes.Black, null, 1);
//			}
//			else
//			{
//				Draw.Text(this, "tagDown" + CurrentBar, false, "2", 0, Low[0] - TickSize * 5, 0, Brushes.Red, indicatorFont, TextAlignment.Center, Brushes.Black, null, 1);
//			}
				
		}
		
				
		protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
		{
		  //  base.OnRender(chartControl, chartScale); //adding the override and omitting this line prevents plots from displaying
		}

		#region Properties
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="EMALength", Description="Length of EMA used to determine market direction", Order=1, GroupName="Parameters")]
		public int EMALength
		{ get; set; }

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> EntryLongCountStart
		{
			get { return Values[0]; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> EntryShortCountStart
		{
			get { return Values[1]; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> EntryLong
		{
			get { return Values[2]; }
		}

		[Browsable(false)]
		[XmlIgnore]
		public Series<double> EntryShort
		{
			get { return Values[3]; }
		}
		#endregion

	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private EntryCounter[] cacheEntryCounter;
		public EntryCounter EntryCounter(int eMALength)
		{
			return EntryCounter(Input, eMALength);
		}

		public EntryCounter EntryCounter(ISeries<double> input, int eMALength)
		{
			if (cacheEntryCounter != null)
				for (int idx = 0; idx < cacheEntryCounter.Length; idx++)
					if (cacheEntryCounter[idx] != null && cacheEntryCounter[idx].EMALength == eMALength && cacheEntryCounter[idx].EqualsInput(input))
						return cacheEntryCounter[idx];
			return CacheIndicator<EntryCounter>(new EntryCounter(){ EMALength = eMALength }, input, ref cacheEntryCounter);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.EntryCounter EntryCounter(int eMALength)
		{
			return indicator.EntryCounter(Input, eMALength);
		}

		public Indicators.EntryCounter EntryCounter(ISeries<double> input , int eMALength)
		{
			return indicator.EntryCounter(input, eMALength);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.EntryCounter EntryCounter(int eMALength)
		{
			return indicator.EntryCounter(Input, eMALength);
		}

		public Indicators.EntryCounter EntryCounter(ISeries<double> input , int eMALength)
		{
			return indicator.EntryCounter(input, eMALength);
		}
	}
}

#endregion
