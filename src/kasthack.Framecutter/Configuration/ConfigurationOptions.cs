namespace kasthack.Framecutter.Configuration;

public class ConfigurationOptions
{
    public TelegramOptions Telegram { get; set; } = default!;

    public CuttingOptions Cutting { get; set; } = default!;
}
