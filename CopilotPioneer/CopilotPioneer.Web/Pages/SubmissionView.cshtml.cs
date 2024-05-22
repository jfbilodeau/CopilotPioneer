using System.Net;
using CopilotPioneer.Web.Models;
using CopilotPioneer.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CopilotPioneer.Web.Pages;

public class SubmissionViewComment
{
    public string SubmissionId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
}

public class SubmissionView(PioneerService pioneerService) : PageModel
{
    public PioneerService PioneerService { get; private set; } = pioneerService;

    [BindProperty(Name = "id", SupportsGet = true)]
    public string Id { get; set; } = string.Empty;

    public Submission Submission { get; set; } = new();

    [BindProperty]
    public string SubmissionId { get; set; } = string.Empty;
    [BindProperty]
    public Comment Comment { get; set; } = new();

    public async Task<IActionResult> OnGet()
    {
        var submission = await PioneerService.GetSubmissionById(Id);

        if (submission == null)
        {
            return NotFound();
        }

        Submission = submission;

        return Page();
    }
    
    public async Task<IActionResult> OnPostCommentAsync()
    {
        if (User.Identity?.Name == null)
        {
            return Unauthorized();
        }
        
        if (ModelState.IsValid == false)
        {
            return Page();
        }

        var comment = new Comment
        {
            Author = User.Identity.Name,
            Content = Comment.Content,
        };

        await PioneerService.AddCommentToSubmission(SubmissionId, comment);

        return RedirectToPage();
    }
}