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
        var userId = User.Identity?.Name;
        
        if (userId == null)
        {
            return Unauthorized();
        }
        
        Profile = await pioneerService.GetProfileOrDefault(userId);
        
        return Page();
    }

    public async Task<ActionResult> OnPost()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }
        
        var userId = User.Identity?.Name;
        
        if (userId == null)
        {
            return Unauthorized();
        }
        
        var profile = await pioneerService.GetProfileOrDefault(userId);
        
        if (profile.Id != userId)
        {
            return Unauthorized();
        }

        profile.Name = Profile.Name;

        await pioneerService.UpdateProfile(profile);

        return RedirectToPage("ProfileView", new { id = userId});
    }
}