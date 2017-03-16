using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qoollo.Turbo.Threading
{
    /// <summary>
    /// Factory to create <see cref="SignalWaiter"/>
    /// </summary>
    internal struct SignalWaiterFactory
    {
        /// <summary>
        /// Creates SignalWaiterFactory from single source event
        /// </summary>
        /// <param name="sourceEvent">Source event for the created factory</param>
        /// <returns>Created factory</returns>
        public static SignalWaiterFactory Create(SignalEvent sourceEvent)
        {
            if (sourceEvent == null)
                throw new ArgumentNullException(nameof(sourceEvent));

            return new SignalWaiterFactory(sourceEvent);
        }
        /// <summary>
        /// Creates SignalWaiterFactory from several source events
        /// </summary>
        /// <param name="sourceEvent1">First source event for the created factory</param>
        /// <param name="sourceEvent2">Second source event for the created factory</param>
        /// <returns>Created factory</returns>
        public static SignalWaiterFactory Create(SignalEvent sourceEvent1, SignalEvent sourceEvent2)
        {
            if (sourceEvent1 == null)
                throw new ArgumentNullException(nameof(sourceEvent1));
            if (sourceEvent2 == null)
                throw new ArgumentNullException(nameof(sourceEvent2));

            return new SignalWaiterFactory(new SignalEvent[] { sourceEvent1, sourceEvent2 });
        }
        /// <summary>
        /// Creates SignalWaiterFactory from several source events
        /// </summary>
        /// <param name="sourceEvents">Array of source events for the created factory</param>
        /// <returns>Created factory</returns>
        public static SignalWaiterFactory Create(SignalEvent[] sourceEvents)
        {
            if (sourceEvents == null)
                throw new ArgumentNullException(nameof(sourceEvents));

            if (sourceEvents.Length == 1 && sourceEvents[0] != null)
                return new SignalWaiterFactory(sourceEvents[0]);

            SignalEvent[] copy = new SignalEvent[sourceEvents.Length];
            for (int i = 0; i < sourceEvents.Length; i++)
            {
                if (sourceEvents[i] == null)
                    throw new ArgumentNullException(nameof(sourceEvents), "One of the source events is null");
                copy[i] = sourceEvents[i];
            }

            return new SignalWaiterFactory(copy);
        }


        /// <summary>
        /// Creates SignalWaiterFactory from several SignalWaiterFactory
        /// </summary>
        /// <param name="factory1">First SignalWaiterFactory as the source of SignalEvent</param>
        /// <param name="factory2">Second SignalWaiterFactory as the source of SignalEvent</param>
        /// <returns>Created factory</returns>
        public static SignalWaiterFactory Create(SignalWaiterFactory factory1, SignalWaiterFactory factory2)
        {
            if (factory1._sourceEvent != null && factory2._sourceEvent != null)
                return new SignalWaiterFactory(new SignalEvent[] { factory1._sourceEvent, factory2._sourceEvent });

            List<SignalEvent> eventList = new List<SignalEvent>();

            if (factory1._sourceEvent != null)
                eventList.Add(factory1._sourceEvent);
            else if (factory1._sourceEventList != null)
                eventList.AddRange(factory1._sourceEventList);

            if (factory2._sourceEvent != null)
                eventList.Add(factory2._sourceEvent);
            else if (factory2._sourceEventList != null)
                eventList.AddRange(factory2._sourceEventList);

            if (eventList.Count == 1)
                return new SignalWaiterFactory(eventList[0]);

            return new SignalWaiterFactory(eventList.ToArray());
        }
        /// <summary>
        /// Creates SignalWaiterFactory from several SignalWaiterFactory
        /// </summary>
        /// <param name="factories">Array of SignalWaiterFactory as the source of SignalEvent</param>
        /// <returns>Created factory</returns>
        public static SignalWaiterFactory Create(SignalWaiterFactory[] factories)
        {
            if (factories == null)
                throw new ArgumentNullException(nameof(factories));

            List<SignalEvent> eventList = new List<SignalEvent>();

            for (int i = 0; i < factories.Length; i++)
            {
                if (factories[i]._sourceEvent != null)
                    eventList.Add(factories[i]._sourceEvent);
                else if (factories[i]._sourceEventList != null)
                    eventList.AddRange(factories[i]._sourceEventList);
            }

            if (eventList.Count == 1)
                return new SignalWaiterFactory(eventList[0]);

            return new SignalWaiterFactory(eventList.ToArray());
        }


        // ===================

        private readonly SignalEvent _sourceEvent;
        private readonly SignalEvent[] _sourceEventList;


        /// <summary>
        /// SignalWaiterFactory constructor
        /// </summary>
        /// <param name="sourceEvent">Source event for this factory</param>
        internal SignalWaiterFactory(SignalEvent sourceEvent)
        {
            _sourceEvent = sourceEvent;
            _sourceEventList = null;
        }
        /// <summary>
        /// SignalWaiterFactory constructor
        /// </summary>
        /// <param name="sourceEvents">List of source events for this factory</param>
        internal SignalWaiterFactory(SignalEvent[] sourceEvents)
        {
            _sourceEvent = null;
            _sourceEventList = sourceEvents;
        }


        /// <summary>
        /// Creates <see cref="SignalWaiter"/> to wait for some event
        /// </summary>
        /// <param name="signalMode">Signalling mode of the created waiter</param>
        /// <returns>Created SignalWaiter</returns>
        public SignalWaiter CreateWaiter(SignalMode signalMode)
        {
            if (_sourceEvent != null)
                return new SignalWaiter(_sourceEvent, signalMode);
            if (_sourceEventList != null)
                return new SignalWaiter(_sourceEventList, signalMode);

            return new SignalWaiter(Enumerable.Empty<SignalEvent>(), signalMode);
        }
        /// <summary>
        /// Creates <see cref="SignalWaiter"/> to wait for some event
        /// </summary>
        /// <returns>Created SignalWaiter</returns>
        public SignalWaiter CreateWaiter()
        {
            return CreateWaiter(SignalMode.All);
        }
    }
}
