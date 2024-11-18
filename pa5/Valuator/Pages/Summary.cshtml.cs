﻿using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Globalization;
using System.Net;

using StackExchange.Redis;

namespace Valuator.Pages;

public class SummaryModel : PageModel
{
    private readonly ILogger<SummaryModel> _logger;

    public SummaryModel(ILogger<SummaryModel> logger)
    {
        _logger = logger;
    }

    public void OnGet(string id, string region)
    {
        _logger.LogDebug("textId: {id} region: {region}", id, region);

        // Инициализация свойства Rank и Similarity значениями из Redis БД
        var redisConnectionString = Environment.GetEnvironmentVariable($"DB_{region}");

        if(!string.IsNullOrEmpty(redisConnectionString))
        {
            Connect(id, redisConnectionString);
        }
    }

    private void Connect(string id, string redisConnectionString)
    {
        ConfigurationOptions options = ConfigurationOptions.Parse(redisConnectionString);
        ConnectionMultiplexer connection = ConnectionMultiplexer.Connect(options);

        try
        {
            if (connection.IsConnected)
            {
                EndPoint endPoint = connection.GetEndPoints().First();
                RedisKey[] keys = connection.GetServer(endPoint).Keys(pattern: "*").Where(x => x.ToString().Contains(id)).ToArray();

                if (keys.Length > 0)
                {
                    var db = connection.GetDatabase();

                    string rankKey = keys.First(x => x.ToString().StartsWith("RANK–"));
                    string similarityKey = keys.First(x => x.ToString().StartsWith("SIMILARITY-"));

                    Rank = Convert.ToDouble(db.StringGet(rankKey), CultureInfo.InvariantCulture);
                    Similarity = Convert.ToDouble(db.StringGet(similarityKey), CultureInfo.InvariantCulture);
                }

                connection.Close();
            }
        }
        catch
        {
            connection.Close();
            Connect(id, redisConnectionString);
        }
    }

    public double Rank { get; set; }
    public double Similarity { get; set; }
}
