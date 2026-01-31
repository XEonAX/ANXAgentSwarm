namespace ANXAgentSwarm.Core.Interfaces;

/// <summary>
/// Interface for workspace file system operations.
/// All paths are relative to the workspace root.
/// </summary>
public interface IWorkspaceService
{
    /// <summary>
    /// Reads a file from the workspace.
    /// </summary>
    /// <param name="relativePath">Path relative to workspace root.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The file contents.</returns>
    Task<string> ReadFileAsync(string relativePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes content to a file in the workspace.
    /// </summary>
    /// <param name="relativePath">Path relative to workspace root.</param>
    /// <param name="content">Content to write.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task WriteFileAsync(string relativePath, string content, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists files in the workspace.
    /// </summary>
    /// <param name="relativePath">Path relative to workspace root (empty for root).</param>
    /// <param name="recursive">Whether to list recursively.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of file paths.</returns>
    Task<IEnumerable<string>> ListFilesAsync(
        string relativePath = "",
        bool recursive = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a file exists in the workspace.
    /// </summary>
    /// <param name="relativePath">Path relative to workspace root.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if file exists.</returns>
    Task<bool> FileExistsAsync(string relativePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a file from the workspace.
    /// </summary>
    /// <param name="relativePath">Path relative to workspace root.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteFileAsync(string relativePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a directory in the workspace.
    /// </summary>
    /// <param name="relativePath">Path relative to workspace root.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task CreateDirectoryAsync(string relativePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the absolute path for a relative workspace path.
    /// </summary>
    /// <param name="relativePath">Path relative to workspace root.</param>
    /// <returns>Absolute path.</returns>
    string GetAbsolutePath(string relativePath);
}
