using Microsoft.EntityFrameworkCore;
using MLYSO.Web.Models;              // AppDbContext
using MLYSO.Web.Models.Twin;

// Alias – isim çakışmalarını önler
using TwinContainerType = MLYSO.Web.Models.Twin.ContainerType;
using TwinBoxType = MLYSO.Web.Models.Twin.BoxType;
using TwinWarehouse = MLYSO.Web.Models.Twin.Warehouse;

namespace MLYSO.Web.Data;

public static class SeedTwin
{
    public static async Task EnsureAsync(AppDbContext db)
    {
        if (!await db.ContainerTypes.AnyAsync())
        {
            db.ContainerTypes.AddRange(
                new TwinContainerType { Code = "20GP", Name = "20' Standard", Mode = TransportMode.Sea, InnerL = 5898, InnerW = 2352, InnerH = 2393, MaxPayloadKg = 28200 },
                new TwinContainerType { Code = "40GP", Name = "40' Standard", Mode = TransportMode.Sea, InnerL = 12032, InnerW = 2352, InnerH = 2393, MaxPayloadKg = 28500 },
                new TwinContainerType { Code = "40HC", Name = "40' High Cube", Mode = TransportMode.Sea, InnerL = 12032, InnerW = 2352, InnerH = 2698, MaxPayloadKg = 28500 },
                new TwinContainerType { Code = "45HC", Name = "45' High Cube", Mode = TransportMode.Sea, InnerL = 13556, InnerW = 2352, InnerH = 2698, MaxPayloadKg = 27500 },

                new TwinContainerType { Code = "EURO-TR", Name = "Euro Trailer", Mode = TransportMode.Road, InnerL = 13620, InnerW = 2470, InnerH = 2690, MaxPayloadKg = 27000 },

                new TwinContainerType { Code = "RAIL-45", Name = "Rail 45'", Mode = TransportMode.Rail, InnerL = 13556, InnerW = 2352, InnerH = 2698, MaxPayloadKg = 28000 },
                new TwinContainerType { Code = "RAIL-53", Name = "Rail 53'", Mode = TransportMode.Rail, InnerL = 16002, InnerW = 2438, InnerH = 2692, MaxPayloadKg = 29000 },

                new TwinContainerType { Code = "AKE", Name = "ULD AKE (LD3)", Mode = TransportMode.Air, InnerL = 1534, InnerW = 1562, InnerH = 1625, MaxPayloadKg = 1580, IsULD = true },
                new TwinContainerType { Code = "PMC", Name = "ULD PMC (P6P)", Mode = TransportMode.Air, InnerL = 3175, InnerW = 2438, InnerH = 1600, MaxPayloadKg = 6800, IsULD = true }
            );
        }

        if (!await db.BoxTypes.AnyAsync())
        {
            db.BoxTypes.AddRange(
                new TwinBoxType { Code = "B-600x400x200", Name = "EuroMod 600x400x200", L = 600, W = 400, H = 200, AvgWeightKg = 7 },
                new TwinBoxType { Code = "B-600x400x300", Name = "EuroMod 600x400x300", L = 600, W = 400, H = 300, AvgWeightKg = 10 },
                new TwinBoxType { Code = "B-400x300x300", Name = "EuroMod 400x300x300", L = 400, W = 300, H = 300, AvgWeightKg = 6 },
                new TwinBoxType { Code = "B-300x200x200", Name = "EuroMod 300x200x200", L = 300, W = 200, H = 200, AvgWeightKg = 3 }
            );
        }

        if (!await db.Warehouses.AnyAsync())
        {
            db.Warehouses.Add(new TwinWarehouse
            {
                Name = "Dijital İkiz Depo (Sabit Alan)",
                LengthMm = 40000,
                WidthMm = 20000,
                HeightMm = 10000,
                Mode = TransportMode.Sea
            });
        }

        await db.SaveChangesAsync();
    }
}
