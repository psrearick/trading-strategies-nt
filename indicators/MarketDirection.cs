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
	public class MarketDirection : Indicator
	{
		#region Variables
		private PriceActionUtils PA;
		private Legs LegShort;
		private Legs LegLong;
		private Series<TrendDirection> Direction;
		private Brush brushUp1;
		private Brush brushUp2;
		private Brush brushDown1;
		private Brush brushDown2;
		private bool hasBullishTrend;
		private bool hasBearishTrend;
		private Ray bullishTrendline;
		private Ray bullishTrendChannelLine;
		private Ray bearishTrendline;
		private Ray bearishTrendChannelLine;
		#endregion

		#region OnStateChange()
		protected override void OnStateChange()
		{
			#region State.SetDefaults
			if (State == State.SetDefaults)
			{
				Description									= @"";
				Name										= "Market Direction";
				Calculate									= Calculate.OnBarClose;
				IsOverlay									= true;
				DisplayInDataBox							= true;
				DrawOnPricePanel							= true;
				DrawHorizontalGridLines						= true;
				DrawVerticalGridLines						= true;
				PaintPriceMarkers							= true;
				ScaleJustification							= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				IsSuspendedWhileInactive					= true;

//				brushUp1 = Brushes.Green.Clone();
//				brushUp1.Opacity = 1.000;
//				brushUp1.Freeze();

//				brushUp2 = Brushes.Green.Clone();
//				brushUp2.Opacity = 0.600;
//				brushUp2.Freeze();

//				brushDown1 = Brushes.Red.Clone();
//				brushDown1.Opacity = 1.000;
//				brushDown1.Freeze();

//				brushDown2 = Brushes.Red.Clone();
//				brushDown2.Opacity = 0.600;
//				brushDown2.Freeze();

				brushUp1 = Brushes.Green.Clone();
				brushUp1.Opacity = 0.500;
				brushUp1.Freeze();

				brushUp2 = Brushes.Green.Clone();
				brushUp2.Opacity = 0.250;
				brushUp2.Freeze();

				brushDown1 = Brushes.Red.Clone();
				brushDown1.Opacity = 0.500;
				brushDown1.Freeze();

				brushDown2 = Brushes.Red.Clone();
				brushDown2.Opacity = 0.250;
				brushDown2.Freeze();
			}
			#endregion

			#region State.Configure
			else if (State == State.Configure)
			{
				LegShort	= Legs(6);
				LegLong		= Legs(16);
			}
			#endregion

			#region State.DataLoaded
			else if (State == State.DataLoaded)
			{
				Direction 	= new Series<TrendDirection>(this, MaximumBarsLookBack.Infinite);
			}
			#endregion
		}
		#endregion

		#region OnBarUpdate()
		protected override void OnBarUpdate()
		{
			if (CurrentBar < 81) {
				Direction[0] = TrendDirection.Flat;
				return;
			}

			BackBrush = null;

//			double stdDev 		= LegShort.LegLengthStandardDeviations[0];
//			double StdDevSMA	= SMA(LegShort.LegLengthStandardDeviations, 9)[0];
//			double legLengthAvg	= LegShort.AverageLegLengths[0];
//			double legLengthSMA = SMA(LegShort.AverageLegLengths, 9)[0];

			double stdDev 		= LegLong.LegLengthStandardDeviations[0];
			double StdDevSMA	= SMA(LegLong.LegLengthStandardDeviations, 9)[0];
			double legLengthAvg	= LegLong.AverageLegLengths[0];
			double legLengthSMA = SMA(LegLong.AverageLegLengths, 9)[0];
			double legDir		= LegLong.LegDirectionRatios[0];

			bool tightChannelLegs	= (stdDev < StdDevSMA) && (legLengthAvg > legLengthSMA);
			bool broadChannelLegs	= (stdDev >= StdDevSMA) && (legLengthAvg > legLengthSMA);
			bool longerUpLegs		= legDir > 0.5;
			bool longerDownLegs		= legDir <= 0.5;



//			Print(LegLong.LegBars[0]);
//			Print(LegLong.LegDirectionRatios[0]);
//			Print(LegLong.AverageLegLengths[0]);
//			Print(SMA(LegLong.AverageLegLengths, 81)[0]);
//			Print(LegLong.LegLengthStandardDeviations[0]);
//			Print(SMA(LegLong.LegLengthStandardDeviations, 81)[0]);
//			Print("==========");

//			int currentTrendLength	= LegShort.BarsAgoStarts[0];
//			int lastBarOfTrend		= currentTrendLength + 1;
//			int previousTrendLength	= LegShort.BarsAgoStarts[lastBarOfTrend];
//			int firstBarOfTrend		= currentTrendLength + previousTrendLength;

//			for (int i = lastBarOfTrend; i <= firstBarOfTrend; i++) {
//				Direction[i] 							= LegShort.LegDirectionAtBar(i);
//			}

//			if (Leg.Starts[0] != Leg.Starts[1] && Direction[firstBarOfTrend] != TrendDirection.Flat) {
//				Brush TrendBrush 			= Direction[firstBarOfTrend] == TrendDirection.Bullish ? brushUp1 : brushDown1;
//				Brush TrendChannelBrush 	= Direction[firstBarOfTrend] == TrendDirection.Bullish ? brushUp2 : brushDown2;
//				double startYHigh 			= High[firstBarOfTrend];
//				double endYHigh 			= High[lastBarOfTrend];
//				double startYLow			= Low[firstBarOfTrend];
//				double endYLow				= Low[lastBarOfTrend];

//				if (Direction[firstBarOfTrend] == TrendDirection.Bullish && hasBullishTrend) {
//					double previousStartYHigh	= bullishTrendChannelLine.StartAnchor.Price;
//					double previousStartYLow	= bullishTrendline.StartAnchor.Price;

//					if (previousStartYHigh < startYHigh && previousStartYLow < startYLow) {
//						bullishTrendChannelLine.EndAnchor.Price	= endYHigh;
//						bullishTrendline.StartAnchor.Price		= endYLow;

//						return;
//					}
//				} else if (Direction[firstBarOfTrend] == TrendDirection.Bullish) {

//					double trendStartY 			= Direction[firstBarOfTrend] == TrendDirection.Bullish ? startYLow : startYHigh;
//					double trendEndY 			= Direction[firstBarOfTrend] == TrendDirection.Bullish ? endYLow : endYHigh;
//					double trendChannelStartY 	= Direction[firstBarOfTrend] == TrendDirection.Bullish ? startYHigh : startYLow;
//					double trendChannelEndY		= Direction[firstBarOfTrend] == TrendDirection.Bullish ? endYHigh : endYLow;

//					bullishTrendline 		= Draw.Ray(this, "rayTrend" + (CurrentBar - lastBarOfTrend).ToString(), firstBarOfTrend, trendStartY, lastBarOfTrend, trendEndY, TrendBrush);
//					bullishTrendChannelLine = Draw.Ray(this, "rayTrendChannel" + (CurrentBar - lastBarOfTrend).ToString(), firstBarOfTrend, trendChannelStartY, lastBarOfTrend, trendChannelEndY, TrendChannelBrush);
//				} else if (hasBearishTrend) {
//					double previousStartYHigh	= bearishTrendline.StartAnchor.Price;
//					double previousStartYLow	= bearishTrendChannelLine.StartAnchor.Price;

//					if (previousStartYHigh > startYHigh && previousStartYLow > startYLow) {
//						bearishTrendline.StartAnchor.Price			= endYHigh;
//						bearishTrendChannelLine.StartAnchor.Price	= endYLow;

//						return;
//					}
//				} else {
//					double trendStartY 			= Direction[firstBarOfTrend] == TrendDirection.Bullish ? startYLow : startYHigh;
//					double trendEndY 			= Direction[firstBarOfTrend] == TrendDirection.Bullish ? endYLow : endYHigh;
//					double trendChannelStartY 	= Direction[firstBarOfTrend] == TrendDirection.Bullish ? startYHigh : startYLow;
//					double trendChannelEndY		= Direction[firstBarOfTrend] == TrendDirection.Bullish ? endYHigh : endYLow;
//					Draw.Ray(this, "rayTrend" + lastBarOfTrend, firstBarOfTrend, trendStartY, lastBarOfTrend, trendEndY, TrendBrush);
//					Draw.Ray(this, "rayTrendChannel" + lastBarOfTrend, firstBarOfTrend, trendChannelStartY, lastBarOfTrend, trendChannelEndY, TrendChannelBrush);
//				}
//			}
		}
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private PR.MarketDirection[] cacheMarketDirection;
		public PR.MarketDirection MarketDirection()
		{
			return MarketDirection(Input);
		}

		public PR.MarketDirection MarketDirection(ISeries<double> input)
		{
			if (cacheMarketDirection != null)
				for (int idx = 0; idx < cacheMarketDirection.Length; idx++)
					if (cacheMarketDirection[idx] != null &&  cacheMarketDirection[idx].EqualsInput(input))
						return cacheMarketDirection[idx];
			return CacheIndicator<PR.MarketDirection>(new PR.MarketDirection(), input, ref cacheMarketDirection);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.PR.MarketDirection MarketDirection()
		{
			return indicator.MarketDirection(Input);
		}

		public Indicators.PR.MarketDirection MarketDirection(ISeries<double> input )
		{
			return indicator.MarketDirection(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.PR.MarketDirection MarketDirection()
		{
			return indicator.MarketDirection(Input);
		}

		public Indicators.PR.MarketDirection MarketDirection(ISeries<double> input )
		{
			return indicator.MarketDirection(input);
		}
	}
}

#endregion
