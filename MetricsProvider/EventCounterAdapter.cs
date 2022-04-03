using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;

namespace Shark.Metrics
{
    /// <summary>
    /// Monitors all .NET EventCounters and exposes them as Prometheus counters and gauges.
    /// </summary>
    /// <remarks>
    /// Rate-based .NET event counters are transformed into Prometheus gauges indicating count per escond.
    /// Incrementing .NET event counters are transformed into Prometheus counters.
    /// </remarks>
    public sealed class EventCounterAdapter : IDisposable
    {
        private readonly EventCounterAdapterOptions _options;

        // Each event counter is published either in gauge or counter form,
        // depending on the type of the native .NET event counter (incrementing or aggregating).,
        private readonly SharkGauge _gauge;
        private readonly SharkCounter _counter;
        private readonly Listener _listener;

        public static IDisposable StartListening() => new EventCounterAdapter(EventCounterAdapterOptions.Default);

        public static IDisposable StartListening(EventCounterAdapterOptions options) => new EventCounterAdapter(options);

        private EventCounterAdapter(EventCounterAdapterOptions options)
        {
            _options = options;
            _gauge = new SharkGauge();
            _counter = new SharkCounter();

            _listener = new Listener(OnEventSourceCreated, OnEventWritten);
        }

        public void Dispose()
        {
            // Disposal means we stop listening but we do not remove any published data just to keep things simple.
            _listener.Dispose();
        }

        private bool OnEventSourceCreated(EventSource source)
        {
            return _options.EventSourceFilterPredicate(source.Name);
        }

        private void OnEventWritten(EventWrittenEventArgs args)
        {
            // This deserialization here is pretty gnarly.
            // We just skip anything that makes no sense.

            if (args.EventName != "EventCounters")
                return; // Do not know what it is and do not care.

            if (args.Payload == null)
                return; // What? Whatever.

            var eventSourceName = args.EventSource.Name;

            foreach (var item in args.Payload)
            {
                if (item is not IDictionary<string, object> e)
                    continue;

                //Get counter Name
                if (!e.TryGetValue("Name", out var nameWrapper))
                    continue;

                var name = nameWrapper as string;

                if (name == null)
                    continue; // What? Whatever.

                //Get DisplayName
                if (!e.TryGetValue("DisplayName", out var displayNameWrapper))
                    continue;
                var displayName = displayNameWrapper as string ?? "";

                //Get DisplayUnit
                e.TryGetValue("DisplayUnit", out var displayUnitWrapper);
                string displayUnit = displayUnitWrapper as string ?? "";

                //Get CounterType
                if (!e.TryGetValue("CounterType", out var counterTypeWrapper))
                    continue;
                string counterType = counterTypeWrapper as string ?? "";

                //Get Counter Value
                // The event counter can either be
                // 1) an aggregating counter (in which case we use the mean); or
                // 2) an incrementing counter (in which case we use the delta).
                if (e.TryGetValue("Name", out var cName))
                {
                    string? counterName = cName as string;
                    if ((counterName != null) && (counterName.EndsWith("per-second")))
                    {
                        //Increment value
                        e.TryGetValue("Increment", out var val1);
                        var increment = val1 as double?;
                        if (increment == null)
                            continue;

                        //Interval sec
                        e.TryGetValue("IntervalSec", out var val2);
                        var intervalSecFloat = val2 as float?;
                        if (intervalSecFloat == null)
                            continue;
                        decimal intervalSecDecimal = new decimal(intervalSecFloat.Value);
                        if (intervalSecDecimal == 0)
                            continue;
                        double intervalSec = (double)intervalSecDecimal;

                        double counterValue = Math.Round(increment.Value / intervalSec, 2);

                        _gauge.GaugeSetValue(eventSourceName, name, displayUnit, displayName, counterValue);
                    }                    
                    else if (e.TryGetValue("Increment", out var increment))
                    {
                        //counterType=="Sum"
                        //incrementing counter.

                        var value = increment as double?;

                        if (value == null)
                            continue; // What? Whatever.

                        _counter.CounterAddValue(eventSourceName, name, displayUnit, displayName, value.Value);
                    }
                    else if (e.TryGetValue("Mean", out var mean))
                    {
                        //counterType=="Mean"
                        //Additional properties to use in the future: Count, Min, Max, IntervalSec

                        var value = mean as double?;
                        if (value == null)
                            continue;

                        _gauge.GaugeSetValue(eventSourceName, name, displayUnit, displayName, value.Value);
                    }
                }
            }
        }

        private sealed class Listener : EventListener
        {
            public Listener(Func<EventSource, bool> onEventSourceCreated, Action<EventWrittenEventArgs> onEventWritten)
            {
                _onEventSourceCreated = onEventSourceCreated;
                _onEventWritten = onEventWritten;

                foreach (var eventSource in _preRegisteredEventSources)
                    OnEventSourceCreated(eventSource);

                _preRegisteredEventSources.Clear();
            }

            private readonly List<EventSource> _preRegisteredEventSources = new List<EventSource>();

            private readonly Func<EventSource, bool> _onEventSourceCreated;
            private readonly Action<EventWrittenEventArgs> _onEventWritten;

            protected override void OnEventSourceCreated(EventSource eventSource)
            {
                if (_onEventSourceCreated == null)
                {
                    // The way this EventListener thing works is rather strange. Immediately in the base class constructor, before we
                    // have even had time to wire up our subclass, it starts calling OnEventSourceCreated for all already-existing event sources...
                    // We just buffer those calls because CALM DOWN SIR!
                    _preRegisteredEventSources.Add(eventSource);
                    return;
                }

                if (!_onEventSourceCreated(eventSource))
                    return;

                EnableEvents(eventSource, EventLevel.Verbose, EventKeywords.All, new Dictionary<string, string?>()
                {
                    ["EventCounterIntervalSec"] = "1"
                });
            }

            protected override void OnEventWritten(EventWrittenEventArgs eventData)
            {
                _onEventWritten(eventData);
            }
        }
    }
}
