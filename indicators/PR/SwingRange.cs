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
	#region Enums
		#region Timetrame
		public enum ChopLevel {
			Low,
			Mid,
			High
		};
		#endregion
		#endregion

	public class SwingRange : Indicator
	{
		#region Variables
		private ChoppinessIndex chop;
		private ATR atr;

		private Dictionary<ChopLevel, List<int>> chopBarCounts = new Dictionary<ChopLevel, List<int>>()
		{
			[ChopLevel.Low] = new List<int>(),
			[ChopLevel.Mid] = new List<int>(),
			[ChopLevel.High] = new List<int>()
		};

		private Dictionary<ChopLevel, List<double>> chopATRs = new Dictionary<ChopLevel, List<double>>()
		{
			[ChopLevel.Low] = new List<double>(),
			[ChopLevel.Mid] = new List<double>(),
			[ChopLevel.High] = new List<double>()
		};

		private int barCount = 0;

		public Series<ChopLevel> Choppiness;
		#endregion

		#region OnStateChange()
		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"Provides the average size of a price swing.";
				Name										= "Swing Range";
				Calculate									= Calculate.OnBarClose;
				IsOverlay									= true;
				DisplayInDataBox							= true;
				DrawOnPricePanel							= true;
				DrawHorizontalGridLines						= true;
				DrawVerticalGridLines						= true;
				PaintPriceMarkers							= true;
				ScaleJustification							= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				IsSuspendedWhileInactive					= true;

				Period = 14;
				Lookback = 28;

				AddPlot(Brushes.Red, "Upper");
				AddPlot(Brushes.Blue, "Middle");
				AddPlot(Brushes.Red, "Lower");
			}

			if (State == State.Configure)
			{
				chop = ChoppinessIndex(Period);
				atr = ATR(Period);
				Choppiness = new Series<ChopLevel>(this, MaximumBarsLookBack.Infinite);
			}
		}
		#endregion

		#region OnBarUpdate()
		protected override void OnBarUpdate()
		{
			if (CurrentBar < Period)
			{
				Choppiness[0] = ChopLevel.Mid;
				return;
			}

			DetermineChoppiness();
			UpdateBarCount();
			UpdateChopATRs();

			Middle[0] = (High[0] + Low[0]) / 2;

			double distance = chopATRs[Choppiness[0]].Count > 0
				? chopATRs[Choppiness[0]].Average()
				: atr[0] * barCount;

			Upper[0] = Middle[0] + distance;
			Lower[0] = Middle[0] - distance;
		}
		#endregion

		#region UpdateChopATRs()
		private void UpdateChopATRs()
		{
			if (Choppiness[0] == Choppiness[1])
			{
				return;
			}

			int period = chopBarCounts[Choppiness[0]].Count > 0 ? (int)Math.Round(chopBarCounts[Choppiness[0]].Average()) : barCount;

			double range = MAX(High, period)[period + 1] - MIN(Low, period)[period + 1];

			chopATRs[Choppiness[0]].Add(range);
		}
		#endregion

		#region UpdateBarCount()
		private void UpdateBarCount()
		{
			if (barCount == 0)
			{
				barCount++;
				return;
			}

			if (Choppiness[0] != Choppiness[1])
			{
				if (barCount > 4)
					chopBarCounts[Choppiness[0]].Add(barCount);

				barCount = 0;
			}

			if (chopBarCounts[Choppiness[0]].Count > Lookback)
			{
				chopBarCounts[Choppiness[0]].RemoveAt(0);
			}

			barCount++;
		}
		#endregion

		#region DetermineChoppiness()
		private void DetermineChoppiness()
		{
			if (chop[0] > 60)
			{
				Choppiness[0] = ChopLevel.High;
				return;
			}

			if (chop[0] < 40)
			{
				Choppiness[0] = ChopLevel.Low;
				return;
			}

			Choppiness[0] = ChopLevel.Mid;
		}
		#endregion

		#region Properties
		[Browsable(false)]
		[XmlIgnore()]
		public Series<double> Lower
		{
			get { return Values[2]; }
		}

		[Browsable(false)]
		[XmlIgnore()]
		public Series<double> Middle
		{
			get { return Values[1]; }
		}

		[Browsable(false)]
		[XmlIgnore()]
		public Series<double> Upper
		{
			get { return Values[0]; }
		}

		[Range(2, int.MaxValue), NinjaScriptProperty]
		[Display(Name = "Period", GroupName = "Parameters", Order = 0)]
		public int Period
		{ get; set; }

		[Range(2, int.MaxValue), NinjaScriptProperty]
		[Display(Name = "Lookback", GroupName = "Parameters", Order = 1)]
		public int Lookback
		{ get; set; }
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private PR.SwingRange[] cacheSwingRange;
		public PR.SwingRange SwingRange(int period, int lookback)
		{
			return SwingRange(Input, period, lookback);
		}

		public PR.SwingRange SwingRange(ISeries<double> input, int period, int lookback)
		{
			if (cacheSwingRange != null)
				for (int idx = 0; idx < cacheSwingRange.Length; idx++)
					if (cacheSwingRange[idx] != null && cacheSwingRange[idx].Period == period && cacheSwingRange[idx].Lookback == lookback && cacheSwingRange[idx].EqualsInput(input))
						return cacheSwingRange[idx];
			return CacheIndicator<PR.SwingRange>(new PR.SwingRange(){ Period = period, Lookback = lookback }, input, ref cacheSwingRange);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.PR.SwingRange SwingRange(int period, int lookback)
		{
			return indicator.SwingRange(Input, period, lookback);
		}

		public Indicators.PR.SwingRange SwingRange(ISeries<double> input , int period, int lookback)
		{
			return indicator.SwingRange(input, period, lookback);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.PR.SwingRange SwingRange(int period, int lookback)
		{
			return indicator.SwingRange(Input, period, lookback);
		}

		public Indicators.PR.SwingRange SwingRange(ISeries<double> input , int period, int lookback)
		{
			return indicator.SwingRange(input, period, lookback);
		}
	}
}

#endregion
