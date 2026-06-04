using System;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Threading;
using System.Threading.Tasks;
using TiNRS.Tiler.Models;
using TilingLibrary;
using Artwork;
using RenderProgress = Artwork.RenderProgress;

namespace TiNRS.Tiler.Services;

/// <summary>
/// Service responsible for background rendering of tiling artwork
/// </summary>
public class RenderService
{
    private readonly TINRSArtWorkRenderer _renderer;
    private CancellationTokenSource? _currentCts;
    private readonly object _lock = new object();

    public RenderService()
    {
        _renderer = new TINRSArtWorkRenderer();
    }

    /// <summary>
    /// Renders artwork asynchronously with progress reporting
    /// </summary>
    /// <param name="mask">The mask bitmap to use for rendering</param>
    /// <param name="settings">The tiler settings</param>
    /// <param name="progress">Progress reporter</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The rendered result</returns>
    public async Task<RenderResult> RenderAsync(
        Image<Rgba32> mask,
        TilerSettings settings,
        IProgress<RenderProgressInfo>? progress = null,
        CancellationToken cancellationToken = default)
    {
        // Clone the mask for thread safety
        Image<Rgba32> maskCopy;
        lock (_lock)
        {
            maskCopy = mask.Clone();
        }

        try
        {
            // Build the tree structure from the mask
            progress?.Report(new RenderProgressInfo
            {
                PercentComplete = 0,
                Stage = "Building tree structure...",
                IsComplete = false
            });

            await Task.Run(() =>
            {
                _renderer.BuildTree(maskCopy, settings.CoreSettings);
            }, cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            // Create progress wrapper for the TilingLibrary
            var tilingProgress = new Progress<Artwork.RenderProgress>(rp =>
            {
                progress?.Report(new RenderProgressInfo
                {
                    PercentComplete = rp.PercentComplete,
                    Stage = rp.Stage,
                    IsComplete = rp.IsComplete,
                    IntermediateResult = rp.IntermediatePolygons
                });
            });

            // Perform the main rendering
            await Task.Run(() =>
            {
                _renderer.BuildStuff(maskCopy, settings.CoreSettings, cancellationToken, tilingProgress);
            }, cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            // Report completion
            progress?.Report(new RenderProgressInfo
            {
                PercentComplete = 100,
                Stage = "Complete",
                IsComplete = true
            });

            return new RenderResult
            {
                Success = true,
                Renderer = _renderer
            };
        }
        catch (OperationCanceledException)
        {
            progress?.Report(new RenderProgressInfo
            {
                PercentComplete = 0,
                Stage = "Cancelled",
                IsComplete = true,
                WasCancelled = true
            });

            return new RenderResult
            {
                Success = false,
                WasCancelled = true
            };
        }
        catch (Exception ex)
        {
            progress?.Report(new RenderProgressInfo
            {
                PercentComplete = 0,
                Stage = "Error",
                IsComplete = true,
                ErrorMessage = ex.Message
            });

            return new RenderResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
        finally
        {
            maskCopy?.Dispose();
        }
    }

    /// <summary>
    /// Cancels the current rendering operation
    /// </summary>
    public void CancelCurrentRender()
    {
        lock (_lock)
        {
            _currentCts?.Cancel();
        }
    }

    /// <summary>
    /// Starts a new render operation with automatic cancellation of previous renders
    /// </summary>
    public async Task<RenderResult> RenderWithAutoCancelAsync(
        Image<Rgba32> mask,
        TilerSettings settings,
        IProgress<RenderProgressInfo>? progress = null)
    {
        CancellationTokenSource cts;

        lock (_lock)
        {
            // Cancel any existing render
            _currentCts?.Cancel();
            _currentCts?.Dispose();

            // Create new cancellation token
            cts = new CancellationTokenSource();
            _currentCts = cts;
        }

        try
        {
            return await RenderAsync(mask, settings, progress, cts.Token);
        }
        finally
        {
            lock (_lock)
            {
                if (_currentCts == cts)
                {
                    _currentCts = null;
                }
            }
            cts.Dispose();
        }
    }

    /// <summary>
    /// Gets the current renderer instance (for drawing)
    /// </summary>
    public TINRSArtWorkRenderer GetRenderer() => _renderer;
}

/// <summary>
/// Result of a rendering operation
/// </summary>
public class RenderResult
{
    public bool Success { get; set; }
    public bool WasCancelled { get; set; }
    public string? ErrorMessage { get; set; }
    public TINRSArtWorkRenderer? Renderer { get; set; }
}
