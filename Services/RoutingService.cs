using System;
using System.Collections.Generic;
using System.Linq;
using MLYSO.Web.Models;

namespace MLYSO.Web.Services
{
    public class RoutingService
    {
        private static double ToRad(double deg) => deg * Math.PI / 180.0;

        public static double HaversineKm(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371.0;
            var dLat = ToRad(lat2 - lat1);
            var dLon = ToRad(lon2 - lon1);
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c;
        }

        public static List<RoutePlanStop> OptimizeSingle(List<RoutePlanStop> stops)
        {
            if (stops == null || stops.Count == 0) return new();
            if (stops.Count <= 2)
            {
                for (int i = 0; i < stops.Count; i++) stops[i].OrderNo = i;
                return stops.ToList();
            }

            var remaining = stops.ToList();
            var route = new List<RoutePlanStop>();
            var current = remaining[0];
            route.Add(current);
            remaining.RemoveAt(0);

            while (remaining.Count > 0)
            {
                var next = remaining
                    .OrderBy(s => HaversineKm(current.Lat, current.Lng, s.Lat, s.Lng))
                    .First();
                route.Add(next);
                remaining.Remove(next);
                current = next;
            }

            bool improved;
            do
            {
                improved = false;
                for (int i = 1; i < route.Count - 2; i++)
                {
                    for (int k = i + 1; k < route.Count - 1; k++)
                    {
                        var a = route[i - 1]; var b = route[i];
                        var c = route[k]; var d = route[k + 1];

                        var before = HaversineKm(a.Lat, a.Lng, b.Lat, b.Lng)
                                   + HaversineKm(c.Lat, c.Lng, d.Lat, d.Lng);
                        var after = HaversineKm(a.Lat, a.Lng, c.Lat, c.Lng)
                                   + HaversineKm(b.Lat, b.Lng, d.Lat, d.Lng);

                        if (after + 1e-6 < before)
                        {
                            route.Reverse(i, k - i + 1);
                            improved = true;
                        }
                    }
                }
            } while (improved);

            for (int i = 0; i < route.Count; i++) route[i].OrderNo = i;
            return route;
        }

        public static (double km, double minutes) Totals(List<RoutePlanStop> route)
        {
            double km = 0;
            for (int i = 0; i < (route?.Count ?? 0) - 1; i++)
                km += HaversineKm(route[i].Lat, route[i].Lng, route[i + 1].Lat, route[i + 1].Lng);

            var minutes = (km / 35.0) * 60.0;
            return (Math.Round(km, 2), Math.Round(minutes, 1));
        }
    }
}
