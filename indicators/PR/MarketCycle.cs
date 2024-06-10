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
	public class MarketCycle : Indicator
	{
		#region Variables

		public Series<MarketCycleStage> Stage;
		public Series<TrendDirection> Direction;

		private ATR atr;
		private SMA atrMa;
		private StdDev atrStdDev;
		private SMA maLong;
		private SMA maShort;

		private Brush brushUp0;
		private Brush brushUp1;
		private Brush brushUp2;
		private Brush brushDown0;
		private Brush brushDown1;
		private Brush brushDown2;


		#endregion

		#region OnStateChange()
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"";
				Name										= "Market Cycle";
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

			if (State == State.Configure)
			{
				atr 				= ATR(8);
				maShort				= SMA(20);
				maLong				= SMA(50);

				brushUp0 = Brushes.Green.Clone();
				brushUp0.Opacity = 0.600;
				brushUp0.Freeze();

				brushUp1 = Brushes.Green.Clone();
				brushUp1.Opacity = 0.400;
				brushUp1.Freeze();

				brushUp2 = Brushes.Green.Clone();
				brushUp2.Opacity = 0.200;
				brushUp2.Freeze();

				brushDown0 = Brushes.Red.Clone();
				brushDown0.Opacity = 0.600;
				brushDown0.Freeze();

				brushDown1 = Brushes.Red.Clone();
				brushDown1.Opacity = 0.400;
				brushDown1.Freeze();

				brushDown2 = Brushes.Red.Clone();
				brushDown2.Opacity = 0.200;
				brushDown2.Freeze();


				Direction 		= new Series<TrendDirection>(this, MaximumBarsLookBack.Infinite);
				Stage 			= new Series<MarketCycleStage>(this, MaximumBarsLookBack.Infinite);
			}

			if (State == State.DataLoaded)
			{
				atrMa 				= SMA(atr, 10);
				atrStdDev	 		= StdDev(atr, 20);
			}
		}
		#endregion

		#region OnBarUpdate()
		protected override void OnBarUpdate()
		{
			Stage[0] = GetMarketStage();
			Direction[0] = GetTrendDirection();

			BackBrush = GetBackgroundBrush();
		}
		#endregion

		#region GetBackgroundBrush()
		private Brush GetBackgroundBrush()
		{
			if (Direction[0] == TrendDirection.Flat)
				return null;

			if (Stage[0] == MarketCycleStage.TradingRange)
				return null;

			if (Direction[0] == TrendDirection.Bullish)
			{
				if (Stage[0] == MarketCycleStage.Breakout)
					return brushUp0;

				if (Stage[0] == MarketCycleStage.TightChannel)
					return brushUp1;

				if (Stage[0] == MarketCycleStage.BroadChannel)
					return brushUp2;
			}

			if (Stage[0] == MarketCycleStage.Breakout)
					return brushDown0;

			if (Stage[0] == MarketCycleStage.TightChannel)
				return brushDown1;

			if (Stage[0] == MarketCycleStage.BroadChannel)
				return brushDown2;

			return null;

		}
		#endregion

		#region GetTrendDirection()
		private TrendDirection GetTrendDirection()
		{
			if (CurrentBar < 80)
			{
				return TrendDirection.Flat;
			}

			if (Close[0] > maShort[0])
			{
				return TrendDirection.Bullish;
			}

			if (Close[0] < maShort[0])
			{
				return TrendDirection.Bearish;
			}

			return TrendDirection.Flat;
		}
		#endregion

		#region GetMarketStage()
		private MarketCycleStage GetMarketStage()
		{
			if ((CurrentBar < 80) || (atr[0] < (atrMa[0] - atrStdDev[0])))
			{
				return MarketCycleStage.TradingRange;
			}

    		if (Close[0] > maShort[0])
		    {
				if (Close[0] > maLong[0])
				{
					return MarketCycleStage.BroadChannel;
				}

				if (Volume[0] > Volume[1])
		        {
		           return MarketCycleStage.Breakout;
		        }

				if (atr[0] <= (atrMa[0] + atrStdDev[0]))
		        {
		            return MarketCycleStage.TightChannel;
		        }

				return MarketCycleStage.BroadChannel;
		    }

    		if (Close[0] < maShort[0])
		    {
				if (Close[0] < maLong[0])
				{
					return MarketCycleStage.BroadChannel;
				}

				if (Volume[0] > Volume[1])
		        {
		           return MarketCycleStage.Breakout;
		        }

				if (atr[0] <= (atrMa[0] + atrStdDev[0]))
		        {
		            return MarketCycleStage.TightChannel;
		        }

				return MarketCycleStage.BroadChannel;
		    }

			return MarketCycleStage.TradingRange;
		}
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private PR.MarketCycle[] cacheMarketCycle;
		public PR.MarketCycle MarketCycle()
		{
			return MarketCycle(Input);
		}

		public PR.MarketCycle MarketCycle(ISeries<double> input)
		{
			if (cacheMarketCycle != null)
				for (int idx = 0; idx < cacheMarketCycle.Length; idx++)
					if (cacheMarketCycle[idx] != null &&  cacheMarketCycle[idx].EqualsInput(input))
						return cacheMarketCycle[idx];
			return CacheIndicator<PR.MarketCycle>(new PR.MarketCycle(), input, ref cacheMarketCycle);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.PR.MarketCycle MarketCycle()
		{
			return indicator.MarketCycle(Input);
		}

		public Indicators.PR.MarketCycle MarketCycle(ISeries<double> input )
		{
			return indicator.MarketCycle(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.PR.MarketCycle MarketCycle()
		{
			return indicator.MarketCycle(Input);
		}

		public Indicators.PR.MarketCycle MarketCycle(ISeries<double> input )
		{
			return indicator.MarketCycle(input);
		}
	}
}

#endregion
