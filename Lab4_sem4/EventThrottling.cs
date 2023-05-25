using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lab4_sem4
{
    public class EventThrottling
    {
        public class EventThrottlingBus
        {
            public delegate void EventHandler(object sender, EventArgs args);
            private Dictionary<string, List<EventHandlerWrapper>> eventHandlers;
            private Dictionary<string, DateTime> lastEventTimes;
            private int throttleInterval;
            private int retryInterval;
            private int maxRetries;
            private int initialRetryDelay;
            private Random random;
            private object lockObject = new object();

            private class EventHandlerWrapper
            {
                public EventHandler Handler { get; }
                public int Priority { get; }
                public int RetryCount { get; set; }
                public DateTime LastRetryTime { get; set; }

                public EventHandlerWrapper(EventHandler handler, int priority)
                {
                    Handler = handler;
                    Priority = priority;
                    RetryCount = 0;
                    LastRetryTime = DateTime.MinValue;
                }
            }

            public EventThrottlingBus(int throttleInterval, int retryInterval, int maxRetries, int initialRetryDelay)
            {
                eventHandlers = new Dictionary<string, List<EventHandlerWrapper>>();
                this.throttleInterval = throttleInterval;
                this.retryInterval = retryInterval;
                this.maxRetries = maxRetries;
                this.initialRetryDelay = initialRetryDelay;
                random = new Random();
                lastEventTimes = new Dictionary<string, DateTime>();
            }

            public void Subscribe(string eventName, EventHandler eventHandler, int priority)
            {
                lock (lockObject)
                {
                    if (!eventHandlers.ContainsKey(eventName))
                    {
                        eventHandlers[eventName] = new List<EventHandlerWrapper>();
                        lastEventTimes[eventName] = DateTime.MinValue;
                    }

                    eventHandlers[eventName].Add(new EventHandlerWrapper(eventHandler, priority));
                    eventHandlers[eventName].Sort((x, y) => y.Priority.CompareTo(x.Priority));
                }
            }

            public void Unsubscribe(string eventName, EventHandler eventHandler)
            {
                lock (lockObject)
                {
                    if (eventHandlers.ContainsKey(eventName))
                    {
                        eventHandlers[eventName].RemoveAll(wrapper => wrapper.Handler == eventHandler);

                        if (eventHandlers[eventName].Count == 0)
                        {
                            eventHandlers.Remove(eventName);
                            lastEventTimes.Remove(eventName);
                        }
                    }
                }
            }

            public void Publish(string eventName, EventArgs args)
            {
                lock (lockObject)
                {
                    if (!eventHandlers.ContainsKey(eventName))
                        return;

                    DateTime now = DateTime.Now;

                    if ((now - lastEventTimes[eventName]).TotalMilliseconds < throttleInterval)
                        return;

                    lastEventTimes[eventName] = now;

                    foreach (var wrapper in eventHandlers[eventName])
                    {
                        if (wrapper.RetryCount > maxRetries)
                            continue;

                        try
                        {
                            wrapper.Handler.Invoke(this, args);
                        }
                        catch
                        {
                            if (wrapper.RetryCount < maxRetries)
                            {
                                int delay = GetRetryDelay(wrapper.RetryCount);
                                wrapper.RetryCount++;
                                wrapper.LastRetryTime = DateTime.Now.AddMilliseconds(delay);
                                ThreadPool.QueueUserWorkItem(RetryEventHandler, new RetryState(wrapper, args, delay));
                            }
                        }
                    }
                }
            }

            private void RetryEventHandler(object state)
            {
                RetryState retryState = (RetryState)state;
                Thread.Sleep(retryState.Delay);
                Publish(retryState.Wrapper.Handler.Method.Name, retryState.Args);
            }

            private class RetryState
            {
                public EventHandlerWrapper Wrapper { get; }
                public EventArgs Args { get; }
                public int Delay { get; }

                public RetryState(EventHandlerWrapper wrapper, EventArgs args, int delay)
                {
                    Wrapper = wrapper;
                    Args = args;
                    Delay = delay;
                }
            }
            private int GetRetryDelay(int retryCount)
            {
                double delay = initialRetryDelay * Math.Pow(2, retryCount);
                double randomFactor = random.NextDouble() * 0.5 + 0.5; 
                delay *= randomFactor;

                if (delay > retryInterval)
                    delay = retryInterval;

                return (int)delay;
            }
        }
    }
}
