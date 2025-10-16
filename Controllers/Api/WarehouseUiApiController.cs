using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MLYSO.Web.Models;
using MLYSO.Web.Services;

namespace MLYSO.Web.Controllers.Api;

[ApiController]
[Route("api/warehouse/ui-state")]
[Authorize] // en azından girişli kullanıcı
public sealed class WarehouseUiApiController : ControllerBase
{
    private readonly StaticJsonStore<WarehouseUiState> _store;

    public WarehouseUiApiController(IWebHostEnvironment env)
    {
        _store = new(env, "warehouse-ui");
    }

    [HttpGet]
    public IActionResult Get() => Ok(_store.Read());

    [HttpPut]
    public IActionResult Put([FromBody] WarehouseUiStatePatch patch)
    {
        var s = _store.Read();

        // null olmayanları uygula
        if (patch.RackPrefix is not null) s.RackPrefix = patch.RackPrefix;
        if (patch.RackAisle is not null) s.RackAisle = patch.RackAisle.Value;
        if (patch.RackLevel is not null) s.RackLevel = patch.RackLevel.Value;

        if (patch.TwinRack is not null) s.TwinRack = patch.TwinRack;
        if (patch.TwinSku is not null) s.TwinSku = patch.TwinSku;
        if (patch.TwinAddr is not null) s.TwinAddr = patch.TwinAddr;

        if (patch.FlowTitle is not null) s.FlowTitle = patch.FlowTitle;
        if (patch.FlowStatus is not null) s.FlowStatus = patch.FlowStatus;
        if (patch.FlowAssignee is not null) s.FlowAssignee = patch.FlowAssignee;

        _store.Write(s);
        return Ok(s);
    }
}
