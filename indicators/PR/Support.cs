#region Using declarations
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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
	#region MarketCycleStage
	public enum MarketCycleStage {
		Breakout,
		TightChannel,
		BroadChannel,
		TradingRange
	};
	#endregion

	#region TrendDirection
	public enum TrendDirection {
		Bearish,
		Bullish,
		Flat
	};
	#endregion
	#endregion

	public class Utils : Indicator
	{
		public TrendDirection DirectionFromInt(int direction)
		{
			return direction > 0 ? TrendDirection.Bullish : direction < 0 ? TrendDirection.Bearish : TrendDirection.Flat;
		}

		public void PrintMessage(string message = "",
		        [CallerMemberName] string memberName = "",
		        [CallerFilePath] string sourceFilePath = "",
		        [CallerLineNumber] int sourceLineNumber = 0)
		{
			string meta = "[" + sourceLineNumber + "] " + memberName + " (" + sourceFilePath + ") || ";
		    Print(meta + message);
		}

		public void PrintMessage(double message,
		        [CallerMemberName] string memberName = "",
		        [CallerFilePath] string sourceFilePath = "",
		        [CallerLineNumber] int sourceLineNumber = 0)
		{
			PrintMessage(message.ToString(), memberName, sourceFilePath, sourceLineNumber);
		}

		public void PrintMessage(int message,
		        [CallerMemberName] string memberName = "",
		        [CallerFilePath] string sourceFilePath = "",
		        [CallerLineNumber] int sourceLineNumber = 0)
		{
			PrintMessage(message.ToString(), memberName, sourceFilePath, sourceLineNumber);
		}
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private PR.Utils[] cacheUtils;
		public PR.Utils Utils()
		{
			return Utils(Input);
		}

		public PR.Utils Utils(ISeries<double> input)
		{
			if (cacheUtils != null)
				for (int idx = 0; idx < cacheUtils.Length; idx++)
					if (cacheUtils[idx] != null &&  cacheUtils[idx].EqualsInput(input))
						return cacheUtils[idx];
			return CacheIndicator<PR.Utils>(new PR.Utils(), input, ref cacheUtils);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.PR.Utils Utils()
		{
			return indicator.Utils(Input);
		}

		public Indicators.PR.Utils Utils(ISeries<double> input )
		{
			return indicator.Utils(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.PR.Utils Utils()
		{
			return indicator.Utils(Input);
		}

		public Indicators.PR.Utils Utils(ISeries<double> input )
		{
			return indicator.Utils(input);
		}
	}
}

#endregion
