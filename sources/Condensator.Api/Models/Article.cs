namespace Condensator.Api.Models
{
    public class Article
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Content { get; set; }
        public string Link { get; set; }
        public string ImageUrl { get; set; }
        public DateTime PublishDate { get; set; }
        public string NewsFeedId { get; set; }
    }
}
