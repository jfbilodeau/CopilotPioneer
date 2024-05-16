using CopilotPioneer.Web.Models;
using CopilotPioneer.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CopilotPioneer.Web.Pages;

public class ProfileView(PioneerService pioneerService) : PageModel
{
    [BindProperty(Name = "id", SupportsGet = true)]
    public string UserId { get; set; } = string.Empty;
    
    public int PageNumber { get; set; } = 0;

    public Profile Profile { get; private set; } = new();
    
    public List<Submission> Submissions { get; private set; } = new();

    public async Task<ActionResult> OnGet()
    {
        var profile = await pioneerService.GetProfile(UserId);

        if (profile == null)
        {
            if (UserId == User.Identity?.Name)
            {
                return RedirectToPage("ProfileEdit");
            }
            
            Profile = new Profile
            {
                Id = UserId,
            };
        }
        else
        {
            Profile = profile;
        }
        
        Submissions = await pioneerService.GetSubmissionsByFilter(UserId, pageNumber: PageNumber);

        return Page();
    }
}
