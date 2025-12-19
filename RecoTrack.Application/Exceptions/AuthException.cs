using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecoTrack.Application.Exceptions
{
    public sealed class AuthException : AppException
    {
        public AuthException(string message, string errorCode = "AUTH-001") : base(message, errorCode) { }
    }
}
