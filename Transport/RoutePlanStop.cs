namespace MLYSO.Web.Models.Transport
{
    public class RoutePlanStop
    {
        public int Id { get; set; }

        public int RoutePlanId { get; set; }
        public RoutePlan? RoutePlan { get; set; }

        public string OrderNo { get; set; } = string.Empty; // sipariş no
        public int Sequence { get; set; }                   // rota sırası

        public string Title { get; set; } = string.Empty;   // müşteri/mağaza adı
        public string Address { get; set; } = string.Empty;

        public double? Lat { get; set; }
        public double? Lng { get; set; }

        public DateTime? WindowStart { get; set; }
        public DateTime? WindowEnd { get; set; }

        public string Status { get; set; } = "Planned";     // Planned/Completed/Failed
        public string? Note { get; set; }
    }
}
