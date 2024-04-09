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

	    public ObjectPool(int initialSize, Func<T> createFunc, int maxSize = 0)
	    {
	        this.createFunc = createFunc;

	        for (int i = 0; i < initialSize; i++)
	        {
	            items.Add(createFunc());
	        }

			MaxSize = maxSize;
	    }

		public T At(int position)
		{
			return items[position];
		}

	    public T Get()
	    {
	        T item = items.FirstOrDefault(x => !x.IsActive);
	        if (item == null)
	        {
	            item = createFunc();
	            items.Add(item);
				LimitSize();
	        }
	        item.Activate();
	        return item;
	    }

	    public void Release(T item)
	    {
	        item.Deactivate();
	    }

		public IEnumerable<T> ActiveItems
		{
		    get { return items.Where(x => x.IsActive); }
		}

		public void SetMaxSize(int maxSize)
		{
			MaxSize = maxSize;
		}

		public void LimitSize(int maxSize = 0)
		{
			int size = maxSize > 0 ? maxSize : MaxSize;

			if (size < 0)
			{
				return;
			}

		    while (ActiveItems.Count() > size)
		    {
		        T itemToRelease = ActiveItems.FirstOrDefault();
		        if (itemToRelease != null)
		        {
		            Release(itemToRelease);
		        }
		    }
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
