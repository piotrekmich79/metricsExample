using System;
//using OpenTelemetry.Instrumentation.Runtime;
using OpenTelemetry.Metrics;

namespace Shark.Metrics
{
    /// <summary>
    /// Extension methods to simplify registering of dependency instrumentation.
    /// </summary>
    public static class SharkMeterProviderBuilderExtensions
    {
        /// <summary>
        /// Enables runtime instrumentation.
        /// </summary>
        /// <param name="builder"><see cref="MeterProviderBuilder"/> being configured.</param>
        /// <param name="configure">Runtime metrics options.</param>
        /// <returns>The instance of <see cref="MeterProviderBuilder"/> to chain the calls.</returns>
        public static MeterProviderBuilder? AddSharkMetrics(
            this MeterProviderBuilder builder,
            Action<SharkMetricsOptions> configure = null)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            SharkMetricsOptions options = new SharkMetricsOptions();
            configure?.Invoke(options);
            
            if (options.RuntimeMeterEnabled)
            {
                var instrumentation = new SharkRuntimeMeter(options);
                builder.AddMeter(SharkRuntimeMeter.MeterName);
                builder.AddInstrumentation(() => instrumentation);
            }

            if (options.SharkCountersEnabled)
                builder.AddMeter(SharkCounter.MeterName);
            if (options.SharkGaugesEnabled)
                builder.AddMeter(SharkGauge.MeterName);

            return builder;
        }
    }
}
