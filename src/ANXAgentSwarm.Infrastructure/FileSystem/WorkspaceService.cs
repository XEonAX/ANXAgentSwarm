using ANXAgentSwarm.Core.Interfaces;
using ANXAgentSwarm.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ANXAgentSwarm.Infrastructure.FileSystem;

/// <summary>
/// Workspace file system service implementation.
/// Restricts all file access to the workspace folder.
/// </summary>
public class WorkspaceService : IWorkspaceService
{
    private readonly string _workspaceRoot;
    private readonly ILogger<WorkspaceService> _logger;

    public WorkspaceService(IOptions<WorkspaceOptions> options, ILogger<WorkspaceService> logger)
    {
        _workspaceRoot = Path.GetFullPath(options.Value.RootPath);
        _logger = logger;

        // Ensure workspace directory exists
        if (!Directory.Exists(_workspaceRoot))
        {
            Directory.CreateDirectory(_workspaceRoot);
            _logger.LogInformation("Created workspace directory: {Path}", _workspaceRoot);
        }
    }

    public async Task<string> ReadFileAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        var fullPath = GetSafePath(relativePath);
        
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"File not found: {relativePath}", relativePath);
        }

        _logger.LogDebug("Reading file: {Path}", relativePath);
        return await File.ReadAllTextAsync(fullPath, cancellationToken);
    }

    public async Task WriteFileAsync(string relativePath, string content, CancellationToken cancellationToken = default)
    {
        var fullPath = GetSafePath(relativePath);
        
        // Ensure directory exists
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        _logger.LogDebug("Writing file: {Path}", relativePath);
        await File.WriteAllTextAsync(fullPath, content, cancellationToken);
    }

    public Task<IEnumerable<string>> ListFilesAsync(
        string relativePath = "",
        bool recursive = false,
        CancellationToken cancellationToken = default)
    {
        var fullPath = GetSafePath(relativePath);

        if (!Directory.Exists(fullPath))
        {
            return Task.FromResult(Enumerable.Empty<string>());
        }

        var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        var files = Directory.GetFiles(fullPath, "*", searchOption)
            .Select(f => Path.GetRelativePath(_workspaceRoot, f))
            .ToList();

        return Task.FromResult<IEnumerable<string>>(files);
    }

    public Task<bool> FileExistsAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        var fullPath = GetSafePath(relativePath);
        return Task.FromResult(File.Exists(fullPath));
    }

    public Task DeleteFileAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        var fullPath = GetSafePath(relativePath);

        if (File.Exists(fullPath))
        {
            _logger.LogDebug("Deleting file: {Path}", relativePath);
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }

    public Task CreateDirectoryAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        var fullPath = GetSafePath(relativePath);

        if (!Directory.Exists(fullPath))
        {
            _logger.LogDebug("Creating directory: {Path}", relativePath);
            Directory.CreateDirectory(fullPath);
        }

        return Task.CompletedTask;
    }

    public string GetAbsolutePath(string relativePath)
    {
        return GetSafePath(relativePath);
    }

    /// <summary>
    /// Gets the full path ensuring it's within the workspace.
    /// </summary>
    private string GetSafePath(string relativePath)
    {
        // Normalize the path
        var normalizedRelative = relativePath
            .Replace('\\', '/')
            .TrimStart('/');

        var fullPath = Path.GetFullPath(Path.Combine(_workspaceRoot, normalizedRelative));

        // Security: Ensure path is within workspace
        if (!fullPath.StartsWith(_workspaceRoot, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Attempted access outside workspace: {Path}", relativePath);
            throw new UnauthorizedAccessException($"Access denied: path is outside workspace - {relativePath}");
        }

        return fullPath;
    }
}
