namespace RankCalculator
{
    public class RankTaskMessage
    {
        public string TextId { get; set; } = null!;
        public string? Text { get; set; }
        public string RedisConnectionString { get; set; } = null!;
    }
}
