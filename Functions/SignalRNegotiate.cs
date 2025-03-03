using System.Collections.Generic;
using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace PizzaFunction.Functions
{
    public class SignalRNegotiate
    {
        private readonly ILogger _logger;

        public SignalRNegotiate(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger("negotiate");
        }

        [Function("SignalRNegotiate")]
        public async Task <HttpResponseData> Negotiate(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequestData req,
            [SignalRConnectionInfoInput(HubName = "orders")] SignalRConnectionInfo connectionInfo)
        {
            _logger.LogInformation($"SignalR Connection URL = '{connectionInfo.Url}'");

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");
            // response.WriteString($"Connection URL = '{connectionInfo.Url}'");

            var responseBody = new
            {
                url = connectionInfo.Url,
                accessToken = connectionInfo.AccessToken
            };

            response.WriteStringAsync(JsonSerializer.Serialize(responseBody));

            return response;
        }
    }
}