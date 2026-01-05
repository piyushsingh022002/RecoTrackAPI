using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RecoTrack.Infrastructure.ServicesV2;

namespace RecoTrackApi.Controllers
{
    [Authorize]
    [ApiController]
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
            if (request == null || string.IsNullOrWhiteSpace(request.Email_Action))
            {
                return NoContent();
            }

            var userJwt = Request.Headers["Authorization"].ToString().Replace("Bearer ", string.Empty);
            await _emailService.SendEmailAsync(userJwt, request.Email_Action);

            return Ok();
        }


    }
    public class EmailRequest
    {
        public string Email_Action { get; set; } = default!;
    }

}
