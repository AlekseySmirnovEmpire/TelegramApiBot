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

        if (!user.AgeConfirmed)
        {
            await client.SendMessage("Для начала пользования ботом необходимо подтвердить возраст!", chatId);
            await client.SendMessageWithButtons(
                "Для продолжения пользования ботом Вы должны быть старше 18 лет.\nВы подтверждаете, что вам больше 18 лет?",
                chatId,
                AgeConfirm.AgeConfirmButtons());
            return;
        }

        await client.SendMessageWithButtons(
            $"{user.Name.Split(" ").First()}, добро пожаловать в бота!\nВыберите действие, что вы хотите сделать:",
            chatId,
            MainMenu.MainMenuButtons());
    }
}