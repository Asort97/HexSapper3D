using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [SerializeField] private TMP_Text levelText;

    [Header("Menus")]
    [SerializeField] private CanvasGroup menuGroup;
    [SerializeField] private CanvasGroup gameplayGroup;

    [Header("Win UI")]
    [SerializeField] private RectTransform winPanel;
    [SerializeField] private ParticleSystem winParticles;
    private Sequence _winTween;

    [Space(5)]
    [Header("AD Revive")]
    [SerializeField] private RectTransform adPanel;
    [SerializeField] private float adPanelTimer = 10f;
    [SerializeField] private TMP_Text adPanelTimerText;
    [SerializeField] private Image adPanelTimerBar;
    public static GameManager Instance;
    private bool _adPanelShowed;
    public bool AdReviveUsed;
    private float _adTimer;
    public Action LoseEvent;
    public Action WinEvent;
    public bool isGameStarted;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        _adTimer = adPanelTimer;

        _winTween = DOTween.Sequence()
        .AppendCallback(() => winPanel.gameObject.SetActive(true))
        .Append(winPanel.DOScale(1.4f, 0.7f).SetEase(Ease.OutBounce))
        .InsertCallback(0.2f, () => winParticles.Play())
        .AppendInterval(1.5f)
        .Append(winPanel.DOScale(0.5f, 0.5f).SetEase(Ease.InSine))
        .SetAutoKill(false)
        .OnComplete(()=> winPanel.gameObject.SetActive(false));
    }
    
    private void Update()
    {
        AdTimer();
    }

    public void StartGame()
    {
        menuGroup.DOFade(0, 1f).Play().OnComplete(()=> { menuGroup.gameObject.SetActive(false); isGameStarted = true; });

        gameplayGroup.gameObject.SetActive(true);
        gameplayGroup.DOFade(1, 1f).Play();
    }

    public void ShowWinPanel()
    {
        // winParticles.Play();
        _winTween.Restart();
        isGameStarted = true;

        // await UniTask.Delay(TimeSpan.FromSeconds(2.5f));
    }

    private void AdTimer()
    {
        if (_adPanelShowed && adPanel.gameObject.activeInHierarchy)
        {
            if (_adTimer >= 0f)
            {
                _adTimer -= Time.deltaTime;
            }
            else
            {
                ShowAdPanel(false);

                LoseEvent?.Invoke();
            }

            adPanelTimerBar.fillAmount = _adTimer / adPanelTimer;
            adPanelTimerText.text = _adTimer.ToString("F1") + "s";
        }
    }

    public void UpdateLevelText(int level)
    {
        levelText.text = $"Уровень {level}";
    }

    public async void ShowAdPanel(bool show)
    {
        await UniTask.Delay(TimeSpan.FromSeconds(1f));

        if (show)
        {
            adPanel.gameObject.SetActive(true);
            adPanel.anchoredPosition = new Vector2(0, -1600);

            adPanel.DOAnchorPosY(0f, 0.5f)
            .SetEase(Ease.OutSine)
            .Play();

            _adPanelShowed = true;
        }
        else
        {
            adPanel.DOAnchorPosY(-1600f, 0.8f)
            .SetEase(Ease.OutSine)
            .Play()
            .OnComplete(() => adPanel.gameObject.SetActive(false));

            _adPanelShowed = false;
        }
    }

    public void ShowAd()
    {
        AdReviveUsed = true;

        adPanel.DOAnchorPosY(-1600f, 0.8f)
        .SetEase(Ease.OutSine)
        .Play()
        .OnComplete(() => adPanel.gameObject.SetActive(false));

        WinEvent.Invoke();
    }

    public void SkipAd()
    {
        adPanel.DOAnchorPosY(-1600f, 0.8f)
        .SetEase(Ease.OutSine)
        .Play()
        .OnComplete(() => adPanel.gameObject.SetActive(false));

        LoseEvent?.Invoke();
    }

    public void Lose()
    {
        LoseEvent?.Invoke();
    }
}