using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecoTrack.Shared.Settings
{
    public class ServiceJwtSettings
    {
        public string Issuer { get; set; } = String.Empty;      // Your service name
        public string Audience { get; set; } = String.Empty;    // The target service
        public string SecretKey { get; set; } = String.Empty;   // Symmetric key for signing
        public int ExpiryMinutes { get; set; }  // Short-lived token
    }
}
