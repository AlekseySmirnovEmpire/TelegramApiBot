using Microsoft.EntityFrameworkCore;
using TelegramApiBot.Data;
using TelegramApiBot.Data.Entities;
using TelegramApiBot.Data.Types;

namespace TelegramApiBot.Services;

public class QuestionsService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;

    public QuestionsService(IServiceScopeFactory serviceScopeFactory)
    {
        _serviceScopeFactory = serviceScopeFactory;
    }

    public List<Question> FindAllQuestions()
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetService<ApplicationDbContext>();

        return dbContext.Questions.ToList();
    }

    public List<QuestionsToUsers> FindUserQuestionsByUserId(Guid userId)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetService<ApplicationDbContext>();
        
        return dbContext.QuestionsToUsers
            .Include(qtu => qtu.User)
            .Include(qtu => qtu.Question)
            .Where(qtu => qtu.UserId == userId)
            .OrderBy(qtu => qtu.QuestionId)
            .ToList();
    }

    public void UpdateAnswer(User user, int questionId, string answer)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetService<ApplicationDbContext>();

        user.QuestionsToUsers.First(q => q.QuestionId == questionId).Answer = answer;
        dbContext.Database.ExecuteSqlRaw(
            @$"UPDATE ""QuestionsToUsers"" SET ""Answer"" = '{answer.GetAnswer()}' WHERE ""UserId"" = '{user.Id}' AND ""QuestionId"" = {questionId}");
        dbContext.SaveChanges();
    }

    public void InitAnswer(User user, int questionId, string answer)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetService<ApplicationDbContext>();

        var userAnswer = new QuestionsToUsers
        {
            UserId = user.Id,
            QuestionId = questionId,
            Answer = answer.GetAnswer()
        };
        
        dbContext.QuestionsToUsers.Add(userAnswer);
        dbContext.SaveChanges();

        user.QuestionsToUsers ??= new List<QuestionsToUsers>();
        user.QuestionsToUsers.Add(userAnswer);
    }

    public void ClearAllUsersQuestions(User user)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetService<ApplicationDbContext>();
        if (dbContext == null)
        {
            throw new Exception("There is no db context!");
        }

        if (user.SingleAnket != null)
        {
            dbContext.SingleAnkets.Remove(user.SingleAnket);
            dbContext.SaveChanges();
            user.SingleAnket = null;
        }

        if (user.QuestionsToUsers == null || !user.QuestionsToUsers.Any())
        {
            return;
        }

        dbContext.QuestionsToUsers.RemoveRange(user.QuestionsToUsers);
        dbContext.SaveChanges();
        user.QuestionsToUsers = new List<QuestionsToUsers>();
    }
}