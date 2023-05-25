using static Lab4_sem4.EventThrottling;

namespace Lab4_sem4
{
    internal class Program
    {
        static void Main(string[] args)
        {
            EventThrottlingBus eventBus = new EventThrottlingBus(throttleInterval: 1000, retryInterval: 3000, maxRetries: 3, initialRetryDelay: 1000);

            eventBus.Subscribe("Event1", Event1Handler2, priority: 1);
            eventBus.Subscribe("Event1", Event1Handler2, priority: 2);
            eventBus.Subscribe("Event2", Event2Handler3, priority: 3);
            eventBus.Subscribe("Event3", Event3Handler, priority: 1);
            eventBus.Subscribe("Event4", Event4Handler, priority: 1);

            eventBus.Publish("Event1", EventArgs.Empty);
            eventBus.Publish("Event2", EventArgs.Empty);
            eventBus.Publish("Event3", EventArgs.Empty);
            eventBus.Publish("Event4", EventArgs.Empty);

            Console.ReadKey();
        }
        static void Event1Handler2(object sender, EventArgs args)
        {
            Console.WriteLine("Event 1 handled by Handler 1");
            throw new Exception("Simulated exception");
        }

        static void Event2Handler3(object sender, EventArgs args)
        {
            Console.WriteLine("Event 2 handled by Handler 3");
            throw new Exception("Simulated exception");
        }
        static void Event3Handler(object sender, EventArgs args)
        {
            Console.WriteLine("Event 3 handled by Handler 1");
            throw new Exception("Simulated exception");
        }

        static void Event4Handler(object sender, EventArgs args)
        {
            Console.WriteLine("Event 4 handled by Handler 1");
            throw new Exception("Simulated exception");
        }
    }
}