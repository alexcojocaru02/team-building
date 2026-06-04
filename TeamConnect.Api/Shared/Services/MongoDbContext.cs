using MongoDB.Driver;
using Microsoft.Extensions.Logging;
using TeamConnect.Api.Shared.Models;

namespace TeamConnect.Api.Shared.Services
{
    public class MongoDbContext
    {
        private readonly IMongoDatabase _database;

        public MongoDbContext(IConfiguration config, ILogger<MongoDbContext> logger)
        {
            var connectionString = config["MongoDb:ConnectionString"];
            var databaseName = config["MongoDb:DatabaseName"];

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                logger.LogError("MongoDb:ConnectionString is missing or empty");
                throw new InvalidOperationException("MongoDB connection string is not configured.");
            }

            if (string.IsNullOrWhiteSpace(databaseName))
            {
                logger.LogError("MongoDb:DatabaseName is missing or empty");
                throw new InvalidOperationException("MongoDB database name is not configured.");
            }

            logger.LogInformation("Initializing MongoDB context for database {DatabaseName}", databaseName);
            var client = new MongoClient(connectionString);
            _database = client.GetDatabase(databaseName);
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
        public IMongoCollection<TeamConnect.Api.Shared.Models.TeamJoinRequest> TeamJoinRequests => _database.GetCollection<TeamConnect.Api.Shared.Models.TeamJoinRequest>("TeamJoinRequests");

    }
}
