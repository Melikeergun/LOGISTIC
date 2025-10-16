using System.Linq;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using MLYSO.Web.Models;

// --- ÇAKIŞMAYI ÇÖZEN ALIAS'LAR ---
using RouteEntity = MLYSO.Web.Models.Route;
using RouteStopEntity = MLYSO.Web.Models.RouteStop;

namespace MLYSO.Web.Data;

public static class Seed
{
    public static async Task EnsureAsync(AppDbContext db)
    {
        await db.Database.MigrateAsync();
        await EnsureUsersAsync(db);

        // 3) Basit örnek "Route" + "RouteStop"
        if (!await db.Routes.AnyAsync())
        {
            var driverId = await db.Users
                .Where(u => u.Role == Roles.Driver)
                .Select(u => u.Id)
                .FirstOrDefaultAsync();

            var r = new RouteEntity
            {
                DriverUserId = driverId,
                RouteDate = DateTime.UtcNow.Date,
                Vehicle = "34 ABC 123",
                Status = "Planned",
                Stops = Enumerable.Range(1, 5).Select(i => new RouteStopEntity
                {
                    StopNo = i,
                    Customer = $"Müşteri {i}",
                    Address = $"Adres {i}, İstanbul",
                    OrderId = $"ORD-{202500 + i}",
                    PlannedTime = DateTime.UtcNow.Date.AddHours(9).AddMinutes(i * 20),
                    Status = "Pending"
                }).ToList()
            };

            db.Routes.Add(r);
            await db.SaveChangesAsync();
        }

        // 4) Örnek RoutePlan
        if (!await db.RoutePlans.AnyAsync())
        {
            var rp = new RoutePlan
            {
                Name = "İstanbul Merkez Dağıtım",
                VehiclePlate = "34XYZ34",
                CreatedBy = "seed",
                Optimization = "Süre",
                Stops = new List<RoutePlanStop>
                {
                    new(){ Title="Depo",      Address="Başlangıç", Lat=41.01, Lng=28.97, OrderNo=1 },
                    new(){ Title="Beşiktaş",  Address="Semt",      Lat=41.04, Lng=29.00, OrderNo=2 },
                    new(){ Title="Kadıköy",   Address="Semt",      Lat=40.99, Lng=29.03, OrderNo=3 },
                }
            };
            db.RoutePlans.Add(rp);
            await db.SaveChangesAsync();
        }

        // 5) Demo veriler (boşsa ekle)
        if (!await db.WhTasks.AnyAsync())
        {
            db.WhTasks.AddRange(Enumerable.Range(1, 10).Select(i => new WhTask
            {
                Type = (i % 3 == 0) ? "Count" : (i % 2 == 0 ? "Putaway" : "Pick"),
                Status = "Open",
                Location = $"A{i:00}-{(char)('A' + (i % 6))}",
                Sku = $"SKU-{1000 + i}",
                Quantity = 1 + (i % 12),
                CreatedAt = DateTime.UtcNow.AddMinutes(-i * 13),
            }));
            await db.SaveChangesAsync();
        }

        if (!await db.Purchases.AnyAsync())
        {
            db.Purchases.AddRange(Enumerable.Range(1, 12).Select(i => new Purchase
            {
                Supplier = $"TED-{i:00}",
                Sku = $"SKU-{2000 + i}",
                Qty = 5 + (i % 20),
                Price = 10 + i,
                Status = (i % 3 == 0) ? "Approved" : (i % 3 == 1) ? "Planned" : "Received"
            }));
            await db.SaveChangesAsync();
        }

        if (!await db.CrmRisks.AnyAsync())
        {
            db.CrmRisks.AddRange(Enumerable.Range(1, 8).Select(i => new CrmRisk
            {
                Customer = $"CUST-{i:000}",
                Segment = (i % 2 == 0) ? "B2B" : "B2C",
                Risk = 10 + (i % 70),
                Note = (i % 4 == 0) ? "İade artışı" : null
            }));
            await db.SaveChangesAsync();
        }

        if (!await db.AsnOrders.AnyAsync())
        {
            db.AsnOrders.AddRange(Enumerable.Range(1, 10).Select(i => new AsnOrder
            {
                Slot = DateTime.UtcNow.Date.AddDays(1 + (i % 5)).AddHours(10 + (i % 6)),
                Sku = $"SKU-{3000 + i}",
                Qty = 2 + (i % 15),
                Status = (i % 2 == 0) ? "Planned" : "Confirmed"
            }));
            await db.SaveChangesAsync();
        }
    }

    private static async Task EnsureUsersAsync(AppDbContext db)
    {
        async Task Add(string u, string n, string r, string p)
        {
            if (await db.Users.AnyAsync(x => x.Username == u)) return;
            db.Users.Add(new User
            {
                Username = u,
                FullName = n,
                Role = r,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(p),
                IsActive = true
            });
        }

        await Add("admin",   "Platform Admin",     Roles.Admin,            "Admin_123!");
        await Add("planner", "Ops Planner",        Roles.Planning,         "Planner_123!");
        await Add("whmgr",   "Warehouse Manager",  Roles.WarehouseManager, "Whmgr_123!");
        await Add("whop",    "Warehouse Operator", Roles.WarehouseOperator,"Whop_123!");
        await Add("driver",  "Delivery Driver",    Roles.Driver,           "Driver_123!");
        await Add("buyer",   "Purchasing",         Roles.Purchasing,       "Buyer_123!");
        await Add("crm",     "CRM Agent",          Roles.CrmAgent,         "Crm_123!");
        await Add("supplier","Supplier",           Roles.Supplier,         "Supplier_123!");
        await Add("b2b",     "B2B Customer",       Roles.Customer,         "Customer_123!");

        await db.SaveChangesAsync();
    }
}
