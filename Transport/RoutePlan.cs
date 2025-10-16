namespace MLYSO.Web.Models.Transport
{
    public class RoutePlan
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;       // RP-0001
        public string Name { get; set; } = string.Empty;       // Sabah Sevkiyatı
        public DateTime PlanDate { get; set; } = DateTime.UtcNow.Date;
        public string VehiclePlate { get; set; } = string.Empty;
        public string Optimization { get; set; } = "none";     // none | time | distance

        public ICollection<RoutePlanStop> Stops { get; set; } = new List<RoutePlanStop>();
    }
}
