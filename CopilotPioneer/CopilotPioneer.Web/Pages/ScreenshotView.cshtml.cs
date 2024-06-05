using CopilotPioneer.Web.Models;
using CopilotPioneer.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CopilotPioneer.Web.Pages;

public class ScreenshotView(PioneerService pioneerService) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string SubmissionId { get; set; } = string.Empty;
    [BindProperty(SupportsGet = true)]
    public string ScreenshotId { get; set; } = string.Empty;
    [BindProperty(SupportsGet = true)]
    public string Size { get; set; } = string.Empty;
    
    public async Task<ActionResult> OnGetAsync()
    {
        var screenshot = await pioneerService.GetScreenshot(SubmissionId, ScreenshotId);
        
        if (screenshot == null)
        {
            return NotFound();
        }
        
        var stream = await pioneerService.GetScreenshotStream(screenshot, Size);
        
        if (stream == null)
        {
            return NotFound();
        }
        
        if (screenshot.IsDocument) 
        {
            return File(stream, "application/octet-stream", Path.GetFileName(screenshot.OriginalName));
        }
        
        return File(stream, "image/png");
    }
}