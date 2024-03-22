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
	public class TrendTypes : Indicator
	{
		#region Variables
		private Legs Movements;
		private ATR BarRange;
		public Series<int> LegValues;
		public Series<int> SwingValues;
		public Series<int> TrendValues;
		public Series<int> MovementValues;
		private double MinScalpSize;
		private double MinSwingSize;

		private int TrendDirection;
		private double TrendLow;
		private double TrendHigh;
		private int TrendBarCount;
		private int BarsSinceNewHigh;
		private int BarsSinceNewLow;
		#endregion

		#region OnStateChange()
		protected override void OnStateChange()
		{
			#region State.SetDefaults
			if (State == State.SetDefaults)
			{
				Description									= @"Plots Legs, Swings, and Trends";
				Name										= "Trend Types";
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
				AddPlot(Brushes.DarkCyan, "Legs");
				AddPlot(Brushes.Green, "Swings");
				AddPlot(Brushes.Red, "Trends");
			}
			#endregion
			#region State.Configure
			else if (State == State.Configure)
			{
				BarRange	= ATR(10);
				Movements	= Legs();
			}
			#endregion

			#region State.DataLoaded
			else if (State == State.DataLoaded)
			{
				LegValues 		= new Series<int>(this);
				SwingValues 	= new Series<int>(this);
				TrendValues		= new Series<int>(this);
				MovementValues	= new Series<int>(this);
			}
			#endregion
		}
		#endregion

		#region OnBarUpdate()
		protected override void OnBarUpdate()
		{
			if (CurrentBar < 15) {
				LegValues[0] 		= 0;
				SwingValues[0]		= 0;
				TrendValues[0] 		= 0;
				MovementValues[0]	= 0;

				SetPlotValues();
				return;
			}

			SetSwingSize();

			EvaluateTrend();

			SetPlotValues();
		}
		#endregion

		#region EvaluateTrend()
		private void EvaluateTrend()
		{
			InitializeTrend();

			MovementValues[0] = Movements.ValFromStart(0);

//			if (Movements.Starts[0] == Movements.Starts[1]) {
//				UpdateValuesInSameMovement();
//			}

			if (High[0] > TrendHigh) {
				BarsSinceNewHigh = 0;
				TrendHigh = High[0];
			}

			if (Low[0] < TrendLow) {
				BarsSinceNewLow = 0;
				TrendLow = Low[0];
			}
		}
		#endregion

//		#region UpdateValuesInSameMovement()
//		private void UpdateValuesInSameMovement()
//		{

//		}
//		#endregion

		#region InitializeTrend()
		private void InitializeTrend()
		{
			if (TrendLow > 0 && TrendHigh > 0) {
				return;
			}

			TrendLow 			= Low[0];
			TrendHigh 			= High[0];
			TrendDirection		= 0;
			TrendBarCount		= 1;
			BarsSinceNewHigh	= 0;
			BarsSinceNewLow		= 0;
		}
		#endregion

		#region SetSwingSize()
		private void SetSwingSize()
		{
			MinScalpSize = 0.7 * BarRange[0];
			MinSwingSize = MinScalpSize * 3.75;
		}
		#endregion

		#region SetPlotValues()
		private void SetPlotValues()
		{
			Values[0][0] 	= (double) LegValues[0];
			Values[1][0] 	= (double) SwingValues[0];
			Values[2][0] 	= (double) TrendValues[0];
		}
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private PR.TrendTypes[] cacheTrendTypes;
		public PR.TrendTypes TrendTypes()
		{
			return TrendTypes(Input);
		}

		public PR.TrendTypes TrendTypes(ISeries<double> input)
		{
			if (cacheTrendTypes != null)
				for (int idx = 0; idx < cacheTrendTypes.Length; idx++)
					if (cacheTrendTypes[idx] != null &&  cacheTrendTypes[idx].EqualsInput(input))
						return cacheTrendTypes[idx];
			return CacheIndicator<PR.TrendTypes>(new PR.TrendTypes(), input, ref cacheTrendTypes);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.PR.TrendTypes TrendTypes()
		{
			return indicator.TrendTypes(Input);
		}

		public Indicators.PR.TrendTypes TrendTypes(ISeries<double> input )
		{
			return indicator.TrendTypes(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.PR.TrendTypes TrendTypes()
		{
			return indicator.TrendTypes(Input);
		}

		public Indicators.PR.TrendTypes TrendTypes(ISeries<double> input )
		{
			return indicator.TrendTypes(input);
		}
	}
}

#endregion
