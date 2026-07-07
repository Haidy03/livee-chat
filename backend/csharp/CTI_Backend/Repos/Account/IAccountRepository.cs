using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using PhoneNumbers;

namespace VoiceFlow.Repos;

public interface IAccountRepository
{
    Task<Account?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);
    Task<Account?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
}


[BsonIgnoreExtraElements]
public sealed class Account
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    [BsonElement("_id")]
    public string Id { get; set; } = string.Empty;

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    [BsonElement("userId")]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("orgName")]
    public string OrgName { get; set; } = string.Empty;

    [BsonElement("autoAnswer")]
    public bool AutoAnswer { get; set; } = true;

    [BsonElement("autoAnswerSecs")]
    public int AutoAnswerSecs { get; set; } = 30;

    [BsonElement("paramName")]
    public string ParamName { get; set; } = "caller";

    [BsonElement("dialerUrl")]
    public string DialerUrl { get; set; } = string.Empty;

    [BsonElement("dialerMethod")]
    public string DialerMethod { get; set; } = "GET";

    [BsonElement("waitTime")]
    public int WaitTime { get; set; } = 30;

    [BsonElement("ivrTimeout")]
    public int IvrTimeout { get; set; } = 30;

    [BsonElement("limitIvr")]
    public bool LimitIvr { get; set; } = true;

    [BsonElement("outboundRingLimit")]
    public bool OutboundRingLimit { get; set; }

    [BsonElement("internalTimeout")]
    public bool InternalTimeout { get; set; }

    [BsonElement("acwIn")]
    public bool AcwIn { get; set; }

    [BsonElement("acwOut")]
    public bool AcwOut { get; set; }

    [BsonElement("autoAssign")]
    public bool AutoAssign { get; set; }

    [BsonElement("allowReject")]
    public bool AllowReject { get; set; } = true;

    [BsonElement("allowTransferAway")]
    public bool AllowTransferAway { get; set; } = true;

    [BsonElement("notifyOnAgentChanges")]
    public bool NotifyOnAgentChanges { get; set; } = true;

    [BsonElement("sendInvoicesToAdmins")]
    public bool SendInvoicesToAdmins { get; set; } = true;

    [BsonElement("billingEmails")]
    public string BillingEmails { get; set; } = string.Empty;

    [BsonElement("invoiceName")]
    public string InvoiceName { get; set; } = "<default>";

    [BsonElement("registrationNumber")]
    public string RegistrationNumber { get; set; } = string.Empty;

    [BsonElement("vatNumber")]
    public string VatNumber { get; set; } = string.Empty;

    [BsonElement("billingCountry")]
    public string BillingCountry { get; set; } = string.Empty;

    [BsonElement("billingAddress")]
    public string BillingAddress { get; set; } = string.Empty;

    [BsonElement("paymentMethods")]
    public List<object> PaymentMethods { get; set; } = [];

    [BsonElement("phoneNumbers")]
    public List<PhoneNumber> PhoneNumbers { get; set; } = [];

    [BsonElement("showInbound")]
    public bool ShowInbound { get; set; }

    [BsonElement("defaultCountry")]
    public string DefaultCountry { get; set; } = "SA";

    [BsonElement("autoTagging")]
    public bool AutoTagging { get; set; } = true;

    [BsonElement("callTags")]
    public string CallTags { get; set; } = string.Empty;

    [BsonElement("domains")]
    public string Domains { get; set; } = string.Empty;

    [BsonElement("awayStatus")]
    public string AwayStatus { get; set; } = "Away";

    [BsonElement("numberFormat")]
    public string NumberFormat { get; set; } = "intl-no-prefix";
}

