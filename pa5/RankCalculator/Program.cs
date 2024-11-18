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

            // Подписываемся на очередь заданий
            var subscription = connection.SubscribeAsync("rank_tasks", (sender, e) =>
            {
                var json = Encoding.UTF8.GetString(e.Message.Data);

                if(json != null)
                {
                    var rankTaskData = JsonSerializer.Deserialize<RankTaskMessage>(json);

                    if(rankTaskData != null)
                    {
                        // Подключаемся к БД Redis для сохранения рангов
                        string connectionString = rankTaskData.RedisConnectionString;
                        ConnectionMultiplexer redisConnection = ConnectionMultiplexer.Connect(connectionString);

                        if(redisConnection.IsConnected)
                        {
                            IDatabase db = redisConnection.GetDatabase();

                            // Сохраняем результат в Redis
                            var dataID = rankTaskData.TextId;
                            var textValue = rankTaskData.Text ?? "";

                            var rankKey = "RANK–" + dataID;
                            var rankValue = CalculateContentRank(textValue);

                            db.StringSet(rankKey, rankValue);

                            var rankCalculatedEvent = new RankCalculatedEvent()
                            {
                                TextId = dataID,
                                Text = textValue,
                                RankValue = rankValue,
                            };

                            string jsonEventMessage = JsonSerializer.Serialize(rankCalculatedEvent);

                            connection.Publish("RankCalculated", Encoding.UTF8.GetBytes(jsonEventMessage));

                            redisConnection.Close();
                        }
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
