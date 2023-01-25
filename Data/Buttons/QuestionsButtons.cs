using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramApiBot.Data.Buttons;

public class QuestionsButtons
{
    public static IReplyMarkup GetButtons(int questionId)
    {
        return new InlineKeyboardMarkup(
            new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Да", $"Question:{questionId}:Yes"),
                    InlineKeyboardButton.WithCallbackData("Наверно", $"Question:{questionId}:Maybe"),
                    InlineKeyboardButton.WithCallbackData("Нет", $"Question:{questionId}:No")
                }
            });
    }
}