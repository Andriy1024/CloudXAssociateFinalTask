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
using Azure.Storage.Blobs;
using System.Text;
using Azure.Storage.Blobs.Models;

namespace SaveOrderToCosmosDB
{
    public static class OrderItemsReserver
    {
        [FunctionName("OrderItemsReserver")]
        [FixedDelayRetry(5, "00:00:10")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
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
                    IsSuccess = false,
                    Message = "Invalid request",
                });
            }

            if (order.BuyerId == "test-app-logic-send-mail-step")
            {
                return new BadRequestObjectResult(new
                {
                    IsSuccess = false,
                    Message = "Test App Logic Send Mail Step",
                });
            }

            log.LogInformation($"Order: {order.ToString()} processing...");

            await UploadBlobAsync(orderId, orderJson);

            return new OkObjectResult(new
            {
                IsSuccess = true,
                Message = $"Order {order.ToString()} processed."
            });
        }

        private static async Task UploadBlobAsync(int orderId, string jsonOrder)
        {
            var contentType = "application/json";
            var blobName = $"order-{orderId}.json";
            var connectionString = Environment.GetEnvironmentVariable("OrdersStorageConnection") ?? throw new ArgumentNullException("OrdersStorageConnection");
            var storageClient = new BlobServiceClient(connectionString);
            var containerClient = storageClient.GetBlobContainerClient("orders");
            var blobClient = containerClient.GetBlobClient(blobName);

            using (Stream stream = new MemoryStream(Encoding.UTF8.GetBytes(jsonOrder)))
            {
                await blobClient.UploadAsync(stream, new BlobHttpHeaders() { ContentType = contentType });
            }
        }
    }
}
