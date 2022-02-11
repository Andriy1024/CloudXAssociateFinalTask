using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.eShopWeb.ApplicationCore.Services;

public class AzureOrdersFunctionsClient
{
    private readonly HttpClient _client;

    public AzureOrdersFunctionsClient(HttpClient client)
    {
        _client = client;
    }

    public async Task TriggerOrderItemsReserverFunctionAsync<TIn>(TIn order) 
    {
        var body = SerializeContent(order);
        var response = await _client.PostAsync("api/DeliveryOrderProcessor", body);
        response.EnsureSuccessStatusCode();
    }

    private StringContent SerializeContent<TIn>(TIn obj)
    {
        string json = JsonSerializer.Serialize(obj, obj.GetType());
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        return content;
    }
}

public static class AzureFunctionClientServiceCollectionExtensions 
{
    public static IServiceCollection AddOrderItemsReserverFunctionClient(this IServiceCollection services, string baseUrl) 
    {
        if (string.IsNullOrEmpty(baseUrl))
            throw new ArgumentNullException($"{nameof(baseUrl)} is empty.");

        services.AddHttpClient<AzureOrdersFunctionsClient>(httpClient =>
        {
            httpClient.BaseAddress = new Uri(baseUrl);
        });

        return services;
    }
}
