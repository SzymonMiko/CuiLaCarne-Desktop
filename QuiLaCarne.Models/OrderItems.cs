using System.ComponentModel.DataAnnotations.Schema;
using QuiLaCarne.Models.Base;
using QuiLaCarne.Models.Lookup;

namespace QuiLaCarne.Models;

[Table("order_items")]
public class OrderItems : BaseEntity
{
    [Column("quantity")]
    public int Quantity { get; set; } = 1;

    [Column("note")]
    public string? Note { get; set; }

    [Column("order_id")]
    public Guid OrderId { get; set; }

    public Orders Order { get; set; } = null!;

    [Column("dish_id")]
    public Guid DishId { get; set; }

    public Dishes Dish { get; set; } = null!;

    public ICollection<OrderItemsStatus> Statuses { get; set; } = new List<OrderItemsStatus>();
}