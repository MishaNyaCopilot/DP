using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net;
using System.Text;
using System.Text.Json;

using StackExchange.Redis;
using NATS.Client;

namespace Valuator.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private const string _connectionString = "localhost:6379";

    public IndexModel(ILogger<IndexModel> logger)
    {
        _logger = logger;
    }

    public void OnGet()
    {

    }

    public IActionResult OnPost(string text)
    {
        if (string.IsNullOrEmpty(text)) return Redirect("/");

        _logger.LogDebug(text);

        // Подключение к NATS
        IConnection natsConnection = new ConnectionFactory().CreateConnection();

        // Подключение к Redis БД
        ConfigurationOptions options = ConfigurationOptions.Parse(_connectionString);
        ConnectionMultiplexer connection = ConnectionMultiplexer.Connect(options);

        if(connection.IsConnected)
        {
            IDatabase redisDb = connection.GetDatabase();

            // Получение идентификатора текущей задачи
            string id = Guid.NewGuid().ToString();

            // Получение раннее обработанных текстов и проверка на схожесть
            CheckSimilarity(id, text, connection, redisDb);

            // Запись textValue в БД по ключу textKey
            string textKey = "TEXT-" + id;
            string textValue = text;
            redisDb.StringSet(textKey, textValue);

            // Отправляем задание в очередь NATS
            CalculateRankTask(id, textValue, natsConnection);

            natsConnection.Drain();
            natsConnection.Close();
            connection.Close();

            // Перенаправление на страницу summary
            return Redirect($"summary?id={id}");
        }

        return Redirect("/");
    }

    private static void CalculateRankTask(string id, string text, IConnection natsConnection)
    {
        Dictionary<string, string> taskData = new Dictionary<string, string>()
        {
            { id, text },
        };

        string jsonString = JsonSerializer.Serialize(taskData);

        natsConnection.Publish("rank_tasks", Encoding.UTF8.GetBytes(jsonString));
    }

    private static bool CheckSimilarity(string id, string text, ConnectionMultiplexer dbConnection, IDatabase redisDb)
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

        return processedTexts.Contains(text);
    }
}
