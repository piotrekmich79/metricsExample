using System.Collections;
using System.Diagnostics.Metrics;

namespace Shark.Metrics
{
    internal class SharkCounter
    {
        public static string MeterName { get; set; } = "SharkCounter";
        public static string MeterVersion { get; set; } = "1.0.0";


        static Meter _meter = new Meter(MeterName, MeterVersion);
        static Dictionary<string, Counter<double>> _counters = new Dictionary<string, Counter<double>>();
        private static readonly object __lockObj = new object();

        internal void CounterAddValue(string eventSourceName, string name, string units, string displayName, double value)
        {
            var counter = GetOrCreateCounter(eventSourceName, name, units, displayName);

            AddValue(counter, value);
        }

        private Counter<double> GetOrCreateCounter(string eventSourceName, string name, string units, string displayName)
        {
            Counter<double>? counter;
            var key = $"{eventSourceName}-{name}";


            if (!_counters.TryGetValue(key, out counter))
            {
                counter = CreateCounter(eventSourceName, name, units, displayName);
            }

            return counter;
        }

        private static Counter<double> CreateCounter(string eventSourceName, string name, string units, string displayName)
        {
            Counter<double>? counter;
            var key = $"{eventSourceName}-{name}";

            lock (__lockObj)
            {
                if (!_counters.TryGetValue(key, out counter))
                {
                    counter = _meter.CreateCounter<double>(name: $"{MeterName}-{eventSourceName}-{name}", unit: units, description: displayName);
                    _counters.TryAdd(key, counter);
                }
            }

            return counter;
        }

        internal void AddValue(Counter<double> counter, double value)
        {
            counter.Add(value);
        }
    }
}