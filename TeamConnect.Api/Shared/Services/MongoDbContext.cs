using MongoDB.Driver;
using TeamConnect.Api.Shared.Models;

namespace TeamConnect.Api.Shared.Services
{
    public class MongoDbContext
    {
        private readonly IMongoDatabase _database;

        public MongoDbContext(IConfiguration config)
        {
            var client = new MongoClient(config["MongoDb:ConnectionString"]);
            _database = client.GetDatabase(config["MongoDb:DatabaseName"]);
        }

        public IMongoDatabase Database => _database;

        public IMongoCollection<User> Users => _database.GetCollection<User>("Users");
        public IMongoCollection<Team> Teams => _database.GetCollection<Team>("Teams");
        public IMongoCollection<FeedPost> FeedPosts => _database.GetCollection<FeedPost>("FeedPosts");
        public IMongoCollection<FeedPostLike> FeedPostLikes => _database.GetCollection<FeedPostLike>("FeedPostLikes");
        public IMongoCollection<FeedPostComment> FeedPostComments => _database.GetCollection<FeedPostComment>("FeedPostComments");
        public IMongoCollection<Feedback> Feedbacks => _database.GetCollection<Feedback>("Feedbacks");
        public IMongoCollection<TeamActivity> TeamActivities => _database.GetCollection<TeamActivity>("TeamActivities");
        public IMongoCollection<SchemaMigrationRecord> SchemaMigrations => _database.GetCollection<SchemaMigrationRecord>("SchemaMigrations");

    }
}
