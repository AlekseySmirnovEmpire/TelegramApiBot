using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramApiBot.Data.Buttons;

public class AgeConfirm
{
    public static IReplyMarkup AgeConfirmButtons()
    {
        return new InlineKeyboardMarkup(
            new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Да", "AgeConfirming:Yes"),
                    InlineKeyboardButton.WithCallbackData("Нет", "AgeConfirming:No") 
                }
            });
    }
}