namespace Sitecore.Support.Commerce.Engine.Connect.Pipelines.Orders
{
  using Sitecore.Commerce.Connect.CommerceServer.Orders.Models;
  using Sitecore.Commerce.Engine.Connect.Pipelines.Arguments;
  using Sitecore.Commerce.Engine.Connect.Pipelines.Orders;
  using Sitecore.Commerce.Entities;
  using Sitecore.Commerce.Plugin.Orders;
  using System.Collections.Generic;
  using System.Collections.ObjectModel;
  using System.Linq;

  /// <summary>
  ///  Pipeline processor to translate from a <see cref="Order" /> to a <see cref="CommerceOrder" />.
  /// </summary>
  /// <seealso>
  ///     <cref>
  ///         Sitecore.Commerce.Engine.Connect.Pipelines.TranslateODataEntityToEntity{Sitecore.Commerce.Engine.Connect.Pipelines.Arguments.TranslateOrderToEntityRequest,
  ///         Sitecore.Commerce.Engine.Connect.Pipelines.Arguments.TranslateOrderToEntityResult,
  ///         Sitecore.Commerce.Plugin.Orders.Order, Sitecore.Commerce.Connect.CommerceServer.Orders.Models.CommerceOrder}
  ///     </cref>
  /// </seealso>
  public class FixedTranslateOrderToEntity : TranslateOrderToEntity
  {
    /// <summary>
    /// Initializes a new instance of the <see cref="TranslateOrderToEntity"/> class.
    /// </summary>
    /// <param name="entityFactory">The entity factory.</param>
    public FixedTranslateOrderToEntity([NotNull] IEntityFactory entityFactory)
          : base(entityFactory)
    {
    }

    /// <summary>
    /// Translates a <see cref="Order" /> to a specific <see cref="CommerceOrder" />
    /// </summary>
    /// <param name="request">The request.</param>
    /// <param name="source">The  <see cref="Sitecore.Commerce.Plugin.Orders.Order" /></param>
    /// <param name="destination">The <see cref="CommerceOrder" /> to copy to</param>
    protected override void Translate(TranslateOrderToEntityRequest request, Sitecore.Commerce.Plugin.Orders.Order source, CommerceOrder destination)
    {
      base.Translate(request, source, destination);

      destination.ExternalId = source.Id;
      destination.ShopName = source.ShopName;
      destination.Name = source.Name;
      destination.LineItemCount = source.Lines.Count;
      destination.OrderID = source.Id;
      destination.TrackingNumber = source.OrderConfirmationId;
      destination.Status = source.Status;
      destination.OrderDate = source.OrderPlacedDate.DateTime;
      if (source.DateCreated.HasValue)
      {
        destination.Created = source.DateCreated.Value.DateTime;
      }

      if (source.DateUpdated.HasValue)
      {
        destination.LastModified = source.DateUpdated.Value.DateTime;
      }

      if (source.Components != null && source.Components.Any())
      {
        var contactComponent = source.Components.OfType<Sitecore.Commerce.Core.ContactComponent>().FirstOrDefault();
        if (contactComponent != null)
        {
          destination.Email = contactComponent.Email ?? string.Empty;
          destination.UserId = contactComponent.ShopperId;
          destination.CustomerId = contactComponent.CustomerId ?? contactComponent.ShopperId;
        }
      }

      destination.OrderForms = new ReadOnlyCollection<CommerceOrderForm>(new List<CommerceOrderForm> { new CommerceOrderForm() });

      base.TranslateLines(request, source, destination);
      base.TranslateAdjustments(request, source, destination);
      base.TranslateShippingInfo(request, source, destination);
      base.TranslatePaymentInfo(request, source, destination);
      base.TranslateTotals(request, source, destination);
    }

  }
}

