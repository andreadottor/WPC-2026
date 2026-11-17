using Dottor.WPC2026.Web.Models;

namespace Dottor.WPC2026.Web.Services;

/// <summary>Ordini demo generati in memoria (seed fisso: stessi dati a ogni avvio).</summary>
public sealed class OrderService
{
    public static readonly string[] Pizzas =
    [
        "Margherita", "Diavola", "Quattro formaggi", "Capricciosa", "Marinara", "Bufalina",
        "Quattro stagioni", "Ortolana", "Tonno e cipolla", "Boscaiola", "Napoli", "Hawaiana 🍍"
    ];

    private static readonly string[] Streets =
    [
        "Via Roma", "Corso Garibaldi", "Via Mazzini", "Piazza Duomo", "Via dei Mille", "Viale Europa"
    ];

    private static readonly string[] Cities = ["Milano", "Roma", "Torino", "Bologna", "Napoli", "Vicenza"];

    private static readonly string[] Riders =
    [
        "Andrea \"Turbo\" Dottor", "Chiara Ferri", "Luca Moretti", "Sara Galli", "Paolo Conti", "Elena Riva"
    ];

    private readonly Lock _lock = new();
    private readonly List<Order> _orders;

    public OrderService()
    {
        var random = new Random(2026);
        var start = new DateTime(2026, 1, 1, 18, 0, 0);

        _orders = [.. Enumerable.Range(1, 240).Select(id => new Order(
            Id: id,
            Pizza: Pizzas[random.Next(Pizzas.Length)],
            Address: $"{Streets[random.Next(Streets.Length)]} {random.Next(1, 120)}, {Cities[random.Next(Cities.Length)]}",
            Hunger: (HungerLevel)random.Next(4),
            Status: (OrderStatus)random.Next(4),
            CreatedAt: start.AddDays(random.Next(0, 150)).AddMinutes(random.Next(0, 5 * 60)),
            Rider: Riders[random.Next(Riders.Length)]))];
    }

    public IQueryable<Order> GetAll()
    {
        lock (_lock)
        {
            return _orders.ToArray().AsQueryable();
        }
    }

    public Order? GetById(int id)
    {
        lock (_lock)
        {
            return _orders.FirstOrDefault(o => o.Id == id);
        }
    }

    public Order Create(NewOrderModel model)
    {
        lock (_lock)
        {
            var order = new Order(
                Id: _orders.Max(o => o.Id) + 1,
                Pizza: model.Pizza!,
                Address: model.Address!,
                Hunger: model.Hunger!.Value,
                Status: OrderStatus.Received,
                CreatedAt: DateTime.Now,
                Rider: "Da assegnare",
                CustomerEmail: model.Email);
            _orders.Add(order);
            return order;
        }
    }
}
