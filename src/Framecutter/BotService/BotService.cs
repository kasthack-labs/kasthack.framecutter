namespace Framecutter.Services
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Telegram.Bot;
    using Telegram.Bot.Exceptions;
    using Telegram.Bot.Extensions.Polling;
    using Telegram.Bot.Types;
    using Telegram.Bot.Types.Enums;
    using Telegram.Bot.Types.InputFiles;

    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Microsoft.Extensions.Hosting;
    using System.Drawing;
    using System.Drawing.Drawing2D;
    using Framecutter.Resources;

    public class BotService : IHostedService
    {
        private readonly ILogger logger;
        private readonly Configuration.Configuration options;
        private readonly TelegramBotClient bot;

        public BotService(
            IOptions<Configuration.Configuration> options,
            ILogger<BotService> logger
            )
        {
            this.logger = logger;
            this.options = options.Value;
            this.bot = new TelegramBotClient(this.options.Telegram.Token);
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var me = await this.bot.GetMeAsync(cancellationToken).ConfigureAwait(false);
            this.bot.StartReceiving(
                new DefaultUpdateHandler(this.HandleUpdateAsync, this.HandleErrorAsync),
                cancellationToken
            );
        }
        public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            try
            {
                await (update.Type switch
                {
                    UpdateType.Message => this.BotOnMessageReceived(update.Message, cancellationToken),
                    _ => Task.Run(() => this.logger.LogError($"Unknown update type: {update.Type}"), cancellationToken),
                }).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                await this.HandleErrorAsync(botClient, exception, cancellationToken).ConfigureAwait(false);
            }
        }
        private async Task BotOnMessageReceived(Message message, CancellationToken cancellationToken)
        {
            this.logger.LogDebug("Received message from {0}(@{1}): {2}(has files: {3})", message.Chat.Id, message.From.Username, message.Text, (message.Photo?.Any() ?? false) || message.Document != null);

            _ = Task.Run(async () =>
            {
                try
                {
                    await (message.Type switch
                    {
                        MessageType.Text => (message.Text?.Split(' ').FirstOrDefault()) switch
                        {
                            "/start" => this.bot.SendTextMessageAsync(message.Chat, Strings.StartMessage),
                            _ => this.bot.SendTextMessageAsync(message.Chat, Strings.ICantRead, cancellationToken: cancellationToken)
                        },
                        MessageType.Photo => this.ProcessImage(message, message.Photo.MaxBy(a => a.Height * a.Width).FileId, cancellationToken),
                        MessageType.Document => this.ProcessImage(message, message.Document.FileId, cancellationToken),
                        _ => this.bot.SendTextMessageAsync(message.Chat, Strings.ThisIsntAnImage, cancellationToken: cancellationToken),
                    }).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    this.logger.LogError(ex, "Error handling message");
                }
            }, cancellationToken);
        }

        private async Task ProcessImage(Message message, string id, CancellationToken cancellationToken)
        {
            using var readMemoryStream = new MemoryStream();
            try
            {
                _ = await this.bot.GetInfoAndDownloadFileAsync(id, readMemoryStream, cancellationToken).ConfigureAwait(false);
                _ = readMemoryStream.Seek(0, SeekOrigin.Begin);

                using var sourceImage = Image.FromStream(readMemoryStream);
                using var frameImage = Image.FromFile(this.options.Cutting.FramePath);
                using var outputImage = new Bitmap(frameImage.Width, frameImage.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                using (var outputGraphics = Graphics.FromImage(outputImage))
                {
                    outputGraphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    outputGraphics.SmoothingMode = SmoothingMode.HighQuality;
                    outputGraphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    outputGraphics.CompositingQuality = CompositingQuality.HighQuality;

                    //fill target rectangle with the  image

                    var configRect = this.options.Cutting.TargetRectangle;
                    var scale = Math.Max((float)configRect.Width / sourceImage.Width, (float)configRect.Height / sourceImage.Height);
                    var dx = configRect.X + (configRect.Width / 2) - (sourceImage.Width / 2 * scale);
                    var dy = configRect.Y + (configRect.Height / 2) - (sourceImage.Height / 2 * scale);

                    outputGraphics.SetClip(new Rectangle(configRect.X, configRect.Y, configRect.Width, configRect.Height));
                    outputGraphics.TranslateTransform(dx, dy);
                    outputGraphics.ScaleTransform(scale, scale);

                    outputGraphics.DrawImage(sourceImage, 0, 0, sourceImage.Width, sourceImage.Height);

                    outputGraphics.ResetClip();
                    outputGraphics.ResetTransform();

                    outputGraphics.DrawImage(
                        frameImage,
                        destRect: new RectangleF(0, 0, frameImage.Width, frameImage.Height),
                        srcRect: new RectangleF(0, 0, frameImage.Width, frameImage.Height),
                        srcUnit: GraphicsUnit.Pixel);
                }

                using var writeMemoryStream = new MemoryStream();
                outputImage.Save(writeMemoryStream, System.Drawing.Imaging.ImageFormat.Png);
                _ = writeMemoryStream.Seek(0, SeekOrigin.Begin);

                _ = await this.bot.SendDocumentAsync(message.Chat, new InputOnlineFile(writeMemoryStream, $"stream_frame_{DateTime.Now:yyyy-MM-dd_HH-mm}.png"), Strings.FrameIsReady, replyToMessageId: message.MessageId, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _ = await this.bot.SendTextMessageAsync(message.Chat, string.Format(Strings.ErrorWhileProcessing, ex.Message), replyToMessageId: message.MessageId, cancellationToken: cancellationToken).ConfigureAwait(false);
                var filename = $"{DateTime.Now:yyyy-MM-dd_HH-mm}_bad_file_from_chat_{message.Chat.Id}_{message.MessageId}";
                var written = readMemoryStream.Length > 0;
                if (written)
                {
                    using var writeStream = System.IO.File.Create(filename);
                    _ = readMemoryStream.Seek(0, SeekOrigin.Begin);
                    await readMemoryStream.CopyToAsync(writeStream, cancellationToken).ConfigureAwait(false);
                    await writeStream.FlushAsync(cancellationToken).ConfigureAwait(false);
                }
                this.logger.LogError(ex, $"Error cutting image received from '{message.From.FirstName} {message.From.LastName}'(@{message.From.Username} | {message.From.Id}) in message #{message.MessageId}. {(!written ? "" : "Data writtem to " + filename)}");
            }
        }

        public async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken) => this.logger.LogError(exception, exception switch
        {
            ApiRequestException apiRequestException => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => exception.ToString()
        });

        public async Task StopAsync(CancellationToken cancellationToken)
        {
        }
    }
}