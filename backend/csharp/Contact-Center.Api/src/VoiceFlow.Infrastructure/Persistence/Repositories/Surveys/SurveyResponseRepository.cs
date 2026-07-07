using MongoDB.Driver;
using System;

using VoiceFlow.Core.Entities.Surveys;
using VoiceFlow.Core.Interfaces.Repositories.Surveys;

namespace VoiceFlow.Infrastructure.Persistence.Repositories.Surveys
{
    public sealed class SurveyResponseRepository : MongoRepository<SurveyResponse>, ISurveyResponseRepository
    {
        public SurveyResponseRepository(MongoDbContext context) : base(context, "survey_responses") { }


        public async Task<IReadOnlyList<SurveyResponse>> ListAsync(string tenantId, string surveyId, int limit, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct)
        {
            var fb = Builders<SurveyResponse>.Filter;
            var filter = fb.Eq(x => x.TenantId, tenantId) & fb.Eq(x => x.SurveyId, surveyId);
            if (from is not null) filter &= fb.Gte(x => x.At, from.Value);
            if (to is not null) filter &= fb.Lte(x => x.At, to.Value);
            return await Collection.Find(filter).SortByDescending(x => x.At).Limit(limit).ToListAsync(ct);
        }

        public async Task<SurveyResponse?> GetByCallIdAsync(string surveyId, string callId, CancellationToken ct)
        {
            return await Collection.Find(x => x.SurveyId == surveyId && x.CallId == callId).FirstOrDefaultAsync(ct);
        }

        public Task InsertSurveyResponseAsync(SurveyResponse response, CancellationToken ct) =>
        Collection.InsertOneAsync(response, cancellationToken: ct);

    }
}
