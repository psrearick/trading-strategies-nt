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
	public class TradingSignals : Indicator
	{
		private	Series<double>		hist;
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Trading signals for primary strategy";
				Name										= "_Trading Signals";
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
				
				MAFastPeriod								= 21;
				MAMidPeriod									= 50;
				MASlowPeriod								= 200;
			}
			else if (State == State.DataLoaded)
			{
				hist = new Series<double>(this);
			}
		}

		protected override void OnBarUpdate()
		{
			//###########################################################################
			// MOVING AVERAGES
			//###########################################################################
			double maFast 		= SMA(MAFastPeriod)[0];
			double maMid  		= SMA(MAMidPeriod)[0];
			double maSlow 		= SMA(MASlowPeriod)[0];
			
			bool maFastRising	= IsRising(SMA(MAFastPeriod));
			bool maMidRising	= IsRising(SMA(MAMidPeriod));
			bool maSlowRising	= IsRising(SMA(MASlowPeriod));
			
			bool maFastFalling	= IsFalling(SMA(MAFastPeriod));
			bool maMidFalling	= IsFalling(SMA(MAMidPeriod));
			bool maSlowFalling	= IsFalling(SMA(MASlowPeriod));
			
			bool maStackRising	= maFast > maMid && maMid > maSlow;
			bool maStackFalling	= maFast < maMid && maMid < maSlow;
			
			bool allMaRising	= maFastRising && maMidRising && maSlowRising;
			bool allMaFalling	= maFastFalling && maMidFalling && maSlowFalling;
			
			//###########################################################################
			// VWAP
			//###########################################################################
			double vwap 	= OrderFlowVWAP(VWAPResolution.Standard, TradingHours.String2TradingHours("CME US Index Futures RTH"), VWAPStandardDeviations.Three, 1, 2, 3).VWAP[0];
			bool aboveVwap	= Close[0] > vwap;
			bool belowVwap	= Close[0] < vwap;
			
			//###########################################################################
			// RSI
			//###########################################################################
			double rsi		= RSI(14, 3)[0];
			bool highRsi	= rsi > 65;
			bool lowRsi		= rsi < 30;
			
//			//###########################################################################
//			// MACD
//			//###########################################################################
			double macd 		= MACD(12, 26, 9)[0];
            double signal 		= MACD(12, 26, 9).Avg[0];
			double histog 		= macd - signal;
			double priorHistog1 = 0;
			double priorHistog2 = 0;
			
			if (CurrentBar == 0)
			{
				hist[0]			= 0;
				
			}
			else
			{
				hist[0]			= histog;
				priorHistog1	= hist[1];
				priorHistog2	= hist[2];
			}			
			
			bool macdRising		= histog > priorHistog1 || histog > priorHistog2;
			bool macdFalling	= histog < priorHistog1 || histog < priorHistog2;
			
			//###########################################################################
			// Conditions
			//###########################################################################
			bool longCondition	=
				allMaRising
				&& maStackRising
				&& aboveVwap
				&& !highRsi
				&& !macdFalling;
				
			bool shortCondition =
				allMaFalling
				&& maStackFalling
				&& belowVwap
				&& !lowRsi
				&& !macdRising;
			
			//###########################################################################
			// Plot Indicator
			//###########################################################################
			if (longCondition)
			{
				Draw.ArrowDown(this, "arrowDown" + CurrentBar, true, 0, High[0] + TickSize * 4, Brushes.LimeGreen);
			}
			
						
			if (shortCondition)
			{
				Draw.ArrowDown(this, "arrowDown" + CurrentBar, true, 0, High[0] + TickSize * 4, Brushes.Red);
			}
		}

		#region Properties
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Moving Average Fast Period", Description="Moving Average Fast Period", Order=1, GroupName="Parameters")]
		public int MAFastPeriod
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Moving Average Mid Period", Description="Moving Average Mid Period", Order=1, GroupName="Parameters")]
		public int MAMidPeriod
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Moving Average Slow Period", Description="Moving Average Slow Period", Order=1, GroupName="Parameters")]
		public int MASlowPeriod
		{ get; set; }
		
		#endregion
		
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private TradingSignals[] cacheTradingSignals;
		public TradingSignals TradingSignals(int mAFastPeriod, int mAMidPeriod, int mASlowPeriod)
		{
			return TradingSignals(Input, mAFastPeriod, mAMidPeriod, mASlowPeriod);
		}

		public TradingSignals TradingSignals(ISeries<double> input, int mAFastPeriod, int mAMidPeriod, int mASlowPeriod)
		{
			if (cacheTradingSignals != null)
				for (int idx = 0; idx < cacheTradingSignals.Length; idx++)
					if (cacheTradingSignals[idx] != null && cacheTradingSignals[idx].MAFastPeriod == mAFastPeriod && cacheTradingSignals[idx].MAMidPeriod == mAMidPeriod && cacheTradingSignals[idx].MASlowPeriod == mASlowPeriod && cacheTradingSignals[idx].EqualsInput(input))
						return cacheTradingSignals[idx];
			return CacheIndicator<TradingSignals>(new TradingSignals(){ MAFastPeriod = mAFastPeriod, MAMidPeriod = mAMidPeriod, MASlowPeriod = mASlowPeriod }, input, ref cacheTradingSignals);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.TradingSignals TradingSignals(int mAFastPeriod, int mAMidPeriod, int mASlowPeriod)
		{
			return indicator.TradingSignals(Input, mAFastPeriod, mAMidPeriod, mASlowPeriod);
		}

		public Indicators.TradingSignals TradingSignals(ISeries<double> input , int mAFastPeriod, int mAMidPeriod, int mASlowPeriod)
		{
			return indicator.TradingSignals(input, mAFastPeriod, mAMidPeriod, mASlowPeriod);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.TradingSignals TradingSignals(int mAFastPeriod, int mAMidPeriod, int mASlowPeriod)
		{
			return indicator.TradingSignals(Input, mAFastPeriod, mAMidPeriod, mASlowPeriod);
		}

		public Indicators.TradingSignals TradingSignals(ISeries<double> input , int mAFastPeriod, int mAMidPeriod, int mASlowPeriod)
		{
			return indicator.TradingSignals(input, mAFastPeriod, mAMidPeriod, mASlowPeriod);
		}
	}
}

#endregion
