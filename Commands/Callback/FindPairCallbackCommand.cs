using Telegram.Bot.Types;
using TelegramApiBot.Data;
using TelegramApiBot.Data.Types;
using TelegramApiBot.Services;

namespace TelegramApiBot.Commands.Callback;

public class FindPairCallbackCommand : ICallbackCommand
{
    public string Name => "Find_Pair";
    public async Task Execute(TelegramBot client, Update update)
    {
        var user = client.FindUser(update.CallbackQuery.From.Id);
        if (user == null)
        {
            throw new Exception("There is no user!");
        }

        switch (user.SubscribeType)
        {
            case SubscribeTypeEnum.None:
                await client.SendMessage("Это платный контент! Оформите подписку, чтобы им воспользоваться!", user.Key);
                await MainMenuService.SendMainMenu(client, update);
                break;
            case SubscribeTypeEnum.Default:
                await client.SendMessage("Контента пока нет, но вы держитесь!", user.Key);
                await MainMenuService.SendMainMenu(client, update);
                break;
            case SubscribeTypeEnum.Special:
                await client.SendMessage("Контента пока нет, но вы держитесь!", user.Key);
                await MainMenuService.SendMainMenu(client, update);
                break;
            default:
                throw new Exception($"There is something wrong with {user.Key} subscribe in DB!");
        }
    }
}