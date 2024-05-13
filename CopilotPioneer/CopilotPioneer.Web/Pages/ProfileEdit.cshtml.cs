using CopilotPioneer.Web.Models;
using CopilotPioneer.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CopilotPioneer.Web.Pages;

[Authorize]
public class ProfileEdit(PioneerService pioneerService) : PageModel
{
    [BindProperty]
    public Profile Profile { get; set; } = new();

    public async Task<ActionResult> OnGet()
    {
        var profileId = User.Identity?.Name;

        if (profileId == null)
        {
            return Unauthorized();
        }

        Profile = await pioneerService.GetProfileOrDefault(profileId);
        
        return Page();
    }

    public async Task<ActionResult> OnPost()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }
        
        var profileId = User.Identity?.Name;

        if (profileId == null)
        {
            return Unauthorized();
        }

        var currentProfile = await pioneerService.GetProfileOrDefault(profileId);

        currentProfile.Name = Profile?.Name ?? profileId;

        await pioneerService.UpdateProfile(currentProfile);

        Response.Redirect($"/Profile/{profileId}");

        return Page();
    }
}