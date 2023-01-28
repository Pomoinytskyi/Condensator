namespace Condensator.Common.Entities
{
    public class NewsFeed
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Url { get; set; }
        public DateTime LastPulled { get; set; }

    }
}
