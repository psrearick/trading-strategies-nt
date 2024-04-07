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

		#region StandardDeviation()
		public double StandardDeviation(IEnumerable<double> values)
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
