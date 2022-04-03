using Microsoft.AspNetCore.Builder;
using OpenTelemetry.Metrics;
using Shark.Metrics;

namespace SampleApi.Metrics
{
    public class MetricsInitializer
    {
        private static IDisposable? _listenerRegistration;

        public static void AddOpentelemetryMetrics(WebApplicationBuilder builder)
        {
            builder.Services.AddOpenTelemetryMetrics(b =>
            {
                b.AddAspNetCoreInstrumentation();
                b.AddSharkMetrics();
                b.AddPrometheusExporter(options =>
                {
                    options.StartHttpListener = true;
                    // Use your endpoint and port here
                    options.HttpListenerPrefixes = new string[] { $"http://localhost:{9090}/" };
                    options.ScrapeResponseCacheDurationMilliseconds = 0;
                });
            });

            _listenerRegistration = EventCounterAdapter.StartListening();
        }

        //TODO
        void Dispose()
        {
            _listenerRegistration.Dispose();
        }
    }
}
