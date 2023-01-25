using Telegram.Bot.Types;
using TelegramApiBot.Data;
using TelegramApiBot.Data.Buttons;
using TelegramApiBot.Data.Entities;
using TelegramApiBot.Services;

namespace TelegramApiBot.Commands.Callback;

public class QuestionsCallbackCommand : ICallbackCommand
{
    public string Name => "Question";

    private readonly QuestionsService _questionsService;
    private readonly AnketService _anketService;

    public QuestionsCallbackCommand(QuestionsService questionsService, AnketService anketService)
    {
        _questionsService = questionsService;
        _anketService = anketService;
    }

    public async Task Execute(TelegramBot client, Update update)
    {
        var data = update.CallbackQuery?.Data?.Split(":").ToList();
        if (data == null || !data.Any())
        {
            throw new Exception("There is no data!");
        }

        var questionId = Convert.ToInt32(data[1]);
        if (client.QuestionsCount == questionId && data.Count > 2)
        {
            var user = client.FindUser(update.CallbackQuery.From.Id);
            _questionsService.InitAnswer(user, questionId, data[2]);
            _anketService.GenerateSingleAnket(user);
            var anket = user.SingleAnket != null
                ? $"Ваш персональный секретный ключ для анкеты:\n`{user.SingleAnket.Id}`\nВ целях безопасности не сообщайте его постороннему человеку!"
                : "Мы сохранили ваши ответы, но ваша персональная анкета пока что не сгенерировалась!";
            await client.SendMessageWithButtons(
                $"Спасибо, что закончили анкету!\n{anket}",
                update.CallbackQuery.From.Id,
                replyMarkup: MainMenu.ReturnToMainMenuButton(),
                reWrite: true);
            return;
        }

        var question = client.FindQuestion(data.Count == 2 ? questionId : questionId + 1);
        if (question == null)
        {
            throw new Exception("There is no questions.");
        }

        switch (data.Count)
        {
            case 2:
                await InitQuestion(client, update.CallbackQuery.From.Id, question);
                return;
            case 3:
                _questionsService.InitAnswer(client.FindUser(update.CallbackQuery.From.Id), questionId, data[2]);
                break;
        }

        await InitQuestion(client, update.CallbackQuery.From.Id, question);
    }

    private static async Task InitQuestion(TelegramBot client, long chatId, Question question)
    {
        await client.SendMessageWithButtons(
            $"{question.Id}/{client.QuestionsCount}: {question.Text}",
            chatId,
            QuestionsButtons.GetButtons(question.Id),
            reWrite: true);
    }
}