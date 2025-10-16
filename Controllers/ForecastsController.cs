using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MLYSO.Web.Data;
using MLYSO.Web.Models;
using MLYSO.Web.Services.Forecasting;

namespace MLYSO.Web.Controllers
{
    [Authorize]
    public sealed class ForecastsController : Controller
    {
        private readonly AppDbContext _db;
        private readonly Forecaster _fc = new();

        public ForecastsController(AppDbContext db) => _db = db;

        [HttpGet("/forecasts")]
        public async Task<IActionResult> Index()
        {
            await _db.Database.MigrateAsync();
            await SeedForecast.EnsureAsync(_db);

            ViewBag.Warehouses = await _db.Warehouses.AsNoTracking().ToListAsync();
            ViewBag.Skus = await _db.BoxTypes.AsNoTracking()
                               .OrderBy(x => x.Code)
                               .Select(x => x.Code).ToListAsync();
            return View();
        }

        public sealed record ForecastReq(
            int warehouseId,
            string skuCode,
            int horizon,
            string bucket,
            string model,   // "SES" | "HOLT" | "HW" | "AUTO"
            bool optimize,
            int seasonLen,
            bool conf,
            int backtest,
            double? alpha, double? beta, double? gamma);

        [IgnoreAntiforgeryToken]
        [HttpPost("/api/forecasts")]
        [Produces("application/json")]
        public async Task<IActionResult> Make([FromBody] ForecastReq req)
        {
            try
            {
                if (req is null) return BadRequest(new { error = "Boş istek." });
                if (string.IsNullOrWhiteSpace(req.skuCode)) return BadRequest(new { error = "SKU (kutu kodu) boş olamaz." });

                await _db.Database.MigrateAsync();
                await SeedForecast.EnsureAsync(_db);

                char b = (req.bucket?.ToUpperInvariant() ?? "W")[0];
                int horizon = req.horizon > 0 ? req.horizon : 8;
                int season = req.seasonLen > 1 ? req.seasonLen : 12;
                int back = Math.Max(0, req.backtest);

                var rows = await _db.DemandHistories.AsNoTracking()
                    .Where(x => x.WarehouseId == req.warehouseId && x.SkuCode == req.skuCode)
                    .OrderBy(x => x.Date)
                    .Select(x => new { x.Date, x.Quantity })
                    .ToListAsync();

                var (dates, y) = _fc.Bucketize(rows.Select(r => (r.Date, (double)r.Quantity)), b);

                // Model/parametre
                FcModel model = Enum.TryParse<FcModel>((req.model ?? "AUTO").ToUpperInvariant(), out var mm)
                                ? mm : FcModel.AUTO;
                double a = req.alpha ?? double.NaN, be = req.beta ?? double.NaN, ga = req.gamma ?? double.NaN;

                if (req.optimize)
                {
                    if (model == FcModel.SES) (a, _, _) = _fc.TuneSes(y);
                    else if (model == FcModel.HOLT) (a, be, _) = _fc.TuneHolt(y);
                    else if (model == FcModel.HW) (a, be, ga) = _fc.TuneHw(y, season);
                }

                double[] fc, fitted; double sigma;
                FcModel usedModel = model;

                if (model == FcModel.AUTO)
                {
                    var pick = _fc.AutoPick(y, horizon, season);
                    usedModel = pick.model; fc = pick.fc; fitted = pick.fitted; sigma = pick.sigma;
                }
                else if (model == FcModel.SES)
                {
                    var r = Forecaster.Ses(y, horizon, double.IsNaN(a) ? null : a);
                    fc = r.forecast; fitted = r.fitted; sigma = r.sigma;
                }
                else if (model == FcModel.HOLT)
                {
                    var r = Forecaster.Holt(y, horizon, double.IsNaN(a) ? null : a, double.IsNaN(be) ? null : be);
                    fc = r.forecast; fitted = r.fitted; sigma = r.sigma;
                }
                else // HW
                {
                    var r = Forecaster.HoltWintersAdd(
                        y, horizon, season,
                        double.IsNaN(a) ? null : a,
                        double.IsNaN(be) ? null : be,
                        double.IsNaN(ga) ? null : ga);
                    fc = r.forecast; fitted = r.fitted; sigma = r.sigma;
                }

                // Etiketler
                var labels = dates.Select(d =>
                        b == 'M' ? d.ToString("yyyy-MM")
                      : b == 'D' ? d.ToString("yyyy-MM-dd")
                                 : d.ToString("yyyy-'W'ww"))
                    .ToArray();

                // Geçmiş NULL + ileri tahmin
                var forecast = Enumerable.Repeat<double?>(null, y.Count)
                                         .Concat(fc.Select(v => (double?)v))
                                         .ToArray();

                // Güven aralığı
                double z = 1.96;
                double?[] ciLow = Enumerable.Repeat<double?>(null, y.Count).ToArray();
                double?[] ciHigh = Enumerable.Repeat<double?>(null, y.Count).ToArray();
                if (req.conf && sigma > 0)
                {
                    ciLow = ciLow.Concat(fc.Select(v => (double?)(v - z * sigma))).ToArray();
                    ciHigh = ciHigh.Concat(fc.Select(v => (double?)(v + z * sigma))).ToArray();
                }
                else
                {
                    ciLow = ciLow.Concat(Enumerable.Repeat<double?>(null, fc.Length)).ToArray();
                    ciHigh = ciHigh.Concat(Enumerable.Repeat<double?>(null, fc.Length)).ToArray();
                }

                // Eğitim metrikleri
                var (rmse, mape) = Forecaster.Metrics(y, fitted);

                // Backtest
                double? testRmse = null, testMape = null;
                if (back > 0 && y.Count > back + 3)
                {
                    var train = y.Take(y.Count - back).ToList();
                    var test = y.Skip(y.Count - back).ToList();

                    double[] fcTest =
                        usedModel == FcModel.SES ? Forecaster.Ses(train, back).forecast.ToArray() :
                        usedModel == FcModel.HOLT ? Forecaster.Holt(train, back).forecast.ToArray() :
                        Forecaster.HoltWintersAdd(train, back, season).forecast.ToArray();

                    var fit = new double[train.Count + back];
                    for (int i = 0; i < train.Count; i++) fit[i] = train[i];
                    for (int i = 0; i < back; i++) fit[train.Count + i] = fcTest[i];

                    var (rm, mp) = Forecaster.Metrics(train.Concat(test).ToList(), fit);
                    testRmse = rm; testMape = mp;
                }

                var stats = new
                {
                    model = usedModel.ToString(),
                    points = y.Count,
                    horizon,
                    alpha = double.IsNaN(a) ? (double?)null : a,
                    beta = double.IsNaN(be) ? (double?)null : be,
                    gamma = double.IsNaN(ga) ? (double?)null : ga,
                    sum = y.Count > 0 ? y.Sum() : 0,
                    avg = y.Count > 0 ? y.Average() : 0,
                    min = y.Count > 0 ? y.Min() : 0,
                    max = y.Count > 0 ? y.Max() : 0,
                    lastActual = y.Count > 0 ? y[^1] : 0,
                    nextForecast = fc.Length > 0 ? fc[0] : 0,
                    growthPct = (y.Count > 0 && Math.Abs(y[^1]) > 1e-9) ? (fc[0] - y[^1]) / y[^1] * 100.0 : 0,
                    rmse,
                    mape,
                    testRmse,
                    testMape
                };

                return Ok(new
                {
                    labels,
                    actual = y,
                    forecast,
                    ciLow,
                    ciHigh,
                    stats
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("/api/forecasts/export")]
        public async Task<IActionResult> Export(int warehouseId, string skuCode, string bucket = "W")
        {
            char b = (bucket?.ToUpperInvariant() ?? "W")[0];
            var rows = await _db.DemandHistories.AsNoTracking()
                .Where(x => x.WarehouseId == warehouseId && x.SkuCode == skuCode)
                .OrderBy(x => x.Date)
                .Select(x => new { x.Date, x.Quantity })
                .ToListAsync();

            var (dates, y) = _fc.Bucketize(rows.Select(r => (r.Date, (double)r.Quantity)), b);
            var labels = dates.Select(d => b == 'M'
                    ? d.ToString("yyyy-MM")
                    : (b == 'D' ? d.ToString("yyyy-MM-dd") : d.ToString("yyyy-'W'ww")))
                .ToArray();

            var sb = new StringBuilder();
            sb.AppendLine("label,actual");
            for (int i = 0; i < y.Count; i++) sb.AppendLine($"{labels[i]},{y[i]}");

            return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", $"forecast_{skuCode}.csv");
        }
    }
}
