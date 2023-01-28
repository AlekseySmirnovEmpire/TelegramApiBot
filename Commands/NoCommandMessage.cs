using Telegram.Bot.Types;
using TelegramApiBot.Data;

namespace TelegramApiBot.Commands;

public class NoCommandMessage
{
    public static async Task Answer(TelegramBot client, Update update)
    {
        var user = client.FindUser(update.CallbackQuery?.From.Id ?? update.Message.From.Id);
        if (user == null)
        {
            throw new Exception("User not found!");
        }

        var text = user.AgeConfirmed ? ConfirmedAnswer(update) : NonConfirmedAnswer(update);

        await client.SendMessage(text, user.Key);
    }

    private static string NonConfirmedAnswer(Update update)
    {
        if (update.Message?.Text != null)
        {
            return "Не-а, не правильно!";
        }

        if (update.Message?.Audio != null || update.Message?.Voice != null)
        {
            return "Прости, не могу послушать, но думаю, что там что-то очень интересное!";
        } 
        if (update.Message?.Photo != null)
        {
            return "Дикпик просто огонь! Но надо пройти анкету, дружок!";
        }
        if (update.Message?.Sticker != null)
        {
            return "Я люблю стикеры!";
        }
        return update.Message?.Document != null ? "Документики пожалуйста!" : "Неа, не знаю такого!";
    }
    
    private static string ConfirmedAnswer(Update update)
    {
        if (update.Message?.Text != null)
        {
            return "Упс! такой команды нет!";
        }

        if (update.Message?.Audio != null || update.Message?.Voice != null)
        {
            return "Прости, не могу послушать, но думаю, что там что-то очень интересное!";
        } 
        if (update.Message?.Photo != null)
        {
            return "Дикпик просто огонь! Но надо пройти анкету, дружок!";
        }
        if (update.Message?.Sticker != null)
        {
            return "Я люблю стикеры!";
        }
        return update.Message?.Document != null ? "Документики пожалуйста!" : "Неа, не знаю такого!";
    }
}