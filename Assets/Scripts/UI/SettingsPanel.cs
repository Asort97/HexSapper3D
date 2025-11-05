using DG.Tweening;
using UnityEngine;

/// <summary>
/// Управление анимацией панели настроек (открытие/закрытие снизу).
/// Вешается на RectTransform или CanvasGroup панели настроек.
/// </summary>
[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(CanvasGroup))]
public class SettingsPanel : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] private float animationDuration = 1f;
    [SerializeField] private float hiddenYPosition = 1500f;
    [SerializeField] private RectTransform soundPanel;
    [SerializeField] private RectTransform musicPanel;
    private RectTransform _rectTransform;
    private CanvasGroup _canvasGroup;
    private Sequence _openSequence;
    private Sequence _closeSequence;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _canvasGroup = GetComponent<CanvasGroup>();
        BuildSequences();
    }

    private void OnDestroy()
    {
        _openSequence?.Kill();
        _closeSequence?.Kill();
    }

    public void Open(System.Action onComplete = null)
    {
        if (!gameObject.activeInHierarchy)
        {
            gameObject.SetActive(true);
            _canvasGroup.alpha = 0f;
        }

        _openSequence?.Restart();
        
        if (onComplete != null)
        {
            _openSequence.OnComplete(() => onComplete?.Invoke());
        }
    }
 
    public void Close(System.Action onComplete = null)
    {
        _closeSequence?.Restart();
        
        if (onComplete != null)
        {
            _closeSequence.OnComplete(() => onComplete?.Invoke());
        }
    }

    private void BuildSequences()
    {
        _openSequence = DOTween.Sequence()
            .Join(_canvasGroup.DOFade(1f, animationDuration * 0.5f))
            .Join(soundPanel.DOAnchorPosX(0f, animationDuration).SetEase(Ease.OutSine))
            .Join(musicPanel.DOAnchorPosX(0f, animationDuration).SetEase(Ease.OutSine))
            .SetAutoKill(false);

        _closeSequence = DOTween.Sequence()
            .Append(soundPanel.DOAnchorPosX(-hiddenYPosition, animationDuration).SetEase(Ease.OutSine))
            .Join(musicPanel.DOAnchorPosX(hiddenYPosition, animationDuration).SetEase(Ease.OutSine))
            .Join(_canvasGroup.DOFade(0f, animationDuration * 0.5f))
            .SetAutoKill(false)
            .OnComplete(() => gameObject.SetActive(false));
    }
}
