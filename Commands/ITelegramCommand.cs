using Telegram.Bot.Types;
using TelegramApiBot.Data;

namespace TelegramApiBot.Commands;

public interface ITelegramCommand
{
    public string Name { get; }

    public Task Execute(TelegramBot client, Update update);
}