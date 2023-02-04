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
        if (data == null || data.Count is < 2 or > 3)
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
            case "Init":
                await InitPairAnket(client, user);
                break;
            default:
                throw new Exception("There is not currect data chase for pair!");
        }
    }

    private static async Task InitPairAnket(TelegramBot client, User user)
    {
        if (!user.PairAnkets.Any())
        {
            await client.SendMessageWithButtons(
                "Похоже, у вас ещё нет парных анкет!", 
                user.Key,
                MainMenu.ReturnToMainMenuButton(),
                "NoPairAnkets",
                reWrite: true);
            return;
        }

        var text = user.SubscribeType switch
        {
            SubscribeTypeEnum.None =>
                "Похоже, у вас нет подписки! Для пользователей без подписки доступна 1 парная анкета и частичный просмотр её!",
            SubscribeTypeEnum.Default =>
                "Похоже, у вас подписка первого уровня! Для вас доступно 3 парных анкеты и полный их просмотр!",
            SubscribeTypeEnum.Special =>
                "У вас подписка второго уровня, Вас доступен полный просмотр всех ваших парных анкет, которых может быть до 7!",
            _ => throw new Exception($"There is no such subscribe type for user {user.Key}!")
        };

        var buttonText = user.SubscribeType switch
        {
            SubscribeTypeEnum.None => "Смотреть анкету",
            _ => "Смореть анкеты"
        };

        var buttonData = user.SubscribeType switch
        {
            SubscribeTypeEnum.None => $"Pair:Get:{user.PairAnkets.First().PairKey}",
            _ => "Pager:Pairs:1"
        };

        await client.SendMessageWithButtons(
            text, 
            user.Key,
            new InlineKeyboardMarkup(
                new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData(buttonText, buttonData)
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("В меню", "MainMenu")
                    }
                }),
            "SubscribePairService",
            reWrite: true);
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
                MainMenu.ReturnToMainMenuButton(),
                "PairAnketExist");
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

        await client.SendMessageWithButtons(
            $"{message}\n\nВнимание! В целях безопасности ваш персональный ключ был изменён!", 
            user.Key, 
            MainMenu.ReturnToMainMenuButton(),
            "NotifyUserForChangingSingleAnketId");
        await client.SendMessageWithButtons(
            message, 
            pairKey, 
            MainMenu.ReturnToMainMenuButton(),
            "GeneratePairAnket");
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
                    }),
                "UserQuestionsInProgress",
                reWrite: true);
            return;
        }

        if ((user.PairAnkets.Any() && user.SubscribeType == SubscribeTypeEnum.None) 
            || (user.PairAnkets.Count == 3 && user.SubscribeType == SubscribeTypeEnum.Default) 
            || (user.PairAnkets.Count == 7 && user.SubscribeType == SubscribeTypeEnum.Special))
        {
            var limit = user.SubscribeType switch
            {
                SubscribeTypeEnum.None => 1,
                SubscribeTypeEnum.Default => 3,
                SubscribeTypeEnum.Special => 7,
                _ => throw new Exception($"There is no such subscribe type for user {user.Key}!")
            };
            await client.SendMessageWithButtons(
                $"Похоже, вы уже достигли лимита парных анкет! Для вашего уровня подписки он составляет: {limit} анкет.",
                user.Key,
                new InlineKeyboardMarkup(
                    new[]
                    {
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("В меню", "MainMenu")
                        }
                    }),
                "UserOutOfLimitPairAnkets",
                reWrite: true);
            return;
        }

        if (data != "Init")
        {
            await CreatePair(client, user, data);
            return;
        }

        client.UsersForWaitingPairId.Add(user.Key, true);
        await client.SendMessage("Напишите мне код анкеты вашей пары:", user.Key, "WaitForPairId");
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
                    }), 
                "UserWishToRedactAnket",
                reWrite: true);
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
                MainMenu.ReturnToMainMenuButton(),
                "SendUserSingleAnketId");
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
                MainMenu.ReturnToMainMenuButton(),
                "PairAnketSendToUser");
        }
    }
}