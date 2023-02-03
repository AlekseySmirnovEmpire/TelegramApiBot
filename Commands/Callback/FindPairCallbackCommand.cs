using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramApiBot.Data;
using TelegramApiBot.Data.Types;
using User = TelegramApiBot.Data.Entities.User;

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
                await InitForNonSub(client, user);
                break;
            case SubscribeTypeEnum.Default:
                await InitForSub(client, user);
                break;
            case SubscribeTypeEnum.Special:
                await InitForSub(client, user);
                break;
            default:
                throw new Exception($"There is something wrong with {user.Key} subscribe in DB!");
        }
    }

    private static async Task InitForSub(TelegramBot client, User user)
    {
        if (user.SubscribeType == SubscribeTypeEnum.Default)
        {
            await client.SendMessageWithButtons(
                "В разделе \"Пары\" вы можете приниматься запросы от других платный подписчиков на создание пары и смотреть с кем вы уже выбрали быть парой.", 
                user.Key, 
                new InlineKeyboardMarkup(
                    new[]
                    {
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("Пары", "SubPairs:Init")
                        }
                    }),
                true);
            return;
        }

        await client.SendMessageWithButtons(
            "В разделе \"Пары\" вы можете приниматься запросы от других платный подписчиков на создание пары и смотреть с кем вы уже выбрали быть парой.\n" +
            "В разделе \"Подобрать по анкете\" вы можете подобрать себе пару из других подписчиков по вашей анкете.\n" +
            "В \"Подобрать по параметрам\" вы можете выбрать пару по выбранными вами параметрам.", 
            user.Key, 
            new InlineKeyboardMarkup(
                new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("Пары", "SubPairs:Pairs:Init"),
                        InlineKeyboardButton.WithCallbackData("Подобрать по анкете", "SubPairs:Anket:Init"),
                        InlineKeyboardButton.WithCallbackData("Подобрать по параметрам", "SubPairs:Params:Init")
                    }
                }),
            true);
    }
    
    private static async Task InitForNonSub(TelegramBot client, User user)
    {
        await client.SendMessageWithButtons(
            $"{user.Name.Split(" ").First()}, это платный контент! Чтобы начать им пользоваться необходимо оформить подписку. Чтобы это сделать, нажмите на \"Приобрести\"!", 
            user.Key,
            new InlineKeyboardMarkup(
                new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("Приобрести", "Payment:Init"),
                        InlineKeyboardButton.WithCallbackData("Главное меню", "MainMenu")
                    }
                }), 
            true);
    }
}