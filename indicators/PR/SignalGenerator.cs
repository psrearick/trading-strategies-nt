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

		private Utils utils = new Utils();
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
		private List<Condition> entryConditions = new List<Condition>();
		private List<ExitCondition> exitConditions = new List<ExitCondition>();
		public List<List<Condition>> optimalEntryCombinations = new List<List<Condition>>();
		public List<List<ExitCondition>> optimalExitCombinations = new List<List<ExitCondition>>();
		private ObjectPool<Signal> entrySignals;
		private ObjectPool<Signal> exitSignals;
		private ObjectPool<SimTrade> trades;
		private ObjectPool<ParameterType> parameterTypes;

		public IEnumerable<Signal> ActiveEntrySignals
		{
			get { return entrySignals.ActiveItems; }
		}

		public IEnumerable<Signal> ActiveExitSignals
		{
			get { return exitSignals.ActiveItems; }
		}

		public IEnumerable<SimTrade> ActiveTrades
		{
			get { return trades.ActiveItems; }
		}

		public IEnumerable<ParameterType> ActiveParameterTypes
		{
		    get { return parameterTypes.ActiveItems; }
		}

		#endregion

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

				parameterTypes = new ObjectPool<ParameterType>(5, () => new ParameterType());
				trades = new ObjectPool<SimTrade>(200, () => new SimTrade());
				exitSignals = new ObjectPool<Signal>(200, () => new Signal());
				entrySignals = new ObjectPool<Signal>(200, () => new Signal());

				optimalEntryCombinations = new List<List<Condition>>();
				optimalExitCombinations = new List<List<ExitCondition>>();

				SetParameterTypes();
				SetConditions();
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
			AnalyzeConditionPerformance();

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

		#region SetParameterTypes()
		private void SetParameterTypes()
		{
			ParameterType NewExtremeLength = parameterTypes.Get();
			NewExtremeLength.Set("NewExtremeLength", 20, 6, 2);

			ParameterType ProfitTargetMultiplier = parameterTypes.Get();
			ProfitTargetMultiplier.Set("ProfitTargetMultiplier", 20, 0.5, 0.5);

			ParameterType StopLossMultiplier = parameterTypes.Get();
			StopLossMultiplier.Set("StopLossMultiplier", 10, 0.5, 0.5);
		}
		#endregion

		#region SetConditions()
		private void SetConditions()
		{
			#region Entry Conditions
			entryConditions.Add(new RSIRangeCondition());
			entryConditions.Add(new AboveAverageATRCondition());
			entryConditions.Add(new BelowAverageATRCondition());
			entryConditions.Add(new AboveAverageATRByAStdDevCondition());
			entryConditions.Add(new BreakoutCondition());
			entryConditions.Add(new BroadChannelCondition());
			entryConditions.Add(new TightChannelCondition());
			entryConditions.Add(new WeakTrendCondition());
			entryConditions.Add(new StrongTrendCondition());
			entryConditions.Add(new WithTrendTrendBarCondition());
			entryConditions.Add(new BreakoutBarPatternCondition());
			entryConditions.Add(new WeakBarCondition());
			entryConditions.Add(new StrongFollowThroughCondition());
			entryConditions.Add(new WithTrendPressureCondition());
			entryConditions.Add(new StrongWithTrendPressureCondition());
			entryConditions.Add(new EMADivergingCondition());
			entryConditions.Add(new EMAConvergingCondition());
			entryConditions.Add(new WithTrendEMACondition());
			entryConditions.Add(new LeadsFastEMAByMoreThanATRCondition());
			entryConditions.Add(new FastEMADirectionCondition());
			entryConditions.Add(new SlowEMADirectionCondition());
			#endregion

			#region Exit Conditions
			exitConditions.Add(new TrendDirectionChangedCondition());
			exitConditions.Add(new CounterTrendTightChannelCondition());
			exitConditions.Add(new CounterBroadTightChannelCondition());
			exitConditions.Add(new CounterTrendBreakoutsCondition());
			exitConditions.Add(new CounterTrendBreakoutTrendCondition());
			exitConditions.Add(new CounterTrendLegLongCondition());
			exitConditions.Add(new CounterTrendLegShortCondition());
			exitConditions.Add(new DoubleTopBottomCondition());
			exitConditions.Add(new CounterTrendLegAfterDoubleTopBottomCondition());
			exitConditions.Add(new TrailingStopBeyondPreviousExtremeCondition());
			exitConditions.Add(new MovingAverageCrossoverCondition());
			exitConditions.Add(new NoNewExtremeCondition());
			exitConditions.Add(new ProfitTargetCondition());
			exitConditions.Add(new StopLossCondition());
			exitConditions.Add(new CounterTrendPressureCondition());
			exitConditions.Add(new CounterTrendWeakTrendCondition());
			exitConditions.Add(new CounterTrendStrongTrendCondition());
			exitConditions.Add(new RSIOutOfRangeCondition());
			exitConditions.Add(new AboveAverageATRExitCondition());
			exitConditions.Add(new BelowAverageATRExitCondition());
			exitConditions.Add(new AboveAverageATRByAStdDevExitCondition());
			exitConditions.Add(new BelowAverageATRByAStdDevExitCondition());
			exitConditions.Add(new StrongCounterTrendFollowThroughCondition());
			#endregion

		}
		#endregion

		#region AnalyzeConditionPerformance()
		private void AnalyzeConditionPerformance()
		{
			TestIndividualConditions();

			if (CurrentBar % 10 != 0) {
				return;
			}

			Dictionary<List<Condition>, PerformanceMetrics> bestEntryCombinations = GetBestEntryConditionCombinations(2, 4, 5);
			Dictionary<List<ExitCondition>, PerformanceMetrics> bestExitCombinations = GetBestExitConditionCombinations(2, 4, 5);

			if (bestEntryCombinations.Count > 0)
			{
				optimalEntryCombinations = SelectOptimalCombinations(bestEntryCombinations);
			}

			if (bestExitCombinations.Count > 0)
			{
	        	optimalExitCombinations = SelectOptimalCombinations(bestExitCombinations);
			}
		}
		#endregion

		#region TestIndividualConditions()
		private void TestIndividualConditions()
		{
		    // Test entry conditions
		    foreach (Condition entryCondition in entryConditions)
		    {
		        if (entryCondition.IsMet(this))
		        {
		            Signal entrySignal = entrySignals.Get();
		            entrySignal.Set(md.Direction[0], this, SignalType.Entry);
		            entrySignal.EntryConditions[entryCondition] = new List<Parameter>();
		        }
		    }

		    // Test exit conditions for each entry signal
		    foreach (Signal entrySignal in entrySignals.ActiveItems)
		    {
		        foreach (ExitCondition exitCondition in exitConditions)
		        {
		            if (exitCondition.ParameterTypes.Count > 0)
		            {
		                // Generate all possible parameter combinations for the exit condition
		                List<List<Parameter>> parameterCombinations = GenerateParameterCombinations(exitCondition.ParameterTypes);
		                foreach (List<Parameter> parameterCombination in parameterCombinations)
		                {
		                    exitCondition.Reset();
		                    foreach (Parameter parameter in parameterCombination)
		                    {
		                        exitCondition.SetParameterValue(parameter.Type, parameter.Value);
		                    }

		                    if (exitCondition.IsMet(this, entrySignal))
		                    {
		                        Signal exitSignal = exitSignals.Get();
		                        exitSignal.Set(entrySignal.Direction, this, SignalType.Exit);
		                        exitSignal.ExitConditions[exitCondition] = parameterCombination;

		                        SimTrade trade = trades.Get();
		                        trade.Set(this);
		                        trade.EntrySignal = entrySignal;
		                        trade.ExitSignal = exitSignal;
		                    }
		                }
		            }
		            else
		            {
		                if (exitCondition.IsMet(this, entrySignal))
		                {
		                    Signal exitSignal = exitSignals.Get();
		                    exitSignal.Set(entrySignal.Direction, this, SignalType.Exit);
		                    exitSignal.ExitConditions[exitCondition] = new List<Parameter>();

		                    SimTrade trade = trades.Get();
		                    trade.Set(this);
		                    trade.EntrySignal = entrySignal;
		                    trade.ExitSignal = exitSignal;
		                }
		            }
		        }
		    }

//			entrySignals.LimitSize(200);
//			exitSignals.LimitSize(200);
		}
		#endregion

		#region GenerateParameterCombinations()
		private List<List<Parameter>> GenerateParameterCombinations(List<ParameterType> parameterTypes)
		{
		    List<List<Parameter>> combinations = new List<List<Parameter>>();
		    GenerateCombinationsHelper(parameterTypes, 0, new List<Parameter>(), combinations);
		    return combinations;
		}

		private void GenerateCombinationsHelper(List<ParameterType> parameterTypes, int depth, List<Parameter> currentCombination, List<List<Parameter>> combinations)
		{
		    if (depth == parameterTypes.Count)
		    {
		        combinations.Add(new List<Parameter>(currentCombination));
		    }
		    else
		    {
		        foreach (double value in parameterTypes[depth].Values)
		        {
		            Parameter parameter = new Parameter();
		            parameter.Set(parameterTypes[depth], value);
		            currentCombination.Add(parameter);
		            GenerateCombinationsHelper(parameterTypes, depth + 1, currentCombination, combinations);
		            currentCombination.RemoveAt(currentCombination.Count - 1);
		        }
		    }
		}
		#endregion

		#region Top Performing Conditions
		#region GetTopPerformingEntryConditions()
		private Dictionary<Condition, PerformanceMetrics> GetTopPerformingEntryConditions()
		{
		    Dictionary<Condition, PerformanceMetrics> entryConditionPerformance = new Dictionary<Condition, PerformanceMetrics>();

		    foreach (SimTrade trade in trades.ActiveItems)
		    {
		        trade.CalculatePerformance();

		        // Update performance metrics for entry conditions
		        foreach (KeyValuePair<Condition, List<Parameter>> entry in trade.EntrySignal.EntryConditions)
		        {
		            Condition entryCondition = entry.Key;
		            if (!entryConditionPerformance.ContainsKey(entryCondition))
		            {
		                entryConditionPerformance[entryCondition] = new PerformanceMetrics();
		            }
		            UpdatePerformanceMetrics(entryConditionPerformance[entryCondition], trade.Performance);
		        }
		    }

		    // Sort entry conditions based on the desired performance metric
		    List<KeyValuePair<Condition, PerformanceMetrics>> sortedEntryConditions = entryConditionPerformance
		        .OrderByDescending(x => x.Value.NetProfit)
		        .ThenByDescending(x => x.Value.MaxFavorableExcursion)
		        .ThenBy(x => x.Value.MaxAdverseExcursion)
		        .ToList();

		    // Return the top performing entry conditions
		    Dictionary<Condition, PerformanceMetrics> topEntryConditions = new Dictionary<Condition, PerformanceMetrics>();
		    int count = Math.Min(10, sortedEntryConditions.Count);
		    for (int i = 0; i < count; i++)
		    {
		        topEntryConditions[sortedEntryConditions[i].Key] = sortedEntryConditions[i].Value;
		    }

		    return topEntryConditions;
		}
		#endregion

		#region GetTopPerformingExitConditions()
		private Dictionary<ExitCondition, PerformanceMetrics> GetTopPerformingExitConditions()
		{
		    Dictionary<ExitCondition, PerformanceMetrics> exitConditionPerformance = new Dictionary<ExitCondition, PerformanceMetrics>();

		    foreach (SimTrade trade in trades.ActiveItems)
		    {
		        trade.CalculatePerformance();

		        // Update performance metrics for exit conditions
		        foreach (KeyValuePair<ExitCondition, List<Parameter>> exit in trade.ExitSignal.ExitConditions)
		        {
		            ExitCondition exitCondition = exit.Key;
		            if (!exitConditionPerformance.ContainsKey(exitCondition))
		            {
		                exitConditionPerformance[exitCondition] = new PerformanceMetrics();
		            }
		            UpdatePerformanceMetrics(exitConditionPerformance[exitCondition], trade.Performance);
		        }
		    }

		    // Sort exit conditions based on the desired performance metric
		    List<KeyValuePair<ExitCondition, PerformanceMetrics>> sortedExitConditions = exitConditionPerformance
		        .OrderByDescending(x => x.Value.NetProfit)
		        .ThenByDescending(x => x.Value.MaxFavorableExcursion)
		        .ThenBy(x => x.Value.MaxAdverseExcursion)
		        .ToList();

		    // Return the top performing exit conditions
		    Dictionary<ExitCondition, PerformanceMetrics> topExitConditions = new Dictionary<ExitCondition, PerformanceMetrics>();
		    int count = Math.Min(10, sortedExitConditions.Count);
		    for (int i = 0; i < count; i++)
		    {
		        topExitConditions[sortedExitConditions[i].Key] = sortedExitConditions[i].Value;
		    }

		    return topExitConditions;
		}
		#endregion

		#region UpdatePerformanceMetrics()
		private void UpdatePerformanceMetrics(PerformanceMetrics metrics, TradePerformance performance)
		{
		    metrics.NetProfit += performance.NetProfit;
		    metrics.MaxAdverseExcursion = performance.MaxAdverseExcursion > 0
				? Math.Max(metrics.MaxAdverseExcursion, performance.MaxAdverseExcursion)
				: 0;
		    metrics.MaxFavorableExcursion = performance.MaxFavorableExcursion > 0
				? Math.Max(metrics.MaxFavorableExcursion, performance.MaxFavorableExcursion)
				: 0;
		    // Update other performance metrics as needed
		}
		#endregion
		#endregion

		#region GetBestEntryConditionCombinations()
		private Dictionary<List<Condition>, PerformanceMetrics> GetBestEntryConditionCombinations(int minCombinationSize, int maxCombinationSize, int minTradesRequired)
		{
		    Dictionary<Condition, PerformanceMetrics> topEntryConditions = GetTopPerformingEntryConditions();
		    List<Condition> entryConditions = new List<Condition>(topEntryConditions.Keys);

		    Dictionary<List<Condition>, PerformanceMetrics> entryCombinationPerformance = new Dictionary<List<Condition>, PerformanceMetrics>();

		    // Generate combinations of entry conditions
		    for (int i = minCombinationSize; i <= maxCombinationSize && i <= entryConditions.Count; i++)
		    {
		        IEnumerable<IEnumerable<Condition>> combinations = GetCombinations(entryConditions, i);

		        foreach (IEnumerable<Condition> combination in combinations)
		        {
		            List<Condition> combinationList = combination.ToList();

		            // Generate simulated trades for the entry condition combination
		            List<SimTrade> trades = GenerateSimulatedTradesForEntryCombination(combinationList, minTradesRequired);

		            if (trades.Count >= minTradesRequired)
		            {
		                // Calculate performance metrics for the trades
		                PerformanceMetrics combinationMetrics = CalculatePerformanceMetrics(trades);

		                entryCombinationPerformance[combinationList] = combinationMetrics;
		            }
		        }
		    }

		    // Sort entry combinations based on the desired performance metric
		    List<KeyValuePair<List<Condition>, PerformanceMetrics>> sortedEntryCombinations = entryCombinationPerformance
		        .OrderByDescending(x => x.Value.NetProfit)
		        .ThenByDescending(x => x.Value.MaxFavorableExcursion)
		        .ThenBy(x => x.Value.MaxAdverseExcursion)
		        .ToList();

		    // Return the best entry combinations
		    Dictionary<List<Condition>, PerformanceMetrics> bestEntryCombinations = new Dictionary<List<Condition>, PerformanceMetrics>();
		    int count = Math.Min(10, sortedEntryCombinations.Count);
		    for (int i = 0; i < count; i++)
		    {
		        bestEntryCombinations[sortedEntryCombinations[i].Key] = sortedEntryCombinations[i].Value;
		    }

		    return bestEntryCombinations;
		}
		#endregion

		#region GetBestExitConditionCombinations()
		private Dictionary<List<ExitCondition>, PerformanceMetrics> GetBestExitConditionCombinations(int minCombinationSize, int maxCombinationSize, int minTradesRequired)
		{
		    Dictionary<ExitCondition, PerformanceMetrics> topExitConditions = GetTopPerformingExitConditions();
		    List<ExitCondition> exitConditions = new List<ExitCondition>(topExitConditions.Keys);

		    Dictionary<List<ExitCondition>, PerformanceMetrics> exitCombinationPerformance = new Dictionary<List<ExitCondition>, PerformanceMetrics>();

		    // Generate combinations of exit conditions
		    for (int i = minCombinationSize; i <= maxCombinationSize && i <= exitConditions.Count; i++)
		    {
		        IEnumerable<IEnumerable<ExitCondition>> combinations = GetCombinations(exitConditions, i);

		        foreach (IEnumerable<ExitCondition> combination in combinations)
		        {
		            List<ExitCondition> combinationList = combination.ToList();

		            // Generate simulated trades for the entry condition combination
		            List<SimTrade> trades = GenerateSimulatedTradesForExitCombination(combinationList, minTradesRequired);

		            if (trades.Count >= minTradesRequired)
		            {
		                // Calculate performance metrics for the trades
		                PerformanceMetrics combinationMetrics = CalculatePerformanceMetrics(trades);

		                exitCombinationPerformance[combinationList] = combinationMetrics;
		            }
		        }
		    }

		    // Sort entry combinations based on the desired performance metric
		    List<KeyValuePair<List<ExitCondition>, PerformanceMetrics>> sortedExitCombinations = exitCombinationPerformance
		        .OrderByDescending(x => x.Value.NetProfit)
		        .ThenByDescending(x => x.Value.MaxFavorableExcursion)
		        .ThenBy(x => x.Value.MaxAdverseExcursion)
		        .ToList();

		    // Return the best entry combinations
		    Dictionary<List<ExitCondition>, PerformanceMetrics> bestExitCombinations = new Dictionary<List<ExitCondition>, PerformanceMetrics>();
		    int count = Math.Min(10, sortedExitCombinations.Count);
		    for (int i = 0; i < count; i++)
		    {
		        bestExitCombinations[sortedExitCombinations[i].Key] = sortedExitCombinations[i].Value;
		    }

		    return bestExitCombinations;
		}
		#endregion

		#region GenerateSimulatedTradesForEntryCombination()
		private List<SimTrade> GenerateSimulatedTradesForEntryCombination(List<Condition> entryConditions, int minTradesRequired)
		{
		    List<SimTrade> trades = new List<SimTrade>();

		    foreach (Signal entrySignal in entrySignals.ActiveItems)
		    {
		        bool allConditionsMet = true;

		        foreach (Condition condition in entryConditions)
		        {
		            bool conditionMet = false;
		            foreach (var entryConditionPair in entrySignal.EntryConditions)
		            {
		                if (entryConditionPair.Key.GetType() == condition.GetType())
		                {
		                    conditionMet = true;
		                    break;
		                }
		            }

		            if (!conditionMet)
		            {
		                allConditionsMet = false;
		                break;
		            }
		        }

		        if (allConditionsMet)
		        {
		            foreach (Signal exitSignal in exitSignals.ActiveItems)
		            {
		                if (exitSignal.Bar > entrySignal.Bar)
		                {
							bool allExitConditionsMet = true;
		                    foreach (ExitCondition exitCondition in exitSignal.ExitConditions.Keys)
		                    {
		                        if (!exitConditions.Any(c => c.GetType() == exitCondition.GetType()))
		                        {
		                            allExitConditionsMet = false;
		                            break;
		                        }
		                    }

		                    if (allExitConditionsMet)
		                    {
		                        SimTrade trade = new SimTrade();
								trade.Activate();
		                        trade.Set(this);
		                        trade.EntrySignal = entrySignal;
		                        trade.ExitSignal = exitSignal;
		                        trades.Add(trade);

		                        if (trades.Count >= minTradesRequired)
		                        {
		                            return trades;
		                        }
		                    }
		                }
		            }
		        }
		    }

		    return trades;
		}
		#endregion

		#region GenerateSimulatedTradesForExitCombination()
		private List<SimTrade> GenerateSimulatedTradesForExitCombination(List<ExitCondition> exitConditions, int minTradesRequired)
		{
		    List<SimTrade> trades = new List<SimTrade>();

		    foreach (Signal exitSignal in exitSignals.ActiveItems)
		    {
		        bool allConditionsMet = true;
		        foreach (ExitCondition condition in exitConditions)
		        {
		            bool conditionMet = false;
		            foreach (var exitConditionPair in exitSignal.ExitConditions)
		            {
		                if (exitConditionPair.Key.GetType() == condition.GetType())
		                {
		                    conditionMet = true;
		                    break;
		                }
		            }

		            if (!conditionMet)
		            {
		                allConditionsMet = false;
		                break;
		            }
		        }

		        if (allConditionsMet)
		        {
		            foreach (Signal entrySignal in entrySignals.ActiveItems)
		            {
		                if (exitSignal.Bar > entrySignal.Bar)
		                {
		                    bool allEntryConditionsMet = true;
		                    foreach (Condition entryCondition in entrySignal.EntryConditions.Keys)
		                    {
								if (!entryConditions.Any(c => c.GetType() == entryCondition.GetType()))
		                        {
		                            allEntryConditionsMet = false;
		                            break;
		                        }
		                    }

		                    if (allEntryConditionsMet)
		                    {
		                        SimTrade trade = new SimTrade();
								trade.Activate();
		                        trade.Set(this);
		                        trade.EntrySignal = entrySignal;
		                        trade.ExitSignal = exitSignal;
		                        trades.Add(trade);

		                        if (trades.Count >= minTradesRequired)
		                        {
		                            return trades;
		                        }
		                    }
		                }
		            }
		        }
		    }

		    return trades;
		}
		#endregion

		#region GetCombinations()
		private IEnumerable<IEnumerable<T>> GetCombinations<T>(IEnumerable<T> items, int count)
		{
		    if (count == 0)
		        yield return new T[0];
		    else
		    {
		        int startPosition = 0;
		        foreach (T item in items)
		        {
		            IEnumerable<T> remainingItems = items.Skip(++startPosition);
		            foreach (IEnumerable<T> combination in GetCombinations(remainingItems, count - 1))
		                yield return new T[] { item }.Concat(combination);
		        }
		    }
		}
		#endregion

		#region CalculatePerformanceMetrics()
		private PerformanceMetrics CalculatePerformanceMetrics(List<SimTrade> trades)
		{
			PerformanceMetrics metrics = new PerformanceMetrics();

			foreach (SimTrade trade in trades)
			{
				trade.CalculatePerformance();

				metrics.NetProfit += trade.Performance.NetProfit;
		    	metrics.MaxAdverseExcursion = trade.Performance.MaxAdverseExcursion > 0
					? Math.Max(metrics.MaxAdverseExcursion, trade.Performance.MaxAdverseExcursion) : 0;
		    	metrics.MaxFavorableExcursion = trade.Performance.MaxFavorableExcursion > 0
					? Math.Max(metrics.MaxFavorableExcursion, trade.Performance.MaxFavorableExcursion) : 0;
			}

			return metrics;
		}
		#endregion

		#region SelectOptimalCombinations
		private List<List<T>> SelectOptimalCombinations<T>(Dictionary<List<T>, PerformanceMetrics> combinations)
		{
		    // Define the fitness function for evaluating combinations
		    Func<double[], double> fitnessFunction = position =>
		    {
		        // Map the particle's position to the corresponding combination
		        List<T> combination = MapPositionToCombination<T>(position, combinations.Keys.ToList());

		        // Get the performance metrics for the combination
		        PerformanceMetrics metrics = combinations[combination];

		        // Calculate the fitness score based on the performance metrics
		        // Example: Maximize net profit and minimize drawdown
		        double fitnessScore = metrics.NetProfit / (metrics.MaxAdverseExcursion + 1);

		        return fitnessScore;
		    };

		    // Set up the PSO parameters
		    int numParticles = 50;
		    int maxIterations = 100;
		    int dimensions = combinations.Count;
		    double[] lowerBounds = new double[dimensions];
		    double[] upperBounds = new double[dimensions];

		    for (int i = 0; i < dimensions; i++)
		    {
		        lowerBounds[i] = 0.0;
		        upperBounds[i] = 1.0;
		    }

		    // Run PSO to optimize the selection of combinations
		    double[] bestPosition = ParticleSwarmOptimization.Optimize(fitnessFunction, lowerBounds, upperBounds, numParticles, maxIterations);

		    // Map the best position to the optimal combination
		    List<List<T>> optimalCombinations = new List<List<T>>();
		    optimalCombinations.Add(MapPositionToCombination<T>(bestPosition, combinations.Keys.ToList()));

		    return optimalCombinations;
		}
		#endregion

		#region MapPositionToCombination
		private List<T> MapPositionToCombination<T>(double[] position, List<List<T>> combinations)
		{
		    int index = (int)Math.Floor(position[0] * combinations.Count);
		    return combinations[index];
		}
		#endregion
	}


	public class SimTrade : IPoolable
	{
		#region Variables
		public Signal EntrySignal { get; set; }
		public Signal ExitSignal { get; set; }
		public TradePerformance Performance { get; set; }
		public SignalGenerator Source { get; set; }
		public bool IsActive { get; set; }
		#endregion

		public Dictionary<string, double> Indicators
		{
		    get { return EntrySignal.Indicators; }
		}

		#region Enter()
		public void Enter(TrendDirection direction)
		{
			EntrySignal = new Signal();
			EntrySignal.Activate();
			EntrySignal.Set(direction, Source, SignalType.Entry);
		}
		#endregion

		#region Exit()
		public void Exit()
		{
			ExitSignal = new Signal();
			ExitSignal.Activate();
			ExitSignal.Set(EntrySignal.Direction, Source, EntrySignal.Type);
		}
		#endregion

		#region CalculatePerformance()
		public void CalculatePerformance()
		{
			Performance.Calculate(EntrySignal, ExitSignal);
		}
		#endregion

		#region Manage Poolable
		#region Activate()
	    public void Activate()
	    {
	        IsActive = true;
	    }
		#endregion

		#region Set()
		public void Set(SignalGenerator indicator)
		{
			Source = indicator;
			Performance = new TradePerformance();
		}
		#endregion

		#region Deactivate()
	    public void Deactivate()
	    {
	        IsActive = false;
	    }
		#endregion
		#endregion
	}

	public class Signal : IPoolable
	{
		#region Variables
		public Dictionary<ExitCondition, List<Parameter>> ExitConditions = new Dictionary<ExitCondition, List<Parameter>>();
		public Dictionary<Condition, List<Parameter>> EntryConditions = new Dictionary<Condition, List<Parameter>>();
		public Dictionary<string, double> Indicators = new Dictionary<string, double>();
		public SignalType Type { get; set; }
		public DateTime Time { get; set; }
		public int Bar { get; set; }
		public SignalGenerator Source { get; set; }
		public TrendDirection Direction { get; set; }
		public double Price { get; set; }
		public bool IsActive { get; set; }
		#endregion

		#region Manage Poolable
		#region Activate()
	    public void Activate()
	    {
	        IsActive = true;
	    }
		#endregion

		#region SetIndicators()
		public void SetIndicators()
		{
			Indicators["ATR"] = Source.atr[0];
			Indicators["RSI"] = Source.rsi[0];
			Indicators["EMAFast"] = Source.emaFast[0];
			Indicators["EMASlow"] = Source.emaSlow[0];
			Indicators["ATRStdDev"] = Source.stdDevAtr[0];
			Indicators["AvgATR"] = Source.avgAtr[0];
			Indicators["AvgATRFast"] = Source.avgAtrFast[0];
			Indicators["MinATR"] = Source.minATR[0];
			Indicators["MaxATR"] = Source.maxATR[0];
			Indicators["BarsSinceDoubleTop"] = Source.barsSinceDoubleTop[0];
			Indicators["BarsSinceDoubleBottom"] = Source.barsSinceDoubleBottom[0];
			Indicators["SwingLow"] = Source.md.LegLong.BarsAgoStarts[0] > 0
				? Source.MIN(Source.Low, Source.md.LegLong.BarsAgoStarts[0])[0] : Source.Low[0];
			Indicators["SwingHigh"] = Source.md.LegLong.BarsAgoStarts[0] > 0
				? Source.MAX(Source.High, Source.md.LegLong.BarsAgoStarts[0])[0] : Source.High[0];
		}
		#endregion

		#region Set()
		public void Set(TrendDirection direction, SignalGenerator indicator, SignalType type)
		{
			Source = indicator;
			Type = type;
			Direction = direction;
			Time = Source.Time[0];
			Bar = Source.CurrentBar;
			Price = Source.Close[0];
			ExitConditions.Clear();
			EntryConditions.Clear();
			SetIndicators();
		}
		#endregion

		#region Deactivate()
	    public void Deactivate()
	    {
	        IsActive = false;
	    }
		#endregion
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
			double highestHigh = BarsInTrade > 0 ? entry.Source.MAX(entry.Source.High, BarsInTrade)[barsAgoEntry] : entry.Source.High[barsAgoEntry];
			double lowestLow = BarsInTrade > 0 ? entry.Source.MIN(entry.Source.Low, BarsInTrade)[barsAgoEntry] : entry.Source.Low[barsAgoEntry];
			MaxAdverseExcursion = entry.Direction == TrendDirection.Bullish ? lowestLow : highestHigh;
			MaxFavorableExcursion = entry.Direction == TrendDirection.Bullish ? highestHigh : lowestLow;
			NetProfit = entry.Direction == TrendDirection.Bullish ? exit.Price - entry.Price : entry.Price - exit.Price;
			TradeDuration = (exit.Time - entry.Time).Seconds;
		}
		#endregion
	}

	public class PerformanceMetrics
	{
	    public double MaxAdverseExcursion { get; set; }
	    public double MaxFavorableExcursion { get; set; }
		public double NetProfit { get; set; }
	}

	public class Parameter : IPoolable
	{
		#region Variables
		public ParameterType Type { get; set; }
		public double Value { get; set; }
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

	#region Conditions
	#region Condition
	public abstract class Condition
	{
		public List<ParameterType> ParameterTypes = new List<ParameterType>();

		public Dictionary<string, double> ParameterValues = new Dictionary<string, double>();

		public void Reset()
		{
			ParameterValues.Clear();
		}

		public void SetParameterValue(ParameterType type, double value)
		{
			ParameterValues[type.Name] = value;
		}

	    public abstract bool IsMet(SignalGenerator generator);
	}
	#endregion

	#region Exit Condition
	public abstract class ExitCondition
	{
		public List<ParameterType> ParameterTypes = new List<ParameterType>();

		public Dictionary<string, double> ParameterValues = new Dictionary<string, double>();

		public void Reset()
		{
			ParameterValues.Clear();
		}

		public void SetParameterValue(ParameterType type, double value)
		{
			ParameterValues[type.Name] = value;
		}

	    public abstract bool IsMet(SignalGenerator generator, Signal entry);
	}
	#endregion

	#region Entry Conditions

	#region EMA

	#region EMADivergingCondition
	public class EMADivergingCondition : Condition
	{
		public override bool IsMet(SignalGenerator generator)
		{
			if (generator.md.Direction[0] == TrendDirection.Bullish) {
				return generator.pa.IsEMABullishDivergence(0, 1);
			}

			return generator.pa.IsEMABearishDivergence(0, 1);
		}
	}
	#endregion

	#region EMAConvergingCondition
	public class EMAConvergingCondition : Condition
	{
		public override bool IsMet(SignalGenerator generator)
		{
			if (generator.md.Direction[0] == TrendDirection.Bullish) {
				return generator.pa.IsEMABullishConvergence(0, 1);
			}

			return generator.pa.IsEMABearishConvergence(0, 1);
		}
	}
	#endregion

	#region WithTrendEMACondition
	public class WithTrendEMACondition : Condition
	{
		public override bool IsMet(SignalGenerator generator)
		{
			if (generator.md.Direction[0] == TrendDirection.Bullish) {
				return generator.pa.IsEMABullish(0);
			}

			return generator.pa.IsEMABearish(0);
		}
	}
	#endregion

	#region LeadsFastEMAByMoreThanATRCondition
	public class LeadsFastEMAByMoreThanATRCondition : Condition
	{
		public override bool IsMet(SignalGenerator generator)
		{
			if (generator.md.Direction[0] == TrendDirection.Bullish) {
				return generator.Low[0] > (generator.emaFast[0] + generator.atr[0]);
			}

			return generator.High[0] < (generator.emaFast[0] - generator.atr[0]);
		}
	}
	#endregion

	#region FastEMADirectionCondition
	public class FastEMADirectionCondition : Condition
	{
		public override bool IsMet(SignalGenerator generator)
		{
			TrendDirection FastEMADirection = generator.pa.IsEMAFastBullish(0)
				? TrendDirection.Bullish
				: generator.pa.IsEMAFastBearish(0)
					? TrendDirection.Bearish
					: TrendDirection.Flat;

			return generator.md.Direction[0] == FastEMADirection;
		}
	}
	#endregion

	#region SlowEMADirectionCondition
	public class SlowEMADirectionCondition : Condition
	{
		public override bool IsMet(SignalGenerator generator)
		{
			TrendDirection SlowEMADirection = generator.pa.IsEMASlowBullish(0)
				? TrendDirection.Bullish
				: generator.pa.IsEMASlowBearish(0)
					? TrendDirection.Bearish
					: TrendDirection.Flat;

			return generator.md.Direction[0] == SlowEMADirection;
		}
	}
	#endregion

	#endregion

	#region RSI

	#region RSIRangeCondition
	public class RSIRangeCondition : Condition
	{
		public override bool IsMet(SignalGenerator generator)
		{
			if (generator.md.Direction[0] == TrendDirection.Bullish) {
				return generator.rsi[0] > 50 && generator.rsi[0] < 70;
			}

			return generator.rsi[0] > 30 && generator.rsi[0] < 50;
		}
	}
	#endregion

	#endregion

	#region ATR

	#region AboveAverageATRCondition
	public class AboveAverageATRCondition : Condition
	{
		public override bool IsMet(SignalGenerator generator)
		{
			return generator.atr[0] > generator.avgAtr[0];
		}
	}
	#endregion

	#region BelowAverageATRCondition
	public class BelowAverageATRCondition : Condition
	{
		public override bool IsMet(SignalGenerator generator)
		{
			return generator.atr[0] < generator.avgAtr[0];
		}
	}
	#endregion

	#region AboveAverageATRByAStdDevCondition
	public class AboveAverageATRByAStdDevCondition : Condition
	{
		public override bool IsMet(SignalGenerator generator)
		{
			return (generator.atr[0] - generator.avgAtr[0]) > generator.stdDevAtr[0];
		}
	}
	#endregion

	#endregion

	#region Chart Patterns

	#region BreakoutCondition
	public class BreakoutCondition : Condition
	{
		public override bool IsMet(SignalGenerator generator)
		{
			return generator.md.Stage[0] == MarketCycleStage.Breakout;
		}
	}
	#endregion

	#region BroadChannelCondition
	public class BroadChannelCondition : Condition
	{
		public override bool IsMet(SignalGenerator generator)
		{
			return generator.md.Stage[0] == MarketCycleStage.BroadChannel;
		}
	}
	#endregion

	#region TightChannelCondition
	public class TightChannelCondition : Condition
	{
		public override bool IsMet(SignalGenerator generator)
		{
			return generator.md.Stage[0] == MarketCycleStage.TightChannel;
		}
	}
	#endregion

	#region WeakTrendCondition
	public class WeakTrendCondition : Condition
	{
		public override bool IsMet(SignalGenerator generator)
		{
			TrendDirection Direction = generator.md.Direction[0];
			int BarsAgo = generator.md.LegLong.BarsAgoStarts[0];

			int PreviousSwing = Direction == TrendDirection.Bearish
				? generator.pa.BarsAgoHigh(0, BarsAgo)
				: generator.pa.BarsAgoLow(0, BarsAgo);

			return Direction == TrendDirection.Bullish
				? generator.pa.IsWeakBullishTrend(0, PreviousSwing)
				: generator.pa.IsWeakBearishTrend(0, PreviousSwing);
		}
	}
	#endregion

	#region StrongTrendCondition
	public class StrongTrendCondition : Condition
	{
		public override bool IsMet(SignalGenerator generator)
		{
			TrendDirection Direction = generator.md.Direction[0];
			int BarsAgo = generator.md.LegLong.BarsAgoStarts[0];

			int PreviousSwing = Direction == TrendDirection.Bearish
				? generator.pa.BarsAgoHigh(0, BarsAgo)
				: generator.pa.BarsAgoLow(0, BarsAgo);

			return Direction == TrendDirection.Bullish
				? generator.pa.IsStrongBullishTrend(0, PreviousSwing)
				: generator.pa.IsStrongBearishTrend(0, PreviousSwing);
		}
	}
	#endregion

	#endregion

	#region Bar Patterns

	#region WithTrendTrendBarCondition
	public class WithTrendTrendBarCondition : Condition
	{
		public override bool IsMet(SignalGenerator generator)
		{
			return generator.pa.IsTrendBar(0)
					&& (generator.md.Direction[0] == TrendDirection.Bullish
							? generator.pa.IsBullishBar(0)
							: generator.pa.IsBearishBar(0)
						);
		}
	}
	#endregion

	#region BreakoutBarPatternCondition
	public class BreakoutBarPatternCondition : Condition
	{
		public override bool IsMet(SignalGenerator generator)
		{
			return generator.pa.DoesInsideOutsideMatch("ii", 0)
				|| generator.pa.DoesInsideOutsideMatch("ioi", 0);
		}
	}
	#endregion

	#region WeakBarCondition
	public class WeakBarCondition : Condition
	{
		public override bool IsMet(SignalGenerator generator)
		{
			return generator.pa.IsDoji(0) || generator.pa.IsTradingRangeBar(0);
		}
	}
	#endregion

	#region StrongFollowThroughCondition
	public class StrongFollowThroughCondition : Condition
	{
		public override bool IsMet(SignalGenerator generator)
		{
			return generator.pa.IsStrongFollowThroughBar(0);
		}
	}
	#endregion

	#endregion

	#region Buy/Sell Pressure

	#region WithTrendPressureCondition
	public class WithTrendPressureCondition : Condition
	{
		public override bool IsMet(SignalGenerator generator)
		{
			TrendDirection Direction = generator.md.Direction[0];
			int BarsAgo = generator.md.LegLong.BarsAgoStarts[0];

			int PreviousSwing = Direction == TrendDirection.Bearish
				? generator.pa.BarsAgoHigh(0, BarsAgo)
				: generator.pa.BarsAgoLow(0, BarsAgo);

			double BuySellPressure = generator.pa.GetBuySellPressure(0, PreviousSwing);

			return Direction == TrendDirection.Bullish ? BuySellPressure > 75 : BuySellPressure < 25;
		}
	}
	#endregion

	#region StrongWithTrendPressureCondition
	public class StrongWithTrendPressureCondition : Condition
	{
		public override bool IsMet(SignalGenerator generator)
		{
			TrendDirection Direction = generator.md.Direction[0];
			int BarsAgo = generator.md.LegLong.BarsAgoStarts[0];

			int PreviousSwing = Direction == TrendDirection.Bearish
				? generator.pa.BarsAgoHigh(0, BarsAgo)
				: generator.pa.BarsAgoLow(0, BarsAgo);

			double BuySellPressure = generator.pa.GetBuySellPressure(0, PreviousSwing);

			return Direction == TrendDirection.Bullish ? BuySellPressure > 90 : BuySellPressure < 10;
		}
	}
	#endregion

	#endregion

	#endregion

	#region Exit Conditions

	#region Trend Direction/Type/Strength

	#region TrendDirectionChangedCondition
	public class TrendDirectionChangedCondition : ExitCondition
	{
		public override bool IsMet(SignalGenerator generator, Signal entry)
		{
			return generator.md.Direction[0] != TrendDirection.Flat
				&& generator.md.Direction[0] != entry.Direction;
		}
	}
	#endregion

	#region CounterTrendTightChannelCondition
	public class CounterTrendTightChannelCondition : ExitCondition
	{
		public override bool IsMet(SignalGenerator generator, Signal entry)
		{
			return generator.md.TightChannels[0] != TrendDirection.Flat
				&& generator.md.TightChannels[0] != entry.Direction;
		}
	}
	#endregion

	#region CounterBroadTightChannelCondition
	public class CounterBroadTightChannelCondition : ExitCondition
	{
		public override bool IsMet(SignalGenerator generator, Signal entry)
		{
			return generator.md.BroadChannels[0] != TrendDirection.Flat
				&& generator.md.BroadChannels[0] != entry.Direction;
		}
	}
	#endregion

	#region CounterTrendBreakoutsCondition
	public class CounterTrendBreakoutsCondition : ExitCondition
	{
		public override bool IsMet(SignalGenerator generator, Signal entry)
		{
			return generator.md.Breakouts[0] != TrendDirection.Flat
				&& generator.md.Breakouts[0] != entry.Direction;
		}
	}
	#endregion

	#region CounterTrendBreakoutTrendCondition
	public class CounterTrendBreakoutTrendCondition : ExitCondition
	{
		public override bool IsMet(SignalGenerator generator, Signal entry)
		{
			if (generator.md.Direction[0] == TrendDirection.Flat)
			{
				return false;
			}

			TrendDirection trendDirection = entry.Direction == TrendDirection.Bullish
				? TrendDirection.Bearish : TrendDirection.Bullish;

			return generator.pa.IsBreakoutTrend(
				0, generator.md.LegLong.BarsAgoStarts[0], trendDirection);
		}
	}
	#endregion

	#region CounterTrendLegLongCondition
	public class CounterTrendLegLongCondition : ExitCondition
	{
		public override bool IsMet(SignalGenerator generator, Signal entry)
		{
			return generator.md.LegLong.LegDirectionAtBar(0) != TrendDirection.Flat
					&& generator.md.LegLong.LegDirectionAtBar(0) != entry.Direction;
		}
	}
	#endregion

	#region CounterTrendLegShortCondition
	public class CounterTrendLegShortCondition : ExitCondition
	{
		public override bool IsMet(SignalGenerator generator, Signal entry)
		{
			return generator.md.LegShort.LegDirectionAtBar(0) != TrendDirection.Flat
					&& generator.md.LegShort.LegDirectionAtBar(0) != entry.Direction;
		}
	}
	#endregion

	#region CounterTrendWeakTrendCondition
	public class CounterTrendWeakTrendCondition : ExitCondition
	{
		public override bool IsMet(SignalGenerator generator, Signal entry)
		{
			if (entry.Direction == TrendDirection.Flat)
			{
				return false;
			}

			int barsAgo = generator.md.LegLong.BarsAgoStarts[0];
			int previousSwing = entry.Direction == TrendDirection.Bearish
				? generator.pa.BarsAgoHigh(0, barsAgo)
				: generator.pa.BarsAgoLow(0, barsAgo);

			if (entry.Direction == TrendDirection.Bullish)
			{
				return generator.pa.IsWeakBearishTrend(0, previousSwing);
			}

			return generator.pa.IsWeakBullishTrend(0, previousSwing);
		}
	}
	#endregion

	#region CounterTrendStrongTrendCondition
	public class CounterTrendStrongTrendCondition : ExitCondition
	{
		public override bool IsMet(SignalGenerator generator, Signal entry)
		{
			if (entry.Direction == TrendDirection.Flat)
			{
				return false;
			}

			int barsAgo = generator.md.LegLong.BarsAgoStarts[0];
			int previousSwing = entry.Direction == TrendDirection.Bearish
				? generator.pa.BarsAgoHigh(0, barsAgo)
				: generator.pa.BarsAgoLow(0, barsAgo);

			if (entry.Direction == TrendDirection.Bullish)
			{
				return generator.pa.IsStrongBearishTrend(0, previousSwing);
			}

			return generator.pa.IsStrongBullishTrend(0, previousSwing);
		}
	}
	#endregion

	#endregion

	#region Chart Patterns

	#region DoubleTopBottomCondition
	public class DoubleTopBottomCondition : ExitCondition
	{
		public override bool IsMet(SignalGenerator generator, Signal entry)
		{
			if (entry.Direction == TrendDirection.Bullish) {
				return generator.barsSinceDoubleTop[0] == 0;
			}

			if (entry.Direction == TrendDirection.Bearish) {
				return generator.barsSinceDoubleBottom[0] == 0;
			}

			return false;
		}
	}
	#endregion

	#region CounterTrendLegAfterDoubleTopBottomCondition
	public class CounterTrendLegAfterDoubleTopBottomCondition : ExitCondition
	{
		public override bool IsMet(SignalGenerator generator, Signal entry)
		{
			if (entry.Direction == TrendDirection.Bullish) {
				return generator.barsSinceDoubleTop[0] > 0
					&& generator.barsSinceDoubleTop[0] < 10
					&& generator.md.LegLong.LegDirectionAtBar(0) == TrendDirection.Bearish;
			}

			if (entry.Direction == TrendDirection.Bearish) {
				return generator.barsSinceDoubleBottom[0] > 0
					&& generator.barsSinceDoubleBottom[0] < 10
					&& generator.md.LegLong.LegDirectionAtBar(0) == TrendDirection.Bullish;
			}

			return false;
		}
	}
	#endregion

	#region StrongCounterTrendFollowThroughCondition
	public class StrongCounterTrendFollowThroughCondition : ExitCondition
	{
		public override bool IsMet(SignalGenerator generator, Signal entry)
		{
			if (entry.Direction == TrendDirection.Flat)
			{
				return false;
			}

			if (entry.Direction == TrendDirection.Bullish) {
				return generator.pa.IsBearishBar(0) && generator.pa.IsStrongFollowThroughBar(0);
			}

			return generator.pa.IsBullishBar(0) && generator.pa.IsStrongFollowThroughBar(0);
		}
	}
	#endregion

	#endregion

	#region Stop Loss / Take Profit

	#region TrailingStopBeyondPreviousExtremeCondition
	public class TrailingStopBeyondPreviousExtremeCondition : ExitCondition
	{
		public override bool IsMet(SignalGenerator generator, Signal entry)
		{
			if (entry.Direction == TrendDirection.Flat)
			{
				return false;
			}

			if (entry.Direction == TrendDirection.Bullish)
			{
				return generator.Low[0] < entry.Indicators["SwingLow"];
			}

			return generator.High[0] > entry.Indicators["SwingHigh"];
		}
	}
	#endregion

	#region NoNewExtremeCondition
	public class NoNewExtremeCondition : ExitCondition
	{
		public override bool IsMet(SignalGenerator generator, Signal entry)
		{
			if (entry.Direction == TrendDirection.Flat)
			{
				return false;
			}

			if (!ParameterValues.Keys.Contains("NewExtremeLength"))
			{
				return false;
			}

			int threshold = (int) ParameterValues["NewExtremeLength"];
			int barsAgo = generator.md.LegLong.BarsAgoStarts[0];

			if (threshold == 0)
			{
				return false;
			}

			if (barsAgo == 0)
			{
				return false;
			}

			if (entry.Direction == TrendDirection.Bullish)
			{
				return generator.MAX(generator.High, threshold)[0] < generator.MAX(generator.High, barsAgo)[0];
			}

			return generator.MIN(generator.Low, threshold)[0] > generator.MIN(generator.Low, barsAgo)[0];
		}
	}
	#endregion

	#region ProfitTargetCondition
	public class ProfitTargetCondition : ExitCondition
	{
		public override bool IsMet(SignalGenerator generator, Signal entry)
		{
			if (entry.Direction == TrendDirection.Flat)
			{
				return false;
			}

			if (!ParameterValues.Keys.Contains("ProfitTargetMultiplier"))
			{
				return false;
			}

			double profitTargetMultiplier = ParameterValues["ProfitTargetMultiplier"];
			double profitTarget = generator.avgAtrFast[0] * profitTargetMultiplier;
			double distanceMoved = entry.Direction == TrendDirection.Bullish
					? generator.Close[0] - entry.Price : entry.Price - generator.Close[0];

			return distanceMoved >= profitTarget;
		}
	}
	#endregion

	#region StopLossCondition
	public class StopLossCondition : ExitCondition
	{
		public override bool IsMet(SignalGenerator generator, Signal entry)
		{
			if (entry.Direction == TrendDirection.Flat)
			{
				return false;
			}

			if (!ParameterValues.Keys.Contains("StopLossMultiplier"))
			{
				return false;
			}

			double stopLossMultiplier = ParameterValues["StopLossMultiplier"];
			double stopLoss = generator.avgAtrFast[0] * stopLossMultiplier;
			double distanceMoved = entry.Direction == TrendDirection.Bullish
					? generator.Close[0] - entry.Price : entry.Price - generator.Close[0];

			if (distanceMoved > 0) {
				return false;
			}

			return Math.Abs(distanceMoved) >= stopLoss;
		}
	}
	#endregion

	#endregion

	#region Buy/Sell Pressure

	#region CounterTrendPressureCondition
	public class CounterTrendPressureCondition : ExitCondition
	{
		public override bool IsMet(SignalGenerator generator, Signal entry)
		{
			if (entry.Direction == TrendDirection.Flat)
			{
				return false;
			}

			int barsAgo = generator.md.LegLong.BarsAgoStarts[0];
			int previousSwing = entry.Direction == TrendDirection.Bearish
				? generator.pa.BarsAgoHigh(0, barsAgo)
				: generator.pa.BarsAgoLow(0, barsAgo);
			double currentBuySellPressure = generator.pa.GetBuySellPressure(0, previousSwing);

			if (entry.Direction == TrendDirection.Bullish)
			{
				return currentBuySellPressure < 25;
			}

			return currentBuySellPressure > 75;
		}
	}
	#endregion

	#endregion

	#region Indicators

	#region RSIOutOfRangeCondition
	public class RSIOutOfRangeCondition : ExitCondition
	{
		public override bool IsMet(SignalGenerator generator, Signal entry)
		{
			if (entry.Direction == TrendDirection.Flat)
			{
				return false;
			}

			if (entry.Direction == TrendDirection.Bullish)
			{
				return generator.rsi[0] < 30;
			}

			return generator.rsi[0] > 70;
		}
	}
	#endregion

	#region MovingAverageCrossoverCondition
	public class MovingAverageCrossoverCondition : ExitCondition
	{
		public override bool IsMet(SignalGenerator generator, Signal entry)
		{
			if (entry.Direction == TrendDirection.Flat)
			{
				return false;
			}

			if (entry.Direction == TrendDirection.Bullish)
			{
				return generator.emaFast[0] < generator.emaSlow[0];
			}

			return generator.emaFast[0] > generator.emaSlow[0];
		}
	}
	#endregion

	#region ATR

	#region AboveAverageATRExitCondition
	public class AboveAverageATRExitCondition : ExitCondition
	{
		public override bool IsMet(SignalGenerator generator, Signal entry)
		{
			return generator.atr[0] > generator.avgAtr[0];
		}
	}
	#endregion

	#region BelowAverageATRExitCondition
	public class BelowAverageATRExitCondition : ExitCondition
	{
		public override bool IsMet(SignalGenerator generator, Signal entry)
		{
			return generator.atr[0] < generator.avgAtr[0];
		}
	}
	#endregion

	#region AboveAverageATRByAStdDevExitCondition
	public class AboveAverageATRByAStdDevExitCondition : ExitCondition
	{
		public override bool IsMet(SignalGenerator generator, Signal entry)
		{
			return (generator.atr[0] - generator.avgAtr[0]) > generator.stdDevAtr[0];
		}
	}
	#endregion

	#region BelowAverageATRByAStdDevExitCondition
	public class BelowAverageATRByAStdDevExitCondition : ExitCondition
	{
		public override bool IsMet(SignalGenerator generator, Signal entry)
		{
			return (generator.avgAtr[0] - generator.atr[0]) < generator.stdDevAtr[0];
		}
	}
	#endregion


	#endregion

	#endregion

	#endregion
	#endregion
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
