using System.Collections.Specialized;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using TelegramApiBot.Data;
using TelegramApiBot.Data.Entities;
using TelegramApiBot.Data.Types;

namespace TelegramApiBot.Services;

public class AnketService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<AnketService> _logger;

    private readonly string _singleAnket = new Regex(@"(?<!fil)[A-Za-z]:\\+[\S\s]*?(?=\\+bin)").Match(
        Path.GetDirectoryName(System.Reflection
            .Assembly.GetExecutingAssembly().CodeBase)).Value + "\\src\\SingleAnket.xlsx";

    private readonly string _pairAnket = new Regex(@"(?<!fil)[A-Za-z]:\\+[\S\s]*?(?=\\+bin)").Match(
        Path.GetDirectoryName(System.Reflection
            .Assembly.GetExecutingAssembly().CodeBase)).Value + "\\src\\PairAnket.xlsx";

    public AnketService(IServiceScopeFactory serviceScopeFactory, ILogger<AnketService> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    public void ChangeAnketId(User user)
    {
        var anket = user.SingleAnket;
        using var scope = _serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetService<ApplicationDbContext>();

        dbContext.SingleAnkets.Remove(anket);
        var newAnket = new SingleAnket
        {
            Name = anket.Name,
            UserId = anket.UserId,
            CreatedAt = DateTime.Now,
            Data = anket.Data
        };

        dbContext.SingleAnkets.Add(newAnket);
        dbContext.SaveChanges();

        user.SingleAnket = newAnket;
    }

    public void GeneratePairAnket(User user, User pair)
    {
        try
        {
            var file = new FileInfo(_pairAnket);
            if (!file.Exists)
            {
                return;
            }

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using var excelStream = new ExcelPackage(file);
            var sheet = excelStream.Workbook.Worksheets.First();
            sheet.Name = $"Анкета - {user.Name} - {pair.Name}";

            using var scope = _serviceScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetService<ApplicationDbContext>();
            var shortList = new NameValueCollection();
            var userQuestionList = dbContext.QuestionsToUsers
                .Include(qtu => qtu.User)
                .Include(qtu => qtu.Question)
                .Where(qtu => qtu.UserId == user.Id)
                .OrderBy(qtu => qtu.QuestionId)
                .ToList();
            var pairQuestionList = dbContext.QuestionsToUsers
                .Include(qtu => qtu.User)
                .Include(qtu => qtu.Question)
                .Where(qtu => qtu.UserId == pair.Id)
                .OrderBy(qtu => qtu.QuestionId)
                .ToList();

            foreach (var userQuestion in userQuestionList)
            {
                var pairQuestion = pairQuestionList.First(qtu => qtu.QuestionId == userQuestion.QuestionId);
                sheet.Cells[userQuestion.QuestionId, userQuestion.QuestionId].Value =
                    userQuestion.QuestionId.ToString();
                sheet.Cells[userQuestion.QuestionId, userQuestion.QuestionId + 1].Value = userQuestion.Question.Text;
                if (userQuestion.Answer == UserAnswer.Yes && pairQuestion.Answer == UserAnswer.Yes)
                {
                    sheet.Cells[userQuestion.QuestionId, userQuestion.QuestionId + 2].Value = UserAnswer.Yes;
                    if (shortList.Count < 7)
                    {
                        shortList.Add(userQuestion.Question.Text, UserAnswer.Yes);
                    }

                    continue;
                }

                if ((userQuestion.Answer == UserAnswer.Yes && pairQuestion.Answer == UserAnswer.Maybe)
                    || (userQuestion.Answer == UserAnswer.Maybe && pairQuestion.Answer == UserAnswer.Yes))
                {
                    sheet.Cells[userQuestion.QuestionId, userQuestion.QuestionId + 2].Value = UserAnswer.Maybe;
                    if (shortList.Count < 7)
                    {
                        shortList.Add(userQuestion.Question.Text, UserAnswer.Maybe);
                    }

                    continue;
                }

                sheet.Cells[userQuestion.QuestionId, userQuestion.QuestionId + 2].Value = UserAnswer.No;
                if (shortList.Count < 7)
                {
                    shortList.Add(userQuestion.Question.Text, UserAnswer.No);
                }
            }

            var shortDescription = shortList.AllKeys
                .Where(key => key != null && shortList[key] != null)
                .Aggregate(string.Empty, (current, key) => current + $"{key} - {shortList[key].ToUpper()}\n");

            var data = excelStream.GetAsByteArray();

            var pairAnketForUser = new PairAnket
            {
                Name = $"Анкета_{user.Name}_{pair.Name}_{DateTime.Now:yyyy-MM-dd}.xlsx",
                UserId = user.Id,
                PairKey = pair.Key,
                ShortDescription = shortDescription,
                Data = data,
                CreatedAt = DateTime.Now
            };

            var pairAnketForPair = new PairAnket
            {
                Name = $"Анкета_{pair.Name}_{user.Name}_{DateTime.Now:yyyy-MM-dd}.xlsx",
                UserId = pair.Id,
                PairKey = user.Key,
                ShortDescription = shortDescription,
                Data = data,
                CreatedAt = DateTime.Now
            };

            var userAnket = user.PairAnkets.FirstOrDefault(pa => pa.PairKey == pair.Key);
            if (userAnket != null)
            {
                user.PairAnkets.Remove(userAnket);
                dbContext.PairAnkets.Remove(userAnket);
                dbContext.SaveChanges();
            }

            var pairAnket = user.PairAnkets.FirstOrDefault(pa => pa.PairKey == pair.Key);
            if (pairAnket != null)
            {
                pair.PairAnkets.Remove(pairAnket);
                dbContext.PairAnkets.Remove(pairAnket);
                dbContext.SaveChanges();
            }

            dbContext.PairAnkets.AddRange(new List<PairAnket>
            {
                pairAnketForUser,
                pairAnketForPair
            });
            dbContext.SaveChanges();

            user.PairAnkets.Add(pairAnketForUser);
            pair.PairAnkets.Add(pairAnketForPair);
        }
        catch (Exception ex)
        {
            _logger.LogError($"Doesn't create pair anket for user {user.Key} and {pair.Key}, error: \"{ex.Message}\"");
        }
    }

    public void GenerateSingleAnket(User user)
    {
        try
        {
            var file = new FileInfo(_singleAnket);
            if (!file.Exists)
            {
                return;
            }

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using var excelStream = new ExcelPackage(file);
            var sheet = excelStream.Workbook.Worksheets.First();
            sheet.Name = $"Анкета - {user.Name}";

            using var scope = _serviceScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetService<ApplicationDbContext>();
            var questionList = dbContext.QuestionsToUsers
                .Include(qtu => qtu.User)
                .Include(qtu => qtu.Question)
                .Where(qtu => qtu.UserId == user.Id)
                .OrderBy(qtu => qtu.QuestionId)
                .ToList();

            questionList.ForEach(qtu =>
            {
                sheet.Cells[qtu.QuestionId, qtu.QuestionId].Value = qtu.QuestionId.ToString();
                sheet.Cells[qtu.QuestionId, qtu.QuestionId + 1].Value = qtu.Question.Text;
                sheet.Cells[qtu.QuestionId, qtu.QuestionId + 2].Value = qtu.Answer;
            });

            var newFile = new SingleAnket
            {
                Name = $"Анкета_{user.Name}_{DateTime.Now:yyyy-MM-dd}.xlsx",
                UserId = user.Id,
                CreatedAt = DateTime.Now,
                Data = excelStream.GetAsByteArray()
            };

            if (user.SingleAnket != null)
            {
                dbContext.SingleAnkets.Remove(user.SingleAnket);
                dbContext.SaveChanges();
            }

            dbContext.SingleAnkets.Add(newFile);
            dbContext.SaveChanges();
            user.SingleAnket = newFile;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Doesn't create anket for user {user.Key}, error: \"{ex.Message}\"");
        }
    }
}