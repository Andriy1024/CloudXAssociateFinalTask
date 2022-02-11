using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace SaveOrderToCosmosDB
{
    public static class DeliveryOrderProcessor
    {
        private const string _databaseName = "eshop";
        private const string _databaseCollection = "orders";

        [FunctionName("DeliveryOrderProcessor")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            [CosmosDB(
                databaseName: _databaseName,
                collectionName: _databaseCollection,
                ConnectionStringSetting = "CosmosDbConnectionString")] IAsyncCollector<dynamic> documentsOut,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string orderJson = await new StreamReader(req.Body).ReadToEndAsync();

            var order = JsonConvert.DeserializeObject<Order>(orderJson);

            int orderId = order.Id;

            if (orderId == default)
            {
                return new BadRequestObjectResult(new
                {
                    Message = "Invalid request"
                });
            } 

            log.LogInformation($"Order: {order.ToString()} processing...");

            await documentsOut.AddAsync(order);

            return new OkObjectResult(new
            {
                Message = $"Order {order.ToString()} processed."
            });
        }
    }
}
