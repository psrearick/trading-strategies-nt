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
	public class MABand : Indicator
	{
		private EMA i_fast;
        private EMA i_mid;
        private SMA i_slow;

        public Series<bool> maFastRising;
        public Series<bool> maMidRising;
        public Series<bool> maSlowRising;
        public Series<bool> maFastFalling;
        public Series<bool> maMidFalling;
        public Series<bool> maSlowFalling;
        public Series<bool> maStackRising;
        public Series<bool> maStackFalling;
        public Series<bool> allMaRising;
        public Series<bool> allMaFalling;
        public Series<bool> closeAboveFast;
        public Series<bool> closeAboveMid;
        public Series<bool> closeAboveSlow;
        public Series<bool> fastAboveMid;
        public Series<bool> fastBelowMid;
        public Series<bool> midAboveSlow;
        public Series<bool> midBelowSlow;
        public Series<bool> fastAboveSlow;
        public Series<bool> fastBelowSlow;
		
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults) {
				Description									= @"Moving Average Band";
				Name										= "_Moving Average Band";
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
				
				AddPlot(Brushes.LimeGreen, "Fast");
				AddPlot(Brushes.Blue, "Mid");
				AddPlot(Brushes.Red, "Slow");
			}
			
            if (State == State.DataLoaded) {
				i_fast              = EMA(MAFastPeriod);
                i_mid               = EMA(MAMidPeriod);
                i_slow              = SMA(MASlowPeriod);


                maFastRising    = new Series<bool>(this);
                maMidRising     = new Series<bool>(this);
                maSlowRising    = new Series<bool>(this);
                maFastFalling   = new Series<bool>(this);
                maMidFalling    = new Series<bool>(this);
                maSlowFalling   = new Series<bool>(this);
                maStackRising   = new Series<bool>(this);
                maStackFalling  = new Series<bool>(this);
                allMaRising     = new Series<bool>(this);
                allMaFalling    = new Series<bool>(this);
                closeAboveFast  = new Series<bool>(this);
                closeAboveMid   = new Series<bool>(this);
                closeAboveSlow  = new Series<bool>(this);
                fastAboveMid    = new Series<bool>(this);
                fastBelowMid    = new Series<bool>(this);
                midAboveSlow    = new Series<bool>(this);
                midBelowSlow    = new Series<bool>(this);
                fastAboveSlow   = new Series<bool>(this);
                fastBelowSlow   = new Series<bool>(this);
			}
		}

		protected override void OnBarUpdate()
		{
			if (CurrentBar < 1) {
				return;
			}
			
			Fast[0] = i_fast[0];
			Mid[0] = i_mid[0];
			Slow[0] = i_slow[0];
			
			maFastRising[0]		= IsRising(i_fast);
			maMidRising[0]		= IsRising(i_mid);
			maSlowRising[0]		= IsRising(i_slow);
            maFastFalling[0]	= IsFalling(i_fast);
            maMidFalling[0]		= IsFalling(i_mid);
            maSlowFalling[0]	= IsFalling(i_slow);
            fastAboveMid[0]     = i_fast[0] > i_mid[0];
            fastBelowMid[0]     = i_fast[0] < i_mid[0];
            midAboveSlow[0]     = i_mid[0] > i_slow[0];
            midBelowSlow[0]     = i_mid[0] < i_slow[0];
            fastAboveSlow[0]    = i_fast[0] > i_slow[0];
            fastBelowSlow[0]    = i_fast[0] < i_slow[0];
			maStackRising[0]	= fastAboveMid[0] && midAboveSlow[0];
			maStackFalling[0]   = fastBelowMid[0] && midBelowSlow[0];
			allMaRising[0]		= maFastRising[0] && maMidRising[0] && maSlowRising[0];
			allMaFalling[0]		= maFastFalling[0] && maMidFalling[0] && maSlowFalling[0];
            closeAboveFast[0]   = Close[0] > i_fast[0];
            closeAboveMid[0]    = Close[0] > i_mid[0];
            closeAboveSlow[0]   = Close[0] > i_slow[0];
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
		public Series<double> Mid
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
		[Display(Name="Moving Average Fast Period", Description="Moving Average Fast Period", Order=5, GroupName="Parameters")]
		public int MAFastPeriod
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Moving Average Mid Period", Description="Moving Average Mid Period", Order=6, GroupName="Parameters")]
		public int MAMidPeriod
		{ get; set; }
		
		[NinjaScriptProperty]
		[Range(1, int.MaxValue)]
		[Display(Name="Moving Average Slow Period", Description="Moving Average Slow Period", Order=7, GroupName="Parameters")]
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
		private MABand[] cacheMABand;
		public MABand MABand(int mAFastPeriod, int mAMidPeriod, int mASlowPeriod)
		{
			return MABand(Input, mAFastPeriod, mAMidPeriod, mASlowPeriod);
		}

		public MABand MABand(ISeries<double> input, int mAFastPeriod, int mAMidPeriod, int mASlowPeriod)
		{
			if (cacheMABand != null)
				for (int idx = 0; idx < cacheMABand.Length; idx++)
					if (cacheMABand[idx] != null && cacheMABand[idx].MAFastPeriod == mAFastPeriod && cacheMABand[idx].MAMidPeriod == mAMidPeriod && cacheMABand[idx].MASlowPeriod == mASlowPeriod && cacheMABand[idx].EqualsInput(input))
						return cacheMABand[idx];
			return CacheIndicator<MABand>(new MABand(){ MAFastPeriod = mAFastPeriod, MAMidPeriod = mAMidPeriod, MASlowPeriod = mASlowPeriod }, input, ref cacheMABand);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.MABand MABand(int mAFastPeriod, int mAMidPeriod, int mASlowPeriod)
		{
			return indicator.MABand(Input, mAFastPeriod, mAMidPeriod, mASlowPeriod);
		}

		public Indicators.MABand MABand(ISeries<double> input , int mAFastPeriod, int mAMidPeriod, int mASlowPeriod)
		{
			return indicator.MABand(input, mAFastPeriod, mAMidPeriod, mASlowPeriod);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.MABand MABand(int mAFastPeriod, int mAMidPeriod, int mASlowPeriod)
		{
			return indicator.MABand(Input, mAFastPeriod, mAMidPeriod, mASlowPeriod);
		}

		public Indicators.MABand MABand(ISeries<double> input , int mAFastPeriod, int mAMidPeriod, int mASlowPeriod)
		{
			return indicator.MABand(input, mAFastPeriod, mAMidPeriod, mASlowPeriod);
		}
	}
}

#endregion
