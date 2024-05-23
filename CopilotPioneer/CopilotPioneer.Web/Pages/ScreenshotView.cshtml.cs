using CopilotPioneer.Web.Models;
using CopilotPioneer.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CopilotPioneer.Web.Pages;

public class ScreenshotView(PioneerService _pioneerService) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string SubmissionId { get; set; } = string.Empty;
    [BindProperty(SupportsGet = true)]
    public string ScreenshotId { get; set; } = string.Empty;
    [BindProperty(SupportsGet = true)]
    public string Size { get; set; } = string.Empty;
    
    public async Task<ActionResult> OnGetAsync()
    {
        var stream = await _pioneerService.GetScreenshot(SubmissionId, ScreenshotId, Size);
        
        if (stream == null)
        {
            return NotFound();
        }
        
        return File(stream, "image/png");
    }
}