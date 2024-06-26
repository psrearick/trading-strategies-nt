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
		public ChoppinessIndex chop;
		public Series<int> barsSinceDoubleTop;
		public Series<int> barsSinceDoubleBottom;
		private List<ICondition> entryConditions = new List<ICondition>();
		private List<ICondition> exitConditions = new List<ICondition>();
		private List<Combination> optimalCombinations = new List<Combination>();
		private List<Combination> optimalEntryCombinations = new List<Combination>();
		private List<Combination> optimalExitCombinations = new List<Combination>();
		private List<Signal> cachedEntrySignals = new List<Signal>();
		private List<Signal> cachedExitSignals = new List<Signal>();
		private List<Signal> foldEntrySignals = new List<Signal>();
		private List<Signal> foldExitSignals = new List<Signal>();
		private Dictionary<List<ICondition>, double> combinationCache = new Dictionary<List<ICondition>, double>(new ListComparer<ICondition>());
		private GroupedObjectPool<int, Signal> entrySignals;
		private GroupedObjectPool<int, Signal> exitSignals;
		private Dictionary<Type, List<ParameterType>> conditionParameterTypes = new Dictionary<Type, List<ParameterType>>();
		public ObjectPool<Signal> entries;
		public ObjectPool<Signal> exits;
		public ObjectPool<Signal> entriesOnBar;
		private int lastUpdateBar = -1;
		private int lastSignalBar = -1;
		private GeneticAlgorithm ga = new GeneticAlgorithm();
		private List<List<ICondition>> entryInitialPopulation = new List<List<ICondition>>();
		private List<List<ICondition>> exitInitialPopulation = new List<List<ICondition>>();
		private CombinationMetrics combinationMetrics = new CombinationMetrics();

		private int rollingWindowSize = 0;
		private int windowStart = 0;
		private int windowEnd = 0;
		private int numFolds = 0;
		private int foldStart = 0;
		private int foldEnd = 0;
		private int individualPopulation = 8;
		private int individualConditionInterval = 3;
		private int generationsWithoutImprovement = 0;
		private double bestFitness = double.MinValue;
		private double worstFitness = double.MaxValue;
		private double foldBestFitness = double.MinValue;
		private double foldWorstFitness = double.MaxValue;
		private double atrMultiplier = 15;
		private double minTradeThresholdMultiplier = 0.15;
		private double convergenceThreshold = 50;

		private int minIndividualPopulation = 4;
    	private int maxIndividualPopulation = 10;
    	private int minIndividualConditionInterval = 4;
    	private int maxIndividualConditionInterval = 6;
	    private int minPopulationSize = 50;
	    private int maxPopulationSize = 200;
	    private int minGenerations = 100;
	    private int maxGenerations = 500;
	    private double minMutationRate = 0.01;
	    private double maxMutationRate = 0.2;
	    private double minCrossoverRate = 0.6;
	    private double maxCrossoverRate = 0.8;

		private double numFoldMultiplier = 3;
		private int numRuns = 10;

		private DateTime initTime = DateTime.Now;
		private DateTime start = DateTime.Now;

		public List<Signal> CurrentEntries
		{
		    get { return entries.ActiveItems.ToList(); }
		}

		public List<Signal> CurrentExits
		{
		    get { return exits.ActiveItems.ToList(); }
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
				chop					= ChoppinessIndex(14);

				AddDataSeries(Data.BarsPeriodType.Second, 15);
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

				entriesOnBar = new ObjectPool<Signal>(0, () => new Signal());
				exitSignals = new GroupedObjectPool<int, Signal>(0, () => new Signal());
				entrySignals = new GroupedObjectPool<int, Signal>(0, () => new Signal());
				exits = new ObjectPool<Signal>(0, () => new Signal());
				entries = new ObjectPool<Signal>(0, () => new Signal());

				SetParameterTypes();
				SetConditions();
			}
			#endregion
		}
		#endregion

		#region OnBarUpdate()
		protected override void OnBarUpdate()
		{
			if (CurrentBar < 200)
		    {
		        return;
		    }

			if (BarsInProgress == 1)
			{
				return;
			}

			CalculateParameters();
		    UpdateBarsSinceDoubleTopBottom();
			TestIndividualConditions();
			CalculateAdaptiveRollingWindowSize();
			CalculateWindowPosition();
			PruneSignals();

			if (CurrentBar % (2 * rollingWindowSize) == 0)
			{
				bestFitness = double.MinValue;
        		worstFitness = double.MaxValue;
			}

			if (CurrentBar % rollingWindowSize == 0)
		    {
				Print(CurrentBar + " " + Time[0].ToString() + " ==================== " + (DateTime.Now - start).TotalSeconds + " -- " + (DateTime.Now - initTime).TotalSeconds);
				start = DateTime.Now;

				entriesOnBar.ReleaseAll();
				combinationMetrics.Clear();
		        AnalyzeConditionPerformance();
				lastUpdateBar = CurrentBar;
		    }

		    GenerateSignals();
		}
		#endregion

		#region CalculateParameters()
		private void CalculateParameters()
		{
			atrMultiplier = 10 + 10 * ((atr[0] - MIN(atr, 20)[0]) / (MAX(atr, 20)[0] - MIN(atr, 20)[0]));
			atrMultiplier = Math.Max(10, Math.Min(20, atrMultiplier));
			minTradeThresholdMultiplier = Math.Max(0.05, Math.Min(0.3, 0.2 * md.LegShort.LegDirectionRatios[0]));

			if (CurrentBar % individualConditionInterval == 0)
		    {
				double recentFitness = optimalCombinations.Count > 0 ? optimalCombinations.Average(c => c.FitnessScore) : 0;
				double currentFitness = 0;
				List<Signal> currentSignals = entries.ActiveItems.OrderByDescending(s => s.Bar).ToList();
				if (currentSignals.Count > 0)
				{
					currentFitness = currentSignals[0].Combination.FitnessScore;
				}

		        if (currentFitness > recentFitness)
		        {
		            generationsWithoutImprovement = 0;
		        }
		        else
		        {
		            generationsWithoutImprovement++;
		        }

		        // Adjust individual population size based on algorithm's performance
		        if (generationsWithoutImprovement > 10 && individualPopulation < maxIndividualPopulation)
		        {
		            individualPopulation = Math.Min(individualPopulation + 1, maxIndividualPopulation);
		        }
		        else if (generationsWithoutImprovement < 5 && individualPopulation > minIndividualPopulation)
		        {
		            individualPopulation = Math.Max(individualPopulation - 1, minIndividualPopulation);
		        }

		        // Adjust individual condition interval based on algorithm's performance
		        if (generationsWithoutImprovement > 10 && individualConditionInterval < maxIndividualConditionInterval)
		        {
		            individualConditionInterval = Math.Min(individualConditionInterval + 1, maxIndividualConditionInterval);
		        }
		        else if (generationsWithoutImprovement < 5 && individualConditionInterval > minIndividualConditionInterval)
		        {
		            individualConditionInterval = Math.Max(individualConditionInterval - 1, minIndividualConditionInterval);
		        }
		    }
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

		#region CalculateAdaptiveRollingWindowSize()
		private void CalculateAdaptiveRollingWindowSize()
		{
		    double currentATR = avgAtrFast[0];
		    int adaptiveRollingWindowSize = (int)Math.Round(currentATR * atrMultiplier);
		    rollingWindowSize = Math.Max(adaptiveRollingWindowSize, 1);
		}
		#endregion

		#region CalculateWindowPosition()
		private void CalculateWindowPosition()
		{
			windowStart = CurrentBar - rollingWindowSize;
		    windowEnd = CurrentBar - 1;
			numFolds = Math.Min(10, Math.Max(1, (int) Math.Round(rollingWindowSize /(double) (numFoldMultiplier * atrMultiplier), 0)));
		}
		#endregion

		#region SetParameterTypes()
		private void SetParameterTypes()
		{
			List<ParameterType> profitTargetParameters = new List<ParameterType>();
			List<ParameterType> stopLossParameters = new List<ParameterType>();
			List<ParameterType> sltpParameters = new List<ParameterType>();
//			List<ParameterType> newExtremeParameters = new List<ParameterType>();

//		    ParameterType NewExtremeLength = new ParameterType();
//		    NewExtremeLength.Set("NewExtremeLength", 20, 6, 2);
//		    newExtremeParameters.Add(NewExtremeLength);

		    ParameterType ProfitTargetMultiplier = new ParameterType();
		    ProfitTargetMultiplier.Set("ProfitTargetMultiplier", 8, 1, 1);
		    profitTargetParameters.Add(ProfitTargetMultiplier);
		    sltpParameters.Add(ProfitTargetMultiplier);

			ParameterType HighATRMultiplier = new ParameterType();
		    HighATRMultiplier.Set("HighATRMultiplier", 5, 0, 1);
		    profitTargetParameters.Add(HighATRMultiplier);
		    sltpParameters.Add(HighATRMultiplier);


		    ParameterType stopLossMultiplier = new ParameterType();
		    stopLossMultiplier.Set("StopLossMultiplier", 5, 1, 1);
		    stopLossParameters.Add(stopLossMultiplier);
		    sltpParameters.Add(stopLossMultiplier);

		    conditionParameterTypes[typeof(StopLossCondition)] = stopLossParameters;
		    conditionParameterTypes[typeof(ProfitTargetCondition)] = profitTargetParameters;
		    conditionParameterTypes[typeof(SLTPCondition)] = sltpParameters;
//		    conditionParameterTypes[typeof(NoNewExtremeCondition)] = newExtremeParameters;
		}
		#endregion

		#region SetConditions()
		private void SetConditions()
		{
			#region Entry Conditions

//			entryConditions.Add(new MARisingFallingCondition());
			entryConditions.Add(new TrendRisingFallingCondition());
//			entryConditions.Add(new NewHighLowCondition());
			entryConditions.Add(new ValidChoppinessCondition());
			entryConditions.Add(new UpDownTrendCondition());

//			entryConditions.Add(new EMAConvergingCondition());
//			entryConditions.Add(new WithTrendEMACondition());
//			entryConditions.Add(new BelowAverageATRCondition());
//			entryConditions.Add(new AboveAverageATRByAStdDevCondition());
//			entryConditions.Add(new BreakoutCondition());
			entryConditions.Add(new BroadChannelCondition());
//			entryConditions.Add(new WeakBarCondition());
			// entryConditions.Add(new RSIRangeCondition());
			// entryConditions.Add(new AboveAverageATRCondition());
			// entryConditions.Add(new WithTrendTrendBarCondition());
//			entryConditions.Add(new WithTrendPressureCondition());
			// entryConditions.Add(new EMADivergingCondition());
//			entryConditions.Add(new FastEMADirectionCondition());
//			entryConditions.Add(new SlowEMADirectionCondition());

//			entryConditions.Add(new TrendFollowingStrategy206Condition()); // Produces very few trades -- REMOVE
//			entryConditions.Add(new StrongWithTrendPressureCondition()); // Produces few trades -- REMOVE
//			entryConditions.Add(new TightChannelCondition()); // Produces very few trades -- REMOVE
//			entryConditions.Add(new WeakTrendCondition()); // Produces very few trades -- REMOVE
//			entryConditions.Add(new StrongTrendCondition()); // Produces very few trades -- REMOVE
//			entryConditions.Add(new BreakoutBarPatternCondition()); // Produces very few trades -- REMOVE
//			entryConditions.Add(new StrongFollowThroughCondition()); // Produces very few trades -- REMOVE
//			entryConditions.Add(new LeadsFastEMAByMoreThanATRCondition()); // Produces very few trades -- REMOVE


			#endregion

			#region Exit Conditions


			// =================
			// TODO: Randomize Exit and Entry Conditions
			// TODO: Widen Range of Take Profit Conditions
			// TODO: Widen Range of Stop Loss Conditions
			// =================




			List<ExitCondition> singleExitConditions = new List<ExitCondition>();

//			singleExitConditions.Add(new TrendDirectionChangedCondition());
//			// singleExitConditions.Add(new CounterTrendTightChannelCondition()); // Rarely Triggers
//			// singleExitConditions.Add(new CounterTrendBroadChannelCondition()); // Rarely Triggers
//			// singleExitConditions.Add(new CounterTrendBreakoutsCondition()); // Rarely Triggers
//			// singleExitConditions.Add(new CounterTrendBreakoutTrendCondition()); // Rarely Triggers
//			singleExitConditions.Add(new CounterTrendLegLongCondition());
//			singleExitConditions.Add(new CounterTrendLegShortCondition());
//			// singleExitConditions.Add(new DoubleTopBottomCondition()); // Rarely Triggers
//			// singleExitConditions.Add(new CounterTrendLegAfterDoubleTopBottomCondition()); // Rarely Triggers
//			singleExitConditions.Add(new TrailingStopBeyondPreviousExtremeCondition());
//			singleExitConditions.Add(new MovingAverageCrossoverCondition());
//			singleExitConditions.Add(new NoNewExtremeCondition());
			// singleExitConditions.Add(new CounterTrendPressureCondition()); // Rarely Triggers
//			// singleExitConditions.Add(new CounterTrendWeakTrendCondition()); // Rarely Triggers
//			// singleExitConditions.Add(new CounterTrendStrongTrendCondition()); // Rarely Triggers
//			// singleExitConditions.Add(new RSIOutOfRangeCondition()); // Rarely Triggers
//			singleExitConditions.Add(new AboveAverageATRExitCondition());
//			singleExitConditions.Add(new BelowAverageATRExitCondition());
//			singleExitConditions.Add(new AboveAverageATRByAStdDevExitCondition());
//			singleExitConditions.Add(new BelowAverageATRByAStdDevExitCondition());
//			singleExitConditions.Add(new StrongCounterTrendFollowThroughCondition());


//			singleExitConditions.Add(new ProfitTargetCondition());
//			singleExitConditions.Add(new StopLossCondition());
			singleExitConditions.Add(new SLTPCondition());

			foreach (ExitCondition singleExitCondition in singleExitConditions)
			{
				Type exitType = singleExitCondition.GetType();

				if (!conditionParameterTypes.ContainsKey(exitType))
				{
					exitConditions.Add(singleExitCondition);

					continue;
				}

				List<ParameterType> parameterTypes = conditionParameterTypes[exitType];
	            List<List<Parameter>> parameterCombinations = GenerateParameterCombinations(parameterTypes);

	            foreach (List<Parameter> parameterCombination in parameterCombinations)
	            {
	                ExitCondition exitCondition = (ExitCondition)Activator.CreateInstance(exitType);

	                foreach (Parameter parameter in parameterCombination)
	                {
	                    exitCondition.SetParameterValue(parameter.Type, parameter.Value);
	                }

	                exitConditions.Add(exitCondition);
	            }
			}
			#endregion
		}
		#endregion

		#region AnalyzeConditionPerformance()
		private void AnalyzeConditionPerformance()
		{
		    List<Tuple<int, int>> folds = SplitDataIntoFolds();

			Dictionary<SignalType, List<List<ICondition>>> optimalSets = new Dictionary<SignalType, List<List<ICondition>>>();
			optimalSets[SignalType.Entry] = new List<List<ICondition>>();
			optimalSets[SignalType.Exit] = new List<List<ICondition>>();

//			entryInitialPopulation = new List<List<ICondition>>{entryConditions};
			entryInitialPopulation = InitializePopulation(entryConditions, maxPopulationSize, 1, 3);
//			exitInitialPopulation = InitializePopulation(exitConditions, maxPopulationSize, 1, 3);
			exitInitialPopulation = InitializePopulation(exitConditions, maxPopulationSize, 1, 1);

			combinationCache.Clear();

		    foreach (var fold in folds)
		    {
				foldStart = fold.Item1;
				foldEnd = fold.Item2;

		        AnalyzeFoldPerformance(optimalSets);
		    }

		    CalculateOptimalCombinationPerformance(optimalSets);
		}
		#endregion

		#region CalculatePerformanceForConditionSet()
		private Dictionary<SignalType, List<Combination>> CalculatePerformanceForConditionSet(Dictionary<SignalType, List<List<ICondition>>> conditionSets, int take = 5)
		{
			Dictionary<SignalType, List<Combination>> combinations = new Dictionary<SignalType, List<Combination>>();
			combinations[SignalType.Entry] = new List<Combination>();
			combinations[SignalType.Exit] = new List<Combination>();

			if (conditionSets[SignalType.Entry].Count() == 0 || conditionSets[SignalType.Exit].Count() == 0)
			{
				return combinations;
			}

			List<List<ICondition>> conditionEntries = CombineOptimalCombinations<ICondition>(conditionSets[SignalType.Entry], take);
		    List<List<ICondition>> conditionExits = CombineOptimalCombinations<ICondition>(conditionSets[SignalType.Exit], take);

			foreach (var entryCombination in conditionEntries)
			{
				Combination combination = new Combination();
				combination.Conditions = entryCombination.Select(c => (ICondition)c).ToList();
				List<SimTrade> trades = GenerateSimulatedTradesForEntryCombination(windowStart, windowEnd, entryCombination);
	            PerformanceMetrics metrics = CalculatePerformanceMetrics(trades);
				combination.FitnessScore = metrics.FitnessScore;

				if (combination.FitnessScore > 0)
				{
					combinations[SignalType.Entry].Add(combination);
				}
			}

			foreach (var exitCombination in conditionExits)
			{
			    Combination combination = new Combination();
				combination.Conditions = exitCombination.Select(c => (ICondition)c).ToList();
				List<SimTrade> trades = GenerateSimulatedTradesForExitCombination(windowStart, windowEnd, exitCombination);
	            PerformanceMetrics metrics = CalculatePerformanceMetrics(trades);
				combination.FitnessScore = metrics.FitnessScore;

				if (combination.FitnessScore > 0)
				{
					combinations[SignalType.Exit].Add(combination);
				}
			}

			return combinations;
		}
		#endregion

		#region CalculateOptimalCombinationPerformance()
		private void CalculateOptimalCombinationPerformance(Dictionary<SignalType, List<List<ICondition>>> optimalSets)
		{
			optimalEntryCombinations.Clear();
			optimalExitCombinations.Clear();

			Dictionary<SignalType, List<Combination>> optimalSetPerformance = CalculatePerformanceForConditionSet(optimalSets);

			optimalEntryCombinations = optimalSetPerformance[SignalType.Entry];
			optimalExitCombinations = optimalSetPerformance[SignalType.Exit];

			List<double> fitnessScores = optimalCombinations
				.Concat(optimalExitCombinations)
				.Concat(optimalEntryCombinations)
			    .Select(c => c.FitnessScore)
			    .ToList();

			foreach (var combination in optimalEntryCombinations)
			{
				combination.ConfidenceScore = CalculateConfidenceScore(combination.FitnessScore, fitnessScores);
			}

			foreach (var combination in optimalExitCombinations)
			{
				combination.ConfidenceScore = CalculateConfidenceScore(combination.FitnessScore, fitnessScores);
			}

			optimalCombinations = optimalCombinations
				.Concat(optimalExitCombinations)
				.Concat(optimalEntryCombinations)
				.ToList();

			if (optimalCombinations.Count > 30)
			{
				for (int i = 0; i < optimalCombinations.Count - 30; i++)
				{
					optimalCombinations.RemoveAt(0);
				}
			}
		}
		#endregion

		#region PopulateFoldSignals()
		private void PopulateFoldSignals()
		{
			foreach (int key in entrySignals.GetPools().Keys.Where(k => k >= foldStart && k <= foldEnd).ToList())
	        {
	            foreach (Signal signal in entrySignals.GetPool(key).ActiveItems.ToList())
	            {
	                foldEntrySignals.Add(signal);
	                cachedEntrySignals.Add(signal);
	            }
	        }

	        foreach (int key in exitSignals.GetPools().Keys.Where(k => k >= foldStart && k <= foldEnd).ToList())
	        {
	            foreach (Signal signal in exitSignals.GetPool(key).ActiveItems.ToList())
	            {
	                foldExitSignals.Add(signal);
	                cachedExitSignals.Add(signal);
	            }
	        }
		}
		#endregion

		#region FitnessFunction()
		public double FitnessFunction(List<ICondition> combination)
		{
			double cachedScore;
			if (combinationCache.TryGetValue(combination, out cachedScore))
		    {
		        return cachedScore;
		    }

            List<SimTrade> trades = combination[0].Type == SignalType.Entry
				? GenerateSimulatedTradesForEntryCombination(foldStart, foldEnd, combination)
				: GenerateSimulatedTradesForExitCombination(foldStart, foldEnd, combination);
            PerformanceMetrics metrics = CalculatePerformanceMetrics(trades);
			double fitnessScore = metrics.FitnessScore;

			combinationCache[combination] = fitnessScore;

			return fitnessScore;
		}
		#endregion

		#region AnalyzeFoldPerformance()
		private void AnalyzeFoldPerformance(Dictionary<SignalType, List<List<ICondition>>> optimalSets)
		{
	        foldExitSignals.Clear();
	        foldEntrySignals.Clear();
	        PopulateFoldSignals();
			CalculateConvergenceThreshold();

			int eliteCount = (int) Math.Floor((double) ((minPopulationSize + maxPopulationSize) * 0.5) * 0.1);
			List<Dictionary<SignalType, List<List<ICondition>>>> optimalFoldRuns = new List<Dictionary<SignalType, List<List<ICondition>>>>();

			for (int run = 0; run < numRuns; run++)
			{
				Dictionary<SignalType, List<List<ICondition>>> optimalFoldRun = new Dictionary<SignalType, List<List<ICondition>>>();
        		optimalFoldRun[SignalType.Entry] = new List<List<ICondition>>();
       			optimalFoldRun[SignalType.Exit] = new List<List<ICondition>>();

		        List<List<ICondition>> optimalEntryFold = ga.Optimize(
		            FitnessFunction,
		            entryInitialPopulation,
		            convergenceThreshold,
			        minPopulationSize,
			        maxPopulationSize,
			        minGenerations,
			        maxGenerations,
			        minMutationRate,
			        maxMutationRate,
			        minCrossoverRate,
			        maxCrossoverRate,
					eliteCount);

		        List<List<ICondition>> optimalExitFold = ga.Optimize(
		            FitnessFunction,
		            exitInitialPopulation,
		            convergenceThreshold,
			        minPopulationSize,
			        maxPopulationSize,
			        minGenerations,
			        maxGenerations,
			        minMutationRate,
			        maxMutationRate,
			        minCrossoverRate,
			        maxCrossoverRate,
					eliteCount);

		        optimalFoldRun[SignalType.Entry] = optimalEntryFold;
       			optimalFoldRun[SignalType.Exit] = optimalExitFold;

        		optimalFoldRuns.Add(optimalFoldRun);
			}

			Dictionary<SignalType, List<List<ICondition>>> bestFoldCombinations = GetBestCombinationsAcrossRuns(optimalFoldRuns);

		    optimalSets[SignalType.Entry].AddRange(bestFoldCombinations[SignalType.Entry]);
		    optimalSets[SignalType.Exit].AddRange(bestFoldCombinations[SignalType.Exit]);
		}
		#endregion

		#region GetBestCombinationsAcrossRuns()
		private Dictionary<SignalType, List<List<ICondition>>> GetBestCombinationsAcrossRuns(List<Dictionary<SignalType, List<List<ICondition>>>> optimalFoldRuns)
		{
		    Dictionary<SignalType, List<List<ICondition>>> bestCombinations = new Dictionary<SignalType, List<List<ICondition>>>();
		    bestCombinations[SignalType.Entry] = new List<List<ICondition>>();
		    bestCombinations[SignalType.Exit] = new List<List<ICondition>>();

		    foreach (SignalType signalType in new[] { SignalType.Entry, SignalType.Exit })
		    {
		        Dictionary<List<ICondition>, double> combinationScores = new Dictionary<List<ICondition>, double>();

		        foreach (Dictionary<SignalType, List<List<ICondition>>> optimalFoldRun in optimalFoldRuns)
		        {
		            foreach (List<ICondition> combination in optimalFoldRun[signalType])
		            {
		                if (!combinationScores.ContainsKey(combination))
		                {
		                    combinationScores[combination] = 0;
		                }
		                combinationScores[combination] += FitnessFunction(combination);
		            }
		        }

		        List<KeyValuePair<List<ICondition>, double>> sortedCombinations = combinationScores
		            .OrderByDescending(x => x.Value)
		            .ToList();

		        int count = Math.Min(10, sortedCombinations.Count);
		        for (int i = 0; i < count; i++)
		        {
		            bestCombinations[signalType].Add(sortedCombinations[i].Key);
		        }
		    }

		    return bestCombinations;
		}
		#endregion

		#region CalculateConvergenceThreshold()
		private void CalculateConvergenceThreshold()
		{
		    if (foldBestFitness > bestFitness)
		    {
		        bestFitness = foldBestFitness;
		    }

		    if (foldWorstFitness < worstFitness)
		    {
		        worstFitness = foldWorstFitness;
		    }

		    double convergenceThreshold = Math.Min(150, Math.Max(25, 25 + (bestFitness - worstFitness) * 125));
		}
		#endregion

		#region CalculateConfidenceScore()
		private double CalculateConfidenceScore(double fitnessScore, List<double> populationFitnessScores)
		{
		    double mean = populationFitnessScores.Average();
		    double standardDeviation = Math.Sqrt(populationFitnessScores.Average(x => Math.Pow(x - mean, 2)));

		    double zScore = (fitnessScore - mean) / standardDeviation;
		    double confidenceScore = 1 - CumulativeDistributionFunction(zScore);

		    return confidenceScore;
		}

		private double CumulativeDistributionFunction(double zScore)
		{
		    return 1.0 / (1.0 + Math.Exp(-1.702 * zScore));
		}
		#endregion

		#region InitializePopulation()
		private List<List<T>> InitializePopulation<T>(List<T> availableConditions, int populationSize, int minConditions, int maxConditions)
		{
		    List<List<T>> population = new List<List<T>>();
		    Random random = new Random();

		    for (int i = 0; i < populationSize; i++)
		    {
		        int numConditions = random.Next(minConditions, maxConditions + 1);
		        List<T> individual = new List<T>();

		        for (int j = 0; j < numConditions; j++)
		        {
		            int index = random.Next(availableConditions.Count);
					if (individual.Contains(availableConditions[index]))
					{
						continue;
					}

		            individual.Add(availableConditions[index]);
		        }

				if (individual.Count() < numConditions) {
					continue;
				}

		        population.Add(individual);
		    }

		    return population;
		}
		#endregion

		#region GenerateSignals()
		private void GenerateSignals()
		{
		    foreach (Combination entryCombination in optimalEntryCombinations)
		    {
		        if (IsEntryCombinationMet(entryCombination.Conditions))
		        {
		            Signal entrySignal = entries.Get();
		            entrySignal.Set(md.Direction[0], this, SignalType.Entry);
					entrySignal.Combination = entryCombination;
		        }
		    }

		    foreach (Combination exitCombination in optimalExitCombinations)
		    {
		        foreach (Signal entrySignal in cachedEntrySignals)
		        {
		            if (IsExitCombinationMet(exitCombination.Conditions, entrySignal))
		            {
		                Signal exitSignal = exits.Get();
		                exitSignal.Set(entrySignal.Direction, this, SignalType.Exit);
						exitSignal.Combination = exitCombination;
		            }
		        }
		    }
		}
		#endregion

		#region ExitConditions
		#region GetStopLoss
		public double GetStopLoss()
		{
			double minStopLoss = double.MaxValue;

			foreach (Combination exitCombination in optimalExitCombinations)
		    {
		        foreach (ICondition condition in exitCombination.Conditions)
			    {
					if (condition.GetType() == typeof(StopLossCondition))
					{
						if (((StopLossCondition)condition).StopLoss == 0)
						{
							continue;
						}

						minStopLoss = Math.Min(minStopLoss, ((StopLossCondition)condition).StopLoss);
					}

					if (condition.GetType() == typeof(SLTPCondition))
					{
						if (((SLTPCondition)condition).StopLoss == 0)
						{
							continue;
						}

						minStopLoss = Math.Min(minStopLoss, ((SLTPCondition)condition).StopLoss);
					}
			    }
		    }

			return minStopLoss;
		}
		#endregion

		#region GetProfitTarget
		public double GetProfitTarget()
		{
			double minProfitTarget = double.MaxValue;

			foreach (Combination exitCombination in optimalExitCombinations)
		    {
		        foreach (ICondition condition in exitCombination.Conditions)
			    {
					if (condition.GetType() == typeof(ProfitTargetCondition))
					{
						if(((ProfitTargetCondition)condition).ProfitTarget == 0)
						{
							continue;
						}

						minProfitTarget = Math.Min(minProfitTarget, ((ProfitTargetCondition)condition).ProfitTarget);
					}


					if (condition.GetType() == typeof(SLTPCondition))
					{
						if(((SLTPCondition)condition).ProfitTarget == 0)
						{
							continue;
						}

						minProfitTarget = Math.Min(minProfitTarget, ((SLTPCondition)condition).ProfitTarget);
					}
			    }
		    }

			return minProfitTarget;
		}
		#endregion

		#region AreExitConditionsMet
		public bool AreExitConditionsMet(TrendDirection direction)
		{
			foreach (Combination exitCombination in optimalExitCombinations)
		    {
				foreach (Signal entrySignal in cachedEntrySignals)
		        {
					if (IsExitCombinationMet(exitCombination.Conditions, entrySignal) && entrySignal.Direction == direction)
		            {
						return true;
		            }
		        }
		    }

			return false;
		}
		#endregion
		#endregion

		#region Check If Combination Met
		#region IsEntryCombinationMet()
		private bool IsEntryCombinationMet(List<ICondition> entryCombination)
		{
		    foreach (ICondition condition in entryCombination)
		    {
		        if (!((Condition)condition).IsMet(this))
		        {
		            return false;
		        }
		    }
		    return true;
		}
		#endregion

		#region IsExitCombinationMet()
		private bool IsExitCombinationMet(List<ICondition> exitCombination, Signal entrySignal)
		{
		    foreach (ICondition condition in exitCombination)
		    {
		        if (!((ExitCondition)condition).IsMet(this, entrySignal))
		        {
		            return false;
		        }
		    }
		    return true;
		}
		#endregion
		#endregion

		#region PruneSignals()
		private void PruneSignals()
		{
			foreach (int key in entrySignals.GetPools().Keys.Where(k => k < windowStart).ToList())
			{
				entrySignals.PruneGroup(key);
			}

			foreach (int key in exitSignals.GetPools().Keys.Where(k => k < windowStart).ToList())
			{
				exitSignals.PruneGroup(key);
			}

			cachedEntrySignals.RemoveAll(s => s.Bar < windowStart);
			cachedExitSignals.RemoveAll(s => s.Bar < windowStart);
		}
		#endregion

		#region Test Conditions
		#region TestIndividualConditions()
		private void TestIndividualConditions()
		{
			GenerateEntrySignals();

			if (CurrentBar % individualConditionInterval == 0) {
				TestIndividualEntries();
				TestIndividualExits();
			}
		}
		#endregion

		#region GenerateEntrySignals()
		private void GenerateEntrySignals()
		{
			Signal longEntrySignal = entriesOnBar.Get();
			longEntrySignal.Set(TrendDirection.Bullish, this, SignalType.Entry);
			longEntrySignal.Activate();

			Signal shortEntrySignal = entriesOnBar.Get();
			shortEntrySignal.Set(TrendDirection.Bearish, this, SignalType.Entry);
			shortEntrySignal.Activate();
		}
		#endregion

		#region TestIndividualEntries()
		private void TestIndividualEntries()
		{
			if (entrySignals.GetPool(CurrentBar).ActiveItems.Count() > 0)
			{
				return;
			}

//			List<Condition> entryConditionsRandomized = InitializePopulation(entryConditions, individualPopulation, 1, 1).Select(c => (Condition) c[0]).ToList();

			List<Condition> entryConditionsRandomized = entryConditions.Select(c => (Condition) c).ToList();

			foreach (Condition entryCondition in entryConditionsRandomized)
		    {
		        if (entryCondition.IsMet(this))
		        {
		            Signal entrySignal = entrySignals.Get(CurrentBar);
		            entrySignal.Set(md.Direction[0], this, SignalType.Entry);
		            entrySignal.EntryConditions[entryCondition] = new List<Parameter>();
		        }
		    }
		}
		#endregion

		#region TestIndividualExits()
		private void TestIndividualExits()
		{
			if (exitSignals.GetPool(CurrentBar).ActiveItems.Count() > 0)
			{
				return;
			}

			List<ExitCondition> exitConditionsRandomized = InitializePopulation(exitConditions, individualPopulation, 1, 1).Select(c => (ExitCondition) c[0]).ToList();

//			List<ExitCondition> exitConditionsRandomized = exitConditions.Select(c => (ExitCondition) c).ToList();

			foreach (ExitCondition exitCondition in exitConditionsRandomized)
			{
				foreach (Signal entryOnBar in entriesOnBar.ActiveItems)
				{
					if (exitCondition.IsMet(this, entryOnBar))
	                {
	                    Signal exitSignal = exitSignals.Get(CurrentBar);
	                    exitSignal.Set(entryOnBar.Direction, this, SignalType.Exit);
						exitSignal.RelatedSignal = entryOnBar;

						List<Parameter> parameters = new List<Parameter>();
						foreach (ParameterType parameterType in exitCondition.ParameterTypes)
						{
							Parameter parameter = new Parameter();
							parameter.Set(parameterType, exitCondition.ParameterValues[parameterType.Name]);
							parameters.Add(parameter);
						}

	                    exitSignal.ExitConditions[exitCondition] = parameters;
	                }
				}
			}
		}
		#endregion
		#endregion

		#region SplitDataIntoFolds()
		private List<Tuple<int, int>> SplitDataIntoFolds()
		{

		    List<Tuple<int, int>> folds = new List<Tuple<int, int>>();
		    int dataSize = windowEnd - windowStart + 1;
		    int foldSize = dataSize / numFolds;

		    for (int i = 0; i < numFolds; i++)
		    {
		        int foldStart = windowStart + i * foldSize;
		        int foldEnd = (i == numFolds - 1) ? windowEnd : foldStart + foldSize - 1;
		        folds.Add(new Tuple<int, int>(foldStart, foldEnd));
		    }

		    return folds;
		}
		#endregion

		#region CombineOptimalCombinations()
		private List<List<T>> CombineOptimalCombinations<T>(List<List<T>> optimalSet, int take = 5)
		{
		    Dictionary<List<T>, int> combinationVotes = new Dictionary<List<T>, int>();
		    Dictionary<List<T>, double> combinationScores = new Dictionary<List<T>, double>();

		    foreach (List<T> combination in optimalSet)
		    {
		        if (!combinationVotes.ContainsKey(combination))
		        {
		            combinationVotes[combination] = 0;
		        }
		        combinationVotes[combination]++;
		    }

		    foreach (List<T> combination in optimalSet)
		    {
		        if (!combinationScores.ContainsKey(combination))
		        {
		            combinationScores[combination] = 0;
		        }
		        combinationScores[combination] += 1.0 / optimalSet.Count;
		    }

		    Dictionary<List<T>, double> combinedScores = new Dictionary<List<T>, double>();
		    foreach (var combination in combinationVotes.Keys)
		    {
		        double votingScore = (double)combinationVotes[combination] / optimalSet.Count;
		        double averagingScore = combinationScores[combination];
		        double combinedScore = (votingScore + averagingScore) / 2;
		        combinedScores[combination] = combinedScore;
		    }

		    List<KeyValuePair<List<T>, double>> sortedCombinations = combinedScores
		        .OrderByDescending(x => x.Value)
		        .ToList();

		    List<List<T>> optimalCombinations = new List<List<T>>();
		    int count = Math.Min(take, sortedCombinations.Count);
		    for (int i = 0; i < count; i++)
		    {
		        optimalCombinations.Add(sortedCombinations[i].Key);
		    }

		    return optimalCombinations;
		}
		#endregion

		#region Generate Parameter Combinations
		#region GenerateParameterCombinations()
		private List<List<Parameter>> GenerateParameterCombinations(List<ParameterType> parameterTypes)
		{
		    List<List<Parameter>> combinations = new List<List<Parameter>>();
		    GenerateCombinationsHelper(parameterTypes, 0, new List<Parameter>(), combinations);
		    return combinations;
		}
		#endregion

		#region GenerateCombinationsHelper()
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
		#endregion

		#region Generate Simulated Trades
		#region GenerateSimulatedTradesForEntryCombination()
		private List<SimTrade> GenerateSimulatedTradesForEntryCombination(int foldStart, int foldEnd, List<ICondition> combination)
		{
		    List<SimTrade> trades = new List<SimTrade>();
			Dictionary<Signal, bool> barEntrySignals = new Dictionary<Signal, bool>();

		    for (int bar = foldStart; bar <= foldEnd; bar++)
		    {
				bool allConditionsMet = true;
				barEntrySignals.Clear();

				foreach (Condition condition in combination)
				{
					bool conditionMet = false;

					foreach (Signal entrySignal in foldEntrySignals.Where(s => s.Bar == bar))
					{
						if (entrySignal.EntryConditions.ContainsKey(condition))
		                {
							barEntrySignals[entrySignal] = true;

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

				if (!allConditionsMet)
				{
					continue;
				}

				List<Signal> futureBarExitSignals = exitSignals.GetPools()
					.Where(p => p.Key > bar)
					.ToDictionary(
						p => p.Key,
						p => p.Value.ActiveItems.Where(s => s.RelatedSignal.Bar == bar).ToList())
					.Values
					.SelectMany(p => p)
					.ToList();
				foreach (Signal entrySignal in barEntrySignals.Keys)
				{
					foreach (Signal exitSignal in futureBarExitSignals)
					{
						if (exitSignal.RelatedSignal.Direction != entrySignal.Direction)
						{
							continue;
						}

						SimTrade trade = new SimTrade();
                        trade.Activate();
                        trade.Set(this);
                        trade.EntrySignal = entrySignal;
                        trade.ExitSignal = exitSignal;
                        trades.Add(trade);
					}
				}
		    }

		    return trades;
		}
		#endregion

		#region GenerateSimulatedTradesForExitCombination()
		private List<SimTrade> GenerateSimulatedTradesForExitCombination(int foldStart, int foldEnd, List<ICondition> combination)
		{
		    List<SimTrade> trades = new List<SimTrade>();
			Dictionary<Signal, bool> barExitSignals = new Dictionary<Signal, bool>();

		    for (int bar = foldStart; bar <= foldEnd; bar++)
		    {
				bool allConditionsMet = true;
				barExitSignals.Clear();

				foreach (ExitCondition condition in combination)
				{
					bool conditionMet = false;

					foreach (Signal exitSignal in foldExitSignals.Where(s => s.Bar == bar))
					{
						if (exitSignal.ExitConditions.ContainsKey(condition))
		                {
							barExitSignals[exitSignal] = true;

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

				if (!allConditionsMet)
				{
					continue;
				}

				foreach (Signal exitSignal in barExitSignals.Keys)
				{
					SimTrade trade = new SimTrade();
	                trade.Activate();
	                trade.Set(this);
	                trade.EntrySignal = exitSignal.RelatedSignal;
	                trade.ExitSignal = exitSignal;
	                trades.Add(trade);
				}
		    }

		    return trades;
		}
		#endregion
		#endregion

		#region CalculatePerformanceMetrics
		private PerformanceMetrics CalculatePerformanceMetrics(List<SimTrade> trades)
		{
		    PerformanceMetrics metrics = new PerformanceMetrics();

			int minTradesThreshold = (int) Math.Ceiling(rollingWindowSize * minTradeThresholdMultiplier);
		    if (trades.Count < minTradesThreshold || trades.Count == 0)
		    {
		        return metrics;
		    }

		    foreach (SimTrade trade in trades)
		    {
		        trade.CalculatePerformance();
		    }

			// Trades Lists
			List<double> tradesNetProfit = trades.Select(t => t.Performance.NetProfit).ToList();
			List<double> tradesMaxAdverseExcursion = trades.Select(t => t.Performance.MaxAdverseExcursion).ToList();
			List<double> tradesFavorableExcursion = trades.Select(t => t.Performance.MaxFavorableExcursionDifference).ToList();
			List<double> tradesTradeScore = trades.Select(t => t.Performance.TradeScore).ToList();

			// Calculate Total Metrics
			double totalNetProfit = tradesNetProfit.Sum();
			double totalMaxAdverseExcursion = tradesMaxAdverseExcursion.Sum();
			double totalMaxFavorableExcursion = tradesFavorableExcursion.Sum();
			double totalTradeScore = tradesTradeScore.Sum();

			// Calcuate Average Metrics
		    double averageNetProfit = tradesNetProfit.Average();
		    double averageMaxAdverseExcursion = totalMaxAdverseExcursion / trades.Count;
		    double averageMaxFavorableExcursion = totalMaxFavorableExcursion / trades.Count;
		    double averageTradeScore = totalTradeScore / trades.Count;

			// Calculate More Metrics
			double netProfitStdDev = CalcuatePopulationStandardDeviation(tradesNetProfit);
		    double consistencyScoreValue = (
				CalculateConsistencyScore(averageNetProfit, tradesNetProfit) +
				CalculateConsistencyScore(averageMaxAdverseExcursion, tradesMaxAdverseExcursion) +
				CalculateConsistencyScore(averageMaxFavorableExcursion, tradesFavorableExcursion) +
				CalculateConsistencyScore(averageTradeScore, tradesTradeScore)) / 4;
		    double sharpeRatio = (averageNetProfit - 0.02) / netProfitStdDev;
			double maxDrawdown = CalculateMaxDrawdown(trades);
			double winLossRatio = CalculateWinLossRatio(trades);

			// Set Combination Metrics
			combinationMetrics.NetProfit.Add(averageNetProfit);
			combinationMetrics.TradeScore.Add(averageTradeScore);
			combinationMetrics.MaxAdverseExcursion.Add(averageMaxAdverseExcursion);
			combinationMetrics.MaxFavorableExcursion.Add(averageMaxFavorableExcursion);
			combinationMetrics.ConsistencyScore.Add(consistencyScoreValue);
			combinationMetrics.SharpeRatio.Add(sharpeRatio);
			combinationMetrics.MaxDrawdown.Add(maxDrawdown);
			combinationMetrics.WinLossRatio.Add(winLossRatio);

			// Calculate Overfitting Penalty
			double averageVariance = CalculateAverageVarianceForMetrics(combinationMetrics);
			double overfittingPenalty = 1.0 - averageVariance;

			// Normalize the scores based on the population of metric values
			double netProfitScore = NormalizeScore(averageNetProfit, combinationMetrics.NetProfit);
			double tradeScore = NormalizeScore(averageTradeScore, combinationMetrics.TradeScore);
			double maxAdverseExcursionScore = 1.0 - NormalizeScore(averageMaxAdverseExcursion, combinationMetrics.MaxAdverseExcursion);
		    double maxFavorableExcursionScore = 1.0 - NormalizeScore(averageMaxFavorableExcursion, combinationMetrics.MaxFavorableExcursion);
		    double consistencyScore = NormalizeScore(consistencyScoreValue, combinationMetrics.ConsistencyScore);
		    double sharpeRatioScore = NormalizeScore(sharpeRatio, combinationMetrics.SharpeRatio);
		    double maxDrawdownScore = 1.0 - NormalizeScore(maxDrawdown, combinationMetrics.MaxDrawdown);
		    double winLossRatioScore = NormalizeScore(winLossRatio, combinationMetrics.WinLossRatio);

		    // Assign weights to each score based on their importance
		    double netProfitWeight = 0.6;
//		    double tradeScoreWeight = 0.05;
		    double maxAdverseExcursionWeight = 0.15;
		    double maxFavorableExcursionWeight = 0.05;
		    double consistencyWeight = 0.05;
		    double sharpeRatioWeight = 0.05;
		    double maxDrawdownWeight = 0.05;
		    double winLossRatioWeight = 0.05;

		    // Calculate the final fitness score
		    double fitnessScore = (netProfitScore * netProfitWeight) +
//                          (tradeScore * tradeScoreWeight) +
                          (maxAdverseExcursionScore * maxAdverseExcursionWeight) +
                          (maxFavorableExcursionScore * maxFavorableExcursionWeight) +
                          (consistencyScore * consistencyWeight) +
                          (sharpeRatioScore * sharpeRatioWeight) +
                          (maxDrawdownScore * maxDrawdownWeight) +
                          (winLossRatioScore * winLossRatioWeight);

			if (double.IsNaN(fitnessScore))
			{
				fitnessScore = 0.5;
			}

			fitnessScore *= overfittingPenalty;

		    metrics.FitnessScore = fitnessScore;
		    metrics.AverageNetProfit = averageNetProfit;
		    metrics.AverageMaxAdverseExcursion = averageMaxAdverseExcursion;
		    metrics.ConsistencyScore = consistencyScore;
			metrics.NetProfit = totalNetProfit;
			metrics.MaxAdverseExcursion = totalMaxAdverseExcursion;
			metrics.MaxFavorableExcursion = totalMaxFavorableExcursion;
			metrics.SharpeRatio = sharpeRatio;
			metrics.MaxDrawdown = maxDrawdown;
			metrics.WinLossRatio = winLossRatio;

		    return metrics;
		}

		private double CalculateAverageVarianceForMetrics(CombinationMetrics combinationMetrics)
		{
		    List<double> variances = new List<double>();

		    variances.Add(CalculateVariance(combinationMetrics.NetProfit));
		    variances.Add(CalculateVariance(combinationMetrics.TradeScore));
		    variances.Add(CalculateVariance(combinationMetrics.MaxAdverseExcursion));
		    variances.Add(CalculateVariance(combinationMetrics.MaxFavorableExcursion));
		    variances.Add(CalculateVariance(combinationMetrics.ConsistencyScore));
		    variances.Add(CalculateVariance(combinationMetrics.SharpeRatio));
		    variances.Add(CalculateVariance(combinationMetrics.MaxDrawdown));
		    variances.Add(CalculateVariance(combinationMetrics.WinLossRatio));

		    // Remove any infinite or NaN values from the variances list
		    variances = variances.Where(v => !double.IsInfinity(v) && !double.IsNaN(v)).ToList();

		    // If all variances are infinite or NaN, return a default value (e.g., 0)
		    if (variances.Count == 0)
		    {
		        return 0;
		    }

		    double minVariance = variances.Min();
		    double maxVariance = variances.Max();

		    // If the minimum and maximum variances are equal, return a default value (e.g., 0)
		    if (minVariance == maxVariance)
		    {
		        return 0;
		    }

		    List<double> normalizedVariances = variances.Select(v => NormalizeValue(v, minVariance, maxVariance)).ToList();

		    return normalizedVariances.Average();
		}

		private double NormalizeValue(double value, double min, double max)
		{
		    double epsilon = 1e-8;
		    return (value - min) / (max - min + epsilon);
		}

		private double CalculateVariance(List<double> values)
		{
		    double average = values.Average();
		    double sumOfSquares = values.Sum(x => Math.Pow(x - average, 2));

			return sumOfSquares / (values.Count - 1);
		}

		private double CalculateConsistencyScore(double averageValue, List<double> values)
		{
			double stdDev = CalcuatePopulationStandardDeviation(values);

			return 1.0 - (stdDev / Math.Abs(averageValue));
		}

		private double CalcuatePopulationStandardDeviation(List<double> population)
		{
			return Math.Sqrt(CalculateVariance(population));
		}

		private double CalculateMaxDrawdown(List<SimTrade> trades)
		{
			double maxDrawdown = 0;
		    double peakEquity = 0;
		    double currentEquity = 0;

		    foreach (SimTrade trade in trades)
		    {
		        currentEquity += trade.Performance.NetProfit;
		        if (currentEquity > peakEquity)
		            peakEquity = currentEquity;
		        double drawdown = peakEquity - currentEquity;
		        if (drawdown > maxDrawdown)
		            maxDrawdown = drawdown;
		    }

			return maxDrawdown;
		}

		private double CalculateWinLossRatio(List<SimTrade> trades)
		{
		    int winCount = trades.Count(t => t.Performance.NetProfit > 0);
		    int lossCount = trades.Count(t => t.Performance.NetProfit < 0);

			return (lossCount > 0) ? (double) winCount / lossCount : winCount;
		}

		private double NormalizeScore(double value, List<double> population)
		{
		    // Sort the population in ascending order
		    List<double> sortedPopulation = new List<double>(population);
		    sortedPopulation.Sort();

		    // Find the rank of the value in the sorted population
		    int rank = sortedPopulation.BinarySearch(value);
		    if (rank < 0)
		    {
		        rank = ~rank;
		    }

		    // Calculate the normalized score based on the rank
		    double normalizedScore = (double)rank / (population.Count - 1);

		    return normalizedScore;
		}
		#endregion
	}

	#region Trades
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
	#endregion

	#region Signal
	public class Signal : IPoolable
	{
		#region Variables
		public Dictionary<ExitCondition, List<Parameter>> ExitConditions = new Dictionary<ExitCondition, List<Parameter>>();
		public Dictionary<Condition, List<Parameter>> EntryConditions = new Dictionary<Condition, List<Parameter>>();
		public Dictionary<string, double> Indicators = new Dictionary<string, double>();
		public Combination Combination { get; set; }
		public SignalType Type { get; set; }
		public DateTime Time { get; set; }
		public int Bar { get; set; }
		public SignalGenerator Source { get; set; }
		public TrendDirection Direction { get; set; }
		public double Price { get; set; }
		public bool IsActive { get; set; }
		public Signal RelatedSignal { get; set; }
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
	#endregion

	#region Performance
	public class TradePerformance
	{
		#region Variables
		public int BarsInTrade { get; set; }
	    public double MaxAdverseExcursion { get; set; }
	    public double MaxFavorableExcursion { get; set; }
	    public double MaxFavorableExcursionDifference { get; set; }
		public double NetProfit { get; set; }
	    public int TradeDuration { get; set; }
		public double TradeScore { get; set; }
		#endregion

		#region Calculate()
		public void Calculate(Signal entry, Signal exit)
		{
			BarsInTrade = exit.Bar - entry.Bar;
			int barsAgoEntry = entry.Source.CurrentBar - entry.Bar;
			double highestHigh = BarsInTrade > 0 ? entry.Source.MAX(entry.Source.High, BarsInTrade)[barsAgoEntry] : entry.Source.High[barsAgoEntry];
			double lowestLow = BarsInTrade > 0 ? entry.Source.MIN(entry.Source.Low, BarsInTrade)[barsAgoEntry] : entry.Source.Low[barsAgoEntry];
			MaxAdverseExcursion = entry.Direction == TrendDirection.Bullish ? entry.Price - lowestLow : highestHigh - entry.Price;
			MaxFavorableExcursion = entry.Direction == TrendDirection.Bullish ? highestHigh - entry.Price : entry.Price - lowestLow;
			NetProfit = entry.Direction == TrendDirection.Bullish ? exit.Price - entry.Price : entry.Price - exit.Price;
			MaxFavorableExcursionDifference = MaxFavorableExcursion - NetProfit;
			TradeDuration = (exit.Time - entry.Time).Seconds;

			double netProfitScore = NetProfit;
		    double maxAdverseExcursionScore = 1.0 - MaxAdverseExcursion / entry.Indicators["ATR"];
		    double tradeDurationScore = 1.0 - TradeDuration / (24 * 60 * 60);
			double maxFavorableExcursionScore = 1.0 - (MaxFavorableExcursion - netProfitScore);
			TradeScore = netProfitScore + maxAdverseExcursionScore + tradeDurationScore + maxFavorableExcursionScore;
		}
		#endregion
	}

	public class PerformanceMetrics
	{
	    public double MaxAdverseExcursion { get; set; }
	    public double MaxFavorableExcursion { get; set; }
		public double NetProfit { get; set; }
		public double TradeScore { get; set; }
		public double AverageNetProfit { get; set; }
		public double FitnessScore { get; set; }
		public double AverageMaxAdverseExcursion { get; set; }
		public double ConsistencyScore { get; set; }
		public double SharpeRatio { get; set; }
		public double MaxDrawdown { get; set; }
		public double WinLossRatio { get; set; }
	}

	public class CombinationMetrics
	{
		public List<double> NetProfit = new List<double>();
		public List<double> TradeScore = new List<double>();
		public List<double> MaxAdverseExcursion = new List<double>();
		public List<double> MaxFavorableExcursion = new List<double>();
		public List<double> ConsistencyScore = new List<double>();
		public List<double> SharpeRatio = new List<double>();
		public List<double> MaxDrawdown = new List<double>();
		public List<double> WinLossRatio = new List<double>();

		public void Clear()
		{
			NetProfit.Clear();
			TradeScore.Clear();
			MaxAdverseExcursion.Clear();
			MaxFavorableExcursion.Clear();
			ConsistencyScore.Clear();
			SharpeRatio.Clear();
			MaxDrawdown.Clear();
			WinLossRatio.Clear();
		}
	}
	#endregion

	#region Parameters
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
	#endregion

	#region Conditions
	#region Base Classes
	#region Interface
	public interface ICondition
	{
		SignalType Type { get; }

	    void Reset();
	    void SetParameterValue(ParameterType type, double value);
	}
	#endregion

	#region Entry Condition
	public abstract class Condition : ICondition
	{
		public SignalType Type { get { return SignalType.Entry; } }

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
	public abstract class ExitCondition : ICondition
	{
		public SignalType Type { get { return SignalType.Exit; } }

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

	#region Strategies/Combinations

	#region TrendFollowingStrategy206Condition
	public class TrendFollowingStrategy206Condition : Condition
	{
		public override bool IsMet(SignalGenerator generator)
		{
			TrendDirection Direction = generator.md.Direction[0];

			bool emaFastRising 		= generator.emaFast[0] > generator.emaFast[1];
			bool emaFastFalling		= generator.emaFast[0] < generator.emaFast[1];
			bool emaSlowRising		= generator.emaSlow[0] > generator.emaSlow[1];
			bool emaSlowFalling		= generator.emaSlow[0] < generator.emaSlow[1];
			bool maRising			= emaFastRising && emaSlowRising;
			bool maFalling			= emaFastFalling && emaSlowFalling;

			bool lowChop			= generator.chop[0] < 38.2;
			bool highChop			= generator.chop[0] > 61.8;
			bool validChoppiness 	= lowChop || highChop;

			bool rising1			= generator.Close[0] > generator.Close[1];
			bool rising2			= generator.Close[1] > generator.Close[2];
			bool rising3			= generator.Close[2] > generator.Close[3];
			bool rising				= rising1 && rising2 && rising3;

			bool falling1			= generator.Close[0] < generator.Close[1];
			bool falling2			= generator.Close[1] < generator.Close[2];
			bool falling3			= generator.Close[2] < generator.Close[3];
			bool falling			= falling1 && falling2 && falling3;

			bool newHigh			= generator.Close[0] > generator.MAX(generator.High, 10)[1];
			bool newLow				= generator.Close[0] < generator.MIN(generator.Low, 10)[1];

			bool higherHigh1		= generator.High[0] > generator.High[1];
			bool higherHigh2		= generator.High[1] > generator.High[2];
			bool higherHigh3		= generator.High[2] > generator.High[3];
			bool higherHigh			= higherHigh1 && higherHigh2 && higherHigh3;

			bool lowerLow1			= generator.Low[0] < generator.Low[1];
			bool lowerLow2			= generator.Low[1] < generator.Low[2];
			bool lowerLow3			= generator.Low[2] < generator.Low[3];
			bool lowerLow			= lowerLow1 && lowerLow2 && lowerLow3;

			bool highestInTrend		= generator.MAX(generator.High, 3)[0] >= generator.MAX(generator.High, 10)[0];
			bool lowestInTrend		= generator.MIN(generator.Low, 3)[0] <= generator.MIN(generator.Low, 10)[0];

			bool upTrend 			= higherHigh && highestInTrend;
			bool downTrend			= lowerLow && lowestInTrend;

			bool longPatternMatched 	= maRising && rising && newHigh && validChoppiness && upTrend;
			bool shortPatternMatched	= maFalling && falling && newLow && validChoppiness && downTrend;

			if (Direction == TrendDirection.Bullish)
			{
				return longPatternMatched;
			}

			return shortPatternMatched;
		}
	}
	#endregion

	#region UpDownTrendCondition
	public class UpDownTrendCondition : Condition
	{
		public override bool IsMet(SignalGenerator generator)
		{
			TrendDirection Direction = generator.md.Direction[0];

			bool higherHigh1		= generator.High[0] > generator.High[1];
			bool higherHigh2		= generator.High[1] > generator.High[2];
			bool higherHigh3		= generator.High[2] > generator.High[3];
			bool higherHigh			= higherHigh1 && higherHigh2 && higherHigh3;

			bool lowerLow1			= generator.Low[0] < generator.Low[1];
			bool lowerLow2			= generator.Low[1] < generator.Low[2];
			bool lowerLow3			= generator.Low[2] < generator.Low[3];
			bool lowerLow			= lowerLow1 && lowerLow2 && lowerLow3;

			bool highestInTrend		= generator.MAX(generator.High, 3)[0] >= generator.MAX(generator.High, 10)[0];
			bool lowestInTrend		= generator.MIN(generator.Low, 3)[0] <= generator.MIN(generator.Low, 10)[0];

			if (Direction == TrendDirection.Bullish)
			{
				return higherHigh && highestInTrend;
			}

			return lowerLow && lowestInTrend;
		}
	}
	#endregion

	#region ValidChoppinessCondition
	public class ValidChoppinessCondition : Condition
	{
		public override bool IsMet(SignalGenerator generator)
		{
			bool lowChop			= generator.chop[0] < 38.2;
			bool highChop			= generator.chop[0] > 61.8;

			return lowChop || highChop;
		}
	}
	#endregion

	#region NewHighLowCondition
	public class NewHighLowCondition : Condition
	{
		public override bool IsMet(SignalGenerator generator)
		{
			TrendDirection Direction = generator.md.Direction[0];

			bool newHigh			= generator.Close[0] > generator.MAX(generator.High, 10)[1];
			bool newLow				= generator.Close[0] < generator.MIN(generator.Low, 10)[1];

			if (Direction == TrendDirection.Bullish)
			{
				return newHigh;
			}

			return newLow;
		}
	}
	#endregion

	#region TrendRisingFallingCondition
	public class TrendRisingFallingCondition : Condition
	{
		public override bool IsMet(SignalGenerator generator)
		{
			TrendDirection Direction = generator.md.Direction[0];

			bool rising1			= generator.Close[0] > generator.Close[1];
			bool rising2			= generator.Close[1] > generator.Close[2];
			bool rising3			= generator.Close[2] > generator.Close[3];
			bool rising				= rising1 && rising2 && rising3;

			bool falling1			= generator.Close[0] < generator.Close[1];
			bool falling2			= generator.Close[1] < generator.Close[2];
			bool falling3			= generator.Close[2] < generator.Close[3];
			bool falling			= falling1 && falling2 && falling3;

			if (Direction == TrendDirection.Bullish)
			{
				return rising;
			}

			return falling;
		}
	}
	#endregion

	#region MARisingFallingCondition
	public class MARisingFallingCondition : Condition
	{
		public override bool IsMet(SignalGenerator generator)
		{
			TrendDirection Direction = generator.md.Direction[0];

			bool emaFastRising 		= generator.emaFast[0] > generator.emaFast[1];
			bool emaFastFalling		= generator.emaFast[0] < generator.emaFast[1];
			bool emaSlowRising		= generator.emaSlow[0] > generator.emaSlow[1];
			bool emaSlowFalling		= generator.emaSlow[0] < generator.emaSlow[1];
			bool maRising			= emaFastRising && emaSlowRising;
			bool maFalling			= emaFastFalling && emaSlowFalling;

			if (Direction == TrendDirection.Bullish)
			{
				return maRising;
			}

			return maFalling;
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

	#region CounterTrendBroadChannelCondition
	public class CounterTrendBroadChannelCondition : ExitCondition
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
		public double ProfitTarget = 0;

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
			ProfitTarget = generator.avgAtrFast[0] * profitTargetMultiplier;

			if (ParameterValues.Keys.Contains("HighATRMultiplier") && ParameterValues["HighATRMultiplier"] > 1)
			{
				if ((generator.atr[0] - generator.avgAtr[0]) > generator.stdDevAtr[0])
				{
					ProfitTarget = ProfitTarget * ParameterValues["HighATRMultiplier"];
				}
			}

			double distanceMoved = entry.Direction == TrendDirection.Bullish
					? generator.Close[0] - entry.Price : entry.Price - generator.Close[0];

			return distanceMoved >= ProfitTarget;
		}
	}
	#endregion

	#region StopLossCondition
	public class StopLossCondition : ExitCondition
	{
		public double StopLoss = 0;

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
			StopLoss = generator.avgAtrFast[0] * stopLossMultiplier;
			double distanceMoved = entry.Direction == TrendDirection.Bullish
					? generator.Close[0] - entry.Price : entry.Price - generator.Close[0];

			if (distanceMoved > 0) {
				return false;
			}

			return Math.Abs(distanceMoved) >= StopLoss;
		}
	}
	#endregion

	#region SLTPCondition
	public class SLTPCondition : ExitCondition
	{
		public double StopLoss = 0;
		public double ProfitTarget = 0;

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

			if (!ParameterValues.Keys.Contains("ProfitTargetMultiplier"))
			{
				return false;
			}

			double stopLossMultiplier = ParameterValues["StopLossMultiplier"];
			StopLoss = generator.avgAtrFast[0] * stopLossMultiplier;

			double profitTargetMultiplier = ParameterValues["ProfitTargetMultiplier"];
			ProfitTarget = generator.avgAtrFast[0] * profitTargetMultiplier;

			if (ParameterValues.Keys.Contains("HighATRMultiplier") && ParameterValues["HighATRMultiplier"] > 1)
			{
				if ((generator.atr[0] - generator.avgAtr[0]) > generator.stdDevAtr[0])
				{
					ProfitTarget = ProfitTarget * ParameterValues["HighATRMultiplier"];
				}
			}

			double distanceMoved = entry.Direction == TrendDirection.Bullish
					? generator.Close[0] - entry.Price : entry.Price - generator.Close[0];

			if (distanceMoved >= ProfitTarget)
			{
				return true;
			}

			if (distanceMoved > 0)
			{
				return false;
			}

			return Math.Abs(distanceMoved) >= StopLoss;
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

	#region Combination
	public class Combination
	{
		public List<ICondition> Conditions;
		public double FitnessScore;
		public double ConfidenceScore;
	}
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
