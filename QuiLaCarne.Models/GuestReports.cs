using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using QuiLaCarne.Models.Base;
using QuiLaCarne.Models.Lookup;

namespace QuiLaCarne.Models;

[Table("guest_reports")]
public class GuestReports : BaseEntity
{
    [Required]
    [Column("description")]
    public string Description { get; set; } = string.Empty;

    [Column("reporter_id")]
    public Guid ReporterId { get; set; }

    public Users Reporter { get; set; } = null!;

    [Column("reported_user_id")]
    public Guid ReportedUserId { get; set; }

    public Users ReportedUser { get; set; } = null!;

    public ICollection<GuestReportStatus> Statuses { get; set; } = new List<GuestReportStatus>();
}