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
	public class EMADistance : Indicator
	{
		private Point currentMouse;
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Display the Distance between close and an EMA and ATR in Corner";
				Name										= "_EMA Distance / ATR Text";
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
				ATRPeriod									= 14;
				Period										= 21;
				EMADistanceColor							= Brushes.LightCoral;
				ATRColor									= Brushes.Aqua;
			}
			else if (State == State.Historical)
            {
                ChartControl.PreviewMouseMove += mouse_move;
            }
			
		}
		
		protected override void OnRender(ChartControl chartControl, ChartScale chartScale)
        {
			// float endX = chartControl.GetXByBarIndex(ChartBars, ChartBars.ToIndex);
			
			int horizontal = ChartingExtensions.ConvertToHorizontalPixels(this.currentMouse.X, ChartControl.PresentationSource);
			
			DateTime timeSlot = ChartControl.GetTimeBySlotIndex((int)ChartControl.GetSlotIndexByX(horizontal));
			
			int barIndex = Bars.GetBar(timeSlot);
			
			int barsAgo = CurrentBar - Bars.GetBar(timeSlot);
			
			if (barsAgo == -1) {
				barIndex = ChartBars.ToIndex - 1;
			}
			
			double ema = EMA(Period).Values[0].GetValueAt(barIndex);
			double close = Close.GetValueAt(barIndex);
			double open = Open.GetValueAt(barIndex);
			double high = High.GetValueAt(barIndex);
			double low = Low.GetValueAt(barIndex);
			double emaDistance = Math.Round(Math.Abs(ema - close), 2);
			double atr = Math.Round(ATR(ATRPeriod).Values[0].GetValueAt(barIndex), 2);
			
			
			Draw.TextFixed(this, "OpenText", "Open: " + open.ToString() + "\n\n\n\n\n\n\n", TextPosition.BottomLeft, Brushes.White, ChartControl.Properties.LabelFont, Brushes.Transparent, Brushes.Transparent, 0);
			Draw.TextFixed(this, "HighText", "High: " + high.ToString() + "\n\n\n\n\n\n", TextPosition.BottomLeft, Brushes.White, ChartControl.Properties.LabelFont, Brushes.Transparent, Brushes.Transparent, 0);
			Draw.TextFixed(this, "LowText", "Low: " + low.ToString() + "\n\n\n\n\n", TextPosition.BottomLeft, Brushes.White, ChartControl.Properties.LabelFont, Brushes.Transparent, Brushes.Transparent, 0);
			Draw.TextFixed(this, "CloseText", "Close: " + close.ToString() + "\n\n\n\n", TextPosition.BottomLeft, Brushes.White, ChartControl.Properties.LabelFont, Brushes.Transparent, Brushes.Transparent, 0);
			
			Draw.TextFixed(this, "EMADistanceText", "EMA Distance: " + emaDistance.ToString() + "\n\n", TextPosition.BottomLeft, EMADistanceColor, ChartControl.Properties.LabelFont, Brushes.Transparent, Brushes.Transparent, 0);
			Draw.TextFixed(this, "ATRText", "ATR: " + atr.ToString() + "\n", TextPosition.BottomLeft, ATRColor, ChartControl.Properties.LabelFont, Brushes.Transparent, Brushes.Transparent, 0);
}

		
        public void mouse_move(object sender, System.Windows.Input.MouseEventArgs e)
        {
			
			
             currentMouse = new Point(e.GetPosition(ChartPanel as IInputElement).X, e.GetPosition(ChartPanel as IInputElement).Y);
             ForceRefresh();
        }

		protected override void OnBarUpdate()
		{
			//
		}

		#region Properties
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="EMA Period", Description="EMA Period", Order=1, GroupName="Parameters")]
		public int Period
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="ATR Period", Description="ATR Period", Order=2, GroupName="Parameters")]
		public int ATRPeriod
		{ get; set; }
		
		[NinjaScriptProperty]
		[Display(Name="ATR Color", Description="ATR Color", Order=3, GroupName="Parameters")]
		public SolidColorBrush ATRColor
		{ get; set; }

		[NinjaScriptProperty]
		[Display(Name="EMA Distance Color", Description="EMA Distance Color", Order=4, GroupName="Parameters")]
		public SolidColorBrush EMADistanceColor
		{ get; set; }

		#endregion

	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private EMADistance[] cacheEMADistance;
		public EMADistance EMADistance(int period, int aTRPeriod, SolidColorBrush aTRColor, SolidColorBrush eMADistanceColor)
		{
			return EMADistance(Input, period, aTRPeriod, aTRColor, eMADistanceColor);
		}

		public EMADistance EMADistance(ISeries<double> input, int period, int aTRPeriod, SolidColorBrush aTRColor, SolidColorBrush eMADistanceColor)
		{
			if (cacheEMADistance != null)
				for (int idx = 0; idx < cacheEMADistance.Length; idx++)
					if (cacheEMADistance[idx] != null && cacheEMADistance[idx].Period == period && cacheEMADistance[idx].ATRPeriod == aTRPeriod && cacheEMADistance[idx].ATRColor == aTRColor && cacheEMADistance[idx].EMADistanceColor == eMADistanceColor && cacheEMADistance[idx].EqualsInput(input))
						return cacheEMADistance[idx];
			return CacheIndicator<EMADistance>(new EMADistance(){ Period = period, ATRPeriod = aTRPeriod, ATRColor = aTRColor, EMADistanceColor = eMADistanceColor }, input, ref cacheEMADistance);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.EMADistance EMADistance(int period, int aTRPeriod, SolidColorBrush aTRColor, SolidColorBrush eMADistanceColor)
		{
			return indicator.EMADistance(Input, period, aTRPeriod, aTRColor, eMADistanceColor);
		}

		public Indicators.EMADistance EMADistance(ISeries<double> input , int period, int aTRPeriod, SolidColorBrush aTRColor, SolidColorBrush eMADistanceColor)
		{
			return indicator.EMADistance(input, period, aTRPeriod, aTRColor, eMADistanceColor);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.EMADistance EMADistance(int period, int aTRPeriod, SolidColorBrush aTRColor, SolidColorBrush eMADistanceColor)
		{
			return indicator.EMADistance(Input, period, aTRPeriod, aTRColor, eMADistanceColor);
		}

		public Indicators.EMADistance EMADistance(ISeries<double> input , int period, int aTRPeriod, SolidColorBrush aTRColor, SolidColorBrush eMADistanceColor)
		{
			return indicator.EMADistance(input, period, aTRPeriod, aTRColor, eMADistanceColor);
		}
	}
}

#endregion
