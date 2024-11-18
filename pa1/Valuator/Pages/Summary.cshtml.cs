using Microsoft.AspNetCore.Mvc.RazorPages;
using StackExchange.Redis;
using System.Globalization;
using System.Net;

namespace Valuator.Pages;

public class SummaryModel : PageModel
{
    private readonly ILogger<SummaryModel> _logger;
    private readonly string _connectionString = "redis-18259.c276.us-east-1-2.ec2.redns.redis-cloud.com:18259,password=J8sH8wFUajeKFwmzgpi6NwmM05SpsBsq";

    public SummaryModel(ILogger<SummaryModel> logger)
    {
        _logger = logger;
    }

    public double Rank { get; set; }
    public double Similarity { get; set; }

    public void OnGet(string id)
    {
        _logger.LogDebug(id);

        //TODO: проинициализировать свойства Rank и Similarity значениями из БД
        ConfigurationOptions options = ConfigurationOptions.Parse(_connectionString);
        ConnectionMultiplexer connection = ConnectionMultiplexer.Connect(options);

        if (connection.IsConnected)
        {
            EndPoint endPoint = connection.GetEndPoints().First();
            RedisKey[] keys = connection.GetServer(endPoint).Keys(pattern: "*").Where(x => x.ToString().Contains(id)).ToArray();

            if(keys.Length > 0)
            {
                var db = connection.GetDatabase();

                string rankKey = keys.First(x => x.ToString().StartsWith("RANK-"));
                string similarityKey = keys.First(x => x.ToString().StartsWith("SIMILARITY-"));

                Rank = Convert.ToDouble(db.StringGet(rankKey), CultureInfo.InvariantCulture);
                Similarity = Convert.ToDouble(db.StringGet(similarityKey), CultureInfo.InvariantCulture);
            }

            connection.Close();
        }
    }
}
