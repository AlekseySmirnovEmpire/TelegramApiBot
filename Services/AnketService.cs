using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using TelegramApiBot.Data;
using TelegramApiBot.Data.Entities;

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
                sheet.Cells[qtu.QuestionId, qtu.QuestionId + 1].Value = qtu.Answer;
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