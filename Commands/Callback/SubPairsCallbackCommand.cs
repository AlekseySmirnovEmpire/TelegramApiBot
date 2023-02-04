using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramApiBot.Data;

namespace TelegramApiBot.Commands.Callback;

public class SubPairsCallbackCommand : ICallbackCommand
{
    public string Name => "SubPairs";
    public async Task Execute(TelegramBot client, Update update)
    {
        var data = update.CallbackQuery?.Data?.Split(":").ToList();
        if (data == null || data.Count is < 2 or > 3)
        {
            throw new Exception("There is incorrect data.");
        }

        var user = client.FindUser(update.CallbackQuery.From.Id);
        if (user == null)
        {
            throw new Exception("There is no user.");
        }

        await client.SendMessageWithButtons(
            "Данный контент доступен только по подписке второго уровня!", 
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
            "SubscribePairFailed",
            $"Required more then {user.SubscribeType} subscribe!",
            true);
    }
}