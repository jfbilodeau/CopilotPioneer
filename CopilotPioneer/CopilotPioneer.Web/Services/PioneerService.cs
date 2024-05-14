using System.Text.RegularExpressions;
using Azure.Storage.Blobs;
using CopilotPioneer.Web.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Fluent;
using Microsoft.Azure.Cosmos.Linq;

namespace CopilotPioneer.Web.Services;

public class Product
{
    public string Id { get; set; } = "";
    public string Label { get; set; } = "";
}

public partial class PioneerService
{
    private readonly Database _cosmosDbDatabase;
    private readonly Container _submissionsContainer;
    private readonly Container _profileContainer;
    
    private readonly BlobServiceClient _blobServiceClient;
    private readonly BlobContainerClient _screenshotContainerClient;
    
    [GeneratedRegex(@"#\w+")]
    private static partial Regex TagRegex();
    
    public PioneerService(IConfiguration configuration)
    {
        var cosmosDbConnectionString = configuration["CosmosDbConnectionString"];
        var cosmosDbDatabaseName = configuration["CosmosDbDatabaseName"];

        var cosmosClient = new CosmosClientBuilder(cosmosDbConnectionString)
            .WithSerializerOptions(new CosmosSerializationOptions
            {
                PropertyNamingPolicy = CosmosPropertyNamingPolicy.CamelCase
            })
            .Build();
        
        _cosmosDbDatabase = cosmosClient.CreateDatabaseIfNotExistsAsync(cosmosDbDatabaseName).Result;
        _submissionsContainer = _cosmosDbDatabase.CreateContainerIfNotExistsAsync("Submissions", "/author").Result;
        _profileContainer = _cosmosDbDatabase.CreateContainerIfNotExistsAsync("Profiles", "/id").Result;
        
        var blobStorageAccountName = configuration["BlobStorageAccountName"];
        var blobStorageAccountKey = configuration["BlobStorageAccountKey"];

        var connectionString = $"DefaultEndpointsProtocol=https;AccountName={blobStorageAccountName};AccountKey={blobStorageAccountKey};EndpointSuffix=core.windows.net";
        _blobServiceClient = new BlobServiceClient(connectionString);
        _screenshotContainerClient = _blobServiceClient.GetBlobContainerClient("screenshots");
    }
    
    public async Task<Submission> SaveSubmission(string submitter, Submission submission)
    {
        submission.Id = Guid.NewGuid().ToString();
        submission.CreatedDate = DateTime.Now;
        submission.LastModifiedDate = DateTime.Now;
        submission.Author = submitter;
        
        // Extract tags from the submission
        var tags = new List<string>();
        
        var tagMatches = TagRegex().Matches(submission.Notes);
        
        foreach (Match match in tagMatches)
        {
            tags.Add(match.Value);
        }
        
        submission.Tags = tags.ToArray();
        
        await _submissionsContainer.CreateItemAsync(submission);

        return submission;
    }

    public async Task<Submission?> GetSubmissionById(string submissionId)
    {
        var sql = "SELECT * FROM Submissions s where s.id = @submissionId";

        var query = new QueryDefinition(sql)
            .WithParameter("@submissionId", submissionId);

        var feedIterator = _submissionsContainer.GetItemQueryIterator<Submission>(query);

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

    public async Task<List<Submission>> GetLatestSubmissions(int page, int count)
    {
        var sql = "SELECT * FROM Submissions s ORDER BY s.createdDate DESC OFFSET @offset LIMIT @limit";
        
        var query = new QueryDefinition(sql)
            .WithParameter("@offset", page * count)
            .WithParameter("@limit", count);
        
        var feedIterator = _submissionsContainer.GetItemQueryIterator<Submission>(query);
        
        var submissions = new List<Submission>();
        
        while (feedIterator.HasMoreResults)
        {
            var results = await feedIterator.ReadNextAsync();
            submissions.AddRange(results);
        }
        
        return submissions;
    }

    private const int PageSize = 10;

    public async Task<List<Submission>> GetSubmissionsByFilter(string userId = "", string productFilter = "", string tagFilter = "", string sortBy = "", int pageNumber = 1, int pageSize = PageSize)
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
        
        var feedIterator = query.ToFeedIterator();
        
        var submissions = new List<Submission>();
        
        while (feedIterator.HasMoreResults)
        {
            var results = await feedIterator.ReadNextAsync();
            submissions.AddRange(results);
        }
        
        return submissions;
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
            Points = 10
        };
    }

    public async Task UpdateProfile(Profile profile)
    {
        await _profileContainer.UpsertItemAsync(profile);
    }

    public async Task<Profile?> GetProfile(string id)
    {
        var sql = "SELECT * FROM Profiles p where p.id = @id";

        var query = new QueryDefinition(sql)
            .WithParameter("@id", id);

        var feedIterator = _profileContainer.GetItemQueryIterator<Profile>(query);

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
    
    public async void AddPoints(string userId, int points)
    {
        var profile = await GetProfileOrDefault(userId);
        profile.Points += points;
        
        await UpdateProfile(profile);
    }
}