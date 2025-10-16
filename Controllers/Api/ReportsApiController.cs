using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MLYSO.Web.Services;

namespace MLYSO.Web.Controllers.Api;

[ApiController]
[Route("api/reports")]
public class ReportsApiController : ControllerBase
{
    private readonly ReportingService _rep;
    private readonly EtlService _etl;

    public ReportsApiController(ReportingService rep, EtlService etl)
    {
        _rep = rep; _etl = etl;
    }

    /// <summary>KPI kartları (30g sipariş, ort. teslim, dinamik iade %, SLA on-time, açık şikayet)</summary>
    [HttpGet("kpi")]
    [Authorize]
    public async Task<IActionResult> GetKpis()
    {
        var kpi = await _rep.GetKpisAsync();
        return Ok(new { lastEtlUtc = _etl.LastRunUtc, kpi });
    }

    /// <summary>Son 7 gün sipariş trendi (gün/sayı)</summary>
    [HttpGet("trend7")]
    [Authorize]
    public async Task<IActionResult> Trend7()
    {
        var rows = await _rep.TrendLast7Async();
        return Ok(rows);
    }

    /// <summary>KPI'ları CSV olarak dışa aktar</summary>
    [HttpGet("kpi/export")]
    [Authorize(Roles = $"{Models.Roles.Admin},{Models.Roles.Operations}")]
    public async Task<IActionResult> ExportKpiCsv()
    {
        var csv = await _rep.ExportKpiCsvAsync();
        var bytes = System.Text.Encoding.UTF8.GetBytes(csv);
        return File(bytes, "text/csv", "kpi.csv");
    }
}
