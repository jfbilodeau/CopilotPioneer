using CopilotPioneer.Web.Models;
using CopilotPioneer.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CopilotPioneer.Web.Pages;

public class Submissions(PioneerService pioneerService) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string? Id { get; set; } = null;
    public Submission? Submission { get; set; } = null;

    public async Task<IActionResult> OnGet()
    {
        if (Id != null)
        {
            Submission = await pioneerService.GetSubmissionById(Id);

            if (Submission != null)
            {
                return Page();
            }
        }

        return NotFound();
    }
}