using System;

namespace Shark.Metrics
{
    public sealed class EventCounterAdapterOptions
    {
        public static readonly EventCounterAdapterOptions Default = new();

        /// <summary>
        /// By default we subscribe to event counters from all event sources but this allows you to filter by event source name.
        /// </summary>
        public Func<string, bool> EventSourceFilterPredicate { get; set; } = _ => true;
    }
}
