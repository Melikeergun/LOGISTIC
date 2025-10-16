using System.ComponentModel.DataAnnotations;

namespace MLYSO.Web.Models
{
    public class RoutePlan
    {
        public int Id { get; set; }

        [MaxLength(40)]
        public string Code { get; set; } = $"R-{DateTime.UtcNow:yyyyMMddHHmmss}";

        [MaxLength(120)]
        public string Name { get; set; } = "Yeni Rota";

        [MaxLength(16)]
        public string VehiclePlate { get; set; } = string.Empty;

        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
        public string CreatedBy { get; set; } = string.Empty;

        ///  "Süre" | "Maliyet"
        [MaxLength(16)]
        public string Optimization { get; set; } = "Süre";

        public double TotalDistanceKm { get; set; }
        public double TotalEstimatedMinutes { get; set; }

        public List<RoutePlanStop> Stops { get; set; } = new();
    }

    public class RoutePlanStop
    {
        public int Id { get; set; }

        public int RoutePlanId { get; set; }
        public RoutePlan? RoutePlan { get; set; }

        /// Harici geocoding anahtarı 
        public string? GeoKey { get; set; }

        [MaxLength(120)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(240)]
        public string Address { get; set; } = string.Empty;

        public double Lat { get; set; }
        public double Lng { get; set; }

        /// plan üzerindeki sıra numarası 
        public int OrderNo { get; set; }

        public DateTime? Eta { get; set; }

        /// Plan/Atandı/Dağıtımda/Tamamlandı/İade
        [MaxLength(24)]
        public string Status { get; set; } = "Planned";   // <-- DbContext'teki 

        public bool Delivered { get; set; } = false;

        /// Not/yorum 
        public string? Note { get; set; }
    }
}
