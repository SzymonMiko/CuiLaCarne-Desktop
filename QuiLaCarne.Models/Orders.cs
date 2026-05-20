using System.ComponentModel.DataAnnotations.Schema;
using QuiLaCarne.Models.Base;
using QuiLaCarne.Models.Lookup;

namespace QuiLaCarne.Models;

[Table("orders")]
public class Orders : BaseEntity
{
    [Column("table_id")]
    public Guid TableId { get; set; }

    public RestaurantTables Table { get; set; } = null!;

    [Column("user_id")]
    public Guid UserId { get; set; }

    public Users User { get; set; } = null!;

    public ICollection<OrderStatus> Statuses { get; set; } = new List<OrderStatus>();

    public ICollection<OrderItems> Items { get; set; } = new List<OrderItems>();
}