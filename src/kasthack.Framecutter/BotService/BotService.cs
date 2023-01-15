namespace kasthack.Framecutter.Services;

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using kasthack.Framecutter.Configuration;
using kasthack.Framecutter.Rendering;
using kasthack.Framecutter.Resources;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;

internal class BotService : IHostedService, IUpdateHandler
{
    private readonly ILogger logger;
    private readonly ConfigurationOptions options;
    private readonly TelegramBotClient bot;

    public BotService(
        IOptions<ConfigurationOptions> options,
        ILogger<BotService> logger)
    {
        this.logger = logger;
        this.options = options.Value;
        this.bot = new TelegramBotClient(this.options.Telegram.Token);
    }

#pragma warning disable IDE0060
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
#pragma warning restore IDE0060

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _ = await this.bot.GetMeAsync(cancellationToken).ConfigureAwait(false);
        this.bot.StartReceiving(
            updateHandler: this,
            receiverOptions: null,
            cancellationToken: cancellationToken);
    }

    async Task IUpdateHandler.HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        try
        {
            await (update.Type switch
            {
                UpdateType.Message => this.BotOnMessageReceived(update.Message!, cancellationToken),
                _ => Task.Run(() => this.logger.LogError("Unknown update type: {updateType}", update.Type), cancellationToken),
            }).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            await ((IUpdateHandler)this).HandlePollingErrorAsync(botClient, exception, cancellationToken).ConfigureAwait(false);
        }
    }

    Task IUpdateHandler.HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        switch (exception)
        {
            case ApiRequestException apiRequestException:
                this.logger.LogError(
                    apiRequestException,
                    "Telegram API Error:\n[{errorCode}]\n{errorMessage}",
                    apiRequestException.ErrorCode,
                    apiRequestException.Message);
                break;
            default:
                this.logger.LogError(exception, "An exception has occured");
                break;
        }

        return Task.CompletedTask;
    }

    private Task BotOnMessageReceived(Message message, CancellationToken cancellationToken)
    {
        this.logger.LogDebug(
            "Received message from {messageChatId}(@{messageUsername}): {messageText}(has files: {messageHasFiles})",
            message.Chat.Id,
            message.From!.Username,
            message.Text,
            (message.Photo?.Any() ?? false) || message.Document != null);

        _ = Task.Run(
            async () =>
            {
                try
                {
                    await (message.Type switch
                    {
                        MessageType.Text => message.Text?.Split(' ')?.FirstOrDefault() switch
                        {
                            "/start" => this.bot.SendTextMessageAsync(message.Chat, Strings.StartMessage),
                            _ => this.bot.SendTextMessageAsync(message.Chat, Strings.ICantRead, cancellationToken: cancellationToken),
                        },
                        MessageType.Photo => this.ProcessImage(message, message.Photo!.MaxBy(a => a.Height * a.Width)!.FileId, cancellationToken),
                        MessageType.Document => this.ProcessImage(message, message.Document!.FileId, cancellationToken),
                        _ => this.bot.SendTextMessageAsync(message.Chat, Strings.ThisIsntAnImage, cancellationToken: cancellationToken),
                    }).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    this.logger.LogError(ex, "Error handling message");
                }
            },
            cancellationToken);

        return Task.CompletedTask;
    }

    private async Task ProcessImage(Message message, string id, CancellationToken cancellationToken)
    {
        using var readMemoryStream = new MemoryStream();
        try
        {
            _ = await this.bot.GetInfoAndDownloadFileAsync(id, readMemoryStream, cancellationToken).ConfigureAwait(false);
            _ = readMemoryStream.Seek(0, SeekOrigin.Begin);
            using var resultMemoryStream = new SDGRenderer().RenderFrame(readMemoryStream, this.options.Cutting);
            _ = await this.bot.SendDocumentAsync(
                chatId: message.Chat,
                document: new InputOnlineFile(resultMemoryStream, $"stream_frame_{DateTime.Now:yyyy-MM-dd_HH-mm}.png"),
                thumb: Strings.FrameIsReady,
                replyToMessageId: message.MessageId,
                cancellationToken: cancellationToken)
                .ConfigureAwait(false);
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

            this.logger.LogError(
                ex,
                "Error cutting image received from '{messageFirstName} {messageLastName}'(@{messageUsername} | {messageFromId}) in message #{messageId}. Data written to {errorFileName}.",
                message.From!.FirstName,
                message.From.LastName,
                message.From.Username,
                message.From.Id,
                message.MessageId,
                written ? filename : "<nowhere>");
        }
    }
}
