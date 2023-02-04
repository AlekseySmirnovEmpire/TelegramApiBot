using System.Text.Json;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramApiBot.Commands;
using TelegramApiBot.Commands.Callback;
using TelegramApiBot.Data.Entities;
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
    private readonly PairService _pairService;
    private readonly BotanService _botanService;
    public Dictionary<long, bool> UsersForWaitingPairId;

    public Dictionary<int, Question> Questions { get; }

    public TelegramBot(
        ILogger<TelegramBot> logger,
        IEnumerable<ITelegramCommand> telegramCommands,
        IEnumerable<ICallbackCommand> callbackCommands,
        UserService userService,
        QuestionsService questionsService,
        PairService pairService,
        BotanService botanService)
    {
        _client = new TelegramBotClient(Environment.GetEnvironmentVariable("BOT_TOKEN") ?? string.Empty);
        _logger = logger;
        _pairService = pairService;

        _usersInSession = userService.FindAllUsers().ToDictionary(u => u.Key);
        _logger.LogInformation($"Loaded {_usersInSession.Count} users from DB!");

        _commands = telegramCommands.ToDictionary(t => t.Name);
        _callbacks = callbackCommands.ToDictionary(c => c.Name);
        _messagesToDelete = new Dictionary<long, int>();
        Questions = questionsService.FindAllQuestions().ToDictionary(q => q.Id);
        UsersForWaitingPairId = new Dictionary<long, bool>();
        _logger.LogInformation($"Have been loaded {Questions.Count} questions!");

        _botanService = botanService;
    }

    public void AddUser(User user)
    {
        if (_usersInSession.TryGetValue(user.Key, out _))
        {
            return;
        }

        _usersInSession.Add(user.Key, user);
    }

    public List<User> GetAllUsersInSession() => _usersInSession.Values.ToList();

    public void UpdateUserInSession(User user)
    {
        if (!_usersInSession.TryGetValue(user.Key, out var _))
        {
            return;
        }

        _usersInSession[user.Key] = user;
    }

    public User? FindUser(long userKey) => !_usersInSession.TryGetValue(userKey, out var user) ? null : user;

    public Question? FindQuestion(int questionId) =>
        !Questions.TryGetValue(questionId, out var question) ? null : question;

    public async Task SendMessage(string text, long chatId, string commandName, string? botanMessage = null) =>
        await EditAndSendMessage(text, chatId, commandName, null, false, botanMessage);

    public async Task SendMessageWithButtons(
        string text,
        long chatId,
        IReplyMarkup replyMarkup,
        string commandName,
        string? botanMessage = null,
        bool reWrite = false) =>
        await EditAndSendMessage(text, chatId, commandName, replyMarkup, reWrite, botanMessage);

    public void Start() => _client.StartReceiving(
        HandleUpdateAsync,
        HandleErrorAsync,
        new ReceiverOptions(),
        new CancellationTokenSource().Token);

    private async Task EditAndSendMessage(
        string text,
        long chatId,
        string commandName,
        IReplyMarkup? replyMarkup,
        bool reWrite,
        string? botanMessage)
    {
        _botanService.Track(commandName, chatId, botanMessage);
        if (_messagesToDelete.TryGetValue(chatId, out var messageId) && reWrite)
        {
            var message = await _client.EditMessageTextAsync(
                chatId,
                messageId,
                text,
                parseMode: ParseMode.Markdown);
            if (replyMarkup != null)
            {
                message = await _client.EditMessageReplyMarkupAsync(
                    chatId, 
                    message.MessageId, 
                    replyMarkup: replyMarkup as InlineKeyboardMarkup);
                _messagesToDelete[chatId] = message.MessageId;
                return;
            }

            _messagesToDelete.Remove(chatId);
            return;
        }
        
        var mes = await _client.SendTextMessageAsync(
                chatId,
                text,
                replyMarkup: replyMarkup,
                parseMode: ParseMode.Markdown);
        if (replyMarkup != null)
        {
            if (_messagesToDelete.TryGetValue(chatId, out _))
            {
                _messagesToDelete[chatId] = mes.MessageId;
                return;
            }
            
            _messagesToDelete.Add(chatId, mes.MessageId);
            return;
        }
        
        if (_messagesToDelete.TryGetValue(chatId, out _))
        {
            _messagesToDelete.Remove(chatId);
        }
    }

    private async Task DeleteMessageButtons(User? user)
    {
        if (user == null || !_messagesToDelete.TryGetValue(user.Key, out var messageId))
        {
            return;
        }

        var message = await _client.EditMessageReplyMarkupAsync(
            user.Key,
            messageId, 
            new InlineKeyboardMarkup(Array.Empty<InlineKeyboardButton>()));

        _messagesToDelete[user.Key] = message.MessageId;
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
            await DeleteMessageButtons(FindUser(update.CallbackQuery?.From.Id ?? update.Message.From.Id));
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

                    if (!_commands.TryGetValue(update.Message.Text.Trim().ToLower(), out var command))
                    {
                        if (UsersForWaitingPairId.TryGetValue(update.Message.From.Id, out var val) && val)
                        {
                            UsersForWaitingPairId.Remove(update.Message.From.Id);
                            if (Guid.TryParse(update.Message.Text.Trim(), out var anketGuid))
                            {
                                await _pairService.InitPair(
                                    this,
                                    _usersInSession[update.Message.From.Id],
                                    anketGuid);
                                return;
                            }
                        }
                        await NoCommandMessage.Answer(this, update);
                        return;
                    }

                    await command.Execute(this, update);
                    break;
                }
                case UpdateType.CallbackQuery:
                    if (UsersForWaitingPairId.TryGetValue(update.CallbackQuery.From.Id, out var value) && value)
                    {
                        UsersForWaitingPairId.Remove(update.CallbackQuery.From.Id);
                    }
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
            await SendMessage(
                "Упс! Что-то пошло не так!",
                update.CallbackQuery?.From.Id ?? update.Message.From.Id,
                "Error",
                ex.Message);
        }
    }
}