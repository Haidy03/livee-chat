using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace VoiceFlow.Contracts.Profiles;

public class UpdateUserProfileRequest
{
    [Required]
    [EmailAddress]
    [MaxLength(320)]
    [JsonPropertyName("email")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    [JsonPropertyName("first_name")]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    [JsonPropertyName("last_name")]
    public string LastName { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    [JsonPropertyName("display_name")]
    public string DisplayName { get; set; } = string.Empty;

    [Range(1, 99999)]
    [JsonPropertyName("extension_number")]
    public int? ExtensionNumber { get; set; }

    [MaxLength(64)]
    [JsonPropertyName("outbound_cid")]
    public string? OutboundCid { get; set; }

    [Required]
    [MaxLength(64)]
    [JsonPropertyName("role")]
    public string Role { get; set; } = "agent";

    [Required]
    [MaxLength(32)]
    [JsonPropertyName("status")]
    public string Status { get; set; } = "active";

    [Required]
    [JsonPropertyName("disabled")]
    public bool Disabled { get; set; }

    [Required]
    [MaxLength(8)]
    [JsonPropertyName("language")]
    public string Language { get; set; } = "en";

    [Required]
    [MaxLength(64)]
    [JsonPropertyName("timezone")]
    public string Timezone { get; set; } = "UTC";

    [Required]
    [JsonPropertyName("browser_notifications")]
    public bool BrowserNotifications { get; set; } = true;

    [Required]
    [JsonPropertyName("groups")]
    public string[] Groups { get; set; } = System.Array.Empty<string>();

    [Required]
    [JsonPropertyName("record_inbound_internal")]
    public bool RecordInboundInternal { get; set; }

    [Required]
    [JsonPropertyName("record_inbound_external")]
    public bool RecordInboundExternal { get; set; }

    [Required]
    [JsonPropertyName("record_outbound_internal")]
    public bool RecordOutboundInternal { get; set; }

    [Required]
    [JsonPropertyName("record_outbound_external")]
    public bool RecordOutboundExternal { get; set; }

    [Required]
    [JsonPropertyName("record_on_demand")]
    public bool RecordOnDemand { get; set; }

    [JsonPropertyName("skills")]
    public List<ProfileSkillDto>? Skills { get; set; }
}

/// <summary>
/// Payload for <c>PATCH /api/v1/profiles/{id}</c>.
/// Partial update — only fields included in the request body are applied.
/// All properties are nullable so the server can distinguish "omitted"
/// (leave as-is) from "explicitly set to null/empty".
/// Serialize with <c>DefaultIgnoreCondition = WhenWritingNull</c> so
/// untouched fields are not sent.
/// </summary>
public class PatchUserProfileRequest
{
   
    public string? Email { get; set; }

   
    public string? FirstName { get; set; }

   
    public string? LastName { get; set; }

    
    public string? DisplayName { get; set; }

   
    public int? ExtensionNumber { get; set; }

    
    public string? OutboundCid { get; set; }

    
    public string? Role { get; set; }

   
    public string? Status { get; set; }

   
    public bool? Disabled { get; set; }

    
    public string? Language { get; set; }

    
    public string? Timezone { get; set; }

    
    public bool? BrowserNotifications { get; set; }

   
    public string[]? Groups { get; set; }

   
    public bool? RecordInboundInternal { get; set; }

   
    public bool? RecordInboundExternal { get; set; }

    
    public bool? RecordOutboundInternal { get; set; }

    
    public bool? RecordOutboundExternal { get; set; }

   
    public bool? RecordOnDemand { get; set; }

    
    public List<ProfileSkillDto>? Skills { get; set; }

    public string[]? AvailableChannels { get; set; }
}
