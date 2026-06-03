using MongoDB.Driver;
using TeamConnect.Api.Shared.Services;
using FeedbackModel = TeamConnect.Api.Shared.Models.Feedback;

namespace TeamConnect.Api.Modules.Feedback
{
    public class FeedbackRepository : IFeedbackRepository
    {
        private readonly MongoDbContext _context;

        public FeedbackRepository(MongoDbContext context)
        {
            _context = context;
        }

        public Task InsertAsync(FeedbackModel feedback) =>
            _context.Feedbacks.InsertOneAsync(feedback);

        public Task<List<FeedbackModel>> GetReceivedByUserAsync(string userId) =>
            _context.Feedbacks.Find(f => f.ToUserId == userId).SortByDescending(f => f.CreatedAt).ToListAsync();

        public Task<List<FeedbackModel>> GetAllAsync() =>
            _context.Feedbacks.Find(_ => true).ToListAsync();
    }
}
