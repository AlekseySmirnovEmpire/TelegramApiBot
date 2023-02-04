using Telegram.Bot.Types;
using TelegramApiBot.Data;
using TelegramApiBot.Data.Buttons;

namespace TelegramApiBot.Services;

public class AgeConfirmingService
{
    public static async Task<bool> CheckUserConfirm(TelegramBot client, Update update)
    {
        var user = client.FindUser(update.CallbackQuery?.From.Id ?? update.Message.From.Id);
        if (update.Message?.Text == "/start" && user == null)
        {
            return true;
        }
        
        if (user == null)
        {
            throw new Exception("There is no user");
        }

        if (user.AgeConfirmed)
        {
            return true;
        }
        await client.SendMessage(
            "Для начала пользования ботом необходимо подтвердить возраст!", 
            user.Key, 
            "AgeConfirmedInit");
        await client.SendMessageWithButtons(
            "Для продолжения пользования ботом Вы должны быть старше 18 лет.\nВы подтверждаете, что вам больше 18 лет?",
            user.Key,
            AgeConfirm.AgeConfirmButtons(),
            "AgeConfirmedCheck");

        return false;
    }
}