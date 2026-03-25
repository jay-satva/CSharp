namespace MongoConnect.Models
{
    public class MongoDbSettings
    {
        public string ConnectionString { get; set; }
        public string DatabaseName { get; set; }
        public Collections Collections { get; set; }
    }
    public class Collections
    {
        public string UsersCollection { get; set; }
        public string ItemsCollection { get; set; }
    }
}