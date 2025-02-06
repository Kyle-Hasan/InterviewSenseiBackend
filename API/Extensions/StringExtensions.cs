namespace API.Extensions;

public static class StringExtensions
{
    public static string GetStringAfterPattern(this string text, string searchPattern)
    {
        int index = text.IndexOf(searchPattern,StringComparison.Ordinal);
        if (index != -1)
        {
            return text.Substring(index + searchPattern.Length);
        }
        return text;
    }
    
    public static string GetStringBeforePattern(this string text, string searchPattern)
    {
        int index = text.IndexOf(searchPattern, StringComparison.Ordinal);
        return index != -1 ? text.Substring(0, index) : text;
    }
}
