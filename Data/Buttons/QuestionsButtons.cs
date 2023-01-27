using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramApiBot.Data.Buttons;

public class QuestionsButtons
{
    public static IReplyMarkup GetButtons(int questionId, bool isRewrite = false)
    {
        return new InlineKeyboardMarkup(
            new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Да",
                        !isRewrite ? $"Question:{questionId}:Yes" : $"Rewriter:{questionId}:Yes"),
                    InlineKeyboardButton.WithCallbackData("Наверно",
                        !isRewrite ? $"Question:{questionId}:Maybe" : $"Rewriter:{questionId}:Maybe"),
                    InlineKeyboardButton.WithCallbackData("Нет",
                        !isRewrite ? $"Question:{questionId}:No" : $"Rewriter:{questionId}:No")
                }
            });
    }
}