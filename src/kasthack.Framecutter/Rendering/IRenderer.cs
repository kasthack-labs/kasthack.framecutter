namespace kasthack.Framecutter.Rendering;

using System.IO;

using kasthack.Framecutter.Configuration;

public interface IRenderer
{
    Stream RenderFrame(MemoryStream sourceMemoryStream, CuttingOptions options);
}