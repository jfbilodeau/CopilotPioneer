using CopilotPioneer.Web.Models;
using CopilotPioneer.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CopilotPioneer.Web.Pages;

public class ProfileEdit(PioneerService pioneerService) : PageModel
{
    [BindProperty]
    public Profile Profile { get; set; } = new();

    public async Task OnGet()
    {
        var profileId = User.Identity?.Name;

        if (profileId == null)
        {
            Response.StatusCode = 401;
            return;
        }

        Profile = await pioneerService.GetProfileOrDefault(profileId);
    }

    public async Task OnPost()
    {
        if (!ModelState.IsValid)
        {
            return;
        }
        
        var profileId = User.Identity?.Name;

        if (profileId == null)
        {
            Response.StatusCode = 401;
            return;
        }

        var currentProfile = await pioneerService.GetProfileOrDefault(profileId);

        currentProfile.Name = Profile?.Name ?? profileId;

        await pioneerService.UpdateProfile(currentProfile);

        Response.Redirect($"/Profile/{profileId}");
    }
}