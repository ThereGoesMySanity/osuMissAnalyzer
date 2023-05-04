using Microsoft.AspNetCore.Mvc;

namespace OsuMissAnalyzer.Server
{
    [Route("api/bot")]
    [ApiController]
    public class BotController : ControllerBase
    {
        private readonly ServerContext context;

        public BotController(ServerContext context)
        {
            this.context = context;
        }
    }
}