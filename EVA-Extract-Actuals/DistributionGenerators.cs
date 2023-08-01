using EVA_Extract_Actuals;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EVA_Extract_Actuals
{
    public class DistributionGenerators
    {
        public static string BETA_FRONT_LOADED = "BetaFrontLoaded";
        public static string BETA_CENTER_LOADED = "BetaCenterLoaded";
        public static string BETA_BACK_LOADED = "BetaBackLoaded";
        public static string LINEAR_FRONT_LOADED = "LinearFrontLoaded";
        public static string LINEAR_BACK_LOADED = "LinearBackLoaded";
        public static string UNIFORM = "Uniform";
        public static string CUSTOM = "Custom";

        //Lookup tables for distributions generated from Beta(2,4) Beta(4,4) and Beta(4,2) across 50 periods.
        //These are the built in distributions offered in the original 'Simphony EVA'.
        private static decimal[] _betaFrontLoaded = new decimal[] { 0.4M, 1.1M, 1.7M, 2.2M, 2.7M, 3.1M, 3.4M, 3.7M, 3.9M, 4.0M, 4.1M, 4.2M, 4.2M, 4.2M, 4.2M, 4.1M, 4.0M, 3.8M, 3.7M, 3.5M, 3.4M, 3.2M, 3.0M, 2.8M, 2.6M, 2.4M, 2.2M, 2.0M, 1.8M, 1.6M, 1.4M, 1.3M, 1.1M, 1.0M, 0.8M, 0.7M, 0.6M, 0.5M, 0.4M, 0.3M, 0.2M, 0.2M, 0.1M, 0.1M, 0.0M, 0.0M, 0.0M, 0.0M, 0.0M, 0.0M, };
        private static decimal[] _betaCenterLoaded = new decimal[] { 0.0M, 0.05M, 0.05M, 0.1M, 0.2M, 0.3M, 0.4M, 0.6M, 0.8M, 1.0M, 1.3M, 1.6M, 1.8M, 2.1M, 2.4M, 2.7M, 3.0M, 3.3M, 3.5M, 3.8M, 4.0M, 4.1M, 4.2M, 4.3M, 4.4M, 4.4M, 4.3M, 4.2M, 4.1M, 4.0M, 3.8M, 3.5M, 3.3M, 3.0M, 2.7M, 2.4M, 2.1M, 1.8M, 1.6M, 1.3M, 1.0M, 0.8M, 0.6M, 0.4M, 0.3M, 0.2M, 0.1M, 0.05M, 0.05M, 0.0M, };
        private static decimal[] _betaBackLoaded = new decimal[] { 0.0M, 0.0M, 0.0M, 0.0M, 0.0M, 0.0M, 0.1M, 0.1M, 0.2M, 0.2M, 0.3M, 0.4M, 0.5M, 0.6M, 0.7M, 0.8M, 1.0M, 1.1M, 1.3M, 1.4M, 1.6M, 1.8M, 2.0M, 2.2M, 2.4M, 2.6M, 2.8M, 3.0M, 3.2M, 3.4M, 3.5M, 3.7M, 3.8M, 4.0M, 4.1M, 4.2M, 4.2M, 4.2M, 4.2M, 4.1M, 4.0M, 3.9M, 3.7M, 3.4M, 3.1M, 2.7M, 2.2M, 1.7M, 1.1M, 0.4M, };
        public static Dictionary<string, Func<int, decimal, decimal[]>> Generators = new Dictionary<string, Func<int, decimal, decimal[]>>()
        {
            {LINEAR_BACK_LOADED, GenerateLinearBackLoadedDistribution},
            {LINEAR_FRONT_LOADED, GenerateLinearFrontLoadedDistribution},
            {UNIFORM, GenerateUniformDistribution},
            {BETA_FRONT_LOADED, GenerateBetaFrontLoaded},
            {BETA_CENTER_LOADED, GenerateBetaCenterLoaded},
            {BETA_BACK_LOADED, GenerateBetaBackLoaded},
            {CUSTOM, null},
            {"TestTest", TestTest},
        };

        public static List<DistributionType> AllDistributionTypes = new List<DistributionType>()
        {
            new DistributionType() {Code = LINEAR_BACK_LOADED, Name="Linear Back Loaded"},
            new DistributionType() {Code = LINEAR_FRONT_LOADED, Name="Linear Front Loaded"},
            new DistributionType() {Code = UNIFORM, Name="Uniform"},
            new DistributionType() {Code = BETA_FRONT_LOADED, Name="Beta Front Loaded"},
            new DistributionType() {Code = BETA_CENTER_LOADED, Name="Beta Center Loaded"},
            new DistributionType() {Code = BETA_BACK_LOADED, Name="Beta Back Loaded"},
            new DistributionType() {Code = CUSTOM, Name="Custom"}
        };

        public static decimal[] GenerateUniformDistribution(int periods, decimal percentCompleted)
        {
            decimal[] result = new decimal[periods];

            for (var i = 0; i < periods; i++)
            {
                result[i] = 1M - (percentCompleted / 100M);
            }
            var normalizingFactor = 1 / result.Sum();
            for (var i = 0; i < periods; i++)
            {
                result[i] = result[i] * normalizingFactor;
            }

            return result;
        }

        public static decimal[] GenerateLinearFrontLoadedDistribution(int periods, decimal percentCompleted)
        {
            decimal[] result = new decimal[periods];

            var max = 100M - percentCompleted; //arbitrary
            var increment = max / (decimal)periods;
            for (var i = 0; i < periods; i++)
            {
                result[i] = max - (i * increment);
            }
            var normalizingFactor = 1 / result.Sum();
            for (var i = 0; i < periods; i++)
            {
                result[i] = result[i] * normalizingFactor;
            }
            return result;
        }

        public static decimal[] GenerateLinearBackLoadedDistribution(int periods, decimal percentCompleted)
        {
            decimal[] result = new decimal[periods];

            var max = 100M - percentCompleted; //arbitrary
            var increment = max / (decimal)periods;
            var completedIncrements = 0;
            for (var i = periods - 1; i > -1; i--)
            {
                result[i] = max - (completedIncrements * increment);
                completedIncrements++;
            }
            var normalizingFactor = 1 / result.Sum();
            for (var i = 0; i < periods; i++)
            {
                result[i] = result[i] * normalizingFactor;
            }
            return result;
        }

        public static decimal[] GenerateBetaFrontLoaded(int periods, decimal percentCompleted)
        {
            decimal[] result = new decimal[periods];

            // Remove 'percentCompleted' percentage from the distribution
            decimal sum = 0;
            var index = 0;
            while (sum < percentCompleted)
            {
                sum += _betaFrontLoaded[index];
                index++;
            }

            var modifiedBetaFrontLoaded = _betaFrontLoaded.Skip(index).ToArray();
            return ResizeDistribution(periods, modifiedBetaFrontLoaded);
        }

        public static decimal[] GenerateBetaBackLoaded(int periods, decimal percentCompleted)
        {
            decimal[] result = new decimal[periods];

            // Remove 'percentCompleted' percentage from the distribution
            decimal sum = 0;
            var index = 0;
            while (sum < percentCompleted)
            {
                sum += _betaBackLoaded[index];
                index++;
            }

            var modifiedBetaBackLoaded = _betaBackLoaded.Skip(index).ToArray();

            return ResizeDistribution(periods, modifiedBetaBackLoaded);
        }

        public static decimal[] GenerateBetaCenterLoaded(int periods, decimal percentCompleted)
        {
            decimal[] result = new decimal[periods];

            // Remove 'percentCompleted' percentage from the distribution
            decimal sum = 0;
            var index = 0;
            while (sum < percentCompleted)
            {
                sum += _betaCenterLoaded[index];
                index++;
            }

            var modifiedBetaCenterLoaded = _betaCenterLoaded.Skip(index).ToArray();

            return ResizeDistribution(periods, modifiedBetaCenterLoaded);
        }

        public static decimal[] TestTest(int periods, decimal percentCompleted)
        {
            decimal sum = _betaCenterLoaded.Sum();
            decimal[] uniform = GenerateUniformDistribution(50, percentCompleted);
            return ResizeDistribution(periods, uniform);
        }

        public static decimal[] ResizeDistribution(int periods, decimal[] distribution)
        {
            decimal[] resized = new decimal[periods];
            var oldMax = distribution.Length;
            var newMax = periods - 1;
            var interval = ((decimal)oldMax) / ((decimal)periods);
            decimal[] intervals = new decimal[periods]; //end "index" of each 
            for (var i = 1; i < periods; i++)
            {
                intervals[i - 1] = i * interval;
            }
            intervals[periods - 1] = oldMax; //just force it to end on the whole number
            var intervalStart = 0M;
            for (var i = 0; i < periods; i++)
            {
                var intervalEnd = intervals[i];
                var startWhole = (int)Math.Truncate(intervalStart);
                var startFraction = intervalStart - startWhole;
                var endWhole = (int)Math.Truncate(intervalEnd);
                var endFraction = intervalEnd - endWhole;
                var resizedValue = 0M;
                if (startWhole == endWhole) //handle start and end within the same cell, happens when periods is > distribution.Length
                {
                    resizedValue = distribution[startWhole] * (endFraction - startFraction);
                }
                else
                {
                    resizedValue += distribution[startWhole] * (1M - startFraction);
                    for (var j = startWhole + 1; j < endWhole; j++)
                    {
                        resizedValue += distribution[j];
                    }
                    if (endWhole < oldMax)
                    {
                        resizedValue += distribution[endWhole] * endFraction;
                    }
                }
                resized[i] = resizedValue;
                intervalStart = intervalEnd;
            }
            return resized;
        }
    }
}