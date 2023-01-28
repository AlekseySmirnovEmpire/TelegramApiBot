namespace TelegramApiBot.Data.Types;

public static class UserAnswer
{
    public static string Yes => "Да";

    public static string Maybe => "Наверное";

    public static string No => "Нет";

    public static string GetAnswer(this string answer)
    {
        return answer switch
        {
            "Yes" => Yes,
            "Maybe" => Maybe,
            "No" => No,
            _ => throw new Exception("There is no conversion")
        };
    }
}