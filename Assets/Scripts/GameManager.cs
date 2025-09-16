using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [SerializeField] private TMP_Text levelText;
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

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        _adTimer = adPanelTimer;
    }

    private void Update()
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