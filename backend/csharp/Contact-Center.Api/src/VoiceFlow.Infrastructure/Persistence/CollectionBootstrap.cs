using MongoDB.Driver;
using VoiceFlow.Infrastructure.Persistence.Indexes;

namespace VoiceFlow.Infrastructure.Persistence;

public sealed class CollectionBootstrap
{
    private readonly MongoDbContext _context;

    public CollectionBootstrap(MongoDbContext context)
    {
        _context = context;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await AccountIndexes.CreateAsync(_context, cancellationToken);
        await BillingIndexes.CreateAsync(_context, cancellationToken);
        await ProfileIndexes.CreateAsync(_context, cancellationToken);
        await RbacRoleIndexes.CreateAsync(_context, cancellationToken);
        await RbacUserRoleIndexes.CreateAsync(_context, cancellationToken);
        await CallIndexes.CreateAsync(_context, cancellationToken);
        await FlowIndexes.CreateAsync(_context, cancellationToken);
        await ContactIndexes.CreateAsync(_context, cancellationToken);
        await TagIndexes.CreateAsync(_context, cancellationToken);
        await AutoTagIndexes.CreateAsync(_context, cancellationToken);
        await VoiceLibraryItemIndexes.CreateAsync(_context, cancellationToken);
        await SipAccountIndexes.CreateAsync(_context, cancellationToken);
        await SoftphoneCallLogIndexes.CreateAsync(_context, cancellationToken);
        await InvoiceIndexes.CreateAsync(_context, cancellationToken);
        await EditLogIndexes.CreateAsync(_context, cancellationToken);
        await GroupIndexes.CreateAsync(_context, cancellationToken);
        await QueueIndexes.CreateAsync(_context, cancellationToken);
        await SurveysIndexes.CreateAsync(_context, cancellationToken);
        await SurveyResponseIndexes.CreateAsync(_context, cancellationToken);
        await ReportIndexes.CreateAsync(_context, cancellationToken);
        await ReportRunIndexes.CreateAsync(_context, cancellationToken);
        await ReportResultIndexes.CreateAsync(_context, cancellationToken);
        await WrapUpCodeIndexes.CreateAsync(_context, cancellationToken);
        await CampaignIndexes.CreateAsync(_context, cancellationToken);
        await CampaignTargetIndexes.CreateAsync(_context, cancellationToken);
        await CampaignActivityIndexes.CreateAsync(_context, cancellationToken);
        await CampaignReceivedCallIndexes.CreateAsync(_context, cancellationToken);

    }
}
