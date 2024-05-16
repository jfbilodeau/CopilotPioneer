﻿using System.Net;
using CopilotPioneer.Web.Models;
using CopilotPioneer.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CopilotPioneer.Web.Pages;

public class SubmissionView(PioneerService pioneerService) : PageModel
{
    public PioneerService PioneerService { get; private set; } = pioneerService;

    [BindProperty(Name = "id", SupportsGet = true)]
    public string Id { get; set; } = string.Empty;

    public Submission Submission { get; set; } = new();

    public async Task<IActionResult> OnGet()
    {
        var submission = await PioneerService.GetSubmissionById(Id);

        if (submission == null)
        {
            return NotFound();
        }

        Submission = submission;

        return Page();
    }
}