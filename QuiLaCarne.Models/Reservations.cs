using System.ComponentModel.DataAnnotations.Schema;
using QuiLaCarne.Models.Base;
using QuiLaCarne.Models.Lookup;

namespace QuiLaCarne.Models;

[Table("reservations")]
public class Reservations : BaseEntity
{
    [Column("reserved_from")]
    public DateTimeOffset ReservedFrom { get; set; }

    [Column("reserved_until")]
    public DateTimeOffset? ReservedUntil { get; set; }

    [Column("table_id")]
    public Guid TableId { get; set; }

    public RestaurantTables Table { get; set; } = null!;

    [Column("user_id")]
    public Guid UserId { get; set; }

    public Users User { get; set; } = null!;

    public ICollection<ReservationStatus> Statuses { get; set; } = new List<ReservationStatus>();
}