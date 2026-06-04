using CommunityToolkit.Mvvm.ComponentModel;
using TilingLibrary;
using Artwork;
using SixLabors.ImageSharp;
using Color = SixLabors.ImageSharp.Color;

namespace TiNRS.Tiler.Models;

/// <summary>
/// Represents the current state of all artwork generation settings.
/// This is a wrapper around the TilingLibrary.Settings class with additional UI-specific properties.
/// </summary>
public partial class TilerSettings : ObservableObject
{
    /// <summary>
    /// The underlying TilingLibrary settings object
    /// </summary>
    public Settings CoreSettings { get; private set; } = new Settings();

    // Wrapped Core Properties to support notification

    public int BaseTile
    {
        get => CoreSettings.BaseTile;
        set
        {
            if (CoreSettings.BaseTile != value)
            {
                CoreSettings.BaseTile = value;
                OnPropertyChanged();
            }
        }
    }

    public Artwork.Tiling.TilingType TileType
    {
        get => CoreSettings.TileType;
        set
        {
            if (CoreSettings.TileType != value)
            {
                CoreSettings.TileType = value;
                OnPropertyChanged();
            }
        }
    }

    public Artwork.Settings.ArtMode Mode
    {
        get => CoreSettings.Mode;
        set
        {
            if (CoreSettings.Mode != value)
            {
                CoreSettings.Mode = value;
                OnPropertyChanged();
            }
        }
    }

    public int MaxSubDiv
    {
        get => CoreSettings.MaxSubDiv;
        set
        {
            if (CoreSettings.MaxSubDiv != value)
            {
                CoreSettings.MaxSubDiv = value;
                OnPropertyChanged();
            }
        }
    }

    public float DegreesOff
    {
        get => CoreSettings.DegreesOff;
        set
        {
            if (CoreSettings.DegreesOff != value)
            {
                CoreSettings.DegreesOff = value;
                OnPropertyChanged();
            }
        }
    }

    public int Threshold
    {
        get => CoreSettings.Threshold;
        set
        {
            if (CoreSettings.Threshold != value)
            {
                CoreSettings.Threshold = value;
                OnPropertyChanged();
            }
        }
    }

    public float ScaleSmallerFactor
    {
        get => CoreSettings.scalesmallerfactor;
        set
        {
            if (CoreSettings.scalesmallerfactor != value)
            {
                CoreSettings.scalesmallerfactor = value;
                OnPropertyChanged();
            }
        }
    }

    public bool InvertSource
    {
        get => CoreSettings.InvertSource;
        set
        {
            if (CoreSettings.InvertSource != value)
            {
                CoreSettings.InvertSource = value;
                OnPropertyChanged();
            }
        }
    }

    public bool InvertOutput
    {
        get => CoreSettings.InvertOutput;
        set
        {
            if (CoreSettings.InvertOutput != value)
            {
                CoreSettings.InvertOutput = value;
                OnPropertyChanged();
            }
        }
    }

    public bool Symmetry
    {
        get => CoreSettings.Symmetry;
        set
        {
            if (CoreSettings.Symmetry != value)
            {
                CoreSettings.Symmetry = value;
                OnPropertyChanged();
            }
        }
    }

    public bool MarcelPlating
    {
        get => CoreSettings.MarcelPlating;
        set
        {
            if (CoreSettings.MarcelPlating != value)
            {
                CoreSettings.MarcelPlating = value;
                OnPropertyChanged();
            }
        }
    }

    public float BallRadius
    {
        get => CoreSettings.BallRadius;
        set
        {
            if (CoreSettings.BallRadius != value)
            {
                CoreSettings.BallRadius = value;
                OnPropertyChanged();
            }
        }
    }

    public float Gap
    {
        get => CoreSettings.Gap;
        set
        {
            if (CoreSettings.Gap != value)
            {
                CoreSettings.Gap = value;
                OnPropertyChanged();
            }
        }
    }

    public float Rounding
    {
        get => CoreSettings.Rounding;
        set
        {
            if (CoreSettings.Rounding != value)
            {
                CoreSettings.Rounding = value;
                OnPropertyChanged();
            }
        }
    }

    public int ScaleSmaller
    {
        get => CoreSettings.scalesmaller;
        set
        {
            if (CoreSettings.scalesmaller != value)
            {
                CoreSettings.scalesmaller = value;
                OnPropertyChanged();
            }
        }
    }

    public bool AlwaysSubdivide
    {
        get => CoreSettings.alwayssubdivide;
        set
        {
            if (CoreSettings.alwayssubdivide != value)
            {
                CoreSettings.alwayssubdivide = value;
                OnPropertyChanged();
            }
        }
    }

    public int ScaleSmallerLevel
    {
        get => CoreSettings.scalesmallerlevel;
        set
        {
            if (CoreSettings.scalesmallerlevel != value)
            {
                CoreSettings.scalesmallerlevel = value;
                OnPropertyChanged();
            }
        }
    }

    public bool SuperSymmetry
    {
        get => CoreSettings.SuperSymmetry;
        set
        {
            if (CoreSettings.SuperSymmetry != value)
            {
                CoreSettings.SuperSymmetry = value;
                OnPropertyChanged();
            }
        }
    }

    public int XScaleSmallerLevel
    {
        get => CoreSettings.xscalesmallerlevel;
        set
        {
            if (CoreSettings.xscalesmallerlevel != value)
            {
                CoreSettings.xscalesmallerlevel = value;
                OnPropertyChanged();
            }
        }
    }

    public int XScaleCenter
    {
        get => CoreSettings.xscalecenter;
        set
        {
            if (CoreSettings.xscalecenter != value)
            {
                CoreSettings.xscalecenter = value;
                OnPropertyChanged();
            }
        }
    }

    public Artwork.Settings.TriangleScaleMode ScalingMode
    {
        get => CoreSettings.scalingMode;
        set
        {
            if (CoreSettings.scalingMode != value)
            {
                CoreSettings.scalingMode = value;
                OnPropertyChanged();
            }
        }
    }

    public float DistanceToMaskScale
    {
        get => CoreSettings.distanceToMaskScale;
        set
        {
            if (CoreSettings.distanceToMaskScale != value)
            {
                CoreSettings.distanceToMaskScale = value;
                OnPropertyChanged();
            }
        }
    }

    public float DistanceToMaskRange
    {
        get => CoreSettings.distanceToMaskRange;
        set
        {
            if (CoreSettings.distanceToMaskRange != value)
            {
                CoreSettings.distanceToMaskRange = value;
                OnPropertyChanged();
            }
        }
    }
    
    public string DistanceMaskFile
    {
        get => CoreSettings.DistanceMaskFile;
        set
        {
            if (CoreSettings.DistanceMaskFile != value)
            {
                CoreSettings.DistanceMaskFile = value;
                OnPropertyChanged();
            }
        }
    }



    // UI-Specific Properties

    [ObservableProperty]
    private string? _maskFilePath;

    /// <summary>
    /// Whether to automatically reload the mask when the file changes
    /// </summary>
    [ObservableProperty]
    private bool _autoReloadMask = true;

    /// <summary>
    /// Whether to automatically update the preview when settings change
    /// </summary>
    [ObservableProperty]
    private bool _autoUpdate = true;

    [ObservableProperty]
    private int _outputWidth = 1000;

    [ObservableProperty]
    private int _outputHeight = 1000;

    [ObservableProperty]
    private Avalonia.Media.Color _backgroundColor = Avalonia.Media.Colors.White;

    partial void OnBackgroundColorChanged(Avalonia.Media.Color value)
    {
        CoreSettings.BackGroundColor = Color.FromRgba(value.R, value.G, value.B, value.A);
    }

    [ObservableProperty]
    private Avalonia.Media.Color _foregroundColor = Avalonia.Media.Colors.Black;

    partial void OnForegroundColorChanged(Avalonia.Media.Color value)
    {
        // CoreSettings doesn't seem to have ForegroundColor? It's passed as arg to DrawTiling.
        // But we store it here for the UI.
    }

    [ObservableProperty]
    private Avalonia.Media.Color _backgroundHighlight = Avalonia.Media.Colors.Red; // Default?

    partial void OnBackgroundHighlightChanged(Avalonia.Media.Color value)
    {
        CoreSettings.BackgroundHighlight = Color.FromRgba(value.R, value.G, value.B, value.A);
    }

    [ObservableProperty]
    private float _strokeWidth = 1.0f;

    [ObservableProperty]
    private bool _fillPolygons = false;

    /// <summary>
    /// Creates a deep copy of the settings
    /// </summary>
    public TilerSettings Clone()
    {
        var clone = new TilerSettings
        {
            // Observable properties
            MaskFilePath = MaskFilePath,
            AutoReloadMask = AutoReloadMask,
            AutoUpdate = AutoUpdate,
            OutputWidth = OutputWidth,
            OutputHeight = OutputHeight,
            BackgroundColor = BackgroundColor,
            ForegroundColor = ForegroundColor,
            StrokeWidth = StrokeWidth,
            FillPolygons = FillPolygons
        };

        // Copied CoreSettings logic (manually copying to new instance)
        clone.CoreSettings.Mode = CoreSettings.Mode;
        clone.CoreSettings.TileType = CoreSettings.TileType;
        clone.CoreSettings.MaxSubDiv = CoreSettings.MaxSubDiv;
        clone.CoreSettings.DegreesOff = CoreSettings.DegreesOff;
        clone.CoreSettings.Threshold = CoreSettings.Threshold;
        clone.CoreSettings.InvertSource = CoreSettings.InvertSource;
        clone.CoreSettings.InvertOutput = CoreSettings.InvertOutput;
        clone.CoreSettings.Symmetry = CoreSettings.Symmetry;
        clone.CoreSettings.MarcelPlating = CoreSettings.MarcelPlating;
        clone.CoreSettings.BallRadius = CoreSettings.BallRadius;
        clone.CoreSettings.Gap = CoreSettings.Gap;
        clone.CoreSettings.scalesmallerfactor = CoreSettings.scalesmallerfactor;
        clone.CoreSettings.BaseTile = CoreSettings.BaseTile;
        clone.CoreSettings.Rounding = CoreSettings.Rounding;
        clone.CoreSettings.scalesmaller = CoreSettings.scalesmaller;
        clone.CoreSettings.alwayssubdivide = CoreSettings.alwayssubdivide;
        clone.CoreSettings.scalesmallerlevel = CoreSettings.scalesmallerlevel;
        clone.CoreSettings.SuperSymmetry = CoreSettings.SuperSymmetry;
        clone.CoreSettings.xscalesmallerlevel = CoreSettings.xscalesmallerlevel;
        clone.CoreSettings.xscalecenter = CoreSettings.xscalecenter;
        clone.CoreSettings.scalingMode = CoreSettings.scalingMode;
        clone.CoreSettings.distanceToMaskScale = CoreSettings.distanceToMaskScale;
        clone.CoreSettings.distanceToMaskRange = CoreSettings.distanceToMaskRange;
        clone.CoreSettings.DistanceMaskFile = CoreSettings.DistanceMaskFile;

        return clone;
    }
}
