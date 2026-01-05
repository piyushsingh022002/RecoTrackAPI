using Microsoft.AspNetCore.Mvc;
using RecoTrack.Infrastructure.ServicesV2;

namespace RecoTrackApi.Controllers
{
    public class EmailControllerV2 : ControllerBase
    {
        private readonly IEmailService _emailService;

        public EmailControllerV2(IEmailService emailService)
        {
            _emailService = emailService;
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendEmail([FromBody] EmailRequest request)
        {
            // Get User JWT from Authorization header
            var userJwt = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

            if (string.IsNullOrEmpty(userJwt))
            {
                return Unauthorized("User JWT is missing");
            }

            bool result = await _emailService.SendEmailAsync(userJwt, request.To, request.Subject, request.Body);

            if (!result)
                return StatusCode(500, "Failed to send email");

            return Ok(new { Success = true });
        }


    }
    public class EmailRequest
    {
        public string To { get; set; } = default!;
        public string Subject { get; set; } = default!;
        public string Body { get; set; } = default!;
    }

}
