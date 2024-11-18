using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Net;

using StackExchange.Redis;

namespace Valuator.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private string _connectionString = "localhost:6379";

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

        // Подключение к БД
        ConfigurationOptions options = ConfigurationOptions.Parse(_connectionString);
        ConnectionMultiplexer connection = ConnectionMultiplexer.Connect(options);

        if (connection.IsConnected)
        {
            var db = connection.GetDatabase();
            EndPoint endPoint = connection.GetEndPoints().First();
            
            // Получение всех текстовых ключей
            RedisKey[] keys = connection.GetServer(endPoint)
                .Keys(pattern: "*")
                .Where(x => x.ToString().StartsWith("TEXT-"))
                .ToArray();

            // Получение всех текстов, используя их ключи
            HashSet<string> processedTexts = new HashSet<string>();
            foreach (var key in keys)
            {
                string value = db.StringGet(key).ToString();
                processedTexts.Add(value);
            }

            // Запись в БД
            string id = Guid.NewGuid().ToString();

            string textKey = "TEXT-" + id;
            //TODO: сохранить в БД text по ключу textKey
            string textValue = text;
            db.StringSet(textKey, textValue);

            string rankKey = "RANK-" + id;
            //TODO: посчитать rank и сохранить в БД по ключу rankKey
            double rankValue = CalculateContentRank(text);
            db.StringSet(rankKey, rankValue);

            string similarityKey = "SIMILARITY-" + id;
            //TODO: посчитать similarity и сохранить в БД по ключу similarityKey
            int similarityValue = CheckSimilarity(text, processedTexts);
            db.StringSet(similarityKey, similarityValue);

            connection.Close();

            return Redirect($"summary?id={id}");
        }

        return Redirect("/");
    }

    private static double CalculateContentRank(string text)
    {
        int nonAlphabeticCount = 0;

        if(!string.IsNullOrEmpty(text))
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

    private static int CheckSimilarity(string text, HashSet<string> processedTexts)
    {
        if (processedTexts.Contains(text))
        {
            return 1;
        }
        else
        {
            processedTexts.Add(text);
            return 0;
        }
    }
}
