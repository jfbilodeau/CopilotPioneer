using CopilotPioneer.Web.Models;
using CopilotPioneer.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CopilotPioneer.Web.Pages;

public class SubmissionDelete(PioneerService _pioneerService) : PageModel
{
    [BindProperty(Name = "id", SupportsGet = true)]
    public string SubmissionId { get; set; } = string.Empty;
    
    public Submission Submission { get; set; } = new();
    
    public PioneerService PioneerService { get; private set; } = _pioneerService;
    
    public async Task<ActionResult> OnGet()
    {
        var submission = await PioneerService.GetSubmissionById(SubmissionId);
        
        if (submission == null) 
        {
            return NotFound();
        }
        
        Submission = submission;

        return Page();
    }
    
    public async Task<ActionResult> OnPost()
    {
        var submission = await PioneerService.GetSubmissionById(SubmissionId);
        
        if (submission == null)
        {
            return NotFound();
        }

        if (submission.Author != User.Identity?.Name)
        {
            return Unauthorized();
        }

        await PioneerService.DeleteSubmission(submission);
        
        return RedirectToPage("/Index");
    }
}