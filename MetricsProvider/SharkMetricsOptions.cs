
namespace Shark.Metrics
{
    /// <summary>
    /// Options to define the runtime metrics.
    /// </summary>
    public class SharkMetricsOptions
    {
        /// <summary>
        /// Gets or sets a value
        /// </summary>
        public bool RuntimeMeterEnabled { get; set; } = true;
        /// <summary>
        /// Gets or sets a value
        /// </summary>
        public bool SharkCountersEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets a value
        /// </summary>
        public bool SharkGaugesEnabled { get; set; } = true;
    }
}
