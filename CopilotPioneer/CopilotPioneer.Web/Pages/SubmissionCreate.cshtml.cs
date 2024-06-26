﻿using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices;
using CopilotPioneer.Web.Models;
using CopilotPioneer.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CopilotPioneer.Web.Pages;

public class ScreenshotSubmissionModel
{
    [Display(Name = "Screenshot")]
    public IFormFile? File { get; init; }
    
    [Display(Name = "Description (Alt text)")]
    public string? AltText { get; init; } = string.Empty;
}

public class SubmissionCreate(PioneerService pioneerService) : PageModel
{
    public PioneerService PioneerService { get; private set; } = pioneerService;

    [BindProperty]
    public Submission Submission { get; set; } = new();
    
    [BindProperty]
    public ScreenshotSubmissionModel Screenshot1 { get; set; } = new();

    [BindProperty]
    public ScreenshotSubmissionModel Screenshot2 { get; set; } = new();

    [BindProperty]
    public ScreenshotSubmissionModel Screenshot3 { get; set; } = new();

    public void OnGet()
    {
    }
    
    public async Task<IActionResult> OnPost()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }
        
        if (User.Identity?.Name == null)
        {
            return Unauthorized();
        }

        Submission.Author = User.Identity.Name;
        
        var screenshots = new[] {
            Screenshot1,
            Screenshot2,
            Screenshot3,
        }
            .Where(s => s.File != null)
            .ToArray();
        
        var result = await PioneerService.CreateSubmission(Submission, screenshots);

        return RedirectToPage("SubmissionView", new { id = result.Id });
    }
}