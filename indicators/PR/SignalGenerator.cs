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
	public class SignalGenerator : Indicator
	{
		#region Variables
		public PriceActionUtils pa;
		public PriceActionPatterns paPatterns;
		public MarketDirection md;
		public ATR atr;
		public RSI rsi;
		public EMA emaFast;
		public EMA emaSlow;
		public StdDev stdDevAtr;
		public SMA avgAtr;
		public SMA avgAtrFast;
		public MIN minATR;
		public MAX maxATR;
		public Series<int> barsSinceDoubleTop;
		public Series<int> barsSinceDoubleBottom;
//		public List<double> stopLossMultiplier;
//		public List<double> profitTargetMultiplier;
//		private Dictionary<string, double> upperBoundsDict;
//		private Dictionary<string, double> lowerBoundsDict;
//		private double[] upperBounds;
//		private double[] lowerBounds;
		private ObjectPool<ParameterType> parameterTypes;
		public ObjectPool<Parameter> optimizedParameters;
		#endregion

		public IEnumerable<ParameterType> ActiveParameterTypes
		{
		    get { return parameterTypes.ActiveItems; }
		}

		public IEnumerable<Parameter> ActiveOptimizedParameters
		{
		    get { return optimizedParameters.ActiveItems; }
		}

		#region OnStateChange()
		protected override void OnStateChange()
		{
			#region State.SetDefaults
			if (State == State.SetDefaults)
			{
				Description									= @"Generate signals based on optimized parameters";
				Name										= "SignalGenerator";
				Calculate									= Calculate.OnBarClose;
				IsOverlay									= false;
				DisplayInDataBox							= false;
				DrawOnPricePanel							= false;
				ScaleJustification							= NinjaTrader.Gui.Chart.ScaleJustification.Right;
				IsSuspendedWhileInactive					= true;
			}
			#endregion
			#region State.Configure
			else if (State == State.Configure)
			{
				pa 						= PriceActionUtils();
				paPatterns				= PriceActionPatterns();
				md						= MarketDirection(10, 20);
				atr						= ATR(14);
				rsi						= RSI(14, 3);
				emaFast					= EMA(9);
				emaSlow					= EMA(21);

				parameterTypes = new ObjectPool<ParameterType>(50, () => new ParameterType());
				optimizedParameters = new ObjectPool<Parameter>(50, () => new Parameter());
//				SetParameterTypes();
//				stopLossMultiplier		= GenerateRangeOfValues(0.5, 10.0, 0.5);
//				profitTargetMultiplier	= GenerateRangeOfValues(0.5, 10.0, 0.5);
//				upperBoundsDict			= SetUpperBounds();
//				lowerBoundsDict			= SetLowerBounds();
			}
			#endregion
			#region State.DataLoaded
			if (State == State.DataLoaded)
			{
				stdDevAtr				= StdDev(atr, 21);
				avgAtr					= SMA(atr, 21);
				avgAtrFast				= SMA(atr, 9);
				minATR					= MIN(atr, 50);
				maxATR					= MAX(atr, 50);

				barsSinceDoubleTop		= new Series<int>(this);
				barsSinceDoubleBottom	= new Series<int>(this);

//				upperBounds				= upperBoundsDict.Values.ToArray();
//				lowerBounds				= lowerBoundsDict.Values.ToArray();
//				parameterLabels			= upperBoundsDict.Keys.ToArray();
//				optimizedParameters		= new Dictionary<string, double>(lowerBoundsDict);
			}
			#endregion
		}
		#endregion

		#region OnBarUpdate()
		protected override void OnBarUpdate()
		{
			if (CurrentBar < 200) {
				return;
			}

			UpdateBarsSinceDoubleTopBottom();
		}
		#endregion

		#region UpdateBarsSinceDoubleTopBottom()
		private void UpdateBarsSinceDoubleTopBottom()
		{
			barsSinceDoubleBottom[0] = barsSinceDoubleBottom[1] + 1;
			if (paPatterns.IsDoubleBottom(0, 30, 3)) {
				barsSinceDoubleBottom[0] = 0;
			}

			barsSinceDoubleTop[0] = barsSinceDoubleTop[1] + 1;
			if (paPatterns.IsDoubleTop(0, 30, 3)) {
				barsSinceDoubleTop[0] = 0;
			}
		}
		#endregion

//		#region SetParameterTypes
//		private void SetParameterTypes()
//		{}
//		#endregion

//		#region SetUpperBounds()
//		private Dictionary<string, double> SetUpperBounds()
//		{
//			Dictionary<string, double> bounds = new Dictionary<string, double>();
//			return bounds;
//		}
//		#endregion

//		#region SetLowerBounds()
//		private Dictionary<string, double> SetLowerBounds()
//		{
//			Dictionary<string, double> bounds = new Dictionary<string, double>();
//			return bounds;
//		}
//		#endregion

		#region OptimizeParameters()
		public void OptimizeParameters()
		{
		    int numParticles = 50;
		    int maxIterations = 100;

			Func<double[], double> fitnessFunction = CalculateFitness;

			double[] lowerBounds = ActiveParameterTypes.Select(p => p.LowerBound).ToArray();
			double[] upperBounds = ActiveParameterTypes.Select(p => p.UpperBound).ToArray();
			string[] names = ActiveParameterTypes.Select(p => p.Name).ToArray();

		    double[] bestPosition = ParticleSwarmOptimization.Optimize(fitnessFunction, lowerBounds, upperBounds, numParticles, maxIterations);

			foreach (var optimized in optimizedParameters.ActiveItems) {
				optimizedParameters.Release(optimized);
			}

			for (int i = 0; i < bestPosition.Count(); i++) {
				Parameter optimized = optimizedParameters.Get();
				optimized.Set(ActiveParameterTypes.First(p => p.Name == names[i]), bestPosition[i]);
			}
		}
		#endregion

		#region CalculateFitness()
		private double CalculateFitness(double[] position)
		{
//		    double windowSize = position[0];
//		    double stopLossMultiplier = position[1];
//		    double stopLossLimitMultiplier = position[2];
//		    double takeProfitMultiplier = position[3];
//		    double entryThreshold = position[4];

		    double fitnessScore = 0;

//			List<PerformanceData> trades = livePerformanceData.Where(p => p.IsEnabled).ToList();

//		    foreach (var trade in trades)
//		    {
//		        // Calculate scores for each parameter based on their proximity to the historical values
//		        double windowSizeScore = 1.0 - Math.Abs(trade.WindowSize - windowSize) / (upperBounds[0] - lowerBounds[0]);
//		        double stopLossMultiplierScore = 1.0 - Math.Abs(trade.StopLossMultiplier - stopLossMultiplier) / (upperBounds[1] - lowerBounds[1]);
//		        double stopLossLimitMultiplierScore = 1.0 - Math.Abs(trade.StopLossLimitMultiplier - stopLossLimitMultiplier) / (upperBounds[2] - lowerBounds[2]);
//		        double takeProfitMultiplierScore = 1.0 - Math.Abs(trade.TakeProfitMultiplier - takeProfitMultiplier) / (upperBounds[3] - lowerBounds[3]);
//		        double entryThresholdScore = 1.0 - Math.Abs(trade.EntryThreshold - entryThreshold) / (upperBounds[4] - lowerBounds[4]);

//		        // Calculate performance metrics for the trade
//		        double netProfitScore = trade.NetProfit;
//		        double maxAdverseExcursionScore = 1.0 - trade.MaxAdverseExcursion / trade.ATR;
//		        double tradeDurationScore = 1.0 - trade.TradeDuration / (24 * 60); // Assuming trade duration is in minutes

//		        // Combine the scores and performance metrics into a single fitness
//				double tradeScore = windowSizeScore + stopLossMultiplierScore + stopLossLimitMultiplierScore + takeProfitMultiplierScore + entryThresholdScore;
//        		tradeScore += netProfitScore + maxAdverseExcursionScore + tradeDurationScore;

//		        fitnessScore += tradeScore;
//		    }

//		    // Normalize the fitness score based on the number of trades
//		    fitnessScore /= trades.Count;

		    return fitnessScore;
		}
		#endregion
	}

	public class SimTrade
	{
		#region Variables
		public Signal EntrySignal;
		public Signal ExitSignal;
		public TradePerformance Performance = new TradePerformance();
		public Indicator Source;
		#endregion

		#region SimTrade()
		public SimTrade(Indicator indicator)
		{
			Source = indicator;
		}
		#endregion

		#region Enter()
		public void Enter(TrendDirection direction)
		{
			EntrySignal = new Signal(Source, SignalType.Entry);
			EntrySignal.Set(direction);
		}
		#endregion

		#region Exit()
		public void Exit()
		{
			ExitSignal = new Signal(Source, EntrySignal.Type);
			ExitSignal.Set(EntrySignal.Direction);
		}
		#endregion

		#region CalculatePerformance()
		public void CalculatePerformance()
		{
			Performance.Calculate(EntrySignal, ExitSignal);
		}
		#endregion
	}

	public class Signal
	{
		#region Variables
		public List<Parameter> Parameters = new List<Parameter>();
		public Dictionary<string, double> Conditions = new Dictionary<string, double>();
		public SignalType Type;
		public DateTime Time;
		public int Bar;
		public Indicator Source;
		public TrendDirection Direction;
		public double Price;
		#endregion

		#region Signal()
		public Signal(Indicator indicator, SignalType type)
		{
			Source = indicator;
			Type = type;
		}
		#endregion

		#region Set()
		public void Set(TrendDirection direction)
		{
			Direction = direction;
			Time = Source.Time[0];
			Bar = Source.CurrentBar;
			Price = Source.Close[0];
		}
		#endregion
	}

	public class TradePerformance
	{
		#region Variables
		public int BarsInTrade { get; set; }
	    public double MaxAdverseExcursion { get; set; }
	    public double MaxFavorableExcursion { get; set; }
		public double NetProfit { get; set; }
	    public int TradeDuration { get; set; }
		#endregion

		#region Calculate()
		public void Calculate(Signal entry, Signal exit)
		{
			BarsInTrade = exit.Bar - entry.Bar;
			int barsAgoEntry = entry.Source.CurrentBar - entry.Bar;
			double highestHigh = entry.Source.MAX(entry.Source.High, BarsInTrade)[barsAgoEntry];
			double lowestLow = entry.Source.MIN(entry.Source.Low, BarsInTrade)[barsAgoEntry];
			MaxAdverseExcursion = entry.Direction == TrendDirection.Bullish ? lowestLow : highestHigh;
			MaxFavorableExcursion = entry.Direction == TrendDirection.Bullish ? highestHigh : lowestLow;
			NetProfit = entry.Direction == TrendDirection.Bullish ? exit.Price - entry.Price : entry.Price - exit.Price;
			TradeDuration = (exit.Time - entry.Time).Seconds;
		}
		#endregion
	}

	public class Parameter : IPoolable
	{
		#region Variables
		public ParameterType Type;
		public double Value;
		public bool IsActive { get; set; }
		#endregion

		#region Manage Poolable
	    public void Activate()
	    {
	        IsActive = true;
	    }

		public void Set(ParameterType type, double value)
		{
			Type = type;
			Value = value;
		}

	    public void Deactivate()
	    {
	        IsActive = false;
	    }
		#endregion
	}

	public class ParameterType : IPoolable
	{
		#region Variables
		public string Name;
		public double UpperBound;
		public double LowerBound;
		public double Step;
		public double[] Values;
		public bool IsActive { get; set; }
		#endregion

		#region Manage Poolable
	    public void Activate()
	    {
	        IsActive = true;
	    }

		public void Set(string name, double upperBound, double lowerBound, double step)
		{
			Name = name;
			UpperBound = upperBound;
			LowerBound = lowerBound;
			Step = step;
			Values = Helpers.GenerateRangeOfValues(lowerBound, upperBound, step).ToArray();
		}

	    public void Deactivate()
	    {
	        IsActive = false;
	    }
		#endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.

namespace NinjaTrader.NinjaScript.Indicators
{
	public partial class Indicator : NinjaTrader.Gui.NinjaScript.IndicatorRenderBase
	{
		private PR.SignalGenerator[] cacheSignalGenerator;
		public PR.SignalGenerator SignalGenerator()
		{
			return SignalGenerator(Input);
		}

		public PR.SignalGenerator SignalGenerator(ISeries<double> input)
		{
			if (cacheSignalGenerator != null)
				for (int idx = 0; idx < cacheSignalGenerator.Length; idx++)
					if (cacheSignalGenerator[idx] != null &&  cacheSignalGenerator[idx].EqualsInput(input))
						return cacheSignalGenerator[idx];
			return CacheIndicator<PR.SignalGenerator>(new PR.SignalGenerator(), input, ref cacheSignalGenerator);
		}
	}
}

namespace NinjaTrader.NinjaScript.MarketAnalyzerColumns
{
	public partial class MarketAnalyzerColumn : MarketAnalyzerColumnBase
	{
		public Indicators.PR.SignalGenerator SignalGenerator()
		{
			return indicator.SignalGenerator(Input);
		}

		public Indicators.PR.SignalGenerator SignalGenerator(ISeries<double> input )
		{
			return indicator.SignalGenerator(input);
		}
	}
}

namespace NinjaTrader.NinjaScript.Strategies
{
	public partial class Strategy : NinjaTrader.Gui.NinjaScript.StrategyRenderBase
	{
		public Indicators.PR.SignalGenerator SignalGenerator()
		{
			return indicator.SignalGenerator(Input);
		}

		public Indicators.PR.SignalGenerator SignalGenerator(ISeries<double> input )
		{
			return indicator.SignalGenerator(input);
		}
	}
}

#endregion
