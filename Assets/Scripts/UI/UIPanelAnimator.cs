using DG.Tweening;
using System;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
[RequireComponent(typeof(RectTransform))]
public class UIPanelAnimator : MonoBehaviour
{
    [Header("Position")]
    [SerializeField] private float hiddenYPosition = -1600f;
    [SerializeField] private float showDuration = 0.5f;
    [SerializeField] private float hideDuration = 0.8f;
    private CanvasGroup _canvasGroup;
    private RectTransform _rect;
    private Sequence showTween;
    private Sequence hideTween;
    private bool _isBuilded;

    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        _rect = GetComponent<RectTransform>();

        if(!_isBuilded)
            BuildTweens();
    }

    private void BuildTweens()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        _rect = GetComponent<RectTransform>();


        showTween = DOTween.Sequence()
        .OnStart(()=>
        {
            _canvasGroup.alpha = 0f;
            _rect.anchoredPosition = new Vector2(_rect.anchoredPosition.x, hiddenYPosition);
        })
        .Append(_canvasGroup.DOFade(1f, showDuration).SetEase(Ease.OutSine))
        .Join(_rect.DOAnchorPosY(0f, showDuration).SetEase(Ease.OutSine))
        .SetEase(Ease.OutSine)
        .SetAutoKill(false);

        hideTween = DOTween.Sequence()
        .Append(_canvasGroup.DOFade(0f, showDuration).SetEase(Ease.OutSine))
        .Join(_rect.DOAnchorPosY(hiddenYPosition, showDuration).SetEase(Ease.OutSine))
        .SetEase(Ease.InSine)
        .OnComplete(() => gameObject.SetActive(false))
        .SetAutoKill(false);

        _isBuilded = true;
    }
    
    public void Show()
    {
        gameObject.SetActive(true);

        if (!_isBuilded)
            BuildTweens();
            
        showTween?.Restart();
    }

    public void Hide()
    {
        if (!_isBuilded)
            BuildTweens();
            
        hideTween?.Restart();
    }
}
