// Controllers/UiController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MLYSO.Web.Models;
using System.Security.Claims;

namespace MLYSO.Web.Controllers;

[Authorize]
public class UiController : Controller
{
    // Tekrarlayan rol kümeleri için küçük sabitler
    private const string OpsRoles = Roles.Admin + "," + Roles.Operations + "," + Roles.Planning + "," + Roles.Logistics;
    private const string DriverRoles = Roles.Admin + "," + Roles.Driver;
    private const string ErpRoles = Roles.Admin + "," + Roles.Purchasing;
    private const string CrmRoles = Roles.Admin + "," + Roles.CrmAgent + "," + Roles.CustomerService;
    private const string SupplierRoles = Roles.Admin + "," + Roles.Supplier;
    private const string CustomerRoles = Roles.Admin + "," + Roles.Customer;
    private const string WhManagerRoles = Roles.Admin + "," + Roles.WarehouseManager + "," + Roles.WarehouseChief;
    private const string WhOperatorRoles = Roles.Admin + "," + Roles.WarehouseOperator;

    // /ui => her zaman hub'a
    [HttpGet("/ui")]
    public IActionResult Index() => Redirect("/ui/hub");

    // --- Admin + Operasyon/Planlama/Logistics ---
    [Authorize(Roles = OpsRoles)]
    [HttpGet("/ui/dashboard")]
    public IActionResult Dashboard() => View("~/Views/Dashboard/Index.cshtml");

    [Authorize(Roles = OpsRoles)]
    [HttpGet("/ui/plan")]
    public IActionResult Plan() => View("~/Views/Plan/Index.cshtml");

    [Authorize(Roles = Roles.Admin + "," + Roles.Operations + "," + Roles.Planning + "," + Roles.Logistics + "," + Roles.WarehouseManager + "," + Roles.Driver)]
    [HttpGet("/ui/shipments")]
    [HttpGet("/shipments")] // alias: /shipments da çalışsın
    public IActionResult Shipments() => View("~/Views/Shipments/Index.cshtml");

    // *** DİKKAT ***:
    // Buradan /ui/warehouse kaldırıldı. Bu rota artık SADECE WarehouseController tarafından handle ediliyor.
    // Böylece /ui/warehouse?tab=racks isteğinde "AmbiguousMatchException" oluşmaz.

    // --- Sürücü ---
    [Authorize(Roles = DriverRoles)]
    [HttpGet("/ui/driver")]
    public IActionResult Driver() => View("~/Views/Driver/Index.cshtml");

    // --- Satınalma / ERP ---
    [Authorize(Roles = ErpRoles)]
    [HttpGet("/ui/erp-purchase")]
    public IActionResult ErpPurchase() => View("~/Views/Erp/Purchase.cshtml");

    // --- CRM ---
    [Authorize(Roles = CrmRoles)]
    [HttpGet("/ui/crm-risk")]
    public IActionResult CrmRisk() => View("~/Views/Crm/Risk.cshtml");

    // --- Tedarikçi ---
    [Authorize(Roles = SupplierRoles)]
    [HttpGet("/ui/supplier-asn")]
    public IActionResult SupplierAsn() => View("~/Views/Supplier/Asn.cshtml");

    // --- Müşteri ---
    [Authorize(Roles = CustomerRoles)]
    [HttpGet("/ui/orders")]
    public IActionResult Orders() => View("~/Views/Orders/Index.cshtml");

    [Authorize(Roles = CustomerRoles)]
    [HttpGet("/ui/orders/create")]
    public IActionResult CreateOrder() => View("~/Views/Orders/Create.cshtml");

    // --- Rol Merkezi (Hub) ---
    [Authorize] // sadece girişli kullanıcı
    [HttpGet("/ui/hub")]
    public IActionResult Hub()
    {
        var role = User.FindFirstValue(ClaimTypes.Role) ?? "";
        ViewBag.Role = role;
        return View("~/Views/Ui/Hub.cshtml");
    }

    // --- Depo Operatörü kısa yolu (Operator ekranı) ---
    [Authorize(Roles = WhOperatorRoles)]
    [HttpGet("/ui/tasks")]
    public IActionResult TasksAlias()
        => RedirectToAction(actionName: "Operator", controllerName: "Warehouse");
}
