using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace PizzaFunction.Functions
{
    public class SendTestSignalR
    {
        private readonly ILogger<SendTestSignalR> _logger;

        public SendTestSignalR(ILogger<SendTestSignalR> logger)
        {
            _logger = logger;
        }

        [Function("SendTestSignalR")]
        public async Task <IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");
            return new OkObjectResult("Welcome to Azure Functions!");
        }
    }
}
