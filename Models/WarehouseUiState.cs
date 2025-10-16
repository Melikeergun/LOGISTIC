using System;
using System.Collections.Generic;

namespace MLYSO.Web.Models
{
    public class WarehouseUiState
    {
        // --- Raf Yönetimi (demo) ---
        public string RackPrefix { get; set; } = "A";
        public int RackAisle { get; set; } = 1;
        public int RackLevel { get; set; } = 0;

        // --- Dijital İkiz Eşleşmesi (sizde kullanılıyordu) ---
        public string TwinRack { get; set; } = "A-01";
        public string TwinSku { get; set; } = "";
        public string TwinAddr { get; set; } = "";

        // --- İş Emri Akışı (sizde kullanılıyordu) ---
        public string FlowTitle { get; set; } = "";
        public string FlowStatus { get; set; } = "Open"; // Open / InProgress / Done
        public string FlowAssignee { get; set; } = "";

        // --- Kaydedilen raf girdileri için liste (yeni) ---
        public List<RackItem> RackList { get; set; } = new();
        public int NextId { get; set; } = 1; // artan id
    }

    public class RackItem
    {
        public int Id { get; set; }
        public string RackPrefix { get; set; } = "";
        public int RackAisle { get; set; }
        public int RackLevel { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    // PATCH için null gelen alanlar dokunulmaz
    public record WarehouseUiStatePatch(
        string? RackPrefix, int? RackAisle, int? RackLevel,
        string? TwinRack, string? TwinSku, string? TwinAddr,
        string? FlowTitle, string? FlowStatus, string? FlowAssignee
    );
}
