using Telegram.Bot.Types.ReplyMarkups;

namespace TelegramApiBot.Data.Buttons;

public class MainMenu
{
    public static IReplyMarkup MainMenuButtons()
    {
        return new InlineKeyboardMarkup(
            new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Пройти анкету", "Anket:Init"),
                    InlineKeyboardButton.WithCallbackData("Редактировать анкету", "Anket:Redact")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Парная анкета", "Anket:Pair"),
                    InlineKeyboardButton.WithCallbackData("Найти пару", "Find_Pair:Pair")
                }
            });
    }

    public static IReplyMarkup ReturnToMainMenuButton()
    {
        return new InlineKeyboardMarkup(
            new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Главное меню", "MainMenu")
                }
            });
    }
}