using System;
using System.ComponentModel.DataAnnotations;

namespace MLYSO.Web.Models.Twin
{
    public sealed class DemandHistory
    {
        public int Id { get; set; }

        [Required] public int WarehouseId { get; set; }

        // İstersen BoxTypeId da tutabilirsin; basitlik için SKU kodu kullanıyoruz
        [Required, MaxLength(64)]
        public string SkuCode { get; set; } = "";

        // Dönem tarihi (gün/hafta/ay başı olacak)
        public DateTime Date { get; set; }

        // O dönemdeki toplam adet
        public int Quantity { get; set; }
    }
}
