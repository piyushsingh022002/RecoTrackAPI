using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using StudentRoutineTrackerApi.Services;

namespace StudentRoutineTrackerApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        private readonly IMongoDbService _mongoDbService;

        public TestController(IMongoDbService mongoDbService)
        {
            _mongoDbService = mongoDbService;
        }

        [HttpGet("public")]
        public IActionResult PublicEndpoint() =>
            Ok(new { Message = "Public endpoint is working fine!" });

        [HttpGet("protected")]
        [Authorize]
        public IActionResult ProtectedEndpoint()
        {
            try
            {
                var database = _mongoDbService.GetDatabase();
                var collections = database.ListCollectionNames().ToList();
                return Ok(new
                {
                    Message = "Protected endpoint with JWT and DB connected!",
                    Collections = collections
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }
    }
}
