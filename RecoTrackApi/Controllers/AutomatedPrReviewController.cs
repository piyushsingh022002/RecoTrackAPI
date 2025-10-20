using Microsoft.AspNetCore.Mvc;
using RecoTrack.Application.Interfaces;
using RecoTrack.Application.Models;

namespace RecoTrackApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AutomatedPrReviewController : ControllerBase
    {
        private readonly IAutomatedPrReviewService _reviewService;

        public AutomatedPrReviewController(IAutomatedPrReviewService reviewService)
        {
            _reviewService = reviewService;
        }

        [HttpPost("analyze")]
        public async Task<IActionResult> Analyze([FromBody] PrMetadata metadata)
        {
            var result = await _reviewService.AnalyzePullRequestAsync(metadata);
            return Ok(result);
        }
    }
}
