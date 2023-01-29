using Microsoft.EntityFrameworkCore;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramApiBot.Data;
using TelegramApiBot.Data.Buttons;
using TelegramApiBot.Data.Entities;
using TelegramApiBot.Data.Types;

namespace TelegramApiBot.Services;

public class PairService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public PairService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task InitPair(TelegramBot client, User user, Guid pairId)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetService<ApplicationDbContext>();

        var pair = dbContext.Users
            .Include(u => u.SingleAnket)
            .Include(u => u.QuestionsToUsers)
                .ThenInclude(qtu => qtu.Question)
            .Include(u => u.BlackList)
            .Include(u => u.PairAnkets)
            .FirstOrDefault(u => u.SingleAnket != null && u.SingleAnket.Id == pairId && user.AgeConfirmed);
        if (pair == null)
        {
            await client.SendMessageWithButtons(
                "Извините, но вы указали неверный ключ или ваша пара ещё не прошла свою анкету!",
                user.Key,
                MainMenu.ReturnToMainMenuButton());
            return;
        }

        if (pair.PairAnkets.Any(pa => pa.PairKey == user.Key) ||
            user.PairAnkets.Any(pa => pa.PairKey == pair.Key))
        {
            var pairAnket = user.PairAnkets.First(pa => pa.PairKey == pair.Key);
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

        if (pair.Id == user.Id)
        {
            await client.SendMessageWithButtons(
                "Нарцизм это весело! Но мне бы анкету другого человека!",
                user.Key,
                MainMenu.ReturnToMainMenuButton());
            return;
        }

        if (user.BlackList.Any(bl => bl.BlockedUserKey == pair.Key))
        {
            await client.SendMessageWithButtons(
                "Данный пользователь находится у вас в чёрном списке! Удалите его оттуда, прежде чем создавать с ним парную анкету!",
                user.Key,
                MainMenu.ReturnToMainMenuButton());
            return;
        }

        if (pair.BlackList.Any(bl => bl.BlockedUserKey == user.Key))
        {
            await client.SendMessageWithButtons(
                "Данный пользователь добавил вас в свой чёрный список!",
                user.Key,
                MainMenu.ReturnToMainMenuButton());
            return;
        }

        await client.SendMessageWithButtons(
            "Вашей паре был отправлен запрос, анкета появится сразу как только запрос будет подтверждён",
            user.Key,
            MainMenu.ReturnToMainMenuButton());
        await client.SendMessageWithButtons(
            $"Пользователь [{user.NickName ?? user.Name}](tg://user?id={user.Key}) хочет отметить вас как свою пару, вы принимаете его запрос?",
            pair.Key,
            new InlineKeyboardMarkup(
                new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("Да", $"Pair:Set:{user.Key}"),
                        InlineKeyboardButton.WithCallbackData("Нет", $"BlackList:Init:{user.Key}")
                    }
                }));
    }
}