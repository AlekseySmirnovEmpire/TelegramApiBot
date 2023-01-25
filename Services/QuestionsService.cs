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
}