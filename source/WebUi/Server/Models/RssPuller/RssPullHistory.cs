namespace WebUi.Server.Models.RssPuller
{
    public class RssPullHistory
    {
        public string RssId { get; set; } = null!;
        
        //ToDo: Add Index for UpdateTime
        public DateTime UpdateTime { get; set; }
        public List<string> ArticleIds { get; set; } = new List<string>();
        public string RssContent{get; set;} = null!;
    }
}