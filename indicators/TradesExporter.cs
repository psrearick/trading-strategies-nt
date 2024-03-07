#region Using declarations
using System;
using System.IO;
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
	public class TradesExporter : Indicator
	{
		private string path;
		private StreamWriter sw;
		private bool writeHeaders;
		private ISet<int> tradesTracking;
		private List<string> tradeStrings;

		private DateTime startTime = DateTime.MaxValue;
		private DateTime endTime;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Description									= @"This script saves every trade made by a strategy to a file for further analysis with external tools like Excel.";
				Name										= "TradesExporter";
				Calculate									= Calculate.OnBarClose;
				IsOverlay									= false;
				DisplayInDataBox							= false;
				DrawOnPricePanel							= false;
				DrawHorizontalGridLines						= false;
				DrawVerticalGridLines						= false;
				PaintPriceMarkers							= false;
				ScaleJustification							= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				IsSuspendedWhileInactive					= true;
			}
			else if (State == State.Configure)
			{
				string dateTimeNow = DateTime.UtcNow.ToString("yyyyMMddHHmmssffffff", System.Globalization.CultureInfo.InvariantCulture);
				string fileName = StrategyName + "_" + InstrumentName + "_" + dateTimeNow + randStr() + "_trades.csv";
				path = NinjaTrader.Core.Globals.UserDataDir + "\\tradeExports\\" + fileName;
				tradesTracking = new HashSet<int>();
				tradeStrings = new List<string>();
				writeHeaders = true;
			}
			else if (State == State.Terminated)
			{
				writeToFile();
			}
		}

		private void writeToFile()
		{
			if (tradeStrings.Count == 0) {
				return;
			}

			bool fileOpened = false;
		    int retries = 5;
		    while (!fileOpened && retries > 0)
		    {
		        try
		        {
		            sw = File.AppendText(path);
		            fileOpened = true;
		        }
		        catch (IOException)
		        {
					System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer {Interval = 500};
				    timer.Tick += delegate
				    {
				        timer.Stop();
		            	retries--;
				        timer.Dispose();
				    };

				    timer.Start();
		        }
		    }

		    if (fileOpened && sw != null)
		    {
//				sw.WriteLine(
//					"Trade Number, " +
//					"Instrument, " +
//					"Entry Time, " +
//					"Exit Time, " +
//					"Entry Price, " +
//					"Entry Slippage, " +
//					"Exit Price, " +
//					"Exit Slippage, " +
//					"PnL, " +
//					"Quantity, " +
//					"Direction, " +
//					"Commission," +
//					"Strategy Start Time, " +
//					"Strategy End Time"
//				);

				for (int i = 0; i < tradeStrings.Count; i++) {
					sw.WriteLine(tradeStrings[i]  + "," +
						startTime.ToLocalTime() + "," +
						endTime.ToLocalTime());
				}

				sw.Close();
				sw.Dispose();
				sw = null;
				tradeStrings = new List<string>();
				tradesTracking = new HashSet<int>();
			}
		}

		private string randStr() {
			string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
			var stringChars = new char[8];
			Random random = new Random();

			for (int i = 0; i < stringChars.Length; i++)
			{
			    stringChars[i] = chars[random.Next(chars.Length)];
			}

			return new String(stringChars);
		}

		protected override void OnBarUpdate()
		{
			if (Time[0] < startTime) {
				startTime = Time[0];
			}

			endTime = Time[0];
		}

		public void OnNewTrade(Trade trade) {
			if (tradesTracking.Contains(trade.TradeNumber)) {
				return;
			}

			tradesTracking.Add(trade.TradeNumber);

			tradeStrings.Add(
				trade.TradeNumber + "," +
				trade.Entry.Instrument + "," +
				trade.Entry.Time + "," +
				trade.Exit.Time + "," +
				trade.Entry.Price + "," +
				trade.Entry.Slippage + "," +
				trade.Exit.Price + "," +
				trade.Exit.Slippage + "," +
				trade.ProfitCurrency + "," +
				trade.Quantity + "," +
				trade.Entry.MarketPosition + "," +
				trade.Commission
			);
		}

		#region Properties
		[NinjaScriptProperty]
		[Display(Name="Strategy Name", Order=1, GroupName="Parameters")]
		public string StrategyName
		{ get; set; }

		[NinjaScriptProperty]
		[Display(Name="Instrument Name", Order=2, GroupName="Parameters")]
		public string InstrumentName
		{ get; set; }
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private TradesExporter[] cacheTradesExporter;
		public TradesExporter TradesExporter(string strategyName, string instrumentName)
		{
			return TradesExporter(Input, strategyName, instrumentName);
		}

		public TradesExporter TradesExporter(ISeries<double> input, string strategyName, string instrumentName)
		{
			if (cacheTradesExporter != null)
				for (int idx = 0; idx < cacheTradesExporter.Length; idx++)
					if (cacheTradesExporter[idx] != null && cacheTradesExporter[idx].StrategyName == strategyName && cacheTradesExporter[idx].InstrumentName == instrumentName && cacheTradesExporter[idx].EqualsInput(input))
						return cacheTradesExporter[idx];
			return CacheIndicator<TradesExporter>(new TradesExporter(){ StrategyName = strategyName, InstrumentName = instrumentName }, input, ref cacheTradesExporter);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.TradesExporter TradesExporter(string strategyName, string instrumentName)
		{
			return indicator.TradesExporter(Input, strategyName, instrumentName);
		}

		public Indicators.TradesExporter TradesExporter(ISeries<double> input , string strategyName, string instrumentName)
		{
			return indicator.TradesExporter(input, strategyName, instrumentName);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.TradesExporter TradesExporter(string strategyName, string instrumentName)
		{
			return indicator.TradesExporter(Input, strategyName, instrumentName);
		}

		public Indicators.TradesExporter TradesExporter(ISeries<double> input , string strategyName, string instrumentName)
		{
			return indicator.TradesExporter(input, strategyName, instrumentName);
		}
	}
}

#endregion
