
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MLYSO.Web.Services;

namespace MLYSO.Web.Controllers.Api;

[ApiController]
[Route("api/metadata")]
public class MetadataApiController : ControllerBase
{
    private readonly CsvOptionService _csv;
    private readonly PermissionService _perm;

    public MetadataApiController(CsvOptionService csv, PermissionService perm)
    {
        _csv = csv; _perm = perm;
    }

    [HttpGet("options")]
    [AllowAnonymous]
    public IActionResult Options([FromQuery] string? field = null, [FromQuery] int maxPerField = 50)
    {
        if (!string.IsNullOrWhiteSpace(field))
            return Ok(new { field, options = _csv.GetOptions(field) });
        return Ok(_csv.GetAllOptions(maxPerField));
    }

    [HttpGet("schema")]
    [Authorize]
    public IActionResult Schema()
    {
        var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "unknown";
        var columns = _csv.GetColumns();
        var editable = columns.Where(c => _perm.CanEditField(role, c)).ToArray();
        return Ok(new
        {
            role,
            editableFields = editable,
            readOnlyFields = columns.Except(editable).ToArray()
        });
    }
}
