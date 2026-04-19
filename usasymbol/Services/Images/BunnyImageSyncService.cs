using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Options;
using USASymbol.Options;

namespace USASymbol.Services.Images;

public sealed class BunnyImageSyncService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly BunnyOptions _options;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<BunnyImageSyncService> _logger;

    public BunnyImageSyncService(
        HttpClient httpClient,
        IOptions<BunnyOptions> options,
        IWebHostEnvironment environment,
        ILogger<BunnyImageSyncService> logger)
    {
        _httpClient = httpClient;
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<BunnyImageSyncResult> SyncAsync(CancellationToken cancellationToken)
    {
        ValidateConfiguration();

        var result = new BunnyImageSyncResult();
        var localImagesRoot = Path.Combine(_environment.WebRootPath ?? "wwwroot", "images");

        if (!Directory.Exists(localImagesRoot))
        {
            throw new DirectoryNotFoundException($"Local images folder was not found: {localImagesRoot}");
        }

        var files = Directory
            .EnumerateFiles(localImagesRoot, "*", SearchOption.AllDirectories)
            .Where(path => !IsIgnoredFile(path))
            .ToList();
        var remoteFiles = await GetRemoteFilesIndexAsync(cancellationToken);

        _logger.LogInformation(
            "Starting Bunny sync for {Count} local files from {Path}. Remote index contains {RemoteCount} files.",
            files.Count,
            localImagesRoot,
            remoteFiles.Count);

        foreach (var filePath in files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var remotePath = BuildRemotePath(localImagesRoot, filePath);
            var localFileInfo = new FileInfo(filePath);

            if (remoteFiles.TryGetValue(remotePath, out var remoteFile) && !ShouldUpload(localFileInfo, remoteFile))
            {
                result.Skipped++;
                _logger.LogDebug("Skipped unchanged file {RemotePath}", remotePath);
                continue;
            }

            try
            {
                await UploadFileAsync(filePath, remotePath, cancellationToken);
                result.Uploaded++;
                _logger.LogInformation("Uploaded {RemotePath}", remotePath);
            }
            catch (Exception ex)
            {
                result.Failed++;
                _logger.LogError(ex, "Failed to upload {RemotePath}", remotePath);
            }
        }

        return result;
    }

    private async Task<Dictionary<string, BunnyStorageObject>> GetRemoteFilesIndexAsync(CancellationToken cancellationToken)
    {
        var result = new Dictionary<string, BunnyStorageObject>(StringComparer.OrdinalIgnoreCase);
        await LoadRemoteDirectoryAsync("images", result, cancellationToken);
        return result;
    }

    private async Task LoadRemoteDirectoryAsync(
        string remoteDirectory,
        Dictionary<string, BunnyStorageObject> result,
        CancellationToken cancellationToken)
    {
        var requestUri = BuildStorageListUri(remoteDirectory);
        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        request.Headers.Add("AccessKey", _options.StorageApiKey);

        using var response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return;
        }

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var objects = await JsonSerializer.DeserializeAsync<List<BunnyStorageObject>>(stream, JsonOptions, cancellationToken)
            ?? new List<BunnyStorageObject>();

        foreach (var obj in objects)
        {
            var objectName = obj.ObjectName?.Trim();
            if (string.IsNullOrWhiteSpace(objectName))
            {
                continue;
            }

            var normalizedDirectory = remoteDirectory.Trim('/').Replace('\\', '/');
            var fullPath = string.IsNullOrEmpty(normalizedDirectory)
                ? objectName
                : $"{normalizedDirectory}/{objectName}";

            if (obj.IsDirectory)
            {
                await LoadRemoteDirectoryAsync(fullPath, result, cancellationToken);
                continue;
            }

            result[fullPath] = obj;
        }
    }

    private async Task UploadFileAsync(string localPath, string remotePath, CancellationToken cancellationToken)
    {
        var requestUri = BuildStorageUploadUri(remotePath);
        await using var fileStream = File.OpenRead(localPath);
        using var request = new HttpRequestMessage(HttpMethod.Put, requestUri)
        {
            Content = new StreamContent(fileStream)
        };

        request.Headers.Add("AccessKey", _options.StorageApiKey);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

        using var response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new InvalidOperationException(
            $"Bunny upload failed for '{remotePath}' with status {(int)response.StatusCode}: {body}");
    }

    private Uri BuildStorageUploadUri(string remotePath)
    {
        var endpoint = GetStorageEndpoint(_options.StorageRegion);
        var escapedSegments = remotePath
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Select(Uri.EscapeDataString);

        var fullPath = string.Join('/', escapedSegments);
        return new Uri($"https://{endpoint}/{Uri.EscapeDataString(_options.StorageZoneName)}/{fullPath}");
    }

    private Uri BuildStorageListUri(string remoteDirectory)
    {
        var endpoint = GetStorageEndpoint(_options.StorageRegion);
        var escapedSegments = remoteDirectory
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Select(Uri.EscapeDataString);

        var directoryPath = string.Join('/', escapedSegments);
        var suffix = string.IsNullOrEmpty(directoryPath) ? "/" : $"/{directoryPath}/";
        return new Uri($"https://{endpoint}/{Uri.EscapeDataString(_options.StorageZoneName)}{suffix}");
    }

    private static string BuildRemotePath(string localImagesRoot, string filePath)
    {
        var relative = Path.GetRelativePath(localImagesRoot, filePath)
            .Replace('\\', '/')
            .TrimStart('/');

        return $"images/{relative}";
    }

    private static bool IsIgnoredFile(string filePath)
    {
        var extension = Path.GetExtension(filePath);
        return string.Equals(extension, ".db", StringComparison.OrdinalIgnoreCase);
    }

    private static bool ShouldUpload(FileInfo localFile, BunnyStorageObject remoteFile)
    {
        if (remoteFile.Length != localFile.Length)
        {
            return true;
        }

        if (!remoteFile.LastChanged.HasValue)
        {
            return true;
        }

        var localUtc = localFile.LastWriteTimeUtc;
        var remoteUtc = remoteFile.LastChanged.Value.ToUniversalTime();

        return localUtc > remoteUtc.AddSeconds(1);
    }

    private void ValidateConfiguration()
    {
        if (string.IsNullOrWhiteSpace(_options.StorageZoneName))
        {
            throw new InvalidOperationException("Bunny:StorageZoneName is required for image sync.");
        }

        if (string.IsNullOrWhiteSpace(_options.StorageApiKey))
        {
            throw new InvalidOperationException("Bunny:StorageApiKey is required for image sync.");
        }
    }

    private static string GetStorageEndpoint(string? region)
    {
        var normalized = (region ?? string.Empty).Trim().ToLowerInvariant();

        return normalized switch
        {
            "" => "storage.bunnycdn.com",
            "de" => "storage.bunnycdn.com",
            "frankfurt" => "storage.bunnycdn.com",
            "uk" => "uk.storage.bunnycdn.com",
            "london" => "uk.storage.bunnycdn.com",
            "ny" => "ny.storage.bunnycdn.com",
            "newyork" => "ny.storage.bunnycdn.com",
            "new-york" => "ny.storage.bunnycdn.com",
            "la" => "la.storage.bunnycdn.com",
            "losangeles" => "la.storage.bunnycdn.com",
            "los-angeles" => "la.storage.bunnycdn.com",
            _ when normalized.Contains('.') => normalized,
            _ => $"{normalized}.storage.bunnycdn.com"
        };
    }

    private sealed class BunnyStorageObject
    {
        public string? ObjectName { get; set; }
        public long Length { get; set; }
        public DateTimeOffset? LastChanged { get; set; }
        public bool IsDirectory { get; set; }
    }
}
