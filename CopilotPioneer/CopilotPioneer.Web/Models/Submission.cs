using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices;
using Newtonsoft.Json;

namespace CopilotPioneer.Web.Models;

public sealed class Submission
{
    public string Id { get; set; } = "";
    
    public string Author { get; set; } = "";
    public string Product { get; set; } = "";
    
    public DateTime CreatedDate { get; set; } = DateTime.Now;
    public DateTime LastModifiedDate { get; set; } = DateTime.Now;
    
    [Required]
    public string Title { get; set; } = "";
    public string Prompt { get; set; } = "";
    public string Notes { get; set; } = "";
    
    public string[] Tags { get; set; } = [];
    
    public int DailyVotes { get; set; } = 0;
    public bool DailyVoteWinner { get; set; } = false;
    public int WeeklyVotes { get; set; } = 0;
    public bool WeeklyVoteWinner { get; set; } = false;
    
    [JsonIgnore]
    public int TotalVotes => DailyVotes + WeeklyVotes;
    
    public Screenshot[] Screenshots { get; set; } = [];
    
    public Comment[] Comments { get; set; } = [];
}