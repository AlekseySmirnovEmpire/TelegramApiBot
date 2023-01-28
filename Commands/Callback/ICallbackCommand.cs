using Telegram.Bot.Types;
using TelegramApiBot.Data;

namespace TelegramApiBot.Commands.Callback;

public interface ICallbackCommand
{
    public string Name { get; }

    public Task Execute(TelegramBot client, Update update);
}