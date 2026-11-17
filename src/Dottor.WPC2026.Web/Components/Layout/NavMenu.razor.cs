namespace Dottor.WPC2026.Web.Components.Layout;

public partial class NavMenu
{
    private static (string? Number, string Name) SplitChapter(string chapter)
    {
        var parts = chapter.Split(" · ", 2);
        return parts.Length == 2 ? (parts[0], parts[1]) : (null, chapter);
    }
}
