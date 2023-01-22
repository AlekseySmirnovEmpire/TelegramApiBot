using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramApiBot.Data;
using TelegramApiBot.Data.Buttons;
using TelegramApiBot.Data.Types;
using TelegramApiBot.Services;
using User = TelegramApiBot.Data.Entities.User;

namespace TelegramApiBot.Commands;

public class StartTelegramCommand : ITelegramCommand
{
    public string Name => "/start";

    private readonly UserService _userService;

    public StartTelegramCommand(UserService userService)
    {
        _userService = userService;
    }

    public async Task Execute(TelegramBot client, Update update)
    {
        if (update.Message == null)
        {
            return;
        }

        var user = client.FindUser(update.Message.Chat.Id);
        if (user == null)
        {
            user = _userService.Create(new User
            {
                Key = update.Message.Chat.Id,
                Name = $"{update.Message.From?.FirstName} {update.Message.From?.LastName}",
                AgeConfirmed = false,
                NickName = update.Message.From?.Username,
                SubscribeType = SubscribeTypeEnum.None,
                CreatedAt = DateTime.Now
            });
            client.AddUser(user);
            await client.SendMessage(
                $"Здравствуйте, {update.Message.From?.FirstName ?? "гость"}! Добро пожаловать!", 
                user.Key);
            await client.SendMessageWithButtons(
                "Для продолжения пользования ботом Вы должны быть старше 18 лет.\nВы подтверждаете, что вам больше 18 лет?",
                user.Key,
                AgeConfirm.AgeConfirmButtons(),
                false);
            return;
        }

        if (user.AgeConfirmed)
        {
            await client.SendMessageWithButtons(
                $"{user.Name.Split(" ").First()}, добро пожаловать в бота!\nВыберите действие, что вы хотите сделать:",
                user.Key,
                MainMenu.MainMenuButtons(),
                true);
        }
    }
}