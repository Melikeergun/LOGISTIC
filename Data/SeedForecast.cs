using System;
using System.Linq;
using System.Threading.Tasks;
using MLYSO.Web.Models;
using MLYSO.Web.Models.Twin;

namespace MLYSO.Web.Data
{
    public static class SeedForecast
    {
        public static async Task EnsureAsync(AppDbContext db)
        {
            if (db.DemandHistories.Any()) return;

            var wh = db.Warehouses.FirstOrDefault();
            var skuList = db.BoxTypes.Take(3).Select(b => b.Code).ToArray();
            if (wh == null || skuList.Length == 0) return;

            var rnd = new Random(42);
            var start = DateTime.UtcNow.Date.AddDays(-7 * 52); // 52 hafta önce

            foreach (var sku in skuList)
            {
                var date = start;
                int baseLevel = rnd.Next(60, 120);
                for (int i = 0; i < 52; i++)
                {
                    int qty = Math.Max(0, baseLevel + i / 3 + rnd.Next(-15, 16));
                    db.DemandHistories.Add(new DemandHistory
                    {
                        WarehouseId = wh.Id,
                        SkuCode = sku,
                        Date = date,
                        Quantity = qty
                    });
                    date = date.AddDays(7);
                }
            }
            await db.SaveChangesAsync();
        }
    }
}
