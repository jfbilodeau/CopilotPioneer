using CopilotPioneer.Web.Models;
using CopilotPioneer.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CopilotPioneer.Web.Pages;

public class ProfileView(PioneerService pioneerService) : PageModel
{
    [BindProperty(SupportsGet = true)] public string Id { get; set; } = string.Empty;

    public Profile? Profile { get; private set; } = new();

    public async Task OnGet()
    {
        Profile = await pioneerService.GetProfile(Id);

        if (Profile == null)
        {
            Response.StatusCode = 404;
        }
    }
}