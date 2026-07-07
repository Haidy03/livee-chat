using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoiceFlow.Core.Exceptions.Hubspot
{
    public sealed class HubSpotOAuthException : Exception
    {
        public string Code { get; }
        public HubSpotOAuthException(string code, string message) : base(message) { Code = code; }
    }
}
