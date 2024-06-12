using System.Text.RegularExpressions;
using Azure;
using Azure.Communication.Email;
using Azure.Storage.Blobs;
using CopilotPioneer.Web.Models;
using CopilotPioneer.Web.Pages;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Fluent;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using SkiaSharp;
using Point = CopilotPioneer.Web.Models.Point;

namespace CopilotPioneer.Web.Services;

public class Product
{
    public string Id { get; set; } = "";
    public string Label { get; set; } = "";
}

public partial class PioneerService
{
    // For now, hardcode points here.
    private const int PointsPerSubmission = 3;
    private const int PointsPerDailyVoteReceived = 1;
    private const int PointsPerDailyVoteCast = 1;
    private const int PointsPerWeeklyVoteCast = 2;
    private const int PointsPerWeeklyVoteReceived = 2;

    // Screenshot size names
    private const string ScreenShotSizeOriginal = "original";
    private const string ScreenShotSizeHero = "hero";
    private const string ScreenShotSizeThumbnail = "thumbnail";

    // Path to placeholder images
    const string PlaceholderDocumentHeroBlobName = "placeholder/document-hero.png";
    const string PlaceholderDocumentThumbnailBlobName = "placeholder/document-thumbnail.png";

    private readonly ILogger<PioneerService> _logger;
    private readonly IMemoryCache _memoryCache;

    private readonly Container _submissionsContainer;
    private readonly Container _profileContainer;
    private readonly Container _pointsContainer;

    private readonly BlobContainerClient _screenshotContainerClient;

    private readonly EmailClient _emailClient;

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

        Database cosmosDbDatabase = cosmosClient.CreateDatabaseIfNotExistsAsync(cosmosDbDatabaseName).Result;
        _submissionsContainer = cosmosDbDatabase.CreateContainerIfNotExistsAsync("Submissions", "/author").Result;
        _profileContainer = cosmosDbDatabase.CreateContainerIfNotExistsAsync("Profiles", "/id").Result;
        _pointsContainer = cosmosDbDatabase.CreateContainerIfNotExistsAsync("Points", "/userId").Result;

        var blobStorageAccountName = configuration["BlobStorageAccountName"];
        var blobStorageAccountKey = configuration["BlobStorageAccountKey"];

        var connectionString =
            $"DefaultEndpointsProtocol=https;AccountName={blobStorageAccountName};AccountKey={blobStorageAccountKey};EndpointSuffix=core.windows.net";
        var blobServiceClient = new BlobServiceClient(connectionString);
        _screenshotContainerClient = blobServiceClient.GetBlobContainerClient("screenshots");
        _screenshotContainerClient.CreateIfNotExists();

        var emailClientConnectionString = configuration["EmailClientConnectionString"];
        _emailClient = new EmailClient(emailClientConnectionString);

        // Initialize document placeholder images
        _screenshotContainerClient
            .GetBlobClient(PlaceholderDocumentHeroBlobName)
            .Upload(
                File.OpenRead("wwwroot/images/placeholder/document-hero.png"),
                true
            );
        _screenshotContainerClient
            .GetBlobClient(PlaceholderDocumentThumbnailBlobName)
            .Upload(
                File.OpenRead("wwwroot/images/placeholder/document-thumbnail.png"),
                true
            );
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

    private SKImage ResizeImage(SKImage image, int targetWidth, int targetHeight)
    {
        var width = image.Width;
        var height = image.Height;

        var aspectRatio = (float)width / height;

        var targetAspectRatio = (float)targetWidth / targetHeight;

        var scale = aspectRatio > targetAspectRatio
            ? (float)targetWidth / width
            : (float)targetHeight / height;

        var scaledWidth = (int)(width * scale);
        var scaledHeight = (int)(height * scale);

        var targetImageInfo = new SKImageInfo(scaledWidth, scaledHeight);
        using var surface = SKSurface.Create(targetImageInfo);

        var canvas = surface.Canvas;

        canvas.Clear(SKColors.Transparent);

        canvas.Scale(scale);

        canvas.DrawImage(image, 0, 0, new SKPaint { FilterQuality = SKFilterQuality.High, IsAntialias = true });

        canvas.Flush();

        var targetImage = surface.Snapshot();

        return targetImage;
    }

    private async Task<Screenshot?> CreateScreenshot(string submissionId,
        ScreenshotSubmissionModel screenshotSubmissionModel)
    {
        var screenshotId = Guid.NewGuid().ToString();

        var screenshot = new Screenshot
        {
            Id = screenshotId,
            SubmissionId = submissionId,
            AltText = screenshotSubmissionModel.AltText ?? string.Empty,
            OriginalName = $"submissions/{submissionId}/{screenshotId}/{ScreenShotSizeOriginal}.png",
            HeroName = $"submissions/{submissionId}/{screenshotId}/{ScreenShotSizeHero}.png",
            ThumbnailName = $"submissions/{submissionId}/{screenshotId}/{ScreenShotSizeThumbnail}.png",
        };

        // Resize image to hero and thumbnail sizes
        await using var stream = screenshotSubmissionModel.File!.OpenReadStream();

        using var image = SKImage.FromEncodedData(stream);

        if (image != null)
        {
            using var heroImage = ResizeImage(image, 400, 400);
            using var thumbnailImage = ResizeImage(image, 100, 100);

            // Save images
            var fullSizeBlobClient = _screenshotContainerClient.GetBlobClient(screenshot.OriginalName);
            await fullSizeBlobClient.UploadAsync(image.Encode().AsStream(), true);

            var heroBlobClient = _screenshotContainerClient.GetBlobClient(screenshot.HeroName);
            await heroBlobClient.UploadAsync(heroImage.Encode().AsStream(), true);

            var thumbnailBlobClient = _screenshotContainerClient.GetBlobClient(screenshot.ThumbnailName);
            await thumbnailBlobClient.UploadAsync(thumbnailImage.Encode().AsStream(), true);
        }
        else
        {
            // Set thumbnail and hero image to placeholder.
            screenshot.OriginalName =
                $"submissions/{submissionId}/{screenshotId}/{screenshotSubmissionModel.File!.FileName}";
            screenshot.HeroName = PlaceholderDocumentHeroBlobName;
            screenshot.ThumbnailName = PlaceholderDocumentThumbnailBlobName;

            // Could not read image. Upload it as a raw document.
            var documentBlobClient = _screenshotContainerClient.GetBlobClient(screenshot.OriginalName);
            await documentBlobClient.UploadAsync(screenshotSubmissionModel.File!.OpenReadStream(), true);

            // Mark as document.
            screenshot.IsDocument = true;
        }

        return screenshot;
    }

    public async Task<Screenshot?> GetScreenshot(string submissionId, string screenshotId)
    {
        var submission = await GetSubmissionById(submissionId);

        var screenshot = submission?.Screenshots.FirstOrDefault(s => s.Id == screenshotId);

        return screenshot;
    }

    public async Task<Stream?> GetScreenshotStream(Screenshot screenshot, string size)
    {
        string path;

        switch (size)
        {
            case ScreenShotSizeOriginal:
                path = screenshot.OriginalName;
                break;

            case ScreenShotSizeHero:
                path = screenshot.HeroName;
                break;

            case ScreenShotSizeThumbnail:
                path = screenshot.ThumbnailName;
                break;

            default:
                return null;
        }

        var blobClient = _screenshotContainerClient.GetBlobClient(path);
        var stream = await blobClient.OpenReadAsync();

        return stream;
    }

    public async Task<Submission> CreateSubmission(Submission submission, ScreenshotSubmissionModel[] screenshots)
    {
        submission.Id = Guid.NewGuid().ToString();
        submission.CreatedDate = DateTime.Now;
        submission.LastModifiedDate = DateTime.Now;

        UpdateSubmissionTags(submission);

        foreach (var screenshot in screenshots)
        {
            var screenshotModel = await CreateScreenshot(submission.Id, screenshot);

            if (screenshotModel != null)
            {
                submission.Screenshots = [..submission.Screenshots, screenshotModel];
            }
            else
            {
                _logger.LogWarning("Could not create screenshot for submission {SubmissionId} ", submission.Id);
            }
        }

        await _submissionsContainer.CreateItemAsync(submission);

        // Award points if necessary.
        if (!await HasSubmittedToday(submission.Author))
        {
            await AwardPoints(submission.Author, PointType.Submission, PointsPerSubmission, submission.Id);
        }

        return submission;
    }

    public async Task<Submission?> GetSubmissionById(string submissionId)
    {
        var sql = "select * from Submissions s where s.id = @submissionId";

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
        var sql = "select * from Submissions s ORDER BY s.createdDate DESC OFFSET @offset LIMIT @limit";

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

    public async Task<List<Submission>> GetSubmissionsByFilter(
        string userId = "",
        string productFilter = "",
        string tagFilter = "",
        bool dailyWinner = false,
        bool weeklyWinner = false,
        string sortBy = "",
        int pageNumber = 1,
        int pageSize = PageSize
    )
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

        if (dailyWinner)
        {
            query = query.Where(s => s.DailyVoteWinner);
        }

        if (weeklyWinner)
        {
            query = query.Where(s => s.WeeklyVoteWinner);
        }

        switch (sortBy)
        {
            case "dailyVotes":
                query = query.OrderByDescending(s => s.DailyVotes);
                break;
            case "weeklyVotes":
                query = query.OrderByDescending(s => s.WeeklyVotes);
                break;
            case "oldest":
                query = query.OrderBy(s => s.CreatedDate);
                break;
            default:
                query = query.OrderByDescending(s => s.CreatedDate);
                break;
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
        // Delete screenshots
        foreach (var screenshot in submission.Screenshots)
        {
            var originalBlobClient = _screenshotContainerClient.GetBlobClient(screenshot.OriginalName);
            await originalBlobClient.DeleteIfExistsAsync();

            var heroBlobClient = _screenshotContainerClient.GetBlobClient(screenshot.HeroName);
            await heroBlobClient.DeleteIfExistsAsync();

            var thumbnailBlobClient = _screenshotContainerClient.GetBlobClient(screenshot.ThumbnailName);
            await thumbnailBlobClient.DeleteIfExistsAsync();
        }

        // Delete actual subscription
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
        var sql = "select * from Profiles p where p.id = @id";

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

    public async Task<Point> AwardPoints(string userId, PointType type, int points, string frame = "",
        string tagId = "")
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
            TagId = tagId,
        };

        await _pointsContainer.CreateItemAsync(point);

        return point;
    }

    private async Task<bool> HasSubmittedToday(string userId)
    {
        var sql =
            "select * from Points p where p.userId = @userId and p.dateCreated >= @dateCreated and p.type = 'Submission'";

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

    public async Task<Point?> GetPoint(string userId, PointType type, string frame)
    {
        var sql = "select p from Points p where p.userId = @userId and p.type = @type and p.frame = @frame";

        var query = new QueryDefinition(sql)
            .WithParameter("@userId", userId)
            .WithParameter("@type", type.ToString())
            .WithParameter("@frame", frame);

        using var feedIterator = _pointsContainer.GetItemQueryIterator<Point>(query);

        while (feedIterator.HasMoreResults)
        {
            var points = await feedIterator.ReadNextAsync();

            foreach (var point in points)
            {
                return point;
            }
        }

        return null;
    }

    public async Task<Point?> GetPoint(string userId, PointType type, DateTime frame)
    {
        return await GetPoint(userId, type, frame.ToString("s"));
    }

    public async Task<bool> HasSubmittedDailyVote(string userId)
    {
        return await GetPoint(userId, PointType.DailyVote, GetPreviousDayStartDate()) != null;
    }

    public async Task<bool> HasSubmittedWeeklyVote(string userId)
    {
        return await GetPoint(userId, PointType.WeeklyVote, GetPreviousWeekStartDate()) != null;
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

    public async Task<List<Submission>> GetWeeklyVoteCandidateSubmission(string userIdToExclude)
    {
        _logger.LogInformation("GetPreviousWeekStartDate: {PreviousWeekStartDate}, GetWeekStartDate: {WeekStartDate}",
            GetPreviousWeekStartDate(), GetWeekStartDate());

        var sql =
            "select * from Submissions s where s.createdDate >= @startOfWeek and s.createdDate < @endOfWeek and s.author != @userIdToExclude";

        var query = new QueryDefinition(sql)
            .WithParameter("@startOfWeek", GetPreviousWeekStartDate())
            .WithParameter("@endOfWeek", GetWeekStartDate())
            .WithParameter("@userIdToExclude", userIdToExclude);

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

    public async Task<List<Submission>> GetDailyVoteCandidateSubmission(string userIdToExclude)
    {
        var sql =
            "select * from Submissions s where s.createdDate >= @yesterday and s.createdDate < @today and s.author != @userIdToExclude";

        var query = new QueryDefinition(sql)
            .WithParameter("@yesterday", GetPreviousDayStartDate())
            .WithParameter("@today", DateTime.Today)
            .WithParameter("@userIdToExclude", userIdToExclude);

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
            candidate.DailySubmissions = await GetDailyVoteCandidateSubmission(userId);
        }

        if (!candidate.WeeklyVoteCast)
        {
            candidate.WeeklySubmissions = await GetWeeklyVoteCandidateSubmission(userId);
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

            submission.DailyVotes++;

            await UpdateSubmission(submission);

            var frame = GetPreviousDayStartDate().ToString("s");
            
            await AwardPoints(
                userId, 
                PointType.DailyVote, 
                PointsPerDailyVoteCast,
                frame, 
                submissionId
            );
            
            await AwardPoints(
                submission.Author, 
                PointType.DailyVoteReceived, 
                PointsPerDailyVoteReceived,
                frame, 
                submissionId
            );
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

            var frame = GetPreviousWeekStartDate().ToString("s");
            
            await AwardPoints(
                userId, 
                PointType.WeeklyVote, 
                PointsPerWeeklyVoteCast,
                frame, 
                submissionId
            );
            
            await AwardPoints(
                submission.Author, 
                PointType.WeeklyVoteReceived, 
                PointsPerWeeklyVoteReceived,
                frame, 
                submissionId
            );
        }
    }

    public async Task<string> AddCommentToSubmission(string submissionId, Comment comment, IUrlHelper url)
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

        // Send email to submission author
        var pathToAuthorProfile = url.PageLink("ProfileView", values: new { id = submission.Author });
        var pathToSubmission = url.PageLink("SubmissionView", values: new { id = submission.Id }, fragment: comment.Id);

        var html = $"""
                    <h1>New comment on your submission</h1>
                    <p>Submission: <a href="{pathToSubmission}">{submission.Title}</a></p>
                    <p><a href="{pathToAuthorProfile}">{comment.Author}</a> has commented on your submission:<br/>
                    <code>{comment.Content}</code>
                    </p>
                    <p><small>This is an automated message. Do not reply.</small></p>
                    """;

        var emailContent = new EmailContent("New comment on your submission")
        {
            Html = html,
        };

        var message = new EmailMessage("DoNotReply@copilotpioneer.com", submission.Author, emailContent);

        var emailSendOperation = await _emailClient.SendAsync(WaitUntil.Completed, message);

        return comment.Id;
    }

    public async Task TallyVotes()
    {
        // Update daily votes as necessary.
        var latestDailyVoteDate = await GetLatestDailyVoteResultsDate();
        var startDate = latestDailyVoteDate.AddDays(1); // Move past last vote winner.
        var endDate = GetPreviousDayStartDate();

        if (startDate < endDate)
        {
            // Calculate votes for each day.
            var dayCount = (endDate - startDate).Days;

            for (var day = 0; day < dayCount; day++)
            {
                var voteDate = startDate.AddDays(day);
                await TallyDailyVotes(voteDate);
            }
        }

        // Update weekly votes as necessary.
        var latestWeeklyVoteDate = await GetLatestWeeklyVoteResultsDate();
        var startWeek = latestWeeklyVoteDate + TimeSpan.FromDays(7);  // Most one week past last vote winner.
        var endWeek = GetPreviousWeekStartDate();

        if (startWeek < endWeek)
        {
            // Calculate votes for each week.
            var weekCount = (endWeek - startWeek).Days / 7;

            for (var week = 0; week < weekCount; week++)
            {
                var voteDate = startWeek.AddDays(week * 7);
                await TallyWeeklyVotes(voteDate);
            }
        }
    }

    private async Task TallyDailyVotes(DateTime voteDate)
    {
        _logger.LogInformation("Tallying daily votes for {VoteDate}", voteDate);
        
        var sql = "select * from Submissions s where s.createdDate >= @startDate and s.createdDate < @endDate";

        var query = new QueryDefinition(sql)
            .WithParameter("@startDate", voteDate)
            .WithParameter("@endDate", voteDate.AddDays(1));

        using var feedIterator = _submissionsContainer.GetItemQueryIterator<Submission>(query);

        List<Submission> submissions = [];

        while (feedIterator.HasMoreResults)
        {
            var results = await feedIterator.ReadNextAsync();

            submissions.AddRange(results);
        }

        if (submissions.IsNullOrEmpty())
        {
            // No submissions to tally. Abort.
            return;
        }

        var mostVotes = submissions.Max(r => r.DailyVotes);

        if (mostVotes == 0)
        {
            // No votes cast. Abort.
            return;
        }

        foreach (var submission in submissions.Where(submission => submission.DailyVotes == mostVotes))
        {
            submission.DailyVoteWinner = true;
            await AwardPoints(submission.Author, PointType.DailyVoteWinner, 0, voteDate.ToString("s"), submission.Id);
            await UpdateSubmission(submission);
        }
    }

    private async Task TallyWeeklyVotes(DateTime voteDate)
    {
        _logger.LogInformation("Tallying weekly votes for {VoteDate}", voteDate);
        
        var sql = "select * from Submissions s where s.createdDate >= @startDate and s.createdDate < @endDate";

        var query = new QueryDefinition(sql)
            .WithParameter("@startDate", voteDate)
            .WithParameter("@endDate", voteDate.AddDays(7));

        using var feedIterator = _submissionsContainer.GetItemQueryIterator<Submission>(query);

        List<Submission> submissions = [];

        while (feedIterator.HasMoreResults)
        {
            var results = await feedIterator.ReadNextAsync();

            submissions.AddRange(results);
        }

        if (submissions.IsNullOrEmpty())
        {
            // No submissions to tally. Abort.
            return;
        }

        var mostVotes = submissions.Max(r => r.WeeklyVotes);

        if (mostVotes == 0)
        {
            // No votes cast. Abort.
            return;
        }

        foreach (var submission in submissions.Where(submission => submission.WeeklyVotes == mostVotes))
        {
            submission.WeeklyVoteWinner = true;
            await AwardPoints(submission.Author, PointType.WeeklyVoteWinner, 0, voteDate.ToString("s"), submission.Id);
            await UpdateSubmission(submission);
        }
    }

    private async Task<DateTime> GetLatestDailyVoteResultsDate()
    {
        var sql =
            "select value p.frame from Points p where p.type = @dailyVoteWinner order by p.frame desc offset 0 limit 1";

        var query = new QueryDefinition(sql)
            .WithParameter("@dailyVoteWinner", PointType.DailyVoteWinner.ToString());

        using var feedIterator = _pointsContainer.GetItemQueryIterator<DateTime>(query);

        var latestDailyVoteWinner = DateTime.Parse("2024-05-15"); // Date Copilot Pioneer was launched.

        while (feedIterator.HasMoreResults)
        {
            var frames = await feedIterator.ReadNextAsync();

            foreach (var frame in frames)
            {
                latestDailyVoteWinner = frame;
            }
        }

        return latestDailyVoteWinner;
    }

    private async Task<DateTime> GetLatestWeeklyVoteResultsDate()
    {
        var sql =
            "select value p.frame from Points p where p.type = @weeklyVoteWinner order by p.frame desc offset 0 limit 1";

        var query = new QueryDefinition(sql)
            .WithParameter("@weeklyVoteWinner", PointType.WeeklyVoteWinner.ToString());

        using var feedIterator = _pointsContainer.GetItemQueryIterator<DateTime>(query);

        var latestWeeklyVoteWinner = DateTime.Parse("2024-05-13"); // Week Copilot Pioneer was launched.

        while (feedIterator.HasMoreResults)
        {
            var frames = await feedIterator.ReadNextAsync();

            foreach (var frame in frames)
            {
                latestWeeklyVoteWinner = frame;
            }
        }

        return latestWeeklyVoteWinner;
    }

    private async Task<List<WinnerProfile>> GetVoteWinner(PointType type, string frame)
    {
        var sql = "select p.userId, p.frame, p.tagId from Points p where p.type = @type and p.frame = @frame";

        var query = new QueryDefinition(sql)
            .WithParameter("@type", type.ToString())
            .WithParameter("@frame", frame);

        using var feedIterator = _pointsContainer.GetItemQueryIterator<Point>(query);

        List<WinnerProfile> winners = [];

        while (feedIterator.HasMoreResults)
        {
            var points = await feedIterator.ReadNextAsync();

            foreach (var point in points)
            {
                // Get profile
                var profile = await GetProfileOrDefault(point.UserId);

                // Get submission
                var submission = await GetSubmissionById(point.TagId);

                var winner = new WinnerProfile
                {
                    Date = DateTime.Parse(point.Frame),
                    Profile = profile,
                    Submission = submission,
                };

                winners.Add(winner);
            }
        }

        return winners;
    }

    public async Task<VoteWinners> GetVoteWinners()
    {
        // Make sure votes are tallied
        await TallyVotes();
        
        var dailyVoteFrame = GetPreviousDayStartDate().AddDays(-1).ToString("s");
        _logger.LogInformation("Fetching daily vote winner for frame {DailyVoteFrame}", dailyVoteFrame);
        var dailyWinner = await GetVoteWinner(PointType.DailyVoteWinner, dailyVoteFrame);

        var weeklyVoteFrame = GetPreviousWeekStartDate().AddDays(-7).ToString("s");
        _logger.LogInformation("Fetching weekly vote winner for frame {WeeklyVoteFrame}", weeklyVoteFrame);
        var weeklyWinner = await GetVoteWinner(PointType.WeeklyVoteWinner, weeklyVoteFrame);

        return new VoteWinners
        {
            DailyWinners = dailyWinner,
            WeeklyWinners = weeklyWinner,
        };
    }
}