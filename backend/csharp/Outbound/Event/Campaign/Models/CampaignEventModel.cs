namespace Outbound.Event.Campaign.Models
{
    /// <summary>Base envelope — every campaign message carries an event discriminator.</summary>
    public class CampaignEventModel
    {
        public string Event { get; set; } = string.Empty;
    }
}
