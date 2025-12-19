using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecoTrack.Application.Exceptions
{
    public abstract class AppException : Exception
    {
        public string ErrorCode { get; }

        protected AppException(string message, string errorCode) : base(message)
        {
            ErrorCode = errorCode;
        }
    }
}
