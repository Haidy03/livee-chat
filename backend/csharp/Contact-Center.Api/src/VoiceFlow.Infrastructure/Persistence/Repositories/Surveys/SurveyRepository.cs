using MongoDB.Driver;
using VoiceFlow.Core.Entities.Surveys;
using VoiceFlow.Core.Enums.Survey;
using VoiceFlow.Core.Interfaces.Repositories.Surveys;

namespace VoiceFlow.Infrastructure.Persistence.Repositories.Surveys
{
    public sealed class SurveyRepository : MongoRepository<Survey>, ISurveyRepository
    {
        public SurveyRepository(MongoDbContext context) : base(context, "surveys") { }

        public async Task<IReadOnlyList<Survey>> ListAsync(string tenantId, string? search, SurveyStatus? status, SurveyLanguage? language, CancellationToken ct)
        {
            var fb = Builders<Survey>.Filter;
            var filter = fb.Eq(x => x.TenantId, tenantId);
            if (status is not null) filter &= fb.Eq(x => x.Status, status.Value);
            if (language is not null) filter &= fb.Eq(x => x.Language, language.Value);
            if (!string.IsNullOrWhiteSpace(search))
                filter &= fb.Regex(x => x.Name, new MongoDB.Bson.BsonRegularExpression(search, "i"));
            return await Collection.Find(filter).SortByDescending(x => x.UpdatedAt).ToListAsync(ct);
        }

        public Task<Survey?> GetAsync(string tenantId, string id, CancellationToken ct) =>
            Collection.Find(x => x.TenantId == tenantId && x.Id == id).FirstOrDefaultAsync(ct)!;

        public Task AddAsync(Survey survey, CancellationToken ct) =>
            Collection.InsertOneAsync(survey, cancellationToken: ct);

        public Task UpdateSurveyAsync(Survey survey, CancellationToken ct = default) =>
            Collection.ReplaceOneAsync(x => x.TenantId == survey.TenantId && x.Id == survey.Id, survey, cancellationToken: ct);

        public async Task<bool> DeleteAsync(string tenantId, string id, CancellationToken ct)
        {
            var res = await Collection.DeleteOneAsync(x => x.TenantId == tenantId && x.Id == id, ct);
            return res.DeletedCount > 0;
        }

        public Task<Survey?> GetSurveyByIdAsync(string id, CancellationToken ct) =>
        Collection.Find(x => x.Id == id).FirstOrDefaultAsync(ct)!;

    }
}
