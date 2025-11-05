using DG.Tweening;
using UnityEngine;

/// <summary>
/// Управление анимацией панели победы (появление, конфетти, исчезновение).
/// Вешается на RectTransform панели победы.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class WinPanel : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] private ParticleSystem winParticles;
    [SerializeField] private float scaleUpDuration = 0.7f;
    [SerializeField] private float showDuration = 1.5f;
    [SerializeField] private float scaleDownDuration = 0.3f;

    private RectTransform _rectTransform;
    private Sequence _winSequence;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        BuildWinSequence();
        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        _winSequence?.Kill();
    }

    /// <summary>
    /// Показать панель победы с анимацией.
    /// </summary>
    public void Show(System.Action onComplete = null)
    {
        _winSequence?.Restart();
        
        if (onComplete != null)
        {
            _winSequence.OnComplete(() => onComplete?.Invoke());
        }
    }

    private void BuildWinSequence()
    {
        if (_rectTransform == null) return;

        _winSequence = DOTween.Sequence()
            .AppendCallback(() => gameObject.SetActive(true))
            .Append(_rectTransform.DOScale(1.4f, scaleUpDuration).SetEase(Ease.OutBounce))
            .InsertCallback(0.2f, () => 
            { 
                winParticles?.Play(); 
                SoundManager.Instance?.Play(SfxType.Win_Confetti); 
            })
            .AppendInterval(showDuration)
            .Append(_rectTransform.DOScale(0.5f, scaleDownDuration).SetEase(Ease.OutBounce))
            .SetAutoKill(false)
            .OnComplete(() =>
            {
                gameObject.SetActive(false);
            });
    }
}
