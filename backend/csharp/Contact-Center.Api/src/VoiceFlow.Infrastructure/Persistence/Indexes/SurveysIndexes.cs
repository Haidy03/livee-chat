using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VoiceFlow.Core.Entities;
using VoiceFlow.Core.Entities.Surveys;

namespace VoiceFlow.Infrastructure.Persistence.Indexes
{
    public static class SurveysIndexes
    {
        public static async Task CreateAsync(MongoDbContext context, CancellationToken ct = default)
        {
            var collection = context.GetCollection<Survey>("surveys");
            var indexModels = new List<CreateIndexModel<Survey>>
        {
           new CreateIndexModel<Survey>(Builders<Survey>.IndexKeys.Ascending(x => x.TenantId).Ascending(x => x.Status)),
            new CreateIndexModel<Survey>(Builders<Survey>.IndexKeys.Descending(x => x.UpdatedAt)),
        };
            await collection.Indexes.CreateManyAsync(indexModels, ct);
        }
    }
}
