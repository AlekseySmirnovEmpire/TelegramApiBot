using Telegram.Bot.Types;
using TelegramApiBot.Data;
using TelegramApiBot.Data.Buttons;

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

        await client.SendMessageWithButtons(
            $"{user.Name.Split(" ").First()}, добро пожаловать в бота!\nВыберите действие, что вы хотите сделать:",
            chatId,
            MainMenu.MainMenuButtons());
    }
}