using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Управление панелью предложения рекламы (показ с таймером, скрытие).
/// Вешается на RectTransform ad панели.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class AdPanel : MonoBehaviour
{
    [Header("Timer")]
    [SerializeField] private float timerDuration = 10f;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private Image timerBar;
    [SerializeField] private Image timerBarBackground;
    [SerializeField, Range(0f, 1f)] private float bgPulseAlpha = 0.45f;

    [Header("Pulse")]
    [SerializeField] private float pulseForce = 0.9f;

    [Header("Animation")]
    [SerializeField] private UIPanelAnimator panelAnimator;
    private float _currentTimer;
    private bool _isVisible = true;
    private Tween timerTween;
    private Tween _bgColorTween;
    private float _pulseAccumulator = 0f;
    private float _bgPulseAccumulator = 0f;
    private Color _bgInitialColor = Color.black;
    public event Action OnTimerExpired;

    private void Awake()
    {
        _currentTimer = timerDuration;
        
        UpdateTimerUI();
    }

    private void Start()
    {
        if (timerText != null)
        {
            timerTween = timerText.transform.DOPunchScale(new Vector3(1f, 1f, 1f) *  (Mathf.Approximately(pulseForce, 0f) ? 0.9f : pulseForce), 0.5f, 0, 0).SetAutoKill(false);
        }

        if (timerBarBackground != null)
        {
            _bgInitialColor = timerBarBackground.color;
            var pulseColor = new Color(1f, 1f, 1f, bgPulseAlpha);
            _bgColorTween = DOTween.Sequence()
                .Append(timerBarBackground.DOColor(pulseColor, 0.12f))
                .Append(timerBarBackground.DOColor(_bgInitialColor, 0.18f))
                .SetAutoKill(false)
                .Pause();
        }
    }
    
    private void Update()
    {
        if (!_isVisible || !gameObject.activeInHierarchy) return;

        _currentTimer -= Time.deltaTime;

        // handle local pulses for text and background
        if (timerTween != null)
        {
            _pulseAccumulator += Time.deltaTime;
            if (_pulseAccumulator >= 1f)
            {
                _pulseAccumulator -= 1f;
                timerTween.Restart();
            }
        }

        if (_bgColorTween != null)
        {
            _bgPulseAccumulator += Time.deltaTime;
            if (_bgPulseAccumulator >= 1f)
            {
                _bgPulseAccumulator -= 1f;
                _bgColorTween.Restart();
            }
        }
        if (_currentTimer <= 0f)
        {
            _currentTimer = 0f;
            UpdateTimerUI();
            OnTimerExpired?.Invoke();
            Hide();
            return;
        }

        UpdateTimerUI();
    }

    /// <summary>
    /// Показать панель с таймером.
    /// </summary>
    public void Show()
    {
        Debug.Log("AD Panel showed");

        _isVisible = true;
        _currentTimer = timerDuration;
        UpdateTimerUI();
        // reset accumulators and immediately trigger a pulse so it feels responsive
        _pulseAccumulator = 0f;
        _bgPulseAccumulator = 0f;
        timerTween?.Restart();
        _bgColorTween?.Restart();

        panelAnimator.Show();
    }

    public void Hide()
    {
        _isVisible = false;
        _currentTimer = timerDuration;
        UpdateTimerUI();
        // pause local pulses
        timerTween?.Pause();
        _bgColorTween?.Pause();

        panelAnimator.Hide();
    }

    public void ResetTimer()
    {
        _currentTimer = timerDuration;
        UpdateTimerUI();
    }

    private void UpdateTimerUI()
    {
        float fill = 1f;
        if (Math.Abs(timerDuration) > Mathf.Epsilon)
        {
            fill = Mathf.Clamp01(_currentTimer / timerDuration);
        }

        if (timerBar != null)
        {
            timerBar.fillAmount = fill;
        }

        if (timerText != null)
        {
            // Показываем число, а рядом маленькую букву 's' (в 2.5 раза меньше и сероватого цвета)
            // Размер в процентах: 100% / 2.5 = 40%
            var secondsValue = Mathf.Max(_currentTimer, 0f).ToString("F1");
            timerText.text = $"{secondsValue}<size=40%><color=#A0A0A0>s</color></size>";
        }
    }
}
