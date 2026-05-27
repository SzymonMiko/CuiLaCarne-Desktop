namespace QuiLaCarne.Models.DTOS;

public class SyncDictionariesResponse
{
    public List<SyncDictionaryItemResponse> Allergens { get; set; } = [];

    public List<SyncDictionaryItemResponse> DishCategories { get; set; } = [];

    public List<SyncDictionaryItemResponse> BanStatuses { get; set; } = [];

    public List<SyncDictionaryItemResponse> ReportStatuses { get; set; } = [];

    public List<SyncDictionaryItemResponse> OrderStatuses { get; set; } = [];

    public List<SyncDictionaryItemResponse> OrderItemStatuses { get; set; } = [];

    public List<SyncDictionaryItemResponse> ReservationStatuses { get; set; } = [];

    public List<SyncDictionaryItemResponse> TableStatuses { get; set; } = [];
}
