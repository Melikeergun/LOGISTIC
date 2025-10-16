using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MLYSO.Web.Controllers;

[Authorize]
public sealed class RacksController : Controller
{
    // Basit view-model’ler (tek dosyada dursun)
    public sealed class RackSceneVm
    {
        public int WarehouseL { get; init; } = 40000; // mm
        public int WarehouseW { get; init; } = 20000;
        public int WarehouseH { get; init; } = 12000;
        public List<RackVm> Racks { get; init; } = new();
    }
    public sealed record RackVm(
        string Code,            // Raf kodu
        int X, int Y, int Z,    // Depo içi başlangıç konumu (mm)
        int Bays,               // Göz sayısı (X yönü)
        int Levels,             // Kat sayısı (Z yönü)
        int BayWidth,           // Bir göz genişliği (mm)
        int LevelHeight,        // Kat yüksekliği (mm)
        int Depth,              // Raf derinliği (Y yönü, mm)
        bool[] Fill             // Bays*Levels uzunlukta: dolu/boş
    );

    [HttpGet("/ui/racks")]
    public IActionResult Index()
    {
        // STATİK örnek sahne – hızlıca güçlü bir demo
        var model = new RackSceneVm
        {
            WarehouseL = 48000,
            WarehouseW = 24000,
            WarehouseH = 12000,
            Racks = new()
            {
                // 3x4 gözlü, 900x1500 boyutlu, 1100 derin raf
                new RackVm(
                    Code: "A-01", X: 2000, Y: 2000, Z: 0,
                    Bays: 3, Levels: 4, BayWidth: 900, LevelHeight: 1500, Depth: 1100,
                    Fill: new []{ true,true,false,  true,false,true,  true,true,true,  false,true,false }
                ),
                new RackVm(
                    Code: "A-02", X: 5200, Y: 2000, Z: 0,
                    Bays: 4, Levels: 4, BayWidth: 900, LevelHeight: 1500, Depth: 1100,
                    Fill: Enumerable.Range(0, 16).Select(i => i%3!=0).ToArray() // örnek
                ),
                new RackVm(
                    Code: "B-01", X: 2000, Y: 4000, Z: 0,
                    Bays: 5, Levels: 3, BayWidth: 900, LevelHeight: 1700, Depth: 1100,
                    Fill: Enumerable.Range(0, 15).Select(i => i%2==0).ToArray()
                )
            }
        };

        return View("Racks3D", model);
    }
}
