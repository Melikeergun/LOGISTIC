using Microsoft.EntityFrameworkCore;
using MLYSO.Web.Models;

namespace MLYSO.Web.Services;

public class ReportingService
{
    private readonly AppDbContext _db;
    public ReportingService(AppDbContext db) { _db = db; }

    public async Task<object> GetKpisAsync()
    {
        var now = DateTime.UtcNow;
        var d30 = now.AddDays(-30);

        // 30 gün sipariş adedi
        var total30 = await _db.OOrders.Where(o => o.PurchaseTs >= d30).CountAsync();

        // Toplam teslim edilen
        var deliveredTotal = await _db.OOrders.Where(o => o.DeliveredCustomerDate != null).CountAsync();

        // Ortalama teslimat süresi (gün) - SQLite güvenli: farkı bellek içinde al
        var pairs = await _db.OOrders
            .Where(o => o.DeliveredCustomerDate != null && o.PurchaseTs != null)
            .Select(o => new { o.PurchaseTs, o.DeliveredCustomerDate })
            .ToListAsync();
        var avgDeliveryDays = pairs.Count == 0 ? 0.0
            : Math.Round(pairs.Average(p => (p.DeliveredCustomerDate!.Value - p.PurchaseTs!.Value).TotalDays), 2);

        // Dinamik siparişlerden iade/değişim oranı
        var dynTotal = await _db.Orders.CountAsync();
        var dynReturns = await _db.Orders
            .Where(o => o.FieldsJson.Contains("\"return_request\":\"yes\"") ||
                        o.FieldsJson.Contains("\"exchange_request\":\"yes\""))
            .CountAsync();
        var dynReturnRate = dynTotal == 0 ? 0.0 : Math.Round(dynReturns * 100.0 / dynTotal, 2);

        // SLA: Tahmin edilen tarihe göre zamanında teslim oranı
        var slaPairs = await _db.OOrders
            .Where(o => o.EstimatedDeliveryDate != null && o.DeliveredCustomerDate != null)
            .Select(o => new { o.EstimatedDeliveryDate, o.DeliveredCustomerDate })
            .ToListAsync();
        var slaOnTimeRate = slaPairs.Count == 0 ? 0.0 :
            Math.Round(100.0 * slaPairs.Count(p => p.DeliveredCustomerDate! <= p.EstimatedDeliveryDate!) / slaPairs.Count, 2);

        // Açık şikayet sayısı (CRM)
        var openComplaints = await _db.Complaints.CountAsync(c => c.Status == "open");

        return new
        {
            orders_30d = total30,
            delivered_total = deliveredTotal,
            avg_delivery_days = avgDeliveryDays,
            dyn_return_rate = dynReturnRate,
            sla_on_time_rate = slaOnTimeRate,
            open_complaints = openComplaints
        };
    }

    public async Task<IEnumerable<object>> TrendLast7Async()
    {
        var d7 = DateTime.UtcNow.Date.AddDays(-6);
        var dates = await _db.OOrders
            .Where(o => o.PurchaseTs != null && o.PurchaseTs >= d7)
            .Select(o => o.PurchaseTs!.Value.Date)
            .ToListAsync();

        return dates.GroupBy(d => d)
            .OrderBy(g => g.Key)
            .Select(g => new { date = g.Key.ToString("yyyy-MM-dd"), orders = g.Count() });
    }

    public async Task<string> ExportKpiCsvAsync()
    {
        var k = await GetKpisAsync();
        var dict = k.GetType().GetProperties()
            .ToDictionary(p => p.Name, p => p.GetValue(k));

        var lines = new List<string> { "metric,value" };
        foreach (var kv in dict)
            lines.Add($"{kv.Key},{kv.Value}");

        return string.Join("\n", lines);
    }
}
