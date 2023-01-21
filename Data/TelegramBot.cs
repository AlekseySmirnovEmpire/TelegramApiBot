﻿using System.Text.Json;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramApiBot.Commands;
using TelegramApiBot.Services;
using User = TelegramApiBot.Data.Entities.User;

namespace TelegramApiBot.Data;

public class TelegramBot
{
    private readonly TelegramBotClient _client;
    private readonly Dictionary<long, User> _usersInSession;
    private readonly ILogger<TelegramBot> _logger;
    private readonly Dictionary<string, ITelegramCommand> _commands;

    public TelegramBot(
        ILogger<TelegramBot> logger,
        StartTelegramCommand startTelegramCommand,
        UserService userService)
    {
        _client = new TelegramBotClient(Environment.GetEnvironmentVariable("BOT_TOKEN") ?? string.Empty);
        _logger = logger;

        _usersInSession = userService.FindAllUsers().ToDictionary(u => u.Key);
        _logger.LogInformation($"Loaded {_usersInSession.Count} users from DB!");

        _commands = new Dictionary<string, ITelegramCommand>
        {
            { startTelegramCommand.Name, startTelegramCommand }
        };
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
        await _client.SendTextMessageAsync(chatId, text);

    public async Task SendMessageWithButtons(string text, long chatId, IReplyMarkup replyMarkup) =>
        await _client.SendTextMessageAsync(chatId, text, replyMarkup: replyMarkup);

    public void Start() => _client.StartReceiving(
        HandleUpdateAsync,
        HandleErrorAsync,
        new ReceiverOptions(),
        new CancellationTokenSource().Token);

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
                        return;
                    }

                    _commands.TryGetValue(update.Message.Text.ToLower(), out var command);
                    if (command == null)
                    {
                        return;
                    }

                    await command.Execute(this, update);
                    break;
                }
                case UpdateType.CallbackQuery:
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
                    throw new Exception("Нет такого метода!");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error: {ex.Message}");
        }
    }
}