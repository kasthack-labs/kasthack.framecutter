namespace kasthack.Framecutter.Configuration;

public enum SizeMode
{
    /// <summary>
    /// Scale for the smaller size to match the target rect &amp; crop.
    /// </summary>
    Fill,

    /// <summary>
    /// Scale for the larger sized to match the target rect (leaves space).
    /// </summary>
    Fit,

    /// <summary>
    /// Scale ignoring proportions.
    /// </summary>
    Stretch,

    /// <summary>
    /// Just center.
    /// </summary>
    Center,
}
