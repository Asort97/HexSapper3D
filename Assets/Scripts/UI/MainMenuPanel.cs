using DG.Tweening;
using UnityEngine;

/// <summary>
/// Управление анимацией главного меню (появление/исчезновение при старте игры).
/// Вешается на CanvasGroup главного меню.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class MainMenuPanel : MonoBehaviour
{
    private CanvasGroup _canvasGroup;
    private Sequence _hideSequence;

    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        BuildHideSequence();
    }

    private void OnDestroy()
    {
        _hideSequence?.Kill();
    }

    /// <summary>
    /// Скрыть меню с анимацией (вызывается при старте игры).
    /// </summary>
    public void Hide(System.Action onComplete = null)
    {
        if (_canvasGroup == null) return;

        gameObject.SetActive(true);

        _hideSequence.OnComplete(() =>
        {
            gameObject.SetActive(false);
            transform.localScale = Vector3.one;
            onComplete?.Invoke();
        });

        _hideSequence.Restart();
    }

    /// <summary>
    /// Показать меню мгновенно.
    /// </summary>
    public void Show()
    {
        if (_canvasGroup == null) return;

        gameObject.SetActive(true);
        _canvasGroup.alpha = 1f;
        transform.localScale = Vector3.one;
    }

    private void BuildHideSequence()
    {
        if (_canvasGroup == null) return;

        _hideSequence = DOTween.Sequence()
            .Append(_canvasGroup.DOFade(0f, 1f))
            .Join(transform.DOScale(2f, 1f))
            .SetAutoKill(false);
    }
}
