using System.ComponentModel.DataAnnotations.Schema;
using QuiLaCarne.Models.Base;
using QuiLaCarne.Models.Lookup;

namespace QuiLaCarne.Models;

[Table("restaurant_tables")]
public class RestaurantTables : BaseEntity
{
    [Column("table_number")]
    public int TableNumber { get; set; }

    [Column("capacity")]
    public int Capacity { get; set; }

    public ICollection<TableStatus> TableStatus { get; set; } = new List<TableStatus>();

    public ICollection<Reservations> Reservations { get; set; } = new List<Reservations>();
    public ICollection<Orders> Orders { get; set; } = new List<Orders>();
}