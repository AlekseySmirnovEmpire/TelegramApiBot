using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramApiBot.Data;
using TelegramApiBot.Services;
using User = TelegramApiBot.Data.Entities.User;

namespace TelegramApiBot.Commands.Callback;

public class RedactCallbackCommand : ICallbackCommand
{
    private readonly QuestionsService _questionsService;
    public string Name => "Redact";

    public RedactCallbackCommand(QuestionsService questionsService)
    {
        _questionsService = questionsService;
    }
    public async Task Execute(TelegramBot client, Update update)
    {
        var data = update.CallbackQuery?.Data?.Split(":").ToList();
        if (data is not { Count: 2 })
        {
            throw new Exception("There is incorrect data.");
        }
        
        var user = client.FindUser(update.CallbackQuery.From.Id);
        if (user == null)
        {
            throw new Exception("There is no user.");
        }

        switch (data.Last())
        {
            case "Restart":
                _questionsService.ClearAllUsersQuestions(user);
                await client.SendMessageWithButtons(
                    "Ваша анкета была успешно очищена! Нажмите \"Начать\", чтобы продолжить и перепройти её заново.",
                    user.Key,
                    new InlineKeyboardMarkup(
                        new[]
                        {
                            new[]
                            {
                                InlineKeyboardButton.WithCallbackData("Начать", "Question:1")
                            }
                        }));
                break;
            default:
                throw new Exception($"There is no action for redact callback for user {user.Key}");
        }
    }
}