// Controllers/CustomerOrdersController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MLYSO.Web.Models;
using MLYSO.Web.Services;
using System.Linq;

namespace MLYSO.Web.Controllers
{
    [Authorize(Roles = Roles.Customer + "," + Roles.CRM + "," + Roles.CustomerService + "," + Roles.Admin)]
    public class CustomerOrdersController : Controller
    {
        private readonly StaticJsonStore<CustomerOrdersState> _store;

        public CustomerOrdersController(IWebHostEnvironment env)
        {
            _store = new StaticJsonStore<CustomerOrdersState>(env, "customer_orders");
            EnsureSeed();
        }

        private void EnsureSeed()
        {
            var s = _store.Read();
            if (s.Orders.Count == 0)
            {
                s.Orders.AddRange(new[]
                {
                    new CustomerOrder { Id="ORD-1001", OrderNo="SIP-2025-0001", Customer="Melike K.", Status="Shipped",    CreatedAt=DateTime.UtcNow.AddDays(-3)},
                    new CustomerOrder { Id="ORD-1002", OrderNo="SIP-2025-0002", Customer="Ali V.",    Status="Delivered",  CreatedAt=DateTime.UtcNow.AddDays(-5), DeliveredAt=DateTime.UtcNow.AddDays(-1)},
                    new CustomerOrder { Id="ORD-1003", OrderNo="SIP-2025-0003", Customer="Bora S.",   Status="ReturnRequested", CreatedAt=DateTime.UtcNow.AddDays(-10), ReturnRequestedAt=DateTime.UtcNow.AddDays(-2), ReturnReason="Beden uymadı" }
                });
                _store.Write(s);
            }
        }

        // ----- LİSTE SAYFALARI -----
        [HttpGet("/customer/orders")]
        public IActionResult Index()
        {
            var s = _store.Read();
            return View("Index", s);
        }

        [HttpGet("/customer/orders/delivered")]
        public IActionResult Delivered()
        {
            var s = _store.Read();
            return View("Delivered", s);
        }

        // İade ekranı için BİRDEN FAZLA ALIAS:
        //  - /customer/orders/returns
        //  - /customer/returns          (Home’daki kartı buna yönlendirin)
        //  - /returns                   (kısa link)
        [HttpGet("/customer/orders/returns")]
        [HttpGet("/customer/returns")]
        [HttpGet("/returns")]
        public IActionResult Returns([FromQuery] string? status = null)
        {
            var s = _store.Read();

            // İstenirse status=ReturnRequested/Returned/Delivered vb. filtreleme
            if (!string.IsNullOrWhiteSpace(status))
            {
                var st = status.Trim();
                s = new CustomerOrdersState
                {
                    Orders = s.Orders.Where(o => string.Equals(o.Status, st, StringComparison.OrdinalIgnoreCase)).ToList()
                };
                ViewBag.Filter = st;
            }

            return View("Returns", s);
        }

        // ----- İŞLEMLER -----

        // Teslimatı onayla (PoD sonrası)
        [ValidateAntiForgeryToken]
        [HttpPost("/customer/orders/{id}/confirm-delivery")]
        public IActionResult ConfirmDelivery(string id)
        {
            var s = _store.Read();
            var o = s.Orders.FirstOrDefault(x => x.Id == id);
            if (o == null) return NotFound();

            o.Status = "Delivered";
            o.DeliveredAt = DateTime.UtcNow;
            _store.Write(s);

            TempData["ok"] = $"{o.OrderNo} teslim edildi.";
            return RedirectToAction(nameof(Delivered));
        }

        // Müşteri iade talebi oluşturur
        [ValidateAntiForgeryToken]
        [HttpPost("/customer/orders/{id}/request-return")]
        public IActionResult RequestReturn(string id, string reason)
        {
            var s = _store.Read();
            var o = s.Orders.FirstOrDefault(x => x.Id == id);
            if (o == null) return NotFound();

            o.Status = "ReturnRequested";
            o.ReturnRequestedAt = DateTime.UtcNow;
            o.ReturnReason = reason;
            _store.Write(s);

            TempData["ok"] = $"{o.OrderNo} için iade talebi oluşturuldu.";
            // Kart doğrudan iade sayfasını açtığı için iade ekranına dönüyoruz
            return RedirectToAction(nameof(Returns));
        }

        // CRM/CustomerService/Admin iade onaylar
        [Authorize(Roles = Roles.CRM + "," + Roles.CustomerService + "," + Roles.Admin)]
        [ValidateAntiForgeryToken]
        [HttpPost("/customer/orders/{id}/approve-return")]
        public IActionResult ApproveReturn(string id)
        {
            var s = _store.Read();
            var o = s.Orders.FirstOrDefault(x => x.Id == id);
            if (o == null) return NotFound();

            o.Status = "Returned";
            o.ReturnApprovedAt = DateTime.UtcNow;
            _store.Write(s);

            TempData["ok"] = $"{o.OrderNo} iade onaylandı.";
            return RedirectToAction(nameof(Returns));
        }

        // (Opsiyonel) İade talebini iptal et – müşteri vazgeçtiyse
        [ValidateAntiForgeryToken]
        [HttpPost("/customer/orders/{id}/cancel-return")]
        public IActionResult CancelReturn(string id)
        {
            var s = _store.Read();
            var o = s.Orders.FirstOrDefault(x => x.Id == id);
            if (o == null) return NotFound();

            // Basit senaryo: iptal edildiğinde kayıt tekrar "Delivered" görünür
            o.Status = "Delivered";
            // DeliveredAt var; ek alanınız yoksa tarih set etmeyebilirsiniz.
            o.DeliveredAt ??= DateTime.UtcNow;
            o.ReturnReason = null;
            _store.Write(s);

            TempData["ok"] = $"{o.OrderNo} için iade talebi iptal edildi.";
            return RedirectToAction(nameof(Returns));
        }
    }
}











