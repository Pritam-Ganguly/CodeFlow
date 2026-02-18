using CodeFlow.core.Models.Services;
using CodeFlow.Web.Filters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CodeFlow.Web.API
{
    [ApiController]
    [Authorize]
    [Route("api/cache")]
    public class CacheController : ControllerBase
    {
        private readonly IRedisCacheInvalidationService _redisCacheInvalidationService;

        public CacheController(IRedisCacheInvalidationService redisCacheInvalidationService)
        {
          _redisCacheInvalidationService = redisCacheInvalidationService;   
        }

        [LogAction]
        [HttpGet("clear_all")]
        public async Task<IActionResult> ClearAll()
        {
            try
            {
                await _redisCacheInvalidationService.InvalidateByPatternAsync("*");
                return Ok(new { message = "All cache cleared" });
            }
            catch (Exception ex) 
            { 
                return BadRequest(ex.Message);
            }
        }
    }
}
