using System.Text.Json;
using System.Text.Json.Serialization;
using log4net;
using System.Net.Http;

namespace MindFlayer.saas;

public static class OpenRouterModelRepository
{
    private static readonly ILog Log = LogManager.GetLogger(typeof(OpenRouterModelRepository));
    private static readonly HttpClient HttpClient = new();
    private static readonly SemaphoreSlim CacheLock = new(1, 1);
    
    private static HashSet<string>? _cachedModelIds;
    private static DateTime _lastCacheUpdate = DateTime.MinValue;
    private static readonly TimeSpan CacheExpiry = TimeSpan.FromHours(1);

    public static async Task<bool> IsOpenRouterModelAsync(string modelId)
    {
        await EnsureCacheLoadedAsync();
        return _cachedModelIds?.Contains(modelId) ?? false;
    }

    public static bool IsOpenRouterModel(string modelId)
    {
        // Quick check without loading - for factory use
        return _cachedModelIds?.Contains(modelId) ?? false;
    }

    public static async Task<HashSet<string>> GetModelIdsAsync()
    {
        await EnsureCacheLoadedAsync();
        return _cachedModelIds ?? new HashSet<string>();
    }

    public static HashSet<string> GetModelIds()
    {
        // Return cached models if available, empty set otherwise
        return _cachedModelIds ?? new HashSet<string>();
    }

    private static async Task EnsureCacheLoadedAsync()
    {
        await CacheLock.WaitAsync();
        try
        {
            if (_cachedModelIds == null || DateTime.UtcNow - _lastCacheUpdate > CacheExpiry)
            {
                await LoadModelsAsync();
            }
        }
        finally
        {
            CacheLock.Release();
        }
    }

    private static async Task LoadModelsAsync()
    {
        try
        {
            Log.Info("Loading OpenRouter models from API...");
            
            var response = await HttpClient.GetAsync("https://openrouter.ai/api/v1/models");
            response.EnsureSuccessStatusCode();
            
            var jsonContent = await response.Content.ReadAsStringAsync();
            var apiResponse = JsonSerializer.Deserialize<OpenRouterApiResponse>(jsonContent);
            
            if (apiResponse?.Data != null)
            {
                _cachedModelIds = apiResponse.Data.Select(m => m.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
                _lastCacheUpdate = DateTime.UtcNow;
                Log.Info($"Successfully loaded {_cachedModelIds.Count} OpenRouter models");
            }
            else
            {
                Log.Warn("OpenRouter API response was null or invalid");
                _cachedModelIds = new HashSet<string>();
            }
        }
        catch (Exception ex)
        {
            Log.Error("Failed to load OpenRouter models", ex);
            _cachedModelIds = new HashSet<string>();
        }
    }

    public static async Task ClearCacheAsync()
    {
        await CacheLock.WaitAsync();
        try
        {
            _cachedModelIds = null;
            _lastCacheUpdate = DateTime.MinValue;
        }
        finally
        {
            CacheLock.Release();
        }
    }
}

public class OpenRouterApiResponse
{
    [JsonPropertyName("data")]
    public List<OpenRouterModel>? Data { get; set; }
}

public class OpenRouterModel
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
    
    [JsonPropertyName("context_length")]
    public int Context_Length { get; set; }
    
    [JsonPropertyName("pricing")]
    public OpenRouterPricing? Pricing { get; set; }
    
    [JsonPropertyName("supported_parameters")]
    public string[] Supported_Parameters { get; set; } = Array.Empty<string>();
}

public class OpenRouterPricing
{
    [JsonPropertyName("prompt")]
    public string Prompt { get; set; } = string.Empty;
    
    [JsonPropertyName("completion")]
    public string Completion { get; set; } = string.Empty;
    
    [JsonPropertyName("request")]
    public string Request { get; set; } = string.Empty;
    
    [JsonPropertyName("image")]
    public string Image { get; set; } = string.Empty;
}