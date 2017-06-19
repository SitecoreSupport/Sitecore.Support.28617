namespace Sitecore.Support.Commerce.Engine.Connect.Pipelines.Orders
{
  using System;
  using System.Collections.Generic;
  using System.Globalization;
  using System.Linq;

  using Sitecore.Commerce.Connect.CommerceServer.Orders.Models;
  using Sitecore.Commerce.ServiceProxy;
  using Sitecore.Commerce.Core;
  using Sitecore.Commerce.Entities;
  using Sitecore.Commerce.Pipelines;
  using Sitecore.Commerce.Plugin.Orders;
  using Sitecore.Commerce.Services.Orders;
  using Sitecore.Diagnostics;
  using Microsoft.OData.Client;
  using Sitecore.Commerce.Engine.Connect.Pipelines;

  /// <summary>
  /// Pipeline processor to get visitor's orders
  /// </summary>
  /// <seealso cref="Sitecore.Commerce.Engine.Connect.Pipelines.PipelineProcessor" />
  public class FixedGetVisitorOrders : PipelineProcessor
  {
    /// <summary>
    /// Initializes a new instance of the <see cref="GetVisitorOrders"/> class.
    /// </summary>
    /// <param name="entityFactory">The entity factory.</param>
    /// <param name="itemsToTake">The items to take</param>
    public FixedGetVisitorOrders([NotNull]IEntityFactory entityFactory, string itemsToTake)
    {
      Assert.ArgumentNotNull(entityFactory, "entityFactory");

      this.EntityFactory = entityFactory;
      this.ItemsToTake = Convert.ToInt32(itemsToTake, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Gets the entity factory.
    /// </summary>
    /// <value>
    /// The entity factory.
    /// </value>
    public IEntityFactory EntityFactory { get; private set; }

    /// <summary>
    /// Gets the items to take.
    /// </summary>
    /// <value>
    /// The items to take.
    /// </value>
    public int ItemsToTake { get; private set; }

    /// <summary>
    /// Process the Pipeline event
    /// </summary>
    /// <param name="args">The arguments.</param>
    public override void Process(ServicePipelineArgs args)
    {
      GetVisitorOrdersRequest request;
      GetVisitorOrdersResult result;
      FixedPipelineUtility.ValidateArguments(args, out request, out result);

      try
      {
        Assert.IsNotNull(request.CustomerID, "request.CustomerID");
        Assert.IsNotNull(request.Shop.Name, "request.Shop.Name");

        var orderHeaders = new List<CommerceOrderHeader>();

        var container = this.GetContainer(request.Shop.Name, request.CustomerID);

        var skip = 0;
        var ordersList = Sitecore.Commerce.ServiceProxy.Proxy.GetValue(
          container.GetList(
            string.Format(CultureInfo.InvariantCulture, "Orders-ByCustomer-{0}", request.CustomerID),
            "Sitecore.Commerce.Plugin.Orders.Order, Sitecore.Commerce.Plugin.Orders",
            skip,
            this.ItemsToTake).Expand("Items($expand=Components)"));

        orderHeaders.AddRange(this.TranslateOrderHeaders(ordersList.Items));

        while (skip < ordersList.TotalItemCount)
        {
          skip += this.ItemsToTake;
          ordersList = Sitecore.Commerce.ServiceProxy.Proxy.GetValue(
            container.GetList(
              string.Format(CultureInfo.InvariantCulture, "Orders-ByCustomer-{0}", request.CustomerID),
              "Sitecore.Commerce.Plugin.Orders.Order, Sitecore.Commerce.Plugin.Orders",
              skip,
              this.ItemsToTake).Expand("Items($expand=Components)"));
          if (ordersList == null || !ordersList.Items.Any())
          {
            continue;
          }

          orderHeaders.AddRange(this.TranslateOrderHeaders(ordersList.Items));
        }

        result.OrderHeaders = orderHeaders;
      }
      catch (ArgumentException e)
      {
        result.Success = false;
        result.SystemMessages.Add(FixedPipelineUtility.CreateSystemMessage(e));
      }
      catch (DataServiceQueryException aggregateException)
      {
        result.Success = false;
        result.SystemMessages.Add(FixedPipelineUtility.CreateSystemMessage(aggregateException.Message));
      }

      base.Process(args);
    }

    /// <summary>
    /// Translate a list of <see cref="Order"/> to a list of <see cref="CommerceOrderHeader"/>.
    /// </summary>
    /// <param name="orders">the list of <see cref="Order"/>.</param>
    /// <returns>A list of <see cref="CommerceOrderHeader"/>.</returns>
    public IEnumerable<CommerceOrderHeader> TranslateOrderHeaders(IEnumerable<CommerceEntity> orders)
    {
      Assert.ArgumentNotNull(orders, "orders");

      var orderHeaders = new List<CommerceOrderHeader>();
      var commerceEntities = orders as CommerceEntity[] ?? orders.ToArray();
      if (!commerceEntities.Any())
      {
        return orderHeaders;
      }

      foreach (var order in commerceEntities.Cast<Order>())
      {
        var orderHeader = this.EntityFactory.Create<CommerceOrderHeader>("OrderHeader");

        orderHeader.ExternalId = order.Id;
        orderHeader.ShopName = order.ShopName;
        orderHeader.Name = order.Name;
        orderHeader.OrderID = order.OrderConfirmationId;
        orderHeader.TrackingNumber = order.OrderConfirmationId;
        orderHeader.Status = order.Status;
        orderHeader.OrderDate = order.OrderPlacedDate.DateTime;
        if (order.DateCreated.HasValue)
        {
          orderHeader.Created = order.DateCreated.Value.DateTime;
        }

        if (order.DateUpdated.HasValue)
        {
          orderHeader.LastModified = order.DateUpdated.Value.DateTime;
        }

        if (order.Components != null && order.Components.Any())
        {
          var contactComponent = order.Components.OfType<ContactComponent>().FirstOrDefault();
          if (contactComponent != null)
          {
            orderHeader.Email = contactComponent.Email ?? string.Empty;
            orderHeader.UserId = contactComponent.ShopperId;
            orderHeader.CustomerId = contactComponent.CustomerId ?? contactComponent.ShopperId;
          }
        }

        orderHeaders.Add(orderHeader);
      }

      return orderHeaders;
    }
  }
}
