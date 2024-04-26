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
	public class Trends : Indicator
	{
		#region Variables

		private Swings SwingIdentifier;
		private TrendDirection Trend = TrendDirection.Flat;
		private int MinTrendBars = 27;
		private int TrendStart = 0;
		private int BarsInBullTrend = 0;
		private int BarsInBearTrend = 0;
		private int BarsInTrend = 0;
		private double BreakoutExtreme = 0;

		private PriceActionUtils PA;

		public Series<double> TrendValues;
		public Series<double> TrendStarts;
		public Series<double> LegValues;
		public Series<double> LegStarts;
		public Series<double> SwingValues;
		public Series<double> SwingStarts;

		#endregion

		#region OnStageChange()
		protected override void OnStateChange()
		{
			#region State.SetDefaults
			if (State == State.SetDefaults)
			{
				Description									= @"Enter the description for your new custom Indicator here.";
				Name										= "Trends";
				Calculate									= Calculate.OnBarClose;
				IsOverlay									= false;
				DisplayInDataBox							= true;
				DrawOnPricePanel							= true;
				PaintPriceMarkers							= true;
				ScaleJustification							= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				//Disable this property if your indicator requires custom values that cumulate with each new market data event.
				//See Help Guide for additional information.
				IsSuspendedWhileInactive					= true;
				AddPlot(Brushes.DarkCyan, "Trend Direction");
				AddPlot(Brushes.Red, "Legs In Swing");
			}
			#endregion

			#region State.Configure
			else if (State == State.Configure)
			{
				SwingIdentifier = Swings();
				PA = PriceActionUtils();
			}
			#endregion

			#region State.DataLoaded
			else if (State == State.DataLoaded)
			{
				TrendValues 	= new Series<double>(this, MaximumBarsLookBack.Infinite);
				TrendStarts 	= new Series<double>(this, MaximumBarsLookBack.Infinite);
				LegValues 		= new Series<double>(this, MaximumBarsLookBack.Infinite);
				LegStarts 		= new Series<double>(this, MaximumBarsLookBack.Infinite);
				SwingValues 	= new Series<double>(this, MaximumBarsLookBack.Infinite);
				SwingStarts 	= new Series<double>(this, MaximumBarsLookBack.Infinite);
			}
			#endregion
		}
		#endregion

		#region OnBarUpdate()
		protected override void OnBarUpdate()
		{
			if (CurrentBar < 35) {
				TrendValues[0] 	= 0;
				TrendStarts[0] 	= 0;
				LegValues[0] 	= 0;
				LegStarts[0] 	= 0;
				SwingValues[0]	= 0;
				SwingStarts[0] 	= 0;
				return;
			}

			if (bullishTrendEnded()) {
				BarsInBullTrend = 0;
			}

			if (bearishTrendEnded()) {
				BarsInBearTrend = 0;
			}

			checkForBullishTrend();
			checkForBearishTrend();

			if (Trend == TrendDirection.Flat) {
				BarsInTrend = 0;
			} else {
				BarsInTrend++;
			}

			setHistoricalValues();

			Value[0] = getTrendValue();

			setLegsInSwing();
		}
		#endregion

		#region GetBarsInTrend()
		public int GetBarsInTrend()
		{
			return BarsInTrend;
		}
		#endregion

		#region getTrendValue()
		private int getTrendValue()
		{
			int trendValue = Trend == TrendDirection.Bullish ? 5 : Trend == TrendDirection.Bearish ? -5 : 0;
			int swingValue = SwingIdentifier[0] > 0 ? 3 : SwingIdentifier[0] < 0 ? -3 : 0;
			int legValue = (SwingIdentifier[0] == 3 || SwingIdentifier[0] == -1) ? 1 : (SwingIdentifier[0] == -3 || SwingIdentifier[0] == 1) ? -1 : 0;

			return trendValue + swingValue + legValue;
		}
		#endregion

		#region setHistoricalValues()
		private void setHistoricalValues()
		{
			TrendValues[0] 	= Trend == TrendDirection.Bullish ? 1 : Trend == TrendDirection.Bearish ? -1 : 0;
			SwingValues[0] 	= SwingIdentifier[0] > 0 ? 1 : SwingIdentifier[0] < 0 ? -1 : 0;
			LegValues[0] 	= (SwingIdentifier[0] == 3 || SwingIdentifier[0] == -1) ? 1 : (SwingIdentifier[0] == -3 || SwingIdentifier[0] == 1) ? -1 : 0;

			TrendStarts[0] 	= TrendValues[0]	== TrendValues[1]	? TrendStarts[1]	: CurrentBar;
			SwingStarts[0] 	= SwingValues[0] 	== SwingValues[1] 	? SwingStarts[1] 	: CurrentBar;
			LegStarts[0] 	= LegValues[0] 		== LegValues[1] 	? LegStarts[1] 		: CurrentBar;
		}
		#endregion

		#region bullishTrendEnded()
		private bool bullishTrendEnded()
		{
			if (Trend != TrendDirection.Bullish) {
				return false;
			}

			if (Low[0] > BreakoutExtreme) {
				return false;
			}

			return SwingIdentifier[0] < 3;
		}
		#endregion

		#region bearishTrendEnded()
		private bool bearishTrendEnded()
		{
			if (Trend != TrendDirection.Bearish) {
				return false;
			}

			if (High[0] < BreakoutExtreme) {
				return false;
			}

			return SwingIdentifier[0] > -3;
		}
		#endregion

		#region endTrend()
		private void endTrend()
		{
			Trend = TrendDirection.Flat;
			BreakoutExtreme = 0;
			TrendStart = 0;
		}
		#endregion

		#region checkForBullishTrend()
		private void checkForBullishTrend()
		{
			if (SwingIdentifier[0] < 0) {
				BarsInBullTrend = 0;
				return;
			}

			BarsInBullTrend++;

			if (Trend == TrendDirection.Bullish) {
				return;
			}
			if (BarsInBullTrend > MinTrendBars) {
				newBullTrend();
			}
		}
		#endregion

		#region checkForBearishTrend()
		private void checkForBearishTrend()
		{
			if (SwingIdentifier[0] > 0) {
				BarsInBearTrend = 0;
				return;
			}

			BarsInBearTrend++;

			if (Trend == TrendDirection.Bearish) {
				return;
			}

			if (BarsInBearTrend > MinTrendBars) {
				newBearTrend();
			}
		}
		#endregion

		#region newBullTrend()
		private void newBullTrend()
		{
			BreakoutExtreme = MIN(Low, BarsInBullTrend + 1)[0];
			Trend = TrendDirection.Bullish;
			BarsInTrend = BarsInBullTrend;
		}
		#endregion

		#region newBearTrend()
		private void newBearTrend()
		{
			BreakoutExtreme = MAX(High, BarsInBearTrend + 1)[0];
			Trend = TrendDirection.Bearish;
			BarsInTrend = BarsInBearTrend;
		}
		#endregion

		#region GetLegsInSwing()
		public int GetLegsInSwing()
		{
			return (int) Values[1][0];
		}
		#endregion

		#region setLegsInSwing()
		public void setLegsInSwing()
		{
			int swingStartBarsAgo = Math.Max(CurrentBar - (int) SwingStarts[0], 1);

			int legs = 0;
			double currentLeg = 0;
			for (int i = 0; i < swingStartBarsAgo; i++) {
				if (LegValues[i] != currentLeg) {
					currentLeg = LegValues[i];

					if (currentLeg == SwingValues[0]) {
						legs++;
					}
				}
			}

			Values[1][0] = legs;
		}
		#endregion

		#region PullbacksInLeg()
		public int PullbacksInLeg()
		{
			if (LegValues[0] > 0) {
				return PA.NumberOfBullPullbacks(0, CurrentBar - (int) LegStarts[0]);
			}

			return PA.NumberOfBearPullbacks(0, CurrentBar - (int) LegStarts[0]);
		}
		#endregion

		#region AveragePullbackLength()
		public double AveragePullbackLength()
		{
			if (LegValues[0] > 0) {
				return PA.AverageBarsInBullTrendPullback(0, CurrentBar - (int) LegStarts[0]);
			}

			return PA.AverageBarsInBearTrendPullback(0, CurrentBar - (int) LegStarts[0]);
		}
		#endregion

		#region isBreakout()
		public bool isBreakout()
		{
			return PullbacksInLeg() <= 1 && AveragePullbackLength() <= 2 && (CurrentBar - (int) LegStarts[0]) >= 4;
		}
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private PR.Trends[] cacheTrends;
		public PR.Trends Trends()
		{
			return Trends(Input);
		}

		public PR.Trends Trends(ISeries<double> input)
		{
			if (cacheTrends != null)
				for (int idx = 0; idx < cacheTrends.Length; idx++)
					if (cacheTrends[idx] != null &&  cacheTrends[idx].EqualsInput(input))
						return cacheTrends[idx];
			return CacheIndicator<PR.Trends>(new PR.Trends(), input, ref cacheTrends);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.PR.Trends Trends()
		{
			return indicator.Trends(Input);
		}

		public Indicators.PR.Trends Trends(ISeries<double> input )
		{
			return indicator.Trends(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.PR.Trends Trends()
		{
			return indicator.Trends(Input);
		}

		public Indicators.PR.Trends Trends(ISeries<double> input )
		{
			return indicator.Trends(input);
		}
	}
}

#endregion
