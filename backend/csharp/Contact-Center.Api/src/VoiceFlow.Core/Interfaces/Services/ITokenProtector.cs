using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VoiceFlow.Core.Interfaces.Services
{
    public interface ITokenProtector
    {
        string ProtectAccessToken(string plaintext);
        string UnprotectAccessToken(string ciphertext);
        string ProtectRefreshToken(string plaintext);
        string UnprotectRefreshToken(string ciphertext);
    }
}
