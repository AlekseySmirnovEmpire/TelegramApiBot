using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramApiBot.Data;
using TelegramApiBot.Data.Buttons;

namespace TelegramApiBot.Services;

public class MainMenuService
{
    public static async Task SendMainMenu(TelegramBot client, Update update)
    {
        var user = client.FindUser(update.CallbackQuery?.From.Id ?? update.Message.From.Id);

        if (user == null)
        {
            throw new Exception("Therer is no user in dictionary.");
        }
        
        await client.SendMessageWithButtons(
            $"{user.Name.Split(" ").First()}, добро пожаловать в бота!\nВыберите действие, что вы хотите сделать:",
            user.Key,
            MainMenu.MainMenuButtons());
    }
}