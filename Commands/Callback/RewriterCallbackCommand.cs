using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramApiBot.Data;
using TelegramApiBot.Data.Buttons;
using TelegramApiBot.Services;
using User = TelegramApiBot.Data.Entities.User;

namespace TelegramApiBot.Commands.Callback;

public class RewriterCallbackCommand : ICallbackCommand
{
    private readonly QuestionsService _questionsService;
    private readonly AnketService _anketService;
    
    public string Name => "Rewriter";

    public RewriterCallbackCommand(QuestionsService questionsService, AnketService anketService)
    {
        _questionsService = questionsService;
        _anketService = anketService;
    }

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

        switch (data.Count)
        {
            case 2:
                await SendNewQuestion(client, user, data.Last());
                break;
            case 3:
                await ReWriteQuestion(client, user, data);
                break;
        }
    }

    private async Task ReWriteQuestion(TelegramBot client, User user, List<string> data)
    {
        if (!int.TryParse(data[1], out var questionId))
        {
            throw new Exception("Incorrect question Id.");
        }

        _questionsService.UpdateAnswer(user, questionId, data.Last());
        _anketService.GenerateSingleAnket(user);

        await client.SendMessageWithButtons(
            $"Ваша анкета успешно отредактирована! ID вашей анкеты:\n`{user.SingleAnket.Id}`\nУбедительная просьба: в целях безопасности не сообщайте его посторонним лицам!", 
            user.Key, 
            new InlineKeyboardMarkup(
                new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("В меню", "MainMenu")
                    }
                }));
    }

    private static async Task SendNewQuestion(TelegramBot client, User user, string data)
    {
        if (!int.TryParse(data, out var questionId))
        {
            throw new Exception("Incorrect question Id.");
        }

        var question = client.Questions[questionId];

        await client.SendMessageWithButtons(
            $"{question.Id}/{client.Questions.Count}: {question.Text}",
            user.Key,
            QuestionsButtons.GetButtons(question.Id, true),
            reWrite: true);
    }
}