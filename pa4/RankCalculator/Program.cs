using System.Text;
using System.Text.Json;

using NATS.Client;
using StackExchange.Redis;

namespace RankCalculator
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            using IConnection connection = new ConnectionFactory().CreateConnection();

            // Создаем клиента Redis для сохранения рангов
            string connectionString = "localhost:6379";
            ConnectionMultiplexer redis = ConnectionMultiplexer.Connect(connectionString);
            IDatabase db = redis.GetDatabase();

            // Подписываемся на очередь заданий
            var subscription = connection.SubscribeAsync("rank_tasks", (sender, e) =>
            {
                var json = Encoding.UTF8.GetString(e.Message.Data);

                if(json != null)
                {
                    Dictionary<string, string> data = JsonSerializer.Deserialize<Dictionary<string, string>>(json);

                    if(data != null)
                    {
                        // Сохраняем результат в Redis
                        var dataID = data.Keys.FirstOrDefault();
                        var textValue = data.Values.FirstOrDefault();

                        var rankKey = "RANK–" + dataID;
                        var rankValue = CalculateContentRank(textValue);

                        if (rankValue > 0.0) db.StringSet(rankKey, rankValue);
                        else throw new ArgumentException("rankValue must be greater than 0");

                        var rankCalculatedEvent = new RankCalculatedEvent()
                        {
                            TextId = dataID,
                            Text = textValue,
                            RankValue = rankValue,
                        };

                        string jsonEventMessage = JsonSerializer.Serialize(rankCalculatedEvent);

                        connection.Publish("RankCalculated", Encoding.UTF8.GetBytes(jsonEventMessage));
                    }
                }
            });

            subscription.Start();

            // Оставляем программу в ожидании сообщений
            Console.WriteLine("RankCalculator is running...");
            await Task.Delay(Timeout.Infinite);
        }

        // Метод для расчета ранга
        private static double CalculateContentRank(string text)
        {
            int nonAlphabeticCount = 0;

            if (!string.IsNullOrEmpty(text))
            {
                foreach (char c in text)
                {
                    if (!char.IsLetter(c))
                    {
                        nonAlphabeticCount++;
                    }
                }

                return (double)nonAlphabeticCount / text.Length;
            }

            return -1.0;
        }
    }
}
