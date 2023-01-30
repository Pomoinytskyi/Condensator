namespace Condensator.Api.Models
{
	public class Article
	{
		public string Id { get; set; }
		public string FeedId { get; set; }
		public string Title { get; set; }
		public string CleanedText { get; set; }
		public string? MetaDescription { get; set; }
		public string MetaLang { get; set; }
		public string MetaFavicon { get; set; }
		public string MetaKeywords { get; set; }
		public string MetaEncoding { get; set; }
		public string CanonicalLink { get; set; }
		public string Domain { get; set; }
		public string TopImage { get; set; }
		public List<string> Tags { get; set; }
		public object? Opengraph { get; set; }
		public List<string> Tweets { get; set; }
		public List<string> Links { get; set; }
		public List<string> Authors { get; set; }
		public string FinalUrl { get; set; }
		public string LinkHash { get; set; }
		public DateTime PublishDate { get; set; }
		public DateTime PublishDatetimeUtc { get; set; }
		public object? AdditionalData { get; set; }
		public string Summary { get; set; }



	}
}
