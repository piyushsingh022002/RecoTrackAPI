using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecoTrack.Application.Exceptions
{
    public sealed class CriticalException : AppException
    {
        public CriticalException(string message, string errorCode = "CRIT-001")
            : base(message, errorCode) { }
    }
}
