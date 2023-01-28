namespace Condensator.Api.Models
{
    public class MongoDatabaseConfiguration
    {
        public string ConnectionString { get; set; } = null!;
        public string DatabaseName { get; set; } = null!;
    }
}
