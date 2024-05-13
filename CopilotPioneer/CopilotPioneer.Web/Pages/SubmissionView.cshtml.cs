using System.Net;
using CopilotPioneer.Web.Models;
using CopilotPioneer.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CopilotPioneer.Web.Pages;

public class SubmissionView(PioneerService pioneerService) : PageModel
{
    public PioneerService PioneerService { get; private set; } = pioneerService;
    
    [BindProperty(SupportsGet = true)]
    public string? Id { get; set; } = null;
    public Submission? Submission { get; set; } = null;

    public async Task<IActionResult> OnGet()
    {
        if (Id != null)
        {
            Submission = await PioneerService.GetSubmissionById(Id);
        }

        return Page();
    }
}