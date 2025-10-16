using System;
using System.Collections.Generic;
using System.Linq;

namespace MLYSO.Web.Services.Forecasting
{
    public enum FcModel { SES, HOLT, HW, AUTO }

    public sealed class Forecaster
    {
        /* ================== Yardımcılar / metrikler ================== */

        public static (double rmse, double mape) Metrics(IReadOnlyList<double> y, IReadOnlyList<double> fitted)
        {
            int n = Math.Min(y.Count, fitted.Count);
            if (n <= 1) return (0, 0);

            double se = 0, ape = 0; int k = 0;
            for (int i = 1; i < n; i++)
            {
                double e = y[i] - fitted[i];
                se += e * e;
                if (Math.Abs(y[i]) > 1e-9) ape += Math.Abs(e) / Math.Abs(y[i]);
                k++;
            }
            return (Math.Sqrt(se / k), (ape / k) * 100.0);
        }

        public static double AutoAlpha(IReadOnlyList<double> y)
        {
            if (y == null || y.Count == 0) return 0.3;

            double bestA = 0.2, bestErr = double.MaxValue;
            for (double a = 0.1; a <= 0.9; a += 0.05)
            {
                var (_, fitted, _) = Ses(y, 1, a);
                var (rmse, _) = Metrics(y, fitted);
                if (rmse < bestErr) { bestErr = rmse; bestA = a; }
            }
            return bestA;
        }

        /* ================== Modeller ================== */

        // Simple Exponential Smoothing
        public static (double[] forecast, double[] fitted, double sigma)
            Ses(IReadOnlyList<double> y, int horizon, double? alpha = null)
        {
            if (y == null || y.Count == 0)
                return (Enumerable.Repeat(0d, Math.Max(1, horizon)).ToArray(), Array.Empty<double>(), 0);

            double a = alpha ?? AutoAlpha(y);
            double level = y[0];

            var fitted = new double[y.Count];
            fitted[0] = y[0];

            for (int t = 1; t < y.Count; t++)
            {
                double pred = level;   // t için 1-adım
                fitted[t] = pred;
                level = a * y[t] + (1 - a) * level;
            }

            var res = y.Skip(1).Zip(fitted.Skip(1), (yy, ff) => yy - ff).ToArray();
            double sigma = res.Length > 1 ? Math.Sqrt(res.Sum(v => v * v) / res.Length) : 0;

            var fc = Enumerable.Repeat(level, Math.Max(1, horizon)).ToArray();
            return (fc, fitted, sigma);
        }

        // Holt (trend)
        public static (double[] forecast, double[] fitted, double sigma)
            Holt(IReadOnlyList<double> y, int horizon, double? alpha = null, double? beta = null)
        {
            if (y == null || y.Count == 0)
                return (Enumerable.Repeat(0d, Math.Max(1, horizon)).ToArray(), Array.Empty<double>(), 0);

            double a = alpha ?? 0.3;
            double b = beta ?? 0.1;

            double level = y[0];
            double trend = y.Count > 1 ? y[1] - y[0] : 0;

            var fitted = new double[y.Count];
            fitted[0] = y[0];

            for (int t = 1; t < y.Count; t++)
            {
                double pred = level + trend;
                fitted[t] = pred;

                double lastLevel = level;
                level = a * y[t] + (1 - a) * (level + trend);
                trend = b * (level - lastLevel) + (1 - b) * trend;
            }

            var res = y.Skip(1).Zip(fitted.Skip(1), (yy, ff) => yy - ff).ToArray();
            double sigma = res.Length > 1 ? Math.Sqrt(res.Sum(v => v * v) / res.Length) : 0;

            var fc = new double[Math.Max(1, horizon)];
            for (int k = 1; k <= fc.Length; k++)
                fc[k - 1] = level + k * trend;

            return (fc, fitted, sigma);
        }

        // Holt-Winters (additive, mevsimsel)
        public static (double[] forecast, double[] fitted, double sigma)
            HoltWintersAdd(IReadOnlyList<double> y, int horizon, int m,
                           double? alpha = null, double? beta = null, double? gamma = null)
        {
            // y yetersizse Holt'a düş (NULL güvenli)
            if (y == null || y.Count < m + 2)
                return Holt(y ?? Array.Empty<double>(), horizon, alpha, beta);

            double a = alpha ?? 0.3;
            double b = beta ?? 0.1;
            double g = gamma ?? 0.3;
            int n = y.Count;

            // Başlangıç mevsimsellik
            var season = new double[m];
            double sAvg = y.Take(m).Average();
            for (int i = 0; i < m; i++) season[i] = y[i] - sAvg;

            double level = sAvg;
            double trend = (y.Skip(m).Take(m).Average() - sAvg) / m;

            var fitted = new double[n];
            fitted[0] = y[0];

            for (int t = 1; t < n; t++)
            {
                double s = season[t % m];
                double pred = level + trend + s;
                fitted[t] = pred;

                double lastLevel = level;
                level = a * (y[t] - s) + (1 - a) * (level + trend);
                trend = b * (level - lastLevel) + (1 - b) * trend;
                season[t % m] = g * (y[t] - level) + (1 - g) * s;
            }

            var res = y.Skip(1).Zip(fitted.Skip(1), (yy, ff) => yy - ff).ToArray();
            double sigma = res.Length > 1 ? Math.Sqrt(res.Sum(v => v * v) / res.Length) : 0;

            var fc = new double[Math.Max(1, horizon)];
            for (int k = 1; k <= fc.Length; k++)
                fc[k - 1] = level + k * trend + season[(n + k - 1) % m];

            return (fc, fitted, sigma);
        }

        /* ============== Model seçimi ve küçük optimizasyon ============== */

        public (FcModel model, double[] fc, double[] fitted, double sigma)
            AutoPick(IReadOnlyList<double> y, int horizon, int m)
        {
            var cands = new List<(FcModel, double[] fc, double[] fitted, double sigma, double score)>();

            var s1 = Ses(y, horizon);
            cands.Add((FcModel.SES, s1.forecast, s1.fitted, s1.sigma, Metrics(y, s1.fitted).mape));

            var h1 = Holt(y, horizon);
            cands.Add((FcModel.HOLT, h1.forecast, h1.fitted, h1.sigma, Metrics(y, h1.fitted).mape));

            if (y != null && y.Count >= m + 2)
            {
                var hw = HoltWintersAdd(y, horizon, m);
                cands.Add((FcModel.HW, hw.forecast, hw.fitted, hw.sigma, Metrics(y, hw.fitted).mape));
            }

            var best = cands.OrderBy(x => x.score).First();
            return (best.Item1, best.fc, best.fitted, best.sigma);
        }

        public (double a, double b, double g) TuneSes(IReadOnlyList<double> y)
        {
            double bestA = 0.2, best = double.MaxValue;
            for (double a = 0.1; a <= 0.9; a += 0.05)
            {
                var (_, fitted, _) = Ses(y, 1, a);
                var (rmse, _) = Metrics(y, fitted);
                if (rmse < best) { best = rmse; bestA = a; }
            }
            return (bestA, 0, 0);
        }

        public (double a, double b, double g) TuneHolt(IReadOnlyList<double> y)
        {
            double ta = 0.3, tb = 0.1, best = double.MaxValue;
            for (double a = 0.1; a <= 0.9; a += 0.2)
                for (double b = 0.05; b <= 0.5; b += 0.05)
                {
                    var (_, fitted, _) = Holt(y, 1, a, b);
                    var (rmse, _) = Metrics(y, fitted);
                    if (rmse < best) { best = rmse; ta = a; tb = b; }
                }
            return (ta, tb, 0);
        }

        public (double a, double b, double g) TuneHw(IReadOnlyList<double> y, int m)
        {
            double ta = 0.3, tb = 0.1, tg = 0.3, best = double.MaxValue;
            for (double a = 0.1; a <= 0.9; a += 0.2)
                for (double b = 0.05; b <= 0.5; b += 0.1)
                    for (double g = 0.1; g <= 0.9; g += 0.2)
                    {
                        var (_, fitted, _) = HoltWintersAdd(y, 1, m, a, b, g);
                        var (rmse, _) = Metrics(y, fitted);
                        if (rmse < best) { best = rmse; ta = a; tb = b; tg = g; }
                    }
            return (ta, tb, tg);
        }

        /* ============== Kovaya toplama (eksik gün/hafta/ayları 0 ile doldur) ============== */

        // Instance metod – Controller “_fc.Bucketize(...)” diye çağırır
        public (List<DateTime> dates, List<double> values) Bucketize(IEnumerable<(DateTime date, double qty)> raw, char bucket)
        {
            var ordered = raw.OrderBy(x => x.date).ToList();
            if (!ordered.Any()) return (new List<DateTime>(), new List<double>());

            DateTime Step(DateTime d) => bucket switch
            {
                'D' => d.AddDays(1),
                'M' => d.AddMonths(1),
                _ => d.AddDays(7) // 'W'
            };

            var grouped = ordered
                .GroupBy(x => bucket == 'M'
                                ? new DateTime(x.date.Year, x.date.Month, 1)
                                : (bucket == 'D' ? x.date.Date
                                                 : x.date.Date.AddDays(-(int)x.date.DayOfWeek)))
                .ToDictionary(g => g.Key, g => g.Sum(z => z.qty));

            var start = grouped.Keys.Min();
            var end = grouped.Keys.Max();
            var dates = new List<DateTime>();
            var vals = new List<double>();

            for (var d = start; d <= end; d = Step(d))
            {
                dates.Add(d);
                vals.Add(grouped.TryGetValue(d, out var v) ? v : 0);
            }
            return (dates, vals);
        }
    }
}
