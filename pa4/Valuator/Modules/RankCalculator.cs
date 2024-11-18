using NATS.Client;
using StackExchange.Redis;
using System.Text;

namespace Valuator.Modules
{
    public class RankCalculator
    {
        private readonly string _redisConnectionString;
        private readonly string _queueName;
        private readonly string _subject;
        private readonly ILogger _logger;

        private IConnectionMultiplexer _redis;
        private IConnection _nats;

        public RankCalculator(string subject, string queueName, ILogger logger)
        {
            _subject = subject;
            _queueName = queueName;

            _logger = logger;
        }

        /*public Task<double> CalculateContentRankAsync(string msg, CancellationToken ct)
        {
            Initialize();

            Task.Factory.StartNew(() => ProduceAsync(msg, ct), ct);

            NatsSubscribeAsync(ct);

            return Task.FromResult(0.0);
        }*/

        // Инициализация свойств
        private bool Initialize()
        {
            // Подключение к NATS
            _nats = new ConnectionFactory().CreateConnection();

            // Подключение к Redis
            ConfigurationOptions options = ConfigurationOptions.Parse(_redisConnectionString);
            _redis = ConnectionMultiplexer.Connect(options);

            return _nats.State == ConnState.CONNECTED || _redis.IsConnected;
        }

        private async Task ProduceAsync(string msg, CancellationToken ct)
        {
            using IConnection connection = new ConnectionFactory().CreateConnection();

            while (!ct.IsCancellationRequested)
            {
                _logger.LogInformation("Produced: {msg}", msg);

                byte[] data = Encoding.UTF8.GetBytes(msg);
                connection.Publish(_subject, data);

                await Task.Delay(1000);
            }

            connection.Drain();

            connection.Close();
        }

        private async void NatsSubscribeAsync(CancellationToken ct)
        {
            var queueSubscriber = _nats.SubscribeAsync(_subject, _queueName, async (sender, e) =>
            {
                // Обработка полученного сообщения
                string text = Encoding.UTF8.GetString(e.Message.Data);
                double rank = CalculateContentRank(text);
                await SaveRankToRedisAsync(text, rank);
            });

            queueSubscriber.Start();

            // Бесконечный цикл для поддержания работы приложения
            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(1000);
            }

            queueSubscriber.Unsubscribe();

            _nats.Drain();
            _nats.Close();
        }

        // Метод для сохранения ранга в Redis
        private async Task SaveRankToRedisAsync(string text, double rank)
        {
            // Получаем базу данных Redis
            IDatabase db = _redis.GetDatabase();

            // Сохраняем ранг в Redis
            await db.StringSetAsync(text, rank);
        }

        // Метод для расчета ранга
        private double CalculateContentRank(string text)
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
