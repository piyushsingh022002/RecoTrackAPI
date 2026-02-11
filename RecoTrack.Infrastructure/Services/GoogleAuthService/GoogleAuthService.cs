using Microsoft.Extensions.Configuration;
using Google.Apis.Auth;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecoTrack.Infrastructure.Services.GoogleAuthService
{
    public class GoogleAuthService
    {
        private readonly string _clientId;

        public GoogleAuthService(IConfiguration config)
        {
            _clientId = config["GoogleAuth:ClientId"];
        }

        public async Task<GoogleJsonWebSignature.Payload> VerifyToken(string accessToken)
        {
            return await GoogleJsonWebSignature.ValidateAsync(accessToken,
                new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { _clientId }
                });
        }

        
    }
}
