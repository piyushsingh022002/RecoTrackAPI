using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecoTrack.Application.Exceptions
{
    public sealed class BusinessException : AppException
    {
        public BusinessException(string message, string errorCode = "BUS-001") : base(message, errorCode) { }

    }

}
