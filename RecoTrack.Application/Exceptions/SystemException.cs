using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecoTrack.Application.Exceptions
{
    public sealed class SystemException : AppException
    {
        public SystemException(string message, string errorCode = "SYS-001")
            : base(message, errorCode) { }
    }
}
