using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net;
using System.Text;
using System.Text.Json;

using StackExchange.Redis;
using NATS.Client;
using Valuator.Modules;

namespace Valuator.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly IConnection _natsConnection;

    public IndexModel(ILogger<IndexModel> logger)
    {
        _logger = logger;

        // Подключение к NATS
        _natsConnection = new ConnectionFactory().CreateConnection();
    }

    public void OnGet()
    {
        Environment.SetEnvironmentVariable("DB_RUS", "localhost:6000", EnvironmentVariableTarget.Process);
        Environment.SetEnvironmentVariable("DB_EU", "localhost:6001", EnvironmentVariableTarget.Process);
        Environment.SetEnvironmentVariable("DB_OTHER", "localhost:6002", EnvironmentVariableTarget.Process);
    }

    public IActionResult OnPost(string text, string country)
    {
        if (string.IsNullOrEmpty(text)) return Redirect("/");

        _logger.LogDebug("text: {text} \n country: {country}", text, country);

        // Получение региона по стране
        var region = ShardMap.GetRegionByCountry(country);

        // Подключение к Redis БД
        // Получение параметров подключения к Redis из переменных окружения
        var redisConnectionString = Environment.GetEnvironmentVariable($"DB_{region}");
        
        if (!string.IsNullOrEmpty(redisConnectionString))
        {
            var redisConnection = ConnectionMultiplexer.Connect(redisConnectionString);

            if (redisConnection.IsConnected)
            {
                IDatabase redisDb = redisConnection.GetDatabase();

                // Получение идентификатора текущей задачи
                string id = Guid.NewGuid().ToString();

                // Логирование
                Dictionary<string, string> lookUp = new Dictionary<string, string>()
                {
                    { id, region },
                };

                var lookUpData = JsonSerializer.Serialize(lookUp);
                _natsConnection.Publish("DbRequested", Encoding.UTF8.GetBytes(lookUpData));

                // Получение раннее обработанных текстов и проверка на схожесть
                int similarityValue = CheckSimilarity(id, text, redisConnection, redisDb);

                // Публикация события SimilarityCalculated
                var similarityEvent = new SimilarityCalculatedEvent
                {
                    TextId = id,
                    SimilarityValue = similarityValue
                };

                var similarityEventMessage = JsonSerializer.Serialize(similarityEvent);
                _natsConnection.Publish("SimilarityCalculated", Encoding.UTF8.GetBytes(similarityEventMessage));

                // Запись textValue в БД по ключу textKey
                string textKey = "TEXT-" + id;
                string textValue = text;
                redisDb.StringSet(textKey, textValue);

                // Отправляем задание в очередь NATS
                CalculateRankTask(id, textValue, redisConnectionString);

                redisConnection.Close();

                // Перенаправление на страницу summary
                return Redirect($"summary?id={id}&region={region}");
            }
        }

        return Redirect("/");
    }

    private void CalculateRankTask(string id, string text, string redisConnectionString)
    {
        var taskData = new RankTaskMessage()
        {
            TextId = id,
            Text = text,
            RedisConnectionString = redisConnectionString,
        };

        string jsonString = JsonSerializer.Serialize(taskData);

        _natsConnection.Publish("rank_tasks", Encoding.UTF8.GetBytes(jsonString));
    }

    private static int CheckSimilarity(string id, string text, ConnectionMultiplexer dbConnection, IDatabase redisDb)
    {
        EndPoint endPoint = dbConnection.GetEndPoints().First();

        // Получение всех текстовых ключей
        RedisKey[] keys = dbConnection.GetServer(endPoint)
            .Keys(pattern: "*")
            .Where(x => x.ToString().StartsWith("TEXT-"))
            .ToArray();

        // Получение всех текстов, используя их ключи
        HashSet<string> processedTexts = new HashSet<string>();
        foreach (var key in keys)
        {
            string value = redisDb.StringGet(key).ToString();
            processedTexts.Add(value);
        }

        // Вычисление similarityValue и запись в БД по ключу similarityKey
        string similarityKey = "SIMILARITY-" + id;
        int similarityValue = processedTexts.Contains(text) ? 1 : 0;

        redisDb.StringSet(similarityKey, similarityValue);

        return similarityValue;
    }
}

public class SimilarityCalculatedEvent
{
    public string TextId { get; set; } = null!;
    public string? Text { get; set; }
    public double SimilarityValue { get; set; }
}

public class RankTaskMessage
{
    public string TextId { get; set; } = null!;
    public string? Text { get; set; }
    public string RedisConnectionString { get; set; } = null!;
}