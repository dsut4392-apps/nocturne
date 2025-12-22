using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nocturne.Connectors.Configurations;
using Nocturne.Connectors.Core.Interfaces;
using Nocturne.Connectors.Core.Models;
using Nocturne.Connectors.Core.Services;
using Nocturne.Connectors.MyFitnessPal.Models;
using Nocturne.Core.Models;
using Nocturne.Core.Constants;

namespace Nocturne.Connectors.MyFitnessPal.Services;

/// <summary>
/// Connector service for MyFitnessPal food diary data
/// Supports both direct integration via dependency injection and HTTP fallback
/// </summary>
public class MyFitnessPalConnectorService : BaseConnectorService<MyFitnessPalConnectorConfiguration>
{
    private readonly MyFitnessPalConnectorConfiguration _config;
    private readonly Func<
        IEnumerable<Food>,
        CancellationToken,
        Task<IEnumerable<Food>>
    >? _createFoodAsync;
    private const string MyFitnessPalApiUrl =
        "https://www.myfitnesspal.com/api/services/authenticate_diary_key";

    /// <summary>
    /// Gets the connector source identifier
    /// </summary>
    public override string ConnectorSource => DataSources.MyFitnessPalConnector;

    /// <summary>
    /// Gets the service name for this connector
    /// </summary>
    public override string ServiceName => "MyFitnessPal";

    public override List<SyncDataType> SupportedDataTypes => new() { SyncDataType.Food };

    /// <summary>
    /// Fetches food data from MyFitnessPal
    /// </summary>
    protected override async Task<IEnumerable<Food>> FetchFoodsAsync(DateTime? from = null, DateTime? to = null)
    {
        var username = _config.MyFitnessPalUsername;
        if (string.IsNullOrWhiteSpace(username))
        {
            _logger.LogWarning("Cannot fetch MyFitnessPal data - Username not configured");
            return Enumerable.Empty<Food>();
        }

        var diaryResponse = await FetchDiaryAsync(username, from, to);
        return ConvertToNightscoutFoods(diaryResponse);
    }

    /// <summary>
    /// Initializes a new instance of the MyFitnessPalConnectorService
    /// </summary>
    /// <param name="config">Connector configuration</param>
    /// <param name="logger">Logger instance</param>
    /// <param name="httpClient">HTTP client instance</param>
    /// <param name="createFoodAsync">Optional delegate for creating food records directly</param>
    public MyFitnessPalConnectorService(
        HttpClient httpClient,
        IOptions<MyFitnessPalConnectorConfiguration> config,
        ILogger<MyFitnessPalConnectorService> logger,
        IApiDataSubmitter? apiDataSubmitter = null,
        IConnectorMetricsTracker? metricsTracker = null,
        Func<IEnumerable<Food>, CancellationToken, Task<IEnumerable<Food>>>? createFoodAsync = null,
        IConnectorStateService? stateService = null
    )
        : base(httpClient, logger, apiDataSubmitter, metricsTracker, stateService)
    {
        _config = config?.Value ?? throw new ArgumentNullException(nameof(config));
        _createFoodAsync = createFoodAsync;
    }

    /// <summary>
    /// Authenticates with MyFitnessPal (not required for public diary access)
    /// </summary>
    /// <returns>Always returns true as no authentication is required</returns>
    public override async Task<bool> AuthenticateAsync()
    {
        // MyFitnessPal diary API doesn't require authentication for public diaries
        _logger.LogInformation("MyFitnessPal connector authenticated (no auth required)");
        return await Task.FromResult(true);
    }

    /// <summary>
    /// Fetches diary data from MyFitnessPal for a specific username and date range
    /// </summary>
    /// <param name="username">MyFitnessPal username</param>
    /// <param name="fromDate">Start date for diary data (optional, defaults to today)</param>
    /// <param name="toDate">End date for diary data (optional, defaults to fromDate)</param>
    /// <returns>Diary response containing food entries</returns>
    public async Task<DiaryResponse> FetchDiaryAsync(
        string username,
        DateTime? fromDate = null,
        DateTime? toDate = null
    )
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new ArgumentException("Username cannot be null or empty", nameof(username));
        }

        var from = fromDate ?? DateTime.Today;
        var to = toDate ?? from;

        // MyFitnessPal uses YYYY-MM-DD format
        var fromFormatted = from.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var toFormatted = to.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        _logger.LogInformation(
            "Fetching MyFitnessPal diary for user {Username} from {FromDate} to {ToDate}",
            username,
            fromFormatted,
            toFormatted
        );

        var requestBody = new
        {
            username,
            from = fromFormatted,
            to = toFormatted,
            show_food_diary = 1,
            show_food_notes = 1,
            show_exercise_diary = 0,
            show_exercise_notes = 0,
        };

        var jsonBody = JsonSerializer.Serialize(requestBody);

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, MyFitnessPalApiUrl)
            {
                Content = new StringContent(jsonBody, Encoding.UTF8, "application/json"),
            };

            using var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var diaryResponse = JsonSerializer.Deserialize<DiaryResponse>(responseContent);

            if (diaryResponse == null)
            {
                throw new InvalidOperationException(
                    "Failed to deserialize MyFitnessPal diary response"
                );
            }

            _logger.LogInformation(
                "[{ConnectorSource}] Successfully fetched MyFitnessPal diary data with {EntryCount} diary entries",
                ConnectorSource,
                diaryResponse.Count
            );

            return diaryResponse;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(
                ex,
                "HTTP error while fetching MyFitnessPal diary for user {Username}",
                username
            );
            throw new InvalidOperationException(
                $"Failed to fetch MyFitnessPal diary: {ex.Message}",
                ex
            );
        }
        catch (JsonException ex)
        {
            _logger.LogError(
                ex,
                "JSON deserialization error while processing MyFitnessPal diary response"
            );
            throw new InvalidOperationException(
                $"Failed to parse MyFitnessPal diary response: {ex.Message}",
                ex
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error while fetching MyFitnessPal diary for user {Username}",
                username
            );
            throw;
        }
    }

    /// <summary>
    /// Converts MyFitnessPal food entries to Nightscout food entries
    /// </summary>
    /// <param name="diaryResponse">MyFitnessPal diary response</param>
    /// <returns>Collection of Nightscout food entries</returns>
    public IEnumerable<Food> ConvertToNightscoutFoods(DiaryResponse diaryResponse)
    {
        var foods = new List<Food>();

        foreach (var diaryEntry in diaryResponse)
        {
            foreach (var foodEntry in diaryEntry.FoodEntries)
            {
                var food = new Food
                {
                    Type = "food",
                    Name = foodEntry.MealName,
                    Portion = foodEntry.Servings,
                    Unit = foodEntry.ServingSize.Unit,
                    Carbs = foodEntry.NutritionalContents.Carbohydrates ?? 0,
                    Protein = foodEntry.NutritionalContents.Protein ?? 0,
                    Fat = foodEntry.NutritionalContents.Fat ?? 0,
                    Energy = (int)(foodEntry.NutritionalContents.Energy?.Value ?? 0),
                };

                foods.Add(food);
            }
        }

        return foods;
    }

    /// <summary>
    /// Fetches glucose data (not applicable for MyFitnessPal)
    /// </summary>
    /// <param name="since">Since timestamp (ignored)</param>
    /// <returns>Empty collection as MyFitnessPal doesn't provide glucose data</returns>
    public override async Task<IEnumerable<Entry>> FetchGlucoseDataAsync(DateTime? since = null)
    {
        _logger.LogWarning(
            "FetchGlucoseDataAsync called on MyFitnessPal connector - MyFitnessPal doesn't provide glucose data"
        );
        return await Task.FromResult(Enumerable.Empty<Entry>());
    }


    /// <summary>
    /// Compute SHA1 hash for API secret authentication
    /// </summary>
    /// <param name="input">Input string to hash</param>
    /// <returns>SHA1 hash as lowercase hex string</returns>
    private static string ComputeSha1Hash(string input)
    {
        using var sha1 = System.Security.Cryptography.SHA1.Create();
        var inputBytes = Encoding.UTF8.GetBytes(input);
        var hashBytes = sha1.ComputeHash(inputBytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    /// <summary>
    /// Disposes the connector service
    /// </summary>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            // HttpClient is managed by the base class
        }
        base.Dispose(disposing);
    }

}
