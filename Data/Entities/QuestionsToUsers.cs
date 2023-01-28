namespace TelegramApiBot.Data.Entities;

public class QuestionsToUsers
{
    public Guid UserId { get; set; }
    
    public User User { get; set; }
    
    public int QuestionId { get; set; }
    
    public Question Question { get; set; }
    
    public string Answer { get; set; }
}