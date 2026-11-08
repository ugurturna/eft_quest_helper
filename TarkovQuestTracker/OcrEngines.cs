using Windows.Globalization;
using Windows.Media.Ocr;

namespace TarkovQuestTracker;

/// <summary>The Windows text recognizer the screen readers use.</summary>
static class OcrEngines
{
    /// <summary>
    /// English if Windows has it; otherwise any other Latin-script one (a Turkish / German / … Windows usually has
    /// only its own, and it reads the English quest and exfil names just as well). Null when there is none, or
    /// when Windows can't do text recognition at all (some "N" editions, a broken language pack).
    /// </summary>
    public static OcrEngine? Create()
    {
        try
        {
            var languages = OcrEngine.AvailableRecognizerLanguages;
            var best = languages.FirstOrDefault(l => l.LanguageTag.StartsWith("en", StringComparison.OrdinalIgnoreCase))
                       ?? languages.FirstOrDefault(IsLatin);
            return best == null ? null : OcrEngine.TryCreateFromLanguage(best);
        }
        catch (Exception)
        {
            return null;
        }
    }

    static bool IsLatin(Language l)
    {
        try
        {
            return l.Script == "Latn";
        }
        catch (Exception)
        {
            return false;
        }
    }
}
