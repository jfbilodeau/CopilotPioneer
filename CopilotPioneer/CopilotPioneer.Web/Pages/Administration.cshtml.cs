﻿using CopilotPioneer.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;

namespace CopilotPioneer.Web.Pages;

[Authorize(Roles = "CopilotPioneer.Administrator")]
public class Administration(PioneerService pioneerService, MemoryCache memoryCache) : PageModel
{
    public void OnGet()
    {
        
    }

    public async void OnPostTallyVotesAsync()
    {
        await pioneerService.TallyVotes();
        
        // Clear the winners from the cache
        memoryCache.Remove("voteWinners");
    }

    public async void OnPostClearMemoryCache()
    {
        memoryCache.Clear();
    }
}