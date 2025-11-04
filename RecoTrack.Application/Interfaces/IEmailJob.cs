using RecoTrack.Application.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecoTrack.Application.Interfaces
{
    public interface IEmailJob
    {
        Task SendEmailAsync(EmailRequestDto request);
    }
}
