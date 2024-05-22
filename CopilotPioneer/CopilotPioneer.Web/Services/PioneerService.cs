using System.Text.RegularExpressions;
using Azure.Storage.Blobs;
using CopilotPioneer.Web.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Fluent;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Extensions.Caching.Memory;

namespace CopilotPioneer.Web.Services;

public class Product
{
    public string Id { get; set; } = "";
    public string Label { get; set; } = "";
}

public partial class PioneerService
{
    // For now, hardcode points here.
    public const int PointsPerSubmission = 3;
    public const int PointsPerDailyVoteReceived = 1;
    public const int PointsPerDailyVoteCast = 1;
    public const int PointsPerWeeklyVoteCast = 2;
    public const int PointsPerWeeklyVoteReceived = 2;

    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<PioneerService> _logger;

    private readonly Database _cosmosDbDatabase;
    private readonly Container _submissionsContainer;
    private readonly Container _profileContainer;
    private readonly Container _pointsContainer;
    private readonly Container _commentsContainer;

    private readonly BlobServiceClient _blobServiceClient;
    private readonly BlobContainerClient _screenshotContainerClient;

    [GeneratedRegex(@"#\w+")]
    private static partial Regex TagRegex();

    public PioneerService(ILogger<PioneerService> logger, IConfiguration configuration, IMemoryCache memoryCache)
    {
        _logger = logger;
        _memoryCache = memoryCache;

        var cosmosDbConnectionString = configuration["CosmosDbConnectionString"];
        var cosmosDbDatabaseName = configuration["CosmosDbDatabaseName"];

        var cosmosClient = new CosmosClientBuilder(cosmosDbConnectionString)
            .WithSerializerOptions(new CosmosSerializationOptions
            {
                PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase,
            })
            .Build();

        _cosmosDbDatabase = cosmosClient.CreateDatabaseIfNotExistsAsync(cosmosDbDatabaseName).Result;
        _submissionsContainer = _cosmosDbDatabase.CreateContainerIfNotExistsAsync("Submissions", "/author").Result;
        _profileContainer = _cosmosDbDatabase.CreateContainerIfNotExistsAsync("Profiles", "/id").Result;
        _pointsContainer = _cosmosDbDatabase.CreateContainerIfNotExistsAsync("Points", "/userId").Result;
        _commentsContainer = _cosmosDbDatabase.CreateContainerIfNotExistsAsync("Comments", "/id").Result;

        var blobStorageAccountName = configuration["BlobStorageAccountName"];
        var blobStorageAccountKey = configuration["BlobStorageAccountKey"];

        var connectionString =
            $"DefaultEndpointsProtocol=https;AccountName={blobStorageAccountName};AccountKey={blobStorageAccountKey};EndpointSuffix=core.windows.net";
        _blobServiceClient = new BlobServiceClient(connectionString);
        _screenshotContainerClient = _blobServiceClient.GetBlobContainerClient("screenshots");
    }

    public Product[] GetProducts()
    {
        // TODO -- pull those from DB
        return
        [
            new Product { Id = "M365", Label = "Microsoft 365" },
            new Product { Id = "Excel", Label = "Excel" },
            new Product { Id = "Loop", Label = "Loop" },
            new Product { Id = "PowerPoint", Label = "PowerPoint" },
            new Product { Id = "Teams", Label = "Teams" },
            new Product { Id = "Whiteboard", Label = "Whiteboard" },
            new Product { Id = "Word", Label = "Word" }
        ];
    }

    public async Task<Submission> CreateSubmission(Submission submission)
    {
        submission.Id = Guid.NewGuid().ToString();
        submission.CreatedDate = DateTime.Now;
        submission.LastModifiedDate = DateTime.Now;

        UpdateSubmissionTags(submission);

        await _submissionsContainer.CreateItemAsync(submission);

        // Award points if necessary.
        if (!await HasSubmittedToday(submission.Author))
        {
            await AwardPoints(submission.Author, PointType.Submission,  PointsPerSubmission);
        }

        return submission;
    }

    public async Task<Submission?> GetSubmissionById(string submissionId)
    {
        var sql = "SELECT * FROM Submissions s where s.id = @submissionId";

        var query = new QueryDefinition(sql)
            .WithParameter("@submissionId", submissionId);

        using var feedIterator = _submissionsContainer.GetItemQueryIterator<Submission>(query);

        while (feedIterator.HasMoreResults)
        {
            var submissions = await feedIterator.ReadNextAsync();

            foreach (var submission in submissions)
            {
                return submission;
            }
        }

        return null;
    }

    public async Task<List<Submission>> GetLatestSubmissions(int page, int count)
    {
        var sql = "SELECT * FROM Submissions s ORDER BY s.createdDate DESC OFFSET @offset LIMIT @limit";

        var query = new QueryDefinition(sql)
            .WithParameter("@offset", page * count)
            .WithParameter("@limit", count);

        using var feedIterator = _submissionsContainer.GetItemQueryIterator<Submission>(query);

        var submissions = new List<Submission>();

        while (feedIterator.HasMoreResults)
        {
            var results = await feedIterator.ReadNextAsync();
            submissions.AddRange(results);
        }

        return submissions;
    }

    private const int PageSize = 10;

    public async Task<List<Submission>> GetSubmissionsByFilter(string userId = "", string productFilter = "",
        string tagFilter = "", string sortBy = "", int pageNumber = 1, int pageSize = PageSize)
    {
        var query = _submissionsContainer.GetItemLinqQueryable<Submission>().AsQueryable();

        if (!string.IsNullOrWhiteSpace(userId))
        {
            query = query.Where(s => s.Author == userId);
        }

        if (!string.IsNullOrWhiteSpace(productFilter))
        {
            query = query.Where(s => s.Product == productFilter);
        }

        if (!string.IsNullOrWhiteSpace(tagFilter))
        {
            if (!tagFilter.StartsWith("#"))
            {
                tagFilter = "#" + tagFilter;
            }

            tagFilter = tagFilter.ToLower();

            query = query.Where(s => s.Tags.Contains(tagFilter));
        }

        if (sortBy == "oldest")
        {
            query = query.OrderBy(s => s.CreatedDate);
        }
        else
        {
            query = query.OrderByDescending(s => s.CreatedDate);
        }

        query = query.Skip(pageNumber * pageSize).Take(pageSize);

        using var feedIterator = query.ToFeedIterator();

        var submissions = new List<Submission>();

        while (feedIterator.HasMoreResults)
        {
            var results = await feedIterator.ReadNextAsync();
            submissions.AddRange(results);
        }

        return submissions;
    }

    public async Task UpdateSubmission(Submission submission)
    {
        submission.LastModifiedDate = DateTime.Now;

        UpdateSubmissionTags(submission);

        await _submissionsContainer.UpsertItemAsync(submission);
    }

    public async Task DeleteSubmission(Submission submission)
    {
        await _submissionsContainer.DeleteItemAsync<Submission>(submission.Id, new PartitionKey(submission.Author));
    }

    private static void UpdateSubmissionTags(Submission submission)
    {
        // Update tags
        var tags = new List<string>();

        var tagMatches = TagRegex().Matches(submission.Notes);

        foreach (Match match in tagMatches)
        {
            tags.Add(match.Value.ToLower());
        }

        submission.Tags = tags.ToArray();
    }

    public string GetProductName(string productId)
    {
        var products = GetProducts();
        var product = products.FirstOrDefault(p => p.Id == productId);

        return product?.Label ?? "[unknown product]";
    }

    public async Task<Profile> GetProfileOrDefault(string id)
    {
        return await GetProfile(id) ?? new Profile
        {
            Id = id,
            Points = 0,
        };
    }

    public async Task UpdateProfile(Profile profile)
    {
        await _profileContainer.UpsertItemAsync(profile);

        // Clear cached profile.
        _memoryCache.Remove($"profile_{profile.Id}");
    }

    public async Task<Profile?> GetProfile(string id)
    {
        var sql = "SELECT * FROM Profiles p where p.id = @id";

        var query = new QueryDefinition(sql)
            .WithParameter("@id", id);

        using var feedIterator = _profileContainer.GetItemQueryIterator<Profile>(query);

        while (feedIterator.HasMoreResults)
        {
            var profiles = await feedIterator.ReadNextAsync();

            foreach (var profile in profiles)
            {
                return profile;
            }
        }

        return null;
    }

    public async Task<Point> AwardPoints(string userId, PointType type, int points, string frame = "")
    {
        var profile = await GetProfileOrDefault(userId);
        profile.Points += points;

        await UpdateProfile(profile);

        var point = new Point
        {
            Id = Guid.NewGuid().ToString(),
            UserId = userId,
            Type = type,
            Amount = points,
            Frame = frame,
        };

        await _pointsContainer.CreateItemAsync(point);

        return point;
    }

    private async Task<bool> HasSubmittedToday(string userId)
    {
        var sql = "SELECT * FROM Points p where p.userId = @userId and p.dateCreated >= @dateCreated";

        var query = new QueryDefinition(sql)
            .WithParameter("@userId", userId)
            .WithParameter("@dateCreated", DateTime.Today);

        using var feedIterator = _pointsContainer.GetItemQueryIterator<Point>(query);

        while (feedIterator.HasMoreResults)
        {
            var points = await feedIterator.ReadNextAsync();

            if (points.Count != 0)
            {
                return true;
            }
        }

        return false;
    }

    public DateTime GetPreviousDayStartDate()
    {
        var date = DateTime.Today - TimeSpan.FromDays(1);

        // Skip weekends
        switch (date.DayOfWeek)
        {
            case DayOfWeek.Saturday:
                date -= TimeSpan.FromDays(1);
                break;
            case DayOfWeek.Sunday:
                date -= TimeSpan.FromDays(2);
                break;
        }

        return date;
    }

    public DateTime GetWeekStartDate()
    {
        var today = DateTime.Today;
        var daysUntilMonday = (int)today.DayOfWeek - (int)DayOfWeek.Monday;
        if (daysUntilMonday < 0)
        {
            daysUntilMonday += 7;
        }

        return today.AddDays(-daysUntilMonday);
    }

    public DateTime GetPreviousWeekStartDate()
    {
        return GetWeekStartDate() - TimeSpan.FromDays(7);
    }

    public async Task<bool> PointsAwarded(string userId, PointType type, string frame)
    {
        var sql =
            "SELECT p FROM Points p where p.userId = @userId and p.type = @type and p.frame = @frame";

        var query = new QueryDefinition(sql)
            .WithParameter("@userId", userId)
            .WithParameter("@type", type.ToString())
            .WithParameter("@frame", frame);

        using var feedIterator = _pointsContainer.GetItemQueryIterator<Point>(query);

        while (feedIterator.HasMoreResults)
        {
            var points = await feedIterator.ReadNextAsync();

            if (points.Any())
            {
                return true;
            }
            // if (points.Count != 0)
            // {
            //     return true;
            // }
        }

        return false;
    }

    public async Task<bool> PointsAwarded(string userId, PointType type, DateTime frame)
    {
        return await PointsAwarded(userId, type, frame.ToString("s"));
    }

    public async Task<bool> HasSubmittedDailyVote(string userId)
    {
        return await PointsAwarded(userId, PointType.DailyVote, GetPreviousDayStartDate());
    }

    public async Task<bool> HasSubmittedWeeklyVote(string userId)
    {
        return await PointsAwarded(userId, PointType.WeeklyVote, GetPreviousWeekStartDate());
    }

    public async Task<List<Profile>> GetProfiles()
    {
        using var feedIterator = _profileContainer.GetItemQueryIterator<Profile>();

        var profiles = new List<Profile>();

        while (feedIterator.HasMoreResults)
        {
            var results = await feedIterator.ReadNextAsync();
            profiles.AddRange(results);
        }

        return profiles;
    }

    public async Task<List<Submission>> GetWeeklyVoteCandidateSubmission()
    {
        var sql = "SELECT * FROM Submissions s where s.createdDate >= @startOfWeek and s.createdDate < @endOfWeek";

        var query = new QueryDefinition(sql)
            .WithParameter("@startOfWeek", GetPreviousWeekStartDate())
            .WithParameter("@endOfWeek", GetWeekStartDate());

        using var feedIterator = _submissionsContainer.GetItemQueryIterator<Submission>(query);

        var submissions = new List<Submission>();

        while (feedIterator.HasMoreResults)
        {
            var results = await feedIterator.ReadNextAsync();
            submissions.AddRange(results);
        }

        // Shuffle to the list
        var submissionsArray = submissions.ToArray();

        Random.Shared.Shuffle(submissionsArray);

        return submissionsArray.ToList();
    }

    public async Task<List<Submission>> GetDailyVoteCandidateSubmission()
    {
        var sql = "SELECT * FROM Submissions s where s.createdDate >= @yesterday and s.createdDate < @today";

        var query = new QueryDefinition(sql)
            .WithParameter("@yesterday", GetPreviousDayStartDate())
            .WithParameter("@today", DateTime.Today);

        using var feedIterator = _submissionsContainer.GetItemQueryIterator<Submission>(query);

        var submissions = new List<Submission>();

        while (feedIterator.HasMoreResults)
        {
            var results = await feedIterator.ReadNextAsync();
            submissions.AddRange(results);
        }

        // Shuffle to the list
        var submissionsArray = submissions.ToArray();

        Random.Shared.Shuffle(submissionsArray);

        return submissionsArray.ToList();
    }

    public async Task<Candidate> GetVoteCandidates(string userId)
    {
        var candidate = new Candidate
        {
            DailyVoteCast = await HasSubmittedDailyVote(userId),
            WeeklyVoteCast = await HasSubmittedWeeklyVote(userId),
        };

        if (!candidate.DailyVoteCast)
        {
            candidate.DailySubmissions = await GetDailyVoteCandidateSubmission();
        }

        if (!candidate.WeeklyVoteCast)
        {
            candidate.WeeklySubmissions = await GetWeeklyVoteCandidateSubmission();
        }

        return candidate;
    }

    public async Task CastDailyVote(string userId, string submissionId)
    {
        if (!await HasSubmittedDailyVote(userId))
        {
            var submission = await GetSubmissionById(submissionId);

            if (submission == null)
            {
                // Ignore...
                return;
            }

            submission.WeeklyVotes++;

            await UpdateSubmission(submission);

            await AwardPoints(userId, PointType.DailyVote, PointsPerDailyVoteCast, GetPreviousDayStartDate().ToString("s"));
            await AwardPoints(submission.Author, PointType.DailyVote, PointsPerDailyVoteReceived, GetPreviousDayStartDate().ToString("s"));
        }
    }

    public async Task CastWeeklyVote(string userId, string submissionId)
    {
        if (!await HasSubmittedWeeklyVote(userId))
        {
            var submission = await GetSubmissionById(submissionId);

            if (submission == null)
            {
                // Ignore...
                return;
            }

            submission.WeeklyVotes++;

            await UpdateSubmission(submission);

            await AwardPoints(userId, PointType.WeeklyVote, PointsPerWeeklyVoteCast, GetPreviousWeekStartDate().ToString("s"));
            await AwardPoints(submission.Author, PointType.WeeklyVote, PointsPerWeeklyVoteReceived, GetPreviousWeekStartDate().ToString("s"));
        }
    }
    
    public async Task<string> AddCommentToSubmission(string submissionId, Comment comment)
    {
        var submission = await GetSubmissionById(submissionId);

        if (submission == null)
        {
            // Ignore
            return "";
        }
        
        comment.Id = Guid.NewGuid().ToString();
        comment.CreatedDate = DateTime.Now;

        submission.Comments = [..submission.Comments, comment];

        await UpdateSubmission(submission);

        return comment.Id;
    }
}