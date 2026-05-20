using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuiLaCarne.Models.Base;

/// <summary>
/// Base for entities that support locale-based name translation.
/// The name field stores a locale key (e.g. "en", "pl") which is resolved at runtime.
/// </summary>
public abstract class BaseTranslatedEntity : BaseEntity
{
    [Column("name_key")]
    public string? NameKey { get; set; }

    [Column("translated_at")]
    public DateTimeOffset? TranslatedAt { get; set; }

    public string GetTranslatedName(string locale)
    {
        // Resolve translation key — implement with your i18n provider
        return NameKey ?? string.Empty;
    }
}
