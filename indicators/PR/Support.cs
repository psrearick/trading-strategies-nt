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

	#region SignalType
	public enum SignalType {
		Entry,
		Exit
	};
	#endregion

	#region Timetrame
	public enum Timeframe {
		High,
		Low
	};
	#endregion
	#endregion

	#region Utils
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
	#endregion

	#region Helpers
	public class Helpers
	{
		#region GenerateRangeOfValues()
		public static List<double> GenerateRangeOfValues(double minValue, double maxValue, double stepSize)
		{
		    List<double> values = new List<double>();
		    for (double value = minValue; value <= maxValue; value += stepSize)
		    {
		        values.Add(value);
		    }
		    return values;
		}
		#endregion

		#region StandardDeviation()
		public static double StandardDeviation(IEnumerable<double> values)
		{
		    double avg = values.Average();
		    double sum = values.Sum(d => Math.Pow(d - avg, 2));
		    double denominator = values.Count() - 1;
		    return Math.Sqrt(sum / denominator);
		}
		#endregion
	}
	#endregion

	#region Particle Swarm
	#region Particle
	public class Particle
	{
	    public double[] Position { get; set; }
	    public double[] Velocity { get; set; }
	    public double[] BestPosition { get; set; }
	    public double BestFitness { get; set; }

	    public Particle(int dimensions)
	    {
	        Position = new double[dimensions];
	        Velocity = new double[dimensions];
	        BestPosition = new double[dimensions];
	        BestFitness = double.MinValue;
	    }
	}
	#endregion

	#region ParticleSwarmOptimization
	public class ParticleSwarmOptimization
	{
	    private const double C1 = 2.0;
	    private const double C2 = 2.0;
	    private const double W = 0.7;

	    public static double[] Optimize(Func<double[], double> fitnessFunction, double[] lowerBounds, double[] upperBounds, int numParticles, int maxIterations)
	    {
	        int dimensions = lowerBounds.Length;
	        Particle[] swarm = InitializeSwarm(numParticles, dimensions, lowerBounds, upperBounds);
	        double[] globalBestPosition = new double[dimensions];
	        double globalBestFitness = double.MinValue;
			Random rand = new Random();

	        for (int i = 0; i < maxIterations; i++)
	        {
	            foreach (Particle particle in swarm)
	            {
	                double fitness = fitnessFunction(particle.Position);

	                if (fitness > particle.BestFitness)
	                {
	                    particle.BestPosition = (double[])particle.Position.Clone();
	                    particle.BestFitness = fitness;
	                }

	                if (fitness > globalBestFitness)
	                {
	                    globalBestPosition = (double[])particle.Position.Clone();
	                    globalBestFitness = fitness;
	                }
	            }

	            foreach (Particle particle in swarm)
	            {
	                for (int j = 0; j < dimensions; j++)
	                {
	                    double r1 = rand.NextDouble();
	                    double r2 = rand.NextDouble();

	                    particle.Velocity[j] = W * particle.Velocity[j]
	                        + C1 * r1 * (particle.BestPosition[j] - particle.Position[j])
	                        + C2 * r2 * (globalBestPosition[j] - particle.Position[j]);

	                    particle.Position[j] += particle.Velocity[j];
	                    particle.Position[j] = Math.Max(lowerBounds[j], Math.Min(particle.Position[j], upperBounds[j]));
	                }
	            }
	        }

	        return globalBestPosition;
	    }

	    private static Particle[] InitializeSwarm(int numParticles, int dimensions, double[] lowerBounds, double[] upperBounds)
	    {
			Random rand = new Random();
	        Particle[] swarm = new Particle[numParticles];

	        for (int i = 0; i < numParticles; i++)
	        {
	            Particle particle = new Particle(dimensions);

	            for (int j = 0; j < dimensions; j++)
	            {
	                particle.Position[j] = rand.NextDouble() * (upperBounds[j] - lowerBounds[j]) + lowerBounds[j];
	                particle.Velocity[j] = rand.NextDouble() * (upperBounds[j] - lowerBounds[j]) * 0.1;
	            }

	            swarm[i] = particle;
	        }

	        return swarm;
	    }
	}
	#endregion
	#endregion

	#region TStudent
	public static class TStudent
	{
	    public static double CDF(double degreesOfFreedom, double t)
	    {
	        if (degreesOfFreedom <= 0)
	        {
	            throw new ArgumentException("Degrees of freedom must be positive.");
	        }

	        double x = degreesOfFreedom / (degreesOfFreedom + Math.Pow(t, 2));
	        double a = degreesOfFreedom / 2;
	        double b = 0.5;

	        double cdf = RegularizedIncompleteBeta(a, b, x);

	        if (t < 0)
	        {
	            cdf = 1 - cdf;
	        }

	        return cdf;
	    }

	    private static double RegularizedIncompleteBeta(double a, double b, double x)
	    {
	        const double Epsilon = 1e-14;
	        const int MaxIterations = 100;

	        if (a <= 0 || b <= 0)
	        {
	            throw new ArgumentException("Parameters a and b must be positive.");
	        }

	        if (x < 0 || x > 1)
	        {
	            throw new ArgumentException("Parameter x must be between 0 and 1 (inclusive).");
	        }

	        if (x == 0)
	        {
	            return 0;
	        }

	        if (x == 1)
	        {
	            return 1;
	        }

	        double logX = Math.Log(x);
	        double logOneMinusX = Math.Log(1 - x);
	        double logBetaAB = LogBeta(a, b);

	        double sum = 1;
	        double term = 1;
	        double n = 0;

	        while (Math.Abs(term) > Epsilon && n < MaxIterations)
	        {
	            term *= (a + n) * x / (a + b + n);
	            sum += term;
	            n++;
	        }

	        return Math.Exp(a * logX + b * logOneMinusX - logBetaAB) * sum;
	    }

	    private static double LogBeta(double a, double b)
	    {
	        return LogGamma(a) + LogGamma(b) - LogGamma(a + b);
	    }

	    private static double LogGamma(double x)
	    {
	        double[] coefficients = {
	            76.18009172947146,
	            -86.50532032941677,
	            24.01409824083091,
	            -1.231739572450155,
	            0.1208650973866179e-2,
	            -0.5395239384953e-5
	        };

	        double y = x;
	        double tmp = x + 5.5;
	        tmp -= (x + 0.5) * Math.Log(tmp);
	        double seriesSum = 1.000000000190015;

	        for (int i = 0; i < coefficients.Length; i++)
	        {
	            y += 1;
	            seriesSum += coefficients[i] / y;
	        }

	        return -tmp + Math.Log(2.5066282746310005 * seriesSum / x);
	    }
	}
	#endregion

	#region Object Pool

	public interface IPoolable
	{
	    bool IsActive { get; set; }
	    void Activate();
	    void Deactivate();
	}

	public class ObjectPool<T> where T : IPoolable, new()
	{
	    private List<T> items = new List<T>();
	    private Func<T> createFunc;
	    public int MaxSize;

	    public ObjectPool(int maxSize, Func<T> createFunc)
	    {
	        this.createFunc = createFunc;
	        MaxSize = maxSize;
	    }

	    public T Get()
	    {
	        T item = items.FirstOrDefault(x => !x.IsActive);
	        if (item == null)
	        {
	            item = createFunc();
	            items.Add(item);
	        }
	        item.Activate();

			if (MaxSize > 0 && items.Count() > MaxSize) {
				items.RemoveAt(0);
			}

	        return item;
	    }

	    public void Release(T item)
	    {
	        item.Deactivate();
	    }

		public void ReleaseAll()
		{
			foreach (T item in items)
			{
				Release(item);
			}
		}

	    public IEnumerable<T> ActiveItems
		{
			get { return items.Where(x => x.IsActive); }
		}

	    public void Prune(bool inactiveOnly = false)
	    {
	        if (inactiveOnly)
	        {
	            items = items.Where(x => x.IsActive).ToList();
	        }
	        else
	        {
	            items.Clear();
	        }
	    }
	}

	public class GroupedObjectPool<TKey, T> where T : IPoolable, new()
	{
	    private Dictionary<TKey, ObjectPool<T>> pools;
	    private Func<T> createFunc;
	    private int maxGroupSize;

	    public GroupedObjectPool(int maxGroupSize, Func<T> createFunc)
	    {
	        this.pools = new Dictionary<TKey, ObjectPool<T>>();
	        this.createFunc = createFunc;
	        this.maxGroupSize = maxGroupSize;
	    }

	    public T Get(TKey key)
	    {
	        return GetPool(key).Get();
	    }

		public ObjectPool<T> GetPool(TKey key)
		{
			ObjectPool<T> pool = null;

			if (!pools.TryGetValue(key, out pool))
	        {
	            pool = new ObjectPool<T>(maxGroupSize, createFunc);
	            pools[key] = pool;
	        }

			return pool;
		}

		public Dictionary<TKey, ObjectPool<T>> GetPools()
		{
			return pools;
		}

	    public void Release(TKey key, T item)
	    {
			ObjectPool<T> pool = null;

	        if (pools.TryGetValue(key, out pool))
	        {
	            pool.Release(item);
	        }
	    }

	    public void ActivateAll(TKey key)
	    {
			ObjectPool<T> pool = null;

	        if (pools.TryGetValue(key, out pool))
	        {
	            foreach (var item in pool.ActiveItems)
	            {
	                item.Activate();
	            }
	        }
	    }

	    public void DeactivateAll(TKey key)
	    {
			ObjectPool<T> pool = null;

	        if (pools.TryGetValue(key, out pool))
	        {
	            foreach (var item in pool.ActiveItems)
	            {
	                item.Deactivate();
	            }
	        }
	    }

	    public void PruneAll(bool inactiveOnly = false)
	    {
	        foreach (var pool in pools.Values)
	        {
	            pool.Prune(inactiveOnly);
	        }

			if (!inactiveOnly) {
				pools.Clear();
			}
	    }

	    public void PruneGroup(TKey key, bool inactiveOnly = false)
	    {
			ObjectPool<T> pool = null;

	        if (pools.TryGetValue(key, out pool))
	        {
	            pool.Prune(inactiveOnly);

				pools.Remove(key);
	        }
	    }
	}
	#endregion

	#region Genetic Optimization
	public class GeneticAlgorithm
	{
	    private readonly Random random = new Random();

		private Utils utils = new Utils();

	   public List<List<T>> Optimize<T>(
		    Func<List<T>, double> fitnessFunction,
		    List<List<T>> initialPopulation,
			double convergenceThreshold,
	        int minPopulationSize,
	        int maxPopulationSize,
	        int minGenerations,
	        int maxGenerations,
	        double minMutationRate,
	        double maxMutationRate,
	        double minCrossoverRate,
	        double maxCrossoverRate,
			int eliteCount)
		{
		    List<List<T>> population = initialPopulation;
	        int populationSize = initialPopulation.Count;
	        double mutationRate = (minMutationRate + maxMutationRate) / 2;
	        double crossoverRate = (minCrossoverRate + maxCrossoverRate) / 2;
	        int generationsSinceImprovement = 0;
	        double bestFitness = double.MinValue;

	        for (int generation = 0; generation < maxGenerations; generation++)
	        {
	            List<double> fitnessScores = EvaluateFitness(population, fitnessFunction);
	            double currentBestFitness = fitnessScores.Max();

	            if (currentBestFitness > bestFitness)
	            {
	                bestFitness = currentBestFitness;
	                generationsSinceImprovement = 0;
	            }
	            else
	            {
	                generationsSinceImprovement++;
	            }

	            if (generationsSinceImprovement >= convergenceThreshold)
	            {
	                break; // Early stopping condition met
	            }

	            // Adjust population size based on fitness improvement
	            if (generationsSinceImprovement > 10 && populationSize < maxPopulationSize)
	            {
	                populationSize = Math.Min(populationSize * 2, maxPopulationSize);
	            }
	            else if (generationsSinceImprovement < 5 && populationSize > minPopulationSize)
	            {
	                populationSize = Math.Max(populationSize / 2, minPopulationSize);
	            }

	            // Adjust mutation rate based on fitness improvement
	            if (generationsSinceImprovement > 10)
	            {
	                mutationRate = Math.Min(mutationRate * 1.2, maxMutationRate);
	            }
	            else if (generationsSinceImprovement < 5)
	            {
	                mutationRate = Math.Max(mutationRate * 0.8, minMutationRate);
	            }

	            // Adjust crossover rate based on fitness improvement
	            if (generationsSinceImprovement > 10)
	            {
	                crossoverRate = Math.Min(crossoverRate * 1.2, maxCrossoverRate);
	            }
	            else if (generationsSinceImprovement < 5)
	            {
	                crossoverRate = Math.Max(crossoverRate * 0.8, minCrossoverRate);
	            }

	            List<List<T>> parents = SelectParents(population, fitnessScores);
	            List<List<T>> offspring = CrossoverOffspring(parents, crossoverRate);
	            MutateOffspring(offspring, mutationRate);
	            population = SelectSurvivors(population, offspring, fitnessScores, populationSize, eliteCount);
	        }

	        return population;
	    }

	    // Initialize the population with random individuals
	    private List<List<T>> InitializePopulation<T>(List<T> initialPopulation, int populationSize)
	    {
	        List<List<T>> population = new List<List<T>>();

	        for (int i = 0; i < populationSize; i++)
	        {
	            List<T> individual = new List<T>(initialPopulation);
	            Shuffle(individual);
	            population.Add(individual);
	        }

	        return population;
	    }

	    // Evaluate the fitness of each individual in the population
	    private List<double> EvaluateFitness<T>(List<List<T>> population, Func<List<T>, double> fitnessFunction)
	    {
	        List<double> fitnessScores = new List<double>();

	        foreach (List<T> individual in population)
	        {
	            double fitness = fitnessFunction(individual);
	            fitnessScores.Add(fitness);
	        }

	        return fitnessScores;
	    }

	    // Select parents for crossover based on their fitness scores
	    private List<List<T>> SelectParents<T>(List<List<T>> population, List<double> fitnessScores)
		{
		    List<List<T>> parents = new List<List<T>>();

		    for (int i = 0; i < population.Count; i++)
		    {
		        int parent1Index = SelectParentIndex(fitnessScores);
		        int parent2Index = SelectParentIndex(fitnessScores);
		        parents.Add(population[parent1Index]);
		        parents.Add(population[parent2Index]);
		    }

		    // If the number of parents is odd, remove the last parent
		    if (parents.Count % 2 != 0)
		    {
		        parents.RemoveAt(parents.Count - 1);
		    }

		    return parents;
		}

	    // Perform crossover between parents to create offspring
	    private List<List<T>> CrossoverOffspring<T>(List<List<T>> parents, double crossoverRate)
		{
		    List<List<T>> offspring = new List<List<T>>();

		    for (int i = 0; i < parents.Count - 1; i += 2)
		    {
		        List<T> parent1 = parents[i];
		        List<T> parent2 = parents[i + 1];
		        List<T> child1 = new List<T>(parent1);
		        List<T> child2 = new List<T>(parent2);

		        if (random.NextDouble() < crossoverRate)
		        {
		            int minLength = Math.Min(parent1.Count, parent2.Count);
		            int crossoverPoint = random.Next(1, minLength);

		            for (int j = crossoverPoint; j < minLength; j++)
		            {
		                T temp = child1[j];
		                child1[j] = child2[j];
		                child2[j] = temp;
		            }
		        }

		        offspring.Add(child1);
		        offspring.Add(child2);
		    }

		    // If the number of parents is odd, add the last parent to the offspring
		    if (parents.Count % 2 != 0)
		    {
		        offspring.Add(parents[parents.Count - 1]);
		    }

		    return offspring;
		}

	    // Mutate the offspring based on the mutation rate
	    private void MutateOffspring<T>(List<List<T>> offspring, double mutationRate)
	    {
	        foreach (List<T> individual in offspring)
	        {
	            for (int i = 0; i < individual.Count; i++)
	            {
	                if (random.NextDouble() < mutationRate)
	                {
	                    int swapIndex = random.Next(individual.Count);
	                    T temp = individual[i];
	                    individual[i] = individual[swapIndex];
	                    individual[swapIndex] = temp;
	                }
	            }
	        }
	    }

		private double CalculatePopulationDiversity<T>(List<List<T>> population)
		{
		    // Calculate the average Hamming distance between individuals in the population
		    double totalDistance = 0;
		    int numComparisons = 0;

		    for (int i = 0; i < population.Count - 1; i++)
		    {
		        for (int j = i + 1; j < population.Count; j++)
		        {
		            double distance = CalculateHammingDistance(population[i], population[j]);
		            totalDistance += distance;
		            numComparisons++;
		        }
		    }

		    return totalDistance / numComparisons;
		}

		private double CalculateHammingDistance<T>(List<T> individual1, List<T> individual2)
		{
		    int maxLength = Math.Max(individual1.Count, individual2.Count);
		    int distance = 0;

		    for (int i = 0; i < maxLength; i++)
		    {
		        if (i >= individual1.Count || i >= individual2.Count || !individual1[i].Equals(individual2[i]))
		        {
		            distance++;
		        }
		    }

		    return (double)distance / maxLength;
		}


	    // Select survivors for the next generation based on fitness scores
	    private List<List<T>> SelectSurvivors<T>(
		    List<List<T>> population,
		    List<List<T>> offspring,
		    List<double> fitnessScores,
		    int populationSize,
		    int eliteCount)
		{
		    List<List<T>> survivors = new List<List<T>>();

		    // Select the elite individuals from the current population
		    var eliteIndividuals = population
		        .Zip(fitnessScores, (individual, fitness) => new { Individual = individual, Fitness = fitness })
		        .OrderByDescending(x => x.Fitness)
		        .Take(eliteCount)
		        .Select(x => x.Individual)
		        .ToList();

		    // Add the elite individuals to the survivors
		    survivors.AddRange(eliteIndividuals);

		    // Combine the remaining individuals from the current population and offspring
		    List<List<T>> remainingIndividuals = new List<List<T>>();
		    remainingIndividuals.AddRange(population);
		    remainingIndividuals.AddRange(offspring);

		    // Remove the elite individuals from the remaining individuals
		    remainingIndividuals = remainingIndividuals.Except(eliteIndividuals).ToList();

		    // Select the best individuals from the remaining individuals to fill the population
		    var selectedIndividuals = remainingIndividuals
		        .Zip(fitnessScores, (individual, fitness) => new { Individual = individual, Fitness = fitness })
		        .OrderByDescending(x => x.Fitness)
		        .Select(x => x.Individual)
		        .Take(populationSize - eliteCount)
		        .ToList();

		    // Add the selected individuals to the survivors
		    survivors.AddRange(selectedIndividuals);

		    return survivors;
		}

	    // Select a parent index based on fitness scores using roulette wheel selection
	    private int SelectParentIndex(List<double> fitnessScores)
	    {
	        double totalFitness = fitnessScores.Sum();
	        double rouletteValue = random.NextDouble() * totalFitness;
	        double cumulativeFitness = 0;

	        for (int i = 0; i < fitnessScores.Count; i++)
	        {
	            cumulativeFitness += fitnessScores[i];
	            if (cumulativeFitness >= rouletteValue)
	            {
	                return i;
	            }
	        }

	        return fitnessScores.Count - 1;
	    }

	    // Shuffle a list randomly
	    private void Shuffle<T>(List<T> list)
	    {
	        int n = list.Count;
	        while (n > 1)
	        {
	            n--;
	            int k = random.Next(n + 1);
	            T value = list[k];
	            list[k] = list[n];
	            list[n] = value;
	        }
	    }
	}
	#endregion

	#region Has Set Comparer
	public class HashSetComparer<T> : IEqualityComparer<HashSet<T>>
	{
	    public bool Equals(HashSet<T> x, HashSet<T> y)
	    {
	        if (x == null && y == null)
	            return true;
	        if (x == null || y == null)
	            return false;
	        if (x.Count != y.Count)
	            return false;

	        return x.SetEquals(y);
	    }

	    public int GetHashCode(HashSet<T> obj)
	    {
	        if (obj == null)
	            return 0;

	        int hash = 17;
	        foreach (var item in obj)
	        {
	            hash = hash * 23 + item.GetHashCode();
	        }

	        return hash;
	    }
	}
	#endregion

	#region ListComparer
	public class ListComparer<T> : IEqualityComparer<List<T>>
	{
	    public bool Equals(List<T> x, List<T> y)
	    {
	        if (x == null && y == null)
	            return true;
	        if (x == null || y == null)
	            return false;
	        if (x.Count != y.Count)
	            return false;

	        return x.SequenceEqual(y);
	    }

	    public int GetHashCode(List<T> obj)
	    {
	        if (obj == null)
	            return 0;

	        int hash = 17;
	        foreach (var item in obj)
	        {
	            hash = hash * 23 + (item != null ? item.GetHashCode() : 0);
	        }

	        return hash;
	    }
	}
	#endregion
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
