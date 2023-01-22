using System.Text.Json;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramApiBot.Commands;
using TelegramApiBot.Commands.Callback;
using TelegramApiBot.Services;
using User = TelegramApiBot.Data.Entities.User;

namespace TelegramApiBot.Data;

public class TelegramBot
{
    private readonly TelegramBotClient _client;
    private readonly Dictionary<long, User> _usersInSession;
    private readonly ILogger<TelegramBot> _logger;
    private readonly Dictionary<string, ITelegramCommand> _commands;
    private readonly Dictionary<string, ICallbackCommand> _callbacks;
    private readonly Dictionary<long, int> _messagesToDelete;

    public TelegramBot(
        ILogger<TelegramBot> logger,
        IEnumerable<ITelegramCommand> telegramCommands,
        IEnumerable<ICallbackCommand> callbackCommands,
        UserService userService)
    {
        _client = new TelegramBotClient(Environment.GetEnvironmentVariable("BOT_TOKEN") ?? string.Empty);
        _logger = logger;

        _usersInSession = userService.FindAllUsers().ToDictionary(u => u.Key);
        _logger.LogInformation($"Loaded {_usersInSession.Count} users from DB!");

        _commands = telegramCommands.ToDictionary(t => t.Name);
        _callbacks = callbackCommands.ToDictionary(c => c.Name);
        _messagesToDelete = new Dictionary<long, int>();
    }

    public void AddUser(User user)
    {
        if (_usersInSession.TryGetValue(user.Key, out _))
        {
            return;
        }

        _usersInSession.Add(user.Key, user);
    }

    public User? FindUser(long userKey) => !_usersInSession.TryGetValue(userKey, out var user) ? null : user;

    public async Task SendMessage(string text, long chatId) =>
        await EditAndSendMessage(text, chatId, null, false);

    public async Task SendMessageWithButtons(string text, long chatId, IReplyMarkup replyMarkup, bool isMainMenu) =>
        await EditAndSendMessage(text, chatId, replyMarkup, isMainMenu);

    public void Start() => _client.StartReceiving(
        HandleUpdateAsync,
        HandleErrorAsync,
        new ReceiverOptions(),
        new CancellationTokenSource().Token);

    private async Task EditAndSendMessage(string text, long chatId, IReplyMarkup? replyMarkup, bool isMainMenu)
    {
        if (_messagesToDelete.TryGetValue(chatId, out var messageId))
        {
            await _client.EditMessageReplyMarkupAsync(
                chatId, 
                messageId, 
                new InlineKeyboardMarkup(Array.Empty<InlineKeyboardButton>()));
            _messagesToDelete.Remove(chatId);
        }

        if (replyMarkup == null)
        {
            await _client.SendTextMessageAsync(chatId, text);
            return;
        }

        var message = await _client.SendTextMessageAsync(chatId, text, replyMarkup: replyMarkup);
        if (!isMainMenu)
        {
            _messagesToDelete.Add(chatId, message.MessageId);
        }
    }

    private Task HandleErrorAsync(
        ITelegramBotClient botClient,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError($"Error: {JsonSerializer.Serialize(exception)}");
        return Task.CompletedTask;
    }

    private async Task HandleUpdateAsync(
        ITelegramBotClient client,
        Update update,
        CancellationToken cancellationToken)
    {
        try
        {
            switch (update.Type)
            {
                case UpdateType.Message:
                {
                    if (update.Message == null
                        || string.IsNullOrEmpty(update.Message.Text))
                    {
                        throw new Exception("No message data");
                    }

                    if (!await AgeConfirmingService.CheckUserConfirm(this, update))
                    {
                        return;
                    }

                    if (!_commands.TryGetValue(update.Message.Text.ToLower(), out var command))
                    {
                        await NoCommandMessage.Answer(this, update);
                        return;
                    }

                    await command.Execute(this, update);
                    break;
                }
                case UpdateType.CallbackQuery:
                    var data = update.CallbackQuery?.Data?.Split(":");
                    if (data == null || !data.Any())
                    {
                        throw new Exception("No callback data!");
                    }
                    
                    if (data.First() != "AgeConfirming" && !await AgeConfirmingService.CheckUserConfirm(this, update))
                    {
                        return;
                    }

                    if (!_callbacks.TryGetValue(data.First(), out var callback))
                    {
                        throw new Exception("Wrong callback data!");
                    }

                    await callback.Execute(this, update);
                    break;
                case UpdateType.Unknown:
                    break;
                case UpdateType.InlineQuery:
                    break;
                case UpdateType.ChosenInlineResult:
                    break;
                case UpdateType.EditedMessage:
                    break;
                case UpdateType.ChannelPost:
                    break;
                case UpdateType.EditedChannelPost:
                    break;
                case UpdateType.ShippingQuery:
                    break;
                case UpdateType.PreCheckoutQuery:
                    break;
                case UpdateType.Poll:
                    break;
                case UpdateType.PollAnswer:
                    break;
                case UpdateType.MyChatMember:
                    break;
                case UpdateType.ChatMember:
                    break;
                case UpdateType.ChatJoinRequest:
                    break;
                default:
                    throw new Exception("There is no method for type!");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error: {ex.Message}");
            await SendMessage("Упс! Что-то пошло не так!", update.CallbackQuery?.From.Id ?? update.Message.From.Id);
        }
    }
}