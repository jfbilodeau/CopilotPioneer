using CopilotPioneer.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CopilotPioneer.Web.Pages;

[Authorize(Roles = "CopilotPioneer.Administrator")]
public class Administration(PioneerService pioneerService) : PageModel
{
    public void OnGet()
    {
        
    }

    public async void OnPostTallyVotesAsync()
    {
        await pioneerService.TallyVotes();
    }
}