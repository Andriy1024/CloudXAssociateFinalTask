using System.Collections.Generic;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Ardalis.GuardClauses;
using Microsoft.eShopWeb.ApplicationCore.Entities;
using Microsoft.eShopWeb.ApplicationCore.Entities.BasketAggregate;
using Microsoft.eShopWeb.ApplicationCore.Entities.OrderAggregate;
using Microsoft.eShopWeb.ApplicationCore.Interfaces;
using Microsoft.eShopWeb.ApplicationCore.Specifications;
using Azure.Messaging.ServiceBus;

namespace Microsoft.eShopWeb.ApplicationCore.Services;

public class OrderService : IOrderService
{
    private readonly IRepository<Order> _orderRepository;
    private readonly IUriComposer _uriComposer;
    private readonly IRepository<Basket> _basketRepository;
    private readonly IRepository<CatalogItem> _itemRepository;
    private readonly AzureOrdersFunctionsClient _orderReserver;
    private readonly ServiceBusClient _serviceBus;

    public OrderService(IRepository<Basket> basketRepository,
        IRepository<CatalogItem> itemRepository,
        IRepository<Order> orderRepository,
        IUriComposer uriComposer,
        ServiceBusClient serviceBus,
        AzureOrdersFunctionsClient orderReserver
        )
    {
        _orderRepository = orderRepository;
        _uriComposer = uriComposer;
        _serviceBus = serviceBus;
        _basketRepository = basketRepository;
        _itemRepository = itemRepository;
        _orderReserver = orderReserver;
    }

    public async Task CreateOrderAsync(int basketId, Address shippingAddress)
    {
        var basketSpec = new BasketWithItemsSpecification(basketId);
        var basket = await _basketRepository.GetBySpecAsync(basketSpec);

        Guard.Against.NullBasket(basketId, basket);
        Guard.Against.EmptyBasketOnCheckout(basket.Items);

        var catalogItemsSpecification = new CatalogItemsSpecification(basket.Items.Select(item => item.CatalogItemId).ToArray());
        var catalogItems = await _itemRepository.ListAsync(catalogItemsSpecification);

        var items = basket.Items.Select(basketItem =>
        {
            var catalogItem = catalogItems.First(c => c.Id == basketItem.CatalogItemId);
            var itemOrdered = new CatalogItemOrdered(catalogItem.Id, catalogItem.Name, _uriComposer.ComposePicUri(catalogItem.PictureUri));
            var orderItem = new OrderItem(itemOrdered, basketItem.UnitPrice, basketItem.Quantity);
            return orderItem;
        }).ToList();

        var order = new Order(basket.BuyerId, shippingAddress, items);

        await _orderRepository.AddAsync(order);

        await _orderReserver.TriggerOrderItemsReserverFunctionAsync(new
        {
            Id = order.Id,
            Amount = order.Total(),
            BuyerId = order.BuyerId,
            Items = order.OrderItems.Select(o => new
            {
                Id = o.Id,
                Name = o.ItemOrdered.ProductName
            })
            .ToArray()
        });


        await SendOrderToServiceBus(new
        {
            Id = order.Id,
            Amount = order.Total(),
            BuyerId = order.BuyerId,
            Items = order.OrderItems.Select(o => new
            {
                Id = o.Id,
                Name = o.ItemOrdered.ProductName
            })
            .ToArray()
        });
    }

    private async Task SendOrderToServiceBus<T>(T order)
    {
        await using ServiceBusSender sender = _serviceBus.CreateSender("orders");

        try
        {
            string messageBody = JsonSerializer.Serialize(order);

            var message = new ServiceBusMessage(messageBody);

            await sender.SendMessageAsync(message);
        }
        catch (Exception exception)
        {
            Console.WriteLine($"{DateTime.Now} :: Exception: {exception.Message}");
        }
        finally
        {
            await sender.DisposeAsync();
        }
    }
}
