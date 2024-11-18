using NATS.Client;
using System;
using System.Text;

namespace Consumer
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Consumer started");

            int number = new Random().Next(1, 10);

            ConnectionFactory cf = new ConnectionFactory();
            using IConnection c = cf.CreateConnection();

            var s = c.SubscribeAsync("valuator.processing.rank", "rank_calculator", (sender, args) =>
            {
                string m = Encoding.UTF8.GetString(args.Message.Data);
                Console.WriteLine("Consuming: {0} from subject {1}", m, args.Message.Subject);

                string r = $"reply from consumer #{number}";
                byte[] data = Encoding.UTF8.GetBytes(r);

                c.Publish("valuator.replying.rank", data);
            });

            s.Start();

            Console.WriteLine("Press Enter to exit");
            Console.ReadLine();

            s.Unsubscribe();

            c.Drain();
            c.Close();
        }
    }
}
