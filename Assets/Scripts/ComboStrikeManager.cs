using UnityEngine.UI;
using TMPro;
using UniRx;
using UnityEngine;
using DG.Tweening;

public class ComboStrikeManager : MonoBehaviour
{
    public static ComboStrikeManager Instance;
    [SerializeField] private TMP_Text comboText;
    [SerializeField] private Image comboBar;
    [SerializeField] private CanvasGroup comboDisplayer;
    [SerializeField] private float comboLifetime = 5f;
    private float _remainingLifetime;
    private ReactiveProperty<int> _comboStrike  = new ReactiveProperty<int>(0);
    private Tween plusComboTween;
    private System.IDisposable _comboSubscription;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        _remainingLifetime = comboLifetime;

        _comboSubscription = _comboStrike.Subscribe(value => UpdateText(value));

        plusComboTween = comboText.transform.DOPunchScale(new Vector2(1, 1) * 0.5f, 0.25f, 0, 0).SetEase(Ease.OutSine).SetAutoKill(false);
    }

    private void OnDestroy()
    {
        _comboSubscription.Dispose();
    }
    
    private void UpdateText(int value) 
    {
        comboText.text = $"{value}x";
    }

    private void Update()
    {
        if (_remainingLifetime >= 0f)
        {
            _remainingLifetime -= Time.deltaTime;
            float normalized = Mathf.Clamp01(_remainingLifetime / comboLifetime);
            comboBar.fillAmount = normalized;
            comboDisplayer.alpha = normalized;
        }
        else
        {
            ResetCombo();
        }
    }

    private void ResetCombo()
    {
        _comboStrike.Value = 0;
    }
    
    public void TryAddCombo()
    {
        _comboStrike.Value += 1;
        _remainingLifetime = comboLifetime;

        plusComboTween.Restart();
    }
}
