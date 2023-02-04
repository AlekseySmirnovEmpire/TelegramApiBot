using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramApiBot.Data;
using TelegramApiBot.Data.Buttons;
using TelegramApiBot.Services;

namespace TelegramApiBot.Commands.Callback;

public class BlackListCallbackCommand : ICallbackCommand
{
    private readonly BlackListService _blackListService;
    
    public string Name => "BlackList";

    public BlackListCallbackCommand(BlackListService blackListService)
    {
        _blackListService = blackListService;
    }
    
    public async Task Execute(TelegramBot client, Update update)
    {
        var data = update.CallbackQuery?.Data?.Split(":").ToList();
        if (data is not { Count: 3 })
        {
            throw new Exception("There is incorrect data.");
        }

        var user = client.FindUser(update.CallbackQuery.From.Id);
        if (user == null)
        {
            throw new Exception("There is no user.");
        }

        if (!long.TryParse(data.Last(), out var blockedUserKey))
        {
            throw new Exception("There is incorrect blockedUserKey.");
        }

        var blockedUser = client.FindUser(blockedUserKey);
        if (blockedUser == null)
        {
            throw new Exception("There is no blockedUser.");
        }

        switch (data[1])
        {
            case "Init":
                await client.SendMessageWithButtons($"Пользователь [{blockedUser.NickName ?? blockedUser.Name}](tg://user?id={blockedUser.Key}) вам знаком?\n" +
                                                    $"Если нет, то мы могли бы добавить его в чёрный список, чтобы он больше не мог к вам обратиться.\n" +
                                                    $"Хотите добавить его в чёрный список?",
                    user.Key,
                    new InlineKeyboardMarkup(
                        new[]
                        {
                            new[]
                            {
                                InlineKeyboardButton.WithCallbackData("Да", $"BlackList:Add:{blockedUser.Key}"),
                                InlineKeyboardButton.WithCallbackData("Нет", "MainMenu")
                            }
                        }),
                    "PairAnketDecline");
                break;
            case "Add":
                _blackListService.AddUserToBlackList(user, blockedUser);
                await client.SendMessageWithButtons(
                    $"Пользователь [{blockedUser.NickName ?? blockedUser.Name}](tg://user?id={blockedUser.Key}) добавлен в ЧС!",
                    user.Key,
                    MainMenu.ReturnToMainMenuButton(),
                    "BlackListAdd");
                break;
            default:
                throw new Exception("There is not currect data chase for black list!");
        }
    }
}