using CopilotPioneer.Web.Models;
using CopilotPioneer.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CopilotPioneer.Web.Pages;

public class SubmissionCreate(PioneerService pioneerService) : PageModel
{
    public PioneerService PioneerService { get; private set; } = pioneerService;

    [BindProperty]
    public Submission Submission { get; set; } = new();

    public void OnGet()
    {
    }
    
    public async Task<IActionResult> OnPost()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }
        
        if (User.Identity?.Name == null)
        {
            return Unauthorized();
        }

        Submission.Author = User.Identity.Name;
        
        var result = await PioneerService.CreateSubmission(Submission);

        return RedirectToPage("SubmissionView", new { id = result.Id });
    }
}