namespace Shoppet_VetClinic.Models;

public static class AppTheme
{
    public const string Primary = "#5C766D";
    public const string Accent = "#F7DD7D";
    public const string DarkAccent = "#427AB5";
    public const string Background = "#EDE9E6";
    public const string NavbarBg = "#F5F5DC";
    public const string TextDark = "#222222";
    public const string CardBorder = "#D5D0C8";

    public static string GetCategoryBadgeStyle(string category) => category switch
    {
        "Food" => "background-color: #5C766D; color: #FFFFFF;",
        "Toys" => "background-color: #E29578; color: #FFFFFF;",
        "Grooming" => "background-color: #4A90E2; color: #FFFFFF;",
        "Accessories" => "background-color: #7D82B8; color: #FFFFFF;",
        "Health" => "background-color: #2A9D8F; color: #FFFFFF;",
        _ => "background-color: #6C757D; color: #FFFFFF;"
    };

    public static string GetStockBadgeStyle(int stock) => stock switch
    {
        <= 0 => "color: #D32F2F; font-weight: 700;",
        <= 5 => "color: #E76F51; font-weight: 600;",
        _ => "color: #6C757D;"
    };
}