namespace Valuator.Modules
{
    public static class ShardMap
    {
        private static readonly Dictionary<string, string> CountryToRegionMap = new Dictionary<string, string>
        {
            { "Russia", "RUS" },
            { "France", "EU" },
            { "Germany", "EU" },
            { "USA", "OTHER" },
            { "India", "OTHER" }
        };

        public static string GetRegionByCountry(string country)
        {
            return CountryToRegionMap.TryGetValue(country, out var region) ? region : "UNKNOWN";
        }
    }
}
