using TMPro;
using UniRx;
using UnityEngine;

/// <summary>
/// Компонент для автоматической локализации TextMeshProUGUI.
/// Подписывается на изменение языка через UniRx и обновляет текст.
/// Вешается на GameObject с TMP_Text.
/// </summary>
[RequireComponent(typeof(TextMeshProUGUI))]
public class LocalizedText : MonoBehaviour
{
    [Header("Localization")]
    [Tooltip("Ключ для перевода (например, 'ui.start_button')")]
    public string localizationKey;

    [Header("Optional")]
    [Tooltip("Если true, текст будет форматироваться с параметрами через string.Format")]
    public bool useFormatting = false;

    private TextMeshProUGUI _text;
    private CompositeDisposable _disposables = new CompositeDisposable();

    private void Awake()
    {
        _text = GetComponent<TextMeshProUGUI>();
    }

    private void Start()
    {
        if (GameLocalization.Instance == null)
        {
            Debug.LogWarning("[LocalizedText] GameLocalization.Instance is null! Localization won't work.");
            return;
        }

        // Подписываемся на изменение языка
        GameLocalization.Instance.CurrentLanguage
            .Subscribe(_ => UpdateText())
            .AddTo(_disposables);

        // Первичная установка текста
        UpdateText();
    }

    private void OnDestroy()
    {
        _disposables?.Dispose();
    }

    /// <summary>
    /// Обновить текст из текущего языка.
    /// </summary>
    public void UpdateText()
    {
        if (_text == null || GameLocalization.Instance == null)
            return;

        string translatedText = GameLocalization.Instance.GetValue(localizationKey);
        _text.text = translatedText;
    }

    /// <summary>
    /// Обновить текст с форматированием (например, "Level {0}").
    /// </summary>
    public void UpdateText(params object[] args)
    {
        if (_text == null || GameLocalization.Instance == null)
            return;

        string translatedText = GameLocalization.Instance.GetValue(localizationKey);
        
        if (useFormatting && args != null && args.Length > 0)
        {
            try
            {
                _text.text = string.Format(translatedText, args);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[LocalizedText] Format error for key '{localizationKey}': {ex.Message}");
                _text.text = translatedText;
            }
        }
        else
        {
            _text.text = translatedText;
        }
    }

    /// <summary>
    /// Изменить ключ локализации на лету.
    /// </summary>
    public void SetKey(string newKey)
    {
        localizationKey = newKey;
        UpdateText();
    }
}
