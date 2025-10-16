using MLYSO.Web.Models.Twin;

namespace MLYSO.Web.Services.Twin;

public sealed class BoxPacker3D
{
    public sealed record BoxSpec(BoxType Type, int Qty);

    public sealed record Placement(
        int X, int Y, int Z,
        int RX, int RY, int RZ,
        BoxType Type);

    // --- KAMU API ---
    public (List<Placement> placed, int usedVolume, double usedWeight) Pack(
        int innerL, int innerW, int innerH,
        IEnumerable<BoxSpec> boxes,
        double maxPayloadKg)
    {
        // Havuzu tekil kutu öğelerine aç (sadelik için)
        var pool = new List<BoxType>();
        foreach (var b in boxes)
            for (int i = 0; i < b.Qty; i++)
                pool.Add(b.Type);

        // Büyük taban alandan küçüğe sırala (katman verimini artırır)
        pool = pool.OrderByDescending(t => t.L * t.W)
                   .ThenByDescending(t => t.H).ToList();

        var placed = new List<Placement>();
        int usedVol = 0;
        double usedW = 0;

        int currentZ = 0;
        while (pool.Count > 0 && currentZ < innerH)
        {
            int layerFreeH = innerH - currentZ;
            if (layerFreeH <= 0) break;

            // 2D guillotine için boş dikdörtgen havuzu
            var free = new List<Rect2D> { new Rect2D(0, 0, innerL, innerW) };
            int layerHeight = 0;

            // Bu katmanda en az bir kutu yerleştirene kadar döneriz
            bool anyInLayer = false;

            // Havuz üzerinde ilerleyebilmek için indeks tabanlı döngü
            for (int pi = 0; pi < pool.Count;)
            {
                var box = pool[pi];

                // Ağırlık limiti
                if (usedW + box.AvgWeightKg > maxPayloadKg)
                {
                    // Daha hafif bir kutu bulma şansı için sıradakine geç
                    pi++;
                    continue;
                }

                // İzinli tüm oryantasyonları üret
                var orients = GetOrientations(box);
                (int x, int y, int rx, int ry, int rz, int l, int w, int h)? best = null;

                // Boş dikdörtgenlerde uygun yer ara
                foreach (var r in free)
                {
                    foreach (var o in orients)
                    {
                        if (o.h > layerFreeH) continue;
                        if (o.l <= r.W && o.w <= r.H)
                        {
                            // Basit seçim: y küçük -> x küçük (soldan-sağdan, önden-arkaya)
                            if (best is null || r.Y < best.Value.y || (r.Y == best.Value.y && r.X < best.Value.x))
                                best = (r.X, r.Y, o.rx, o.ry, o.rz, o.l, o.w, o.h);
                        }
                    }
                }

                if (best is null)
                {
                    // Bu kutu sığmadı; sonraki kutuyu dene
                    pi++;
                    continue;
                }

                // Yerleştir
                var bpos = best.Value;
                placed.Add(new Placement(
                    bpos.x, bpos.y, currentZ,
                    bpos.rx, bpos.ry, bpos.rz,
                    box));

                usedVol += bpos.l * bpos.w * bpos.h;
                usedW += box.AvgWeightKg;
                layerHeight = Math.Max(layerHeight, bpos.h);
                anyInLayer = true;

                // Guillotine böl: kullanılan free rect’i sağ ve üst parçalarına ayır
                SplitFreeRects(free, new Rect2D(bpos.x, bpos.y, bpos.l, bpos.w));

                // Havuzdan bu kutuyu çıkar (yerleştirildi)
                pool.RemoveAt(pi);
            }

            if (!anyInLayer) break; // bu katmanda hiç yerleştiremedik -> bitir
            currentZ += layerHeight;
        }

        return (placed, usedVol, usedW);
    }

    // --- 2D Guillotine yardımcıları ---
    private sealed record Rect2D(int X, int Y, int W, int H);

    private static void SplitFreeRects(List<Rect2D> free, Rect2D used)
    {
        for (int i = 0; i < free.Count; i++)
        {
            var r = free[i];
            if (!Intersects(r, used)) continue;

            // r içinden used’ü çıkar -> sağ ve üst parçalar
            var rightW = r.W - (used.X - r.X) - used.W;
            var topH = r.H - (used.Y - r.Y) - used.H;

            var right = new Rect2D(used.X + used.W, r.Y, Math.Max(0, rightW), used.Y - r.Y + used.H);
            var top = new Rect2D(r.X, used.Y + used.H, r.W, Math.Max(0, topH));

            // solda kalan parça
            var leftW = used.X - r.X;
            var left = new Rect2D(r.X, r.Y, Math.Max(0, leftW), r.H);

            // alt parça
            var bottomH = used.Y - r.Y;
            var bottom = new Rect2D(r.X, r.Y, r.W, Math.Max(0, bottomH));

            // r’yi kaldır, yeni parçaları ekle
            free.RemoveAt(i);
            var candidates = new[] { right, top, left, bottom }
                .Where(q => q.W > 0 && q.H > 0).ToList();
            free.InsertRange(i, candidates);

            // birleştirme (komşu ve aynı yükseklikte/genişlikte)
            MergeFreeRects(free);
            i = -1; // baştan tara
        }
    }

    private static bool Intersects(Rect2D a, Rect2D b)
        => !(b.X >= a.X + a.W || b.X + b.W <= a.X || b.Y >= a.Y + a.H || b.Y + b.H <= a.Y);

    private static void MergeFreeRects(List<Rect2D> free)
    {
        for (int i = 0; i < free.Count; i++)
        {
            for (int j = i + 1; j < free.Count; j++)
            {
                var a = free[i]; var b = free[j];

                // Yatay bitişik ve aynı yükseklikte
                if (a.Y == b.Y && a.H == b.H && (a.X + a.W == b.X || b.X + b.W == a.X))
                {
                    var merged = new Rect2D(Math.Min(a.X, b.X), a.Y, a.W + b.W, a.H);
                    free[i] = merged; free.RemoveAt(j); j--; continue;
                }
                // Düşey bitişik ve aynı genişlikte
                if (a.X == b.X && a.W == b.W && (a.Y + a.H == b.Y || b.Y + b.H == a.Y))
                {
                    var merged = new Rect2D(a.X, Math.Min(a.Y, b.Y), a.W, a.H + b.H);
                    free[i] = merged; free.RemoveAt(j); j--; continue;
                }
            }
        }
    }

    // --- Oryantasyon üretimi (rotasyon izinlerine saygılı) ---
    private static IEnumerable<(int l, int w, int h, int rx, int ry, int rz)> GetOrientations(BoxType t)
    {
        // (rx,ry,rz) 0/90 olarak düşünülür
        var baseList = new List<(int l, int w, int h, int rx, int ry, int rz)>
        {
            (t.L, t.W, t.H, 0, 0, 0),          // L W H
            (t.W, t.L, t.H, 0, 0, 90),         // Z etrafında 90°
            (t.H, t.W, t.L, 90,0, 0),          // X etrafında 90°
            (t.W, t.H, t.L, 90,0,90),
            (t.L, t.H, t.W, 0, 90,0),          // Y etrafında 90°
            (t.H, t.L, t.W, 0, 90,90)
        };

        foreach (var o in baseList)
        {
            bool ok =
                ((o.rx == 0) || t.AllowRotateX) &&
                ((o.ry == 0) || t.AllowRotateY) &&
                ((o.rz == 0) || t.AllowRotateZ);
            if (ok) yield return o;
        }
    }
}
