using System.Text;
using System.Text.Json;

using NATS.Client;

namespace EventsLogger
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            ConsoleColor deafultColor = Console.ForegroundColor;
            ConsoleColor errorColor = ConsoleColor.Red;

            // Подключение к NATS
            IConnection connection = new ConnectionFactory().CreateConnection();

            // Подписка на события
            connection.SubscribeAsync("RankCalculated", (sender, args) =>
            {
                var jsonMessage = Encoding.UTF8.GetString(args.Message.Data);
                var rankEvent = JsonSerializer.Deserialize<RankCalculatedEvent>(jsonMessage);

                if (rankEvent != null)
                {
                    Console.ForegroundColor = deafultColor;
                    Console.WriteLine($"Event: RankCalculated, ID: {rankEvent.TextId}, RankValue: {rankEvent.RankValue}");
                }
                else
                {
                    Console.ForegroundColor = errorColor;
                    Console.WriteLine("# Error when calculating RankValue");
                }
            });

            connection.SubscribeAsync("SimilarityCalculated", (sender, args) =>
            {
                var jsonMessage = Encoding.UTF8.GetString(args.Message.Data);
                var similarityEvent = JsonSerializer.Deserialize<SimilarityCalculatedEvent>(jsonMessage);
                
                if(similarityEvent != null)
                {
                    Console.ForegroundColor = deafultColor;
                    Console.WriteLine($"Event: SimilarityCalculated, ID: {similarityEvent.TextId}, SimilarityValue: {similarityEvent.SimilarityValue}");
                }
                else
                {
                    Console.ForegroundColor = errorColor;
                    Console.WriteLine("# Error when calculating SimilarityValue");
                }
            });

            // Оставляем программу в ожидании сообщений
            Console.WriteLine("EventsLogger is running...");
            await Task.Delay(Timeout.Infinite);
        }
    }

    public class RankCalculatedEvent
    {
        public string TextId { get; set; }
        public string Text { get; set; }
        public double RankValue { get; set; }
    }

    public class SimilarityCalculatedEvent
    {
        public string TextId { get; set; }
        public string Text { get; set; }
        public double SimilarityValue { get; set; }
    }
}
