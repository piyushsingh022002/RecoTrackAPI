using Hangfire;
using Microsoft.AspNetCore.Mvc;
using RecoTrack.Application.Dtos;
using RecoTrack.Application.Interfaces;

namespace RecoTrackApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EmailController : ControllerBase
    {
        private readonly IBackgroundJobClient _backgroundJob;
        public EmailController(IBackgroundJobClient backgroundJob)
        {
            _backgroundJob = backgroundJob;
        }

        [HttpPost("send-fire-and-forget")]
        public IActionResult SendFireAndForget([FromBody] EmailRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.UserId) || string.IsNullOrWhiteSpace(request.ToEmail))
                return BadRequest("UserId and ToEmail are required.");

            // Enqueue a fire-and-forget job using interface-based job - Hangfire will resolve dependencies
            _backgroundJob.Enqueue<IEmailJob>(j => j.SendEmailAsync(request));

            // Immediate response
            return Accepted(new { message = "Your mail will reach you within 5 minutes." });
        }
    }
}
