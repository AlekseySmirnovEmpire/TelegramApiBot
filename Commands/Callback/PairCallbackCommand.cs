using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramApiBot.Data;
using TelegramApiBot.Data.Buttons;
using TelegramApiBot.Data.Types;
using TelegramApiBot.Services;
using User = TelegramApiBot.Data.Entities.User;

namespace TelegramApiBot.Commands.Callback;

public class PairCallbackCommand : ICallbackCommand
{
    private readonly AnketService _anketService;

    public string Name => "Pair";

    public PairCallbackCommand(AnketService anketService)
    {
        _anketService = anketService;
    }

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
            case "Get":
                await GetPairAnket(client, user, data.Last());
                break;
            case "Set":
                await SetPairAnket(client, user, data.Last());
                break;
            default:
                throw new Exception("There is not currect data chase for pair!");
        }
    }

    private async Task CreatePair(TelegramBot client, User user, string data)
    {
        if (!long.TryParse(data, out var pairKey))
        {
            throw new Exception("Incorrect pair key!");
        }

        var pair = client.FindUser(pairKey);
        if (pair == null)
        {
            throw new Exception($"Cannot find pair by pair key {pairKey}!");
        }

        if (pair.PairAnkets.Any(pa => pa.PairKey == user.Key) 
            || user.PairAnkets.Any(pa => pa.PairKey == pairKey))
        {
            var pairAnket = user.PairAnkets.First(pa => pa.PairKey == pairKey);
            var text = user.SubscribeType switch
            {
                SubscribeTypeEnum.None => "У вас уже создана парная анкета с данным пользователем!\n" +
                                          "Не переживайте, если вы (или ваша пара) изменяли ответы в анкете - мы автоматически генерируем новую парную анкету!",
                _ => $"У вас уже создана парная анкета с кодом:\n" +
                     $"`{pairAnket}`\n" +
                     $"Не переживайте, если вы (или ваша пара) изменяли ответы в анкете - мы автоматически генерируем новую парную анкету!"
            };

            await client.SendMessageWithButtons(
                text,
                user.Key,
                MainMenu.ReturnToMainMenuButton());
            return;
        }
        
        _anketService.GeneratePairAnket(user, pair);
        _anketService.ChangeAnketId(user);
        var anket = user.PairAnkets.First(pa => pa.PairKey == pairKey);
        var message = user.SubscribeType switch
        {
            SubscribeTypeEnum.None => $"Парная анкета создана! Вот интересная выборка общих совпадений из неё:\n{anket.ShortDescription}",
            _ => $"Парная анкета создана! Код вашей анкеты: {anket.Id}"
        };

        await client.SendMessageWithButtons($"{message}\n\nВнимание! В целях безопасности ваш персональный ключ был изменён!", user.Key, MainMenu.ReturnToMainMenuButton());
        await client.SendMessageWithButtons(message, pairKey, MainMenu.ReturnToMainMenuButton());
    }

    private async Task SetPairAnket(TelegramBot client, User user, string data)
    {
        if (user.QuestionsToUsers == null || user.QuestionsToUsers.Count < client.Questions.Count)
        {
            await client.SendMessageWithButtons(
                "Вы ещё не закончили анкету! Чтобы редактировать, нужно закончить анкету.\nЖелаете продолжить?",
                user.Key,
                new InlineKeyboardMarkup(
                    new[]
                    {
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("Продолжить",
                                $"Question:{(user.QuestionsToUsers?.Count ?? 0) + 1}")
                        },
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("В меню", "MainMenu")
                        }
                    }));
            return;
        }

        if (data != "Init")
        {
            await CreatePair(client, user, data);
            return;
        }

        client.UsersForWaitingPairId.Add(user.Key, true);
        await client.SendMessage("Напишите мне код анкеты вашей пары:", user.Key);
    }

    private async Task GetPairAnket(TelegramBot client, User user, string data)
    {
        if (user.QuestionsToUsers == null || user.QuestionsToUsers.Count < client.Questions.Count)
        {
            await client.SendMessageWithButtons(
                "Вы ещё не закончили анкету! Чтобы редактировать, нужно закончить анкету.\nЖелаете продолжить?",
                user.Key,
                new InlineKeyboardMarkup(
                    new[]
                    {
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("Продолжить",
                                $"Question:{(user.QuestionsToUsers?.Count ?? 0) + 1}")
                        },
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("В меню", "MainMenu")
                        }
                    }));
            return;
        }

        if (data == "My")
        {
            if (user.SingleAnket == null)
            {
                _anketService.GenerateSingleAnket(user);
            }

            await client.SendMessageWithButtons(
                $"Ваш персональный секретный ключ для анкеты:\n`{user.SingleAnket.Id}`\nОтправьте его своей паре или получите от неё ключ и пройдите в \"Указать пару\", чтобы активировать.\nВ целях безопасности не сообщайте его постороннему человеку!",
                user.Key,
                MainMenu.ReturnToMainMenuButton());
        }

        if (long.TryParse(data, out var pairKey))
        {
            var pair = client.FindUser(pairKey);
            if (pair == null)
            {
                throw new Exception("Doesnt have this user in dictionary!");
            }

            var anket = user.PairAnkets.FirstOrDefault(pa => pa.PairKey == pairKey);

            if (anket == null)
            {
                throw new Exception("There is no pair anket!");
            }

            var text = user.SubscribeType switch
            {
                SubscribeTypeEnum.None =>
                    $"Вот интересная выборка общих совпадений из вашей парной анкеты:\n{anket.ShortDescription}",
                _ => $"Код вашей парной анкеты:\n`{anket.Id}`Не сообщайте его никому в целях безопасности!"
            };

            await client.SendMessageWithButtons(
                text,
                user.Key,
                MainMenu.ReturnToMainMenuButton());
        }
    }
}