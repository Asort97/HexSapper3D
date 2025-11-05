using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Простой UI-компонент для переключения языка.
/// Повесьте на кнопку и назначьте код языка в инспекторе.
/// </summary>
[RequireComponent(typeof(Button))]
public class LanguageSwitcherButton : MonoBehaviour
{
    [Header("Language")]
    [Tooltip("Код языка для этой кнопки (например, 'en', 'ru', 'de')")]
    public string languageCode = "en";

    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(OnClick);
    }

    private void OnDestroy()
    {
        if (_button != null)
            _button.onClick.RemoveListener(OnClick);
    }

    private void OnClick()
    {
        if (GameLocalization.Instance != null)
        {
            GameLocalization.Instance.SetLanguage(languageCode);
        }
        else
        {
            Debug.LogWarning("[LanguageSwitcherButton] GameLocalization.Instance is null!");
        }
    }
}
