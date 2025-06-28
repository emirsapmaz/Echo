using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using System;
using System.Threading.Tasks;

namespace echoo.Controllers
{
    [Authorize]
    public class SpeechController : Controller
    {
        private readonly IConfiguration _configuration;

        public SpeechController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
        public IActionResult GetSpeechToken()
        {
            string speechKey = _configuration["AzureSpeech:SubscriptionKey"];
            string speechRegion = _configuration["AzureSpeech:Region"];

            if (string.IsNullOrEmpty(speechKey) || string.IsNullOrEmpty(speechRegion))
            {
                return BadRequest("Speech service configuration is missing.");
            }

            return Json(new { subscriptionKey = speechKey, region = speechRegion });
        }
    }
}