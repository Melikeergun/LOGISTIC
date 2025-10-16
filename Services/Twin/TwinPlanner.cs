using Microsoft.EntityFrameworkCore;
using MLYSO.Web.Models;
using MLYSO.Web.Models.Twin;

namespace MLYSO.Web.Services.Twin;

public sealed class TwinPlanner
{
    private readonly AppDbContext _db;
    public TwinPlanner(AppDbContext db) => _db = db;

    public async Task<PackingPlan> PlanAsync(int jobId)
    {
        var job = await _db.PackingJobs
            .Include(j => j.Warehouse)
            .Include(j => j.Items).ThenInclude(i => i.BoxType)
            .Include(j => j.Containers).ThenInclude(c => c.ContainerType)
            .SingleAsync(j => j.Id == jobId);

        // 1) Kutu havuzu (kalan adetler)
        var remaining = job.Items.ToDictionary(i => i.BoxTypeId, i => i.Quantity);

        // 2) Konteynerleri adet kadar çoğalt
        var containerTypes = job.Containers
            .SelectMany(c => Enumerable.Range(0, c.Quantity).Select(_ => c.ContainerType))
            .ToList();

        // 2D yerleşim (depo zemini)
        var floor = new FloorPacker(job.Warehouse.WidthMm, job.Warehouse.LengthMm);
        var placedRects = floor.Pack(containerTypes);

        // 3D plan
        var plan = new PackingPlan { PackingJobId = jobId };
        int totalInnerVol = 0, usedVolSum = 0;
        double totalMaxPayload = 0, usedWeightSum = 0;

        foreach (var pr in placedRects)
        {
            var wp = new WarehousePlacement
            {
                ContainerTypeId = pr.C.Id,
                X = pr.X,
                Y = pr.Y,
                RotDeg = pr.RotDeg
            };

            // 3) Bu konteyner için "kalan" kutulardan spec oluştur
            var specs = job.Items
                .Select(i => new BoxPacker3D.BoxSpec(
                    i.BoxType,
                    remaining.TryGetValue(i.BoxTypeId, out var q) ? q : 0))
                .Where(s => s.Qty > 0)
                .ToList();

            if (specs.Count > 0)
            {
                var packer = new BoxPacker3D();
                var (placed, usedVol, usedW) = packer.Pack(
                    pr.C.InnerL, pr.C.InnerW, pr.C.InnerH,
                    specs, pr.C.MaxPayloadKg);

                // 4) Yerleşen kutuları yaz
                foreach (var p in placed)
                {
                    wp.BoxPlacements.Add(new BoxPlacement
                    {
                        BoxTypeId = p.Type.Id,
                        X = p.X,
                        Y = p.Y,
                        Z = p.Z,
                        RotX = p.RX,
                        RotY = p.RY,
                        RotZ = p.RZ
                    });
                }

                // 5) Havuzdan tüket
                foreach (var g in placed.GroupBy(x => x.Type.Id))
                {
                    if (remaining.ContainsKey(g.Key))
                        remaining[g.Key] = Math.Max(0, remaining[g.Key] - g.Count());
                }

                usedVolSum += usedVol;
                usedWeightSum += usedW;
            }

            plan.WarehousePlacements.Add(wp);
            totalInnerVol += pr.C.InnerL * pr.C.InnerW * pr.C.InnerH;
            totalMaxPayload += pr.C.MaxPayloadKg;
        }

        plan.ContainersUsed = plan.WarehousePlacements.Count;
        plan.VolumeUtilizationPct = totalInnerVol == 0 ? 0 : (100.0 * usedVolSum / totalInnerVol);
        plan.WeightUtilizationPct = totalMaxPayload == 0 ? 0 : (100.0 * usedWeightSum / totalMaxPayload);

        _db.PackingPlans.Add(plan);
        await _db.SaveChangesAsync();
        return plan;
    }
}
