namespace TelegramApiBot.Data.Entities;

public class Question
{
    public int Id { get; set; }
    
    public string Text { get; set; }
    
    public List<QuestionsToUsers> QuestionsToUsers { get; set; }
}