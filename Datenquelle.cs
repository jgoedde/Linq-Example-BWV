using System;
using System.Linq;
using System.Collections.Generic;

public class CatalogItem : BaseEntity, IAggregateRoot
{
    public string Name { get; private set; }
    public string Description { get; private set; }
    public decimal Price { get; private set; }
    public string PictureUri { get; private set; }
    public int CatalogTypeId { get; private set; }
    public CatalogType? CatalogType { get; private set; }
    public int CatalogBrandId { get; private set; }
    public CatalogBrand? CatalogBrand { get; private set; }

    public CatalogItem(int catalogTypeId,
        int catalogBrandId,
        string description,
        string name,
        decimal price,
        string pictureUri)
    {
        CatalogTypeId = catalogTypeId;
        CatalogBrandId = catalogBrandId;
        Description = description;
        Name = name;
        Price = price;
        PictureUri = pictureUri;
    }

    public void UpdateDetails(CatalogItemDetails details)
    {
        Name = details.Name;
        Description = details.Description;
        Price = details.Price;
    }

    public void UpdateBrand(int catalogBrandId)
    {
        CatalogBrandId = catalogBrandId;
    }

    public void UpdateType(int catalogTypeId)
    {
        CatalogTypeId = catalogTypeId;
    }

    public void UpdatePictureUri(string pictureName)
    {
        if (string.IsNullOrEmpty(pictureName))
        {
            PictureUri = string.Empty;
            return;
        }

        PictureUri = $"images\\products\\{pictureName}?{new DateTime().Ticks}";
    }

    public struct CatalogItemDetails
    {
        public string? Name { get; }
        public string? Description { get; }
        public decimal Price { get; }

        public CatalogItemDetails(string? name, string? description, decimal price)
        {
            Name = name;
            Description = description;
            Price = price;
        }
    }
}

public interface IAggregateRoot
{
}

public class OrderItem : BaseEntity
{
    public CatalogItemOrdered ItemOrdered { get; private set; }
    public decimal UnitPrice { get; private set; }
    public int Units { get; private set; }

#pragma warning disable CS8618 // Required by Entity Framework
    private OrderItem() { }

    public OrderItem(CatalogItemOrdered itemOrdered, decimal unitPrice, int units)
    {
        ItemOrdered = itemOrdered;
        UnitPrice = unitPrice;
        Units = units;
    }
}

public class Order : BaseEntity, IAggregateRoot
{
#pragma warning disable CS8618 // Required by Entity Framework
    private Order() { }

    public Order(string buyerId, Address shipToAddress, List<OrderItem> items)
    {
        BuyerId = buyerId;
        ShipToAddress = shipToAddress;
        _orderItems = items;
    }

    public string BuyerId { get; private set; }
    public DateTimeOffset OrderDate { get; private set; } = DateTimeOffset.Now;
    public Address ShipToAddress { get; private set; }

    // DDD Patterns comment
    // Using a private collection field, better for DDD Aggregate's encapsulation
    // so OrderItems cannot be added from "outside the AggregateRoot" directly to the collection,
    // but only through the method Order.AddOrderItem() which includes behavior.
    private readonly List<OrderItem> _orderItems = new List<OrderItem>();

    // Using List<>.AsReadOnly()
    // This will create a read only wrapper around the private list so is protected against "external updates".
    // It's much cheaper than .ToList() because it will not have to copy all items in a new collection. (Just one heap alloc for the wrapper instance)
    //https://msdn.microsoft.com/en-us/library/e78dcd75(v=vs.110).aspx
    public IReadOnlyCollection<OrderItem> OrderItems => _orderItems.AsReadOnly();

    public decimal Total()
    {
        var total = 0m;
        foreach (var item in _orderItems)
        {
            total += item.UnitPrice * item.Units;
        }

        return total;
    }
}

public class Address // ValueObject
{
    public string Street { get; private set; }

    public string City { get; private set; }

    public string State { get; private set; }

    public string Country { get; private set; }

    public string ZipCode { get; private set; }

#pragma warning disable CS8618 // Required by Entity Framework
    private Address() { }

    public Address(string street, string city, string state, string country, string zipcode)
    {
        Street = street;
        City = city;
        State = state;
        Country = country;
        ZipCode = zipcode;
    }
}

public class Basket : BaseEntity, IAggregateRoot
{
    public string BuyerId { get; private set; }
    private readonly List<BasketItem> _items = new List<BasketItem>();
    public IReadOnlyCollection<BasketItem> Items => _items.AsReadOnly();

    public int TotalItems => _items.Sum(i => i.Quantity);


    public Basket(string buyerId)
    {
        BuyerId = buyerId;
    }

    public void AddItem(int catalogItemId, decimal unitPrice, int quantity = 1)
    {
        if (!Items.Any(i => i.CatalogItemId == catalogItemId))
        {
            _items.Add(new BasketItem(catalogItemId, quantity, unitPrice));
            return;
        }

        var existingItem = Items.First(i => i.CatalogItemId == catalogItemId);
        existingItem.AddQuantity(quantity);
    }

    public void RemoveEmptyItems()
    {
        _items.RemoveAll(i => i.Quantity == 0);
    }

    public void SetNewBuyerId(string buyerId)
    {
        BuyerId = buyerId;
    }
}

public class BasketItem : BaseEntity
{
    public decimal UnitPrice { get; private set; }
    public int Quantity { get; private set; }
    public int CatalogItemId { get; private set; }
    public int BasketId { get; private set; }

    public BasketItem(int catalogItemId, int quantity, decimal unitPrice)
    {
        CatalogItemId = catalogItemId;
        UnitPrice = unitPrice;
        SetQuantity(quantity);
    }

    public void AddQuantity(int quantity)
    {
        Quantity += quantity;
    }

    public void SetQuantity(int quantity)
    {
        Quantity = quantity;
    }
}

public class Buyer : BaseEntity, IAggregateRoot
{
    public string IdentityGuid { get; private set; }

    private List<PaymentMethod> _paymentMethods = new List<PaymentMethod>();

    public IEnumerable<PaymentMethod> PaymentMethods => _paymentMethods.AsReadOnly();

#pragma warning disable CS8618 // Required by Entity Framework
    private Buyer() { }

    public Buyer(string identity) : this()
    {
        IdentityGuid = identity;
    }

    public void AddPaymentMethod(PaymentMethod paymentMethod)
    {
        _paymentMethods.Add(paymentMethod);
    }
}

public class PaymentMethod : BaseEntity
{
    public string? Alias { get; set; }

    public string?
        CardId { get; set; } // actual card data must be stored in a PCI compliant system, like Stripe

    public string? Last4 { get; set; }
}

/// <summary>
/// Represents a snapshot of the item that was ordered. If catalog item details change, details of
/// the item that was part of a completed order should not change.
/// </summary>
public class CatalogItemOrdered // ValueObject
{
    public CatalogItemOrdered(int catalogItemId, string productName, string pictureUri)
    {
        CatalogItemId = catalogItemId;
        ProductName = productName;
        PictureUri = pictureUri;
    }

#pragma warning disable CS8618 // Required by Entity Framework
    private CatalogItemOrdered() { }

    public int CatalogItemId { get; private set; }
    public string ProductName { get; private set; }
    public string PictureUri { get; private set; }
}

public abstract class BaseEntity
{
    public virtual int Id { get; protected set; }
}

public class CatalogBrand : BaseEntity, IAggregateRoot
{
    public string Brand { get; private set; }

    public CatalogBrand(string brand)
    {
        Brand = brand;
    }
}

public class CatalogType : BaseEntity, IAggregateRoot
{
    public string Type { get; private set; }

    public CatalogType(string type)
    {
        Type = type;
    }
}

public class Datenquelle
{
    public static (List<CatalogItem> catalogItems, List<CatalogBrand> catalogBrands, List<CatalogType> catalogTypes,
        List<BasketItem> basketItems, List<Order> orders, List<Buyer> buyers, List<PaymentMethod> paymentMethods,
        List<Basket> baskets) GibMirDummyDaten()
    {
        // Dummy Catalog Brands
        var catalogBrands = new List<CatalogBrand>
        {
            new CatalogBrand("Nike"),
            new CatalogBrand("Adidas"),
            new CatalogBrand("Puma"),
            new CatalogBrand("Reebok"),
            new CatalogBrand("Under Armour")
        };

        // Dummy Catalog Types
        var catalogTypes = new List<CatalogType>
        {
            new CatalogType("Running Shoes"),
            new CatalogType("Sneakers"),
            new CatalogType("Sandals"),
            new CatalogType("Boots"),
            new CatalogType("Flip Flops")
        };

        // Dummy Catalog Items
        var catalogItems = new List<CatalogItem>();
        for (int i = 0; i < 20; i++)
        {
            catalogItems.Add(new CatalogItem(
                catalogTypes[i % catalogTypes.Count].Id,
                catalogBrands[i % catalogBrands.Count].Id,
                $"Description for Product {i + 1}",
                $"Product {i + 1}",
                20.99m + (i * 5), // Adjust price for variety
                $"product_{i + 1}.jpg"));
        }

        // Dummy Catalog Items Ordered
        var catalogItemsOrdered = catalogItems.Select((item, index) =>
            new CatalogItemOrdered(item.Id, item.Name, item.PictureUri)).ToList();

        // Dummy Basket Items
        var basketItems = new List<BasketItem>
        {
            new BasketItem(catalogItems[0].Id, 2, catalogItems[0].Price),
            new BasketItem(catalogItems[1].Id, 1, catalogItems[1].Price),
            new BasketItem(catalogItems[2].Id, 3, catalogItems[2].Price)
        };

        // Dummy Orders
        var orders = new List<Order>
        {
            new Order("buyer1", new Address("123 Main St", "Anytown", "CA", "USA", "12345"),
                new List<OrderItem>
                {
                    new OrderItem(catalogItemsOrdered[0], catalogItems[0].Price, 2),
                    new OrderItem(catalogItemsOrdered[1], catalogItems[1].Price, 1)
                }),
            new Order("buyer2", new Address("456 Elm St", "Otherville", "NY", "USA", "67890"),
                new List<OrderItem>
                {
                    new OrderItem(catalogItemsOrdered[2], catalogItems[2].Price, 3),
                    new OrderItem(catalogItemsOrdered[3], catalogItems[3].Price, 1),
                    new OrderItem(catalogItemsOrdered[4], catalogItems[4].Price, 2)
                })
        };

        // Dummy Buyers
        var buyers = new List<Buyer> { new Buyer("buyer1"), new Buyer("buyer2") };

        // Dummy Payment Methods
        var paymentMethods = new List<PaymentMethod>
        {
            new PaymentMethod { Alias = "Visa", CardId = "CardId1", Last4 = "1234" },
            new PaymentMethod { Alias = "MasterCard", CardId = "CardId2", Last4 = "5678" },
            new PaymentMethod { Alias = "American Express", CardId = "CardId3", Last4 = "9012" }
        };

        // Assigning Payment Methods to Buyers
        buyers[0].AddPaymentMethod(paymentMethods[0]);
        buyers[1].AddPaymentMethod(paymentMethods[1]);
        buyers[1].AddPaymentMethod(paymentMethods[2]);

        // Dummy Baskets
        var baskets = new List<Basket> { new Basket("buyer1"), new Basket("buyer2") };

        // Adding Items to Baskets
        baskets[0].AddItem(catalogItems[0].Id, catalogItems[0].Price, 2);
        baskets[0].AddItem(catalogItems[1].Id, catalogItems[1].Price, 1);
        baskets[1].AddItem(catalogItems[2].Id, catalogItems[2].Price, 3);
        baskets[1].AddItem(catalogItems[3].Id, catalogItems[3].Price, 1);
        baskets[1].AddItem(catalogItems[4].Id, catalogItems[4].Price, 2);

        return (catalogItems, catalogBrands, catalogTypes, basketItems, orders, buyers, paymentMethods, baskets);
    }
}
