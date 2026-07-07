using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VoiceFlow.Core.Entities.Surveys;

namespace VoiceFlow.Infrastructure.Persistence.Indexes
{
    public static class SurveyResponseIndexes
    {
        public static async Task CreateAsync(MongoDbContext context, CancellationToken ct = default)
        {
            var collection = context.GetCollection<SurveyResponse>("survey_responses");
            var indexModels = new List<CreateIndexModel<SurveyResponse>>
        {
                new CreateIndexModel<SurveyResponse>(
                Builders<SurveyResponse>.IndexKeys.Ascending(x => x.SurveyId).Descending(x => x.At))
        };
            await collection.Indexes.CreateManyAsync(indexModels, ct);
        }
    }
}
