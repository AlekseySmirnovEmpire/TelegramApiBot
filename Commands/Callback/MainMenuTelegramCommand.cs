using Telegram.Bot.Types;
using TelegramApiBot.Data;
using TelegramApiBot.Services;

namespace TelegramApiBot.Commands.Callback;

public class MainMenuTelegramCommand : ITelegramCommand
{
    public string Name => "/menu";
    public async Task Execute(TelegramBot client, Update update)
    {
        var chatId = update.CallbackQuery?.From.Id ?? update.Message.From.Id;

        var user = client.FindUser(chatId);
        if (user == null)
        {
            throw new Exception("There is no user!");
        }

        await MainMenuService.SendMainMenu(client, update);
    }
}