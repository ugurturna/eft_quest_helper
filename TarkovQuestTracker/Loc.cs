using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;

namespace TarkovQuestTracker;

/// <summary>
/// UI language (menus, buttons, messages). Independent from the quest data language.
/// Strings live in Strings/Strings.{lang}.xaml; XAML uses {DynamicResource key}, code uses Loc.T / Loc.F.
/// </summary>
public static class Loc
{
    public static readonly string[] Languages = ["tr", "en"];

    public static string Lang { get; private set; } = "tr";

    /// <summary>Raised after the dictionary is swapped, for strings built in code.</summary>
    public static event Action? Changed;

    static ResourceDictionary? _current;

    public static void Set(string lang)
    {
        if (!Languages.Contains(lang)) lang = "tr";
        var dict = new ResourceDictionary { Source = new Uri($"pack://application:,,,/Strings/Strings.{lang}.xaml") };
        var merged = Application.Current.Resources.MergedDictionaries;
        if (_current != null) merged.Remove(_current);
        merged.Add(dict);
        _current = dict;
        Lang = lang;
        Changed?.Invoke();
    }

    public static string T(string key) => Application.Current?.TryFindResource(key) as string ?? key;

    public static string F(string key, params object?[] args) => string.Format(T(key), args);

    /// <summary>Display name of a language code, in that language.</summary>
    public static string LanguageName(string code) => code switch
    {
        "tr" => "Türkçe",
        "en" => "English",
        _ => code,
    };
}

/// <summary>A selectable option whose label follows the UI language.</summary>
public sealed class Option : ObservableObject
{
    readonly string? _labelKey;
    readonly string? _fixedLabel;

    Option(string key, string? labelKey, string? fixedLabel)
    {
        Key = key;
        _labelKey = labelKey;
        _fixedLabel = fixedLabel;
    }

    /// <summary>Label looked up from the string table.</summary>
    public static Option Localized(string key, string labelKey) => new(key, labelKey, null);

    /// <summary>Label that never changes (trader names, language names).</summary>
    public static Option Fixed(string key, string label) => new(key, null, label);

    public string Key { get; }
    public string Label => _fixedLabel ?? Loc.T(_labelKey!);

    public void Relabel() => OnPropertyChanged(nameof(Label));

    public override string ToString() => Label;
}
