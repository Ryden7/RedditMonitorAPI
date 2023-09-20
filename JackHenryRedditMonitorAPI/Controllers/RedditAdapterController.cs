using JackHenryRedditMonitorAPI.ConfigureAPI;
using Microsoft.AspNetCore.Mvc;
using Reddit.Models;

namespace JackHenryRedditMonitorAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class RedditAdapterController : ControllerBase
    {
        private readonly ILogger<RedditAdapterController> _logger;

        public RedditAdapterController(ILogger<RedditAdapterController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Controller HTTP GET method used to get the Posts with most up votes and Users with most posts since the application began
        /// </summary>
        /// <returns></returns>
        [HttpGet("GetPollInformation")]
        public string GetPollInformation()
        {
            try
            {
                return RedditAdapter.GetPollInformation();
            }
            catch(Exception e)
            {
                _logger.LogError("Error in Polling: " + e.Message);
                return "Error in Polling";
            }
        }
    }
}