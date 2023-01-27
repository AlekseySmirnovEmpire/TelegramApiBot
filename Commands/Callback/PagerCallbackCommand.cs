using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramApiBot.Data;
using User = TelegramApiBot.Data.Entities.User;

namespace TelegramApiBot.Commands.Callback;

public class PagerCallbackCommand : ICallbackCommand
{
    private const int QuestionsPerPage = 5;

    public string Name => "Pager";

    public async Task Execute(TelegramBot client, Update update)
    {
        var data = update.CallbackQuery?.Data?.Split(":").ToList();
        if (data is not { Count: 3 })
        {
            throw new Exception("There is incorrect data.");
        }

        var user = client.FindUser(update.CallbackQuery.From.Id);
        if (user == null)
        {
            throw new Exception("There is no user.");
        }

        switch (data[1])
        {
            case "Question":
                await QuestionPager(user, client, data.Last());
                break;
            default:
                throw new Exception("There is not correct pager!");
        }
    }

    private static async Task QuestionPager(User user, TelegramBot client, string data)
    {
        if (!int.TryParse(data, out var pageNumber) && data != "Init")
        {
            throw new Exception("Not correct data in pager!");
        }

        if (pageNumber <= 0)
        {
            pageNumber = 1;
        }

        if (QuestionsPerPage * pageNumber - client.Questions.Count > QuestionsPerPage)
        {
            throw new Exception("Not correct data in pager! Too big number!");
        }

        await InitPager(user, client, pageNumber);
    }

    private static async Task InitPager(User user, TelegramBot client, int pageNumber)
    {
        var text = "Выберите вопрос:";
        var buttons = pageNumber != 1
            ? new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData("<<", $"Pager:Question:{pageNumber - 1}")
            }
            : new List<InlineKeyboardButton>();

        var end = pageNumber * QuestionsPerPage;
        var start = end - QuestionsPerPage;
        if (end > client.Questions.Count)
        {
            end = client.Questions.Count;
        }

        for (var i = start + 1; i <= end; ++i)
        {
            var quest = client.Questions[i];
            text += $"\n{quest.Id}. {quest.Text}";
            buttons.Add(InlineKeyboardButton.WithCallbackData($"{quest.Id}", $"Rewriter:{quest.Id}"));
        }

        if (end != client.Questions.Count)
        {
            buttons.Add(InlineKeyboardButton.WithCallbackData(">>", $"Pager:Question:{pageNumber + 1}"));
        }

        await client.SendMessageWithButtons(
            text,
            user.Key,
            new InlineKeyboardMarkup(
                new[]
                {
                    buttons.ToArray(),
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("В меню", "MainMenu")
                    }
                }),
            reWrite: true);
    }
}