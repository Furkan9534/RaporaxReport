using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    [Authorize(Roles = "ReportViewer")]
    [HttpGet("report-viewer")]
    public IActionResult GetReportViewer()
    {
        return Ok(new { message = "You have successfully accessed a ReportViewer protected endpoint." });
    }

    [Authorize(Roles = "ReportCreator")]
    [HttpPost("report-creator")]
    public IActionResult PostReportCreator()
    {
        return Ok(new { message = "You have successfully accessed a ReportCreator protected endpoint." });
    }
}
