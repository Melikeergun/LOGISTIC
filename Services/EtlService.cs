using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MLYSO.Web.Models;
using System.Globalization;
using System.Text;

namespace MLYSO.Web.Services
{
    public class EtlService
    {
        private readonly AppDbContext _db;
        private readonly ILogger<EtlService> _log;
        private readonly IWebHostEnvironment _env;
        public DateTime? LastRunUtc { get; private set; }

        public EtlService(AppDbContext db, ILogger<EtlService> log, IWebHostEnvironment env)
        { _db = db; _log = log; _env = env; }

        public async Task<(bool ok, string message)> RunAsync()
        {
            try
            {
                string dir = Path.Combine(_env.ContentRootPath, "App_Data", "kaggle", "olist");
                string ordersCsv = Path.Combine(dir, "olist_orders_dataset.csv");
                string itemsCsv = Path.Combine(dir, "olist_order_items_dataset.csv");
                string payCsv = Path.Combine(dir, "olist_order_payments_dataset.csv");
                string custCsv = Path.Combine(dir, "olist_customers_dataset.csv");

                if (!File.Exists(ordersCsv))
                    return (false, $"Bulunamadı: {ordersCsv}");

                var existing = new HashSet<string>(
                    await _db.OOrders.AsNoTracking()
                        .Select(x => x.OrderId)
                        .Where(x => x != null)
                        .Cast<string>()
                        .ToListAsync()
                );

                int insOrders = 0;

                foreach (var r in ReadCsv(ordersCsv))
                {
                    string oid = Get(r, "order_id");
                    if (string.IsNullOrWhiteSpace(oid) || existing.Contains(oid)) continue;

                    _db.OOrders.Add(new OOrder
                    {
                        OrderId = oid,
                        CustomerId = Get(r, "customer_id"),
                        Status = Get(r, "order_status"),
                        PurchaseTs = ParseDt(Get(r, "order_purchase_timestamp")),
                        ApprovedAt = ParseDt(Get(r, "order_approved_at")),
                        DeliveredCarrierDate = ParseDt(Get(r, "order_delivered_carrier_date")),
                        DeliveredCustomerDate = ParseDt(Get(r, "order_delivered_customer_date")),
                        EstimatedDeliveryDate = ParseDt(Get(r, "order_estimated_delivery_date"))
                    });
                    existing.Add(oid);
                    insOrders++;
                }
                await _db.SaveChangesAsync();

                if (File.Exists(itemsCsv))
                {
                    int batch = 0;
                    foreach (var r in ReadCsv(itemsCsv))
                    {
                        var orderId = Get(r, "order_id");
                        if (string.IsNullOrWhiteSpace(orderId)) continue; // (2) koruma

                        _db.OOrderItems.Add(new OOrderItem
                        {
                            OrderId = orderId,
                            ItemSeq = ParseInt(Get(r, "order_item_id")),
                            ProductId = Get(r, "product_id"),
                            SellerId = Get(r, "seller_id"),
                            ProductCategory = Get(r, "product_category_name"),
                            Price = ParseDec(Get(r, "price")),
                            Freight = ParseDec(Get(r, "freight_value"))
                        });
                        if (++batch % 5000 == 0) await _db.SaveChangesAsync();
                    }
                    await _db.SaveChangesAsync();
                }

                if (File.Exists(payCsv))
                {
                    int batch = 0;
                    foreach (var r in ReadCsv(payCsv))
                    {
                        var orderId = Get(r, "order_id");
                        if (string.IsNullOrWhiteSpace(orderId)) continue; // (2) koruma

                        _db.OPayments.Add(new OPayment
                        {
                            OrderId = orderId,
                            Seq = ParseInt(Get(r, "payment_sequential")),
                            PaymentType = Get(r, "payment_type"),
                            Value = ParseDec(Get(r, "payment_value"))
                        });
                        if (++batch % 5000 == 0) await _db.SaveChangesAsync();
                    }
                    await _db.SaveChangesAsync();
                }

                if (File.Exists(custCsv))
                {
                    int ins = 0;
                    foreach (var r in ReadCsv(custCsv))
                    {
                        var cid = Get(r, "customer_id");
                        if (string.IsNullOrWhiteSpace(cid)) continue;
                        if (await _db.OCustomers.AnyAsync(x => x.CustomerId == cid)) continue;

                        _db.OCustomers.Add(new OCustomer
                        {
                            CustomerId = cid,
                            ZipCode = Get(r, "customer_zip_code_prefix"),
                            City = Get(r, "customer_city"),
                            State = Get(r, "customer_state")
                        });
                        if (++ins % 5000 == 0) await _db.SaveChangesAsync();
                    }
                    await _db.SaveChangesAsync();
                }
                else
                {
                    var ids = await _db.OOrders.AsNoTracking()
                        .Select(o => o.CustomerId)
                        .Where(cid => !string.IsNullOrWhiteSpace(cid))
                        .Distinct()
                        .ToListAsync();

                    var existingCust = new HashSet<string>(
                        await _db.OCustomers.AsNoTracking().Select(c => c.CustomerId).ToListAsync());

                    int created = 0;
                    foreach (var cid in ids)
                        if (!existingCust.Contains(cid!))
                        {
                            _db.OCustomers.Add(new OCustomer { CustomerId = cid! });
                            if (++created % 5000 == 0) await _db.SaveChangesAsync();
                        }
                    if (created > 0) await _db.SaveChangesAsync();
                }

                LastRunUtc = DateTime.UtcNow;
                return (true, $"ETL OK. Yeni sipariş: {insOrders}");
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "ETL failed");
                return (false, ex.Message);
            }
        }

        private static string Get(Dictionary<string, string> row, string key)
            => row.TryGetValue(key, out var v) ? v : "";

        private static DateTime? ParseDt(string s)
            => DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var d) ? d : null;

        private static int ParseInt(string s)
            => int.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var i) ? i : 0;

        private static decimal ParseDec(string s)
            => decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var d) ? d : 0m;

        private static IEnumerable<Dictionary<string, string>> ReadCsv(string path)
        {
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var sr = new StreamReader(fs, Encoding.UTF8, true);
            string? header = sr.ReadLine();
            if (header == null) yield break;
            var cols = SplitCsvLine(header);
            string? line;
            while ((line = sr.ReadLine()) != null)
            {
                var vals = SplitCsvLine(line);
                var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < cols.Count; i++)
                    row[cols[i]] = i < vals.Count ? vals[i] : "";
                yield return row;
            }
        }

        private static List<string> SplitCsvLine(string line)
        {
            var list = new List<string>();
            var sb = new StringBuilder();
            bool inQ = false;
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '\"')
                {
                    if (inQ && i + 1 < line.Length && line[i + 1] == '\"') { sb.Append('\"'); i++; }
                    else inQ = !inQ;
                }
                else if (c == ',' && !inQ)
                { list.Add(sb.ToString()); sb.Clear(); }
                else sb.Append(c);
            }
            list.Add(sb.ToString());
            return list;
        }
    }

    public class EtlSchedulerService : BackgroundService
    {
        private readonly IServiceProvider _sp;
        private readonly ILogger<EtlSchedulerService> _log;
        public EtlSchedulerService(IServiceProvider sp, ILogger<EtlSchedulerService> log)
        { _sp = sp; _log = log; }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var now = DateTime.UtcNow;
                var next = new DateTime(now.Year, now.Month, now.Day, 3, 0, 0, DateTimeKind.Utc);
                if (now >= next) next = next.AddDays(1);
                await Task.Delay(next - now, stoppingToken);

                using var scope = _sp.CreateScope();
                var etl = scope.ServiceProvider.GetRequiredService<EtlService>();
                var (ok, msg) = await etl.RunAsync();
                _log.LogInformation("Nightly ETL: {Ok} - {Msg}", ok, msg);
            }
        }
    }
}
