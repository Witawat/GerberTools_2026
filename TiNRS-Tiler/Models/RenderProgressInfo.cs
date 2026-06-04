namespace TiNRS.Tiler.Models;

/// <summary>
/// Represents the progress state of a rendering operation
/// </summary>
public class RenderProgressInfo
{
    /// <summary>
    /// Progress percentage (0-100)
    /// </summary>
    public int PercentComplete { get; set; }

    /// <summary>
    /// Current stage description
    /// </summary>
    public string Stage { get; set; } = string.Empty;

    /// <summary>
    /// Whether the rendering is complete
    /// </summary>
    public bool IsComplete { get; set; }

    /// <summary>
    /// Whether the rendering was cancelled
    /// </summary>
    public bool WasCancelled { get; set; }

    /// <summary>
    /// Error message if rendering failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Intermediate rendering result (if available for progressive rendering)
    /// </summary>
    public object? IntermediateResult { get; set; }
}
