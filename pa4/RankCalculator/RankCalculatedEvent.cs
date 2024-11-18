namespace RankCalculator
{
    public class RankCalculatedEvent
    {
        public string TextId { get; set; } = null!;
        public string? Text { get; set; }
        public double RankValue { get; set; }
    }
}
