using CopilotPioneer.Web.Models;
using CopilotPioneer.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CopilotPioneer.Web.Pages;

public class Vote(PioneerService pioneerService) : PageModel
{
    public PioneerService PioneerService { get; private set; } = pioneerService;
    
    public Candidate Candidate { get; set; } = new();

    public bool DailyVoteCast => Candidate.DailyVoteCast;
    public bool WeeklyVoteCast => Candidate.WeeklyVoteCast;
    
    public List<Submission> DailySubmissions => Candidate.DailySubmissions;
    public List<Submission> WeeklySubmissions => Candidate.WeeklySubmissions;
    
    [BindProperty]
    public string SubmissionId { get; set; } = string.Empty;
    
    public async Task<ActionResult> OnGetAsync()
    {
        var userId = User.Identity?.Name;

        if (userId == null)
        {
            return Unauthorized();
        }

        Candidate = await PioneerService.GetVoteCandidates(userId);

        return Page();
    }

    public async Task<ActionResult> OnPostCastWeeklyVoteAsync()
    {
        var userId = User.Identity?.Name;

        if (userId == null)
        {
            return Unauthorized();
        }

        await PioneerService.CastWeeklyVote(userId, SubmissionId);

        return RedirectToPage();
    }
}