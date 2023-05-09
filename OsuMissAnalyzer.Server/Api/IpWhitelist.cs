using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NetTools;
using OsuMissAnalyzer.Server.Settings;

namespace OsuMissAnalyzer.Server.Api
{
    public class IpWhitelist
    {
        private readonly RequestDelegate next;
        private readonly ILogger<IpWhitelist> logger;
        private readonly IPAddressRange[] addresses;

        public IpWhitelist(RequestDelegate next, ILogger<IpWhitelist> logger, IOptions<ServerOptions> options)
        {
            this.next = next;
            this.logger = logger;
            this.addresses = options.Value.IpWhitelist.Select(IPAddressRange.Parse).ToArray();
        }

        public async Task Invoke(HttpContext context)
        {
            var remoteIp = context.Connection.RemoteIpAddress;
            logger.LogDebug("Request from {remoteIp}", remoteIp);
            if (addresses.Any(a => a.Contains(remoteIp)))
            {
                await next.Invoke(context);
            }
            else
            {
                logger.LogWarning("Request rejected: {remoteIp}", remoteIp);
                context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            }
        }
    }
}