namespace GlobalPulse.Api.Services;

/// <summary>Pure text helpers used to turn a feed item into an event. No I/O, easy to test.</summary>
public static class EventClassifier
{
    public static string ClassifyCategory(string feedCategory, string title)
    {
        var t = title.ToLowerInvariant();
        if (t.Contains("earthquake") || t.Contains("flood") || t.Contains("wildfire") ||
            t.Contains("hurricane") || t.Contains("tsunami") || feedCategory == "disaster")
            return "disaster";
        if (t.Contains("war") || t.Contains("attack") || t.Contains("missile") ||
            t.Contains("troops") || t.Contains("conflict") || t.Contains("killed"))
            return "conflict";
        if (t.Contains("cyber") || t.Contains("hack") || t.Contains("breach") || t.Contains("ransomware"))
            return "cyber";
        if (t.Contains("election") || t.Contains("sanction") || t.Contains("president") ||
            t.Contains("parliament") || t.Contains("treaty"))
            return "political";
        return "news";
    }

    public static int GuessSeverity(string title)
    {
        var t = title.ToLowerInvariant();
        if (t.Contains("catastrophic") || t.Contains("mass casualt") || t.Contains("nuclear"))
            return 5;
        if (t.Contains("killed") || t.Contains("dead") || t.Contains("major") || t.Contains("critical"))
            return 4;
        if (t.Contains("attack") || t.Contains("explosion") || t.Contains("crisis"))
            return 3;
        if (t.Contains("warning") || t.Contains("alert") || t.Contains("protest"))
            return 2;
        return 1;
    }

    public static string StripHtml(string html)
    {
        return System.Text.RegularExpressions.Regex.Replace(html, "<.*?>", "").Trim();
    }

    public static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];

    // Simple country name extractor from text
    private static readonly string[] _countries = {
        "Afghanistan","Albania","Algeria","Angola","Argentina","Australia","Austria",
        "Bangladesh","Belarus","Belgium","Bolivia","Brazil","Bulgaria","Cambodia",
        "Canada","Chile","China","Colombia","Croatia","Cuba","Czech Republic",
        "Denmark","Egypt","Ethiopia","Finland","France","Germany","Ghana","Greece",
        "Hungary","India","Indonesia","Iran","Iraq","Ireland","Israel","Italy",
        "Japan","Jordan","Kazakhstan","Kenya","Lebanon","Libya","Malaysia","Mexico",
        "Morocco","Myanmar","Netherlands","Nigeria","North Korea","Norway","Pakistan",
        "Palestine","Peru","Philippines","Poland","Portugal","Romania","Russia",
        "Saudi Arabia","Serbia","Somalia","South Africa","South Korea","Spain",
        "Sudan","Sweden","Switzerland","Syria","Taiwan","Thailand","Turkey",
        "Ukraine","United Kingdom","United States","USA","UK","Venezuela","Vietnam",
        "Yemen","Zimbabwe"
    };

    public static string? ExtractCountry(string text)
    {
        foreach (var c in _countries)
            if (text.Contains(c, StringComparison.OrdinalIgnoreCase)) return c;
        return null;
    }
}
