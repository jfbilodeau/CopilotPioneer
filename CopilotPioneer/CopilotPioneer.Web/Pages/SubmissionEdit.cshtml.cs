using CopilotPioneer.Web.Models;
using CopilotPioneer.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CopilotPioneer.Web.Pages;

public class SubmissionEdit(PioneerService pioneerService) : PageModel
{
    public PioneerService PioneerService { get; } = pioneerService;
    
    [BindProperty(Name = "id", SupportsGet = true)]
    public string SubmissionId { get; set; } = string.Empty;
    
    [BindProperty]
    public Submission Submission { get; set; } = new();        

    public async Task<ActionResult> OnGet()
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
        
        Submission = submission;

        return Page();
    }
    
    public async Task<ActionResult> OnPost()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }
        
        var submission = await PioneerService.GetSubmissionById(SubmissionId);
        
        if (submission == null)
        {
            return NotFound();
        }

        if (submission.Author != User.Identity?.Name)
        {
            return Unauthorized();
        }

        submission.Product = Submission.Product;
        submission.Title = Submission.Title;
        submission.Prompt = Submission.Prompt;
        submission.Notes = Submission.Notes;
        
        await PioneerService.UpdateSubmission(submission);
        
        return RedirectToPage("SubmissionView", new { id = SubmissionId });
    }
}