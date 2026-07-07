using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace VoiceFlow.Api.LiveChat.Controllers;

/// <summary>
/// LiveChat project/user configuration.
/// Currently returns a static mock; replace with real project/user lookup
/// backed by the tenant/project store when available.
/// </summary>
[ApiController]
[Route("api/Account")]
[AllowAnonymous] // TODO(livechat-config): switch to [Authorize] once wired to real data.
public sealed class AccountController : ControllerBase
{
    [HttpGet("GetProjectUser")]
    [ProducesResponseType(typeof(ProjectUserConfigDto), StatusCodes.Status200OK)]
    public IActionResult GetProjectUser()
    {
        var dto = new ProjectUserConfigDto
        {
            ChatSlots = 5,
            ClientInactiveTimeout = 5,
            AgentInactiveTimeout = 30,
            ClientDisconnectedTimeout = 2,
            AgentDisconnectedTimeout = 30,
            UserAvailable = false,
            ChattingType = null,
        };
        return Ok(dto);
    }

    public sealed class ProjectUserConfigDto
    {
        [JsonPropertyName("ChatSlots")]
        public int ChatSlots { get; set; }

        [JsonPropertyName("clientInactiveTimeout")]
        public int ClientInactiveTimeout { get; set; }

        [JsonPropertyName("agentInactiveTimeout")]
        public int AgentInactiveTimeout { get; set; }

        [JsonPropertyName("clientDisconnectedTimeout")]
        public int ClientDisconnectedTimeout { get; set; }

        [JsonPropertyName("agentDisconnectedTimeout")]
        public int AgentDisconnectedTimeout { get; set; }

        [JsonPropertyName("user_available")]
        public bool UserAvailable { get; set; }

        [JsonPropertyName("chattingType")]
        public string? ChattingType { get; set; }
    }
}
