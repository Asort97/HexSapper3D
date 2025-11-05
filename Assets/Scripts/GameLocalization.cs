using System.Linq;
using UniRx;
using UnityEngine;

/// <summary>
/// Синглтон-менеджер локализации на базе UniRx.
/// Хранит текущий язык в ReactiveProperty — любые изменения автоматически триггерят подписчиков.
/// </summary>
public class GameLocalization : MonoBehaviour
{
    public static GameLocalization Instance { get; private set; }

    [Header("Data")]
    public LocalizationAsset localizationAsset;

    [Header("Settings")]
    public string defaultLanguage = "en";

    // Реактивная переменная языка — на неё подписываются UI-компоненты
    public ReactiveProperty<string> CurrentLanguage { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Загружаем язык из YandexGame.savesData или дефолт
        string savedLang = YG.YandexGame.savesData.language;
        if (string.IsNullOrEmpty(savedLang)) savedLang = defaultLanguage;
        CurrentLanguage = new ReactiveProperty<string>(savedLang);

        // При изменении языка — сохраняем в YandexGame.savesData
        CurrentLanguage.Subscribe(lang =>
        {
            YG.YandexGame.savesData.language = lang;
            YG.YandexGame.SaveProgress();
            Debug.Log($"[Localization] Language changed to: {lang}");
        }).AddTo(this);
    }

    /// <summary>
    /// Изменить текущий язык (триггерит всех подписчиков).
    /// </summary>
    public void SetLanguage(string language)
    {
        if (CurrentLanguage.Value != language)
            CurrentLanguage.Value = language;
    }

    /// <summary>
    /// Получить перевод по ключу для текущего языка.
    /// </summary>
    public string GetValue(string key)
    {
        if (localizationAsset == null || localizationAsset.localizations == null)
        {
            Debug.LogWarning($"[Localization] LocalizationAsset is null or empty!");
            return key;
        }

        string lang = CurrentLanguage.Value;
        var langData = localizationAsset.localizations.FirstOrDefault(l => l.Language == lang);
        
        if (langData.localizations == null)
        {
            Debug.LogWarning($"[Localization] Language '{lang}' not found in asset!");
            return key;
        }

        var entry = langData.localizations.FirstOrDefault(d => d.key == key);
        if (!string.IsNullOrEmpty(entry.value))
            return entry.value;

        Debug.LogWarning($"[Localization] Key '{key}' not found for language '{lang}'!");
        return key;
    }
}