using System.Collections.Generic;

namespace MLYSO.Web.Models
{
    // --- Warehouse Manager ---
    public class RackState
    {
        public int NextId { get; set; } = 1;
        public List<Rack> Racks { get; set; } = new();
    }
    public class Rack
    {
        public int Id { get; set; }
        public string Code { get; set; } = "";
        public int Capacity { get; set; }
        public int Used { get; set; }
    }

    public class TwinMatchState
    {
        public int NextId { get; set; } = 1;
        public List<TwinMatch> Items { get; set; } = new();
    }
    public class TwinMatch
    {
        public int Id { get; set; }
        public string RackCode { get; set; } = "";
        public string Sku { get; set; } = "";
        public int Qty { get; set; }
    }

    public class WorkOrderState
    {
        public int NextId { get; set; } = 1;
        public List<WorkOrder> Items { get; set; } = new();
    }
    public class WorkOrder
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Status { get; set; } = "Open"; // Open -> InProgress -> Done
        public string? Assignee { get; set; }
    }

    // --- Planning / Forecast ---
    public class ForecastState
    {
        public int NextId { get; set; } = 1;
        public List<ForecastRow> Items { get; set; } = new();
    }
    public class ForecastRow
    {
        public int Id { get; set; }
        public string Sku { get; set; } = "";
        public DateOnly Date { get; set; }
        public int Demand { get; set; }
        public string? Note { get; set; }
    }
}
