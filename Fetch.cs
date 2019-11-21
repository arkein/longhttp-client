using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace LongHttp.Client
{
    public static class Fetch
    {
        //http client with timeout larger than the function lifetime
        private static readonly HttpClient client;

        static Fetch()
        {
            ServicePointManager.ServerCertificateValidationCallback += (a, b, c, d) => true;
            client = new HttpClient(
                new HttpClientHandler() { ServerCertificateCustomValidationCallback = (a, b, c, d) => true })
            { Timeout = TimeSpan.FromMinutes(30) };
        }         

        [FunctionName("Fetch")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "fetch/{time:int}")] HttpRequest req, int time,
            ILogger log)
        {

            if (time <= 0)
            {
                return new BadRequestResult();
            }

            var backendString = Environment.GetEnvironmentVariable("Backend");

            if (string.IsNullOrEmpty(backendString))
            {
                throw new InvalidOperationException("Backend URL is not configured");
            }

            backendString = string.Format(backendString, time);

            log.LogInformation($"Starting communication with the backend that will likely take {time} seconds to complete.");

            //start a request to slow backend, it will take at least {time} seconds to return
            var request = new HttpRequestMessage(HttpMethod.Get, backendString);
            var response = await client.GetAsync(backendString);

            log.LogInformation($"Backend responded: {response}");

            var result = "Empty response message";
            if (response.Content != null) result = await response.Content.ReadAsStringAsync();

            //passthrough the backend response
            return new OkObjectResult(result);
        }
    }
}
