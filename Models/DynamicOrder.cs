
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace MLYSO.Web.Models;

public class DynamicOrder
{
    public int Id { get; set; }

    [MaxLength(36)]
    public string OrderNo { get; set; } = Guid.NewGuid().ToString("N")[..12].ToUpper();

    [MaxLength(80)]
    public string CreatedBy { get; set; } = "";

    [MaxLength(80)]
    public string CreatedByRole { get; set; } = "";

    [MaxLength(60)]
    public string Status { get; set; } = "created";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // All dataset-aligned fields captured in a flexible JSON bag
    public string FieldsJson { get; set; } = "{}";

    [NotMapped]
    public Dictionary<string, object?> Fields
    {
        get => string.IsNullOrWhiteSpace(FieldsJson) ? new() : (JsonSerializer.Deserialize<Dictionary<string, object?>>(FieldsJson) ?? new());
        set => FieldsJson = JsonSerializer.Serialize(value ?? new());
    }
}

