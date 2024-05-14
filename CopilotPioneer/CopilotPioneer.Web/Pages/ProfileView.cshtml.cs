using CopilotPioneer.Web.Models;
using CopilotPioneer.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CopilotPioneer.Web.Pages;

public class ProfileView(PioneerService pioneerService) : PageModel
{
    [BindProperty(Name = "userId", SupportsGet = true)]
    public string UserId { get; set; } = string.Empty;
    
    public int PageNumber { get; set; } = 0;

    public Profile? Profile { get; private set; } = new();
    
    public List<Submission> Submissions { get; private set; } = new();

    public async Task<ActionResult> OnGet()
    {
        if (UserId == string.Empty )
        {
            UserId = User?.Identity?.Name ?? "";
        
            if (UserId == string.Empty)
            {
                return Unauthorized();
            }
        }
        
        Profile = await pioneerService.GetProfile(UserId);

        if (Profile == null)
        {
            return RedirectToPage("ProfileEdit");
        }
        
        Submissions = await pioneerService.GetSubmissionsByFilter(UserId, pageNumber: PageNumber);

        return Page();
    }
}
