namespace kasthack.Framecutter.Configuration;

/// <summary>
/// Frame render options.
/// </summary>
public class CuttingOptions
{
    /// <summary>
    /// Path to template file.
    /// </summary>
    public string FramePath { get; set; } = default!;

    /// <summary>
    /// Rectangle to draw images at.
    /// </summary>
    public Rectangle TargetRectangle { get; set; } = default!;

    // public SizeMode SizeMode { get; set; }
}
