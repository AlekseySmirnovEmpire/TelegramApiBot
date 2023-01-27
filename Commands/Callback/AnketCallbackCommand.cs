using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramApiBot.Data;
using TelegramApiBot.Data.Buttons;

namespace TelegramApiBot.Commands.Callback;

public class AnketCallbackCommand : ICallbackCommand
{
    public string Name => "Anket";
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
            case "Init" when user.QuestionsToUsers == null || user.QuestionsToUsers.Count == 0:
                await client.SendMessageWithButtons(
                    "Сейчас вам будет предложено пройти анкету." +
                    "\nВ ней вам предстоит выбрать один из трёх вариантов: \"Да\", \"Нет\" и \"Наверное\"." +
                    "\nЕсли вы готовы - нажмите начать.",
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
            case "Init" when user.QuestionsToUsers.Count > 0 && user.QuestionsToUsers.Count < client.Questions.Count:
                await client.SendMessageWithButtons(
                    "Вы уже начали проходить анкету! Если хотите продолжить - нажмите кнопку:",
                    user.Key,
                    new InlineKeyboardMarkup(
                        new[]
                        {
                            new[]
                            {
                                InlineKeyboardButton.WithCallbackData("Продолжить", $"Question:{user.QuestionsToUsers.Count + 1}")
                            }
                        }));
                break;
            case "Init" when user.QuestionsToUsers.Count == client.Questions.Count:
                var anket = user.SingleAnket != null
                    ? $"Ваш персональный секретный ключ для анкеты:\n`{user.SingleAnket.Id}`\nВ целях безопасности не сообщайте его постороннему человеку!"
                    : "Мы сохранили ваши ответы, но ваша персональная анкета пока что не сгенерировалась! Если вы видете это сообщение не в первый раз - обратитесь в поддержку!";
                await client.SendMessageWithButtons(
                    $"Вы уже прошли анкету!\n{anket}", 
                    user.Key,
                    MainMenu.ReturnToMainMenuButton());
                break;
            case "Redact" when user.QuestionsToUsers.Count < client.Questions.Count:
                await client.SendMessageWithButtons(
                    "Вы ещё не закончили анкету! Чтобы редактировать, нужно закончить анкету.\nЖелаете продолжить?",
                    user.Key,
                    new InlineKeyboardMarkup(
                        new[]
                        {
                            new[]
                            {
                                InlineKeyboardButton.WithCallbackData("Продолжить", $"Question:{user.QuestionsToUsers.Count + 1}")
                            },
                            new[]
                            {
                                InlineKeyboardButton.WithCallbackData("В меню", "MainMenu")
                            }
                        }));
                break;
                case "Redact" when user.QuestionsToUsers.Count == client.Questions.Count:
                    await client.SendMessageWithButtons(
                    "Если вы хотите изменить некоторые вопросы в своей анкете - нажмите \"Редактировать\".\nЕсли вы хотите перепройти анкету полностью - нажмите \"Пройти заново\".",
                    user.Key,
                    new InlineKeyboardMarkup(
                        new[]
                        {
                            new[]
                            {
                                InlineKeyboardButton.WithCallbackData("Редактировать", "Redact:Single")
                            },
                            new[]
                            {
                                InlineKeyboardButton.WithCallbackData("Пройти заново", "Redact:Restart")
                            }
                        }));
                    break;
            default:
                throw new Exception("There is not currect data chase for anket!");
        }
    }
}