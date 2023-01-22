using Telegram.Bot.Types;
using TelegramApiBot.Data;
using TelegramApiBot.Data.Buttons;
using TelegramApiBot.Services;

namespace TelegramApiBot.Commands.Callback;

public class AgeConfirmTelegramCommand : ICallbackCommand
{
    public string Name => "AgeConfirming";

    private readonly UserService _userService;

    public AgeConfirmTelegramCommand(UserService userService)
    {
        _userService = userService;
    }

    public async Task Execute(TelegramBot client, Update update)
    {
        var answer = update.CallbackQuery?.Data?.Split(":")[1];
        if (string.IsNullOrEmpty(answer))
        {
            throw new Exception("No data in context!");
        }

        var chatId = update.CallbackQuery.From.Id;
        var user = client.FindUser(chatId);
        if (user is null)
        {
            throw new Exception("No user by id in session!");
        }
        switch (answer)
        {
            case "Yes":
                user.AgeConfirmed = true;
                _userService.UpdateUser(user);
                await client.SendMessage("Отлично! Давай приступим!", chatId);
                await client.SendMessageWithButtons(
                    $"{user.Name.Split(" ").First()}, добро пожаловать в бота!\nВыберите действие, что вы хотите сделать:",
                    chatId,
                    MainMenu.MainMenuButtons(),
                    true);
                break;
            case "No":
                await client.SendMessage("Чтобы пользоваться ботом необходимо быть старше 18 лет!", chatId);
                break;
            default:
                throw new Exception("There is no answer for age confirm!");
        }
    }
}