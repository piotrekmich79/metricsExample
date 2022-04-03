using System.Collections.Concurrent;
using System.Diagnostics.Metrics;

namespace Shark.Metrics
{
    internal class SharkGauge
    {
        public static string MeterName { get; set; } = "SharkGauge";
        public static string MeterVersion { get; set; } = "1.0.0";

        static Meter _meter = new Meter(MeterName, MeterVersion);
        static ConcurrentDictionary<string, Gauge<double>> _gauges = new ConcurrentDictionary<string, Gauge<double>>();

        private static readonly object __lockObj = new object();

        private class Gauge<T>
        {
            public T Value { get; set; }
        }

        private static Gauge<double>? GetGauge(string eventSourceName, string name)
        {
            Gauge<double>? gauge=null;
            var key = $"{eventSourceName}-{name}";

            _gauges.TryGetValue(key, out gauge);

            return gauge;
        }

        private static void SetValue(Gauge<double> gauge, double value)
        {
            gauge.Value = value;
        }

        public void GaugeSetValue(string eventSourceName, string name, string units, string displayName, double value)
        {
            var gauge = GetOrCreateGauge(eventSourceName, name, units, displayName);

            SetValue(gauge, value);
        }

        private Gauge<double> GetOrCreateGauge(string eventSourceName, string name, string units, string displayName)
        {
            Gauge<double>? gauge;
            var key = $"{eventSourceName}-{name}";

            gauge = GetGauge(key, name);
            if (gauge==null)
            {
                gauge = CreateGauge(eventSourceName, name, units, displayName);
            }

            return gauge;
        }

        private static Gauge<double> CreateGauge(string eventSourceName, string name, string units, string displayName)
        {
            Gauge<double>? gauge;
            var key = $"{eventSourceName}-{name}";

            lock (__lockObj)
            {
                if (!_gauges.TryGetValue(key, out gauge))
                {
                    gauge = new Gauge<double>();
                    _gauges.TryAdd(key, gauge);
                    _meter.CreateObservableGauge($"{MeterName}-{eventSourceName}-{name}", () => GaugeGetValue(eventSourceName, name), units, displayName);
                }
            }

            return gauge;
        }

        private static double GaugeGetValue(string eventSourceName, string name)
        {
            var gauge = GetGauge(eventSourceName, name);

            if (gauge != null)
                return gauge.Value;

            return 0;
        }
    }
}