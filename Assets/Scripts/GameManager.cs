using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour, ICampaignListener
{
    [Header("Core Managers")]
    [SerializeField] private CampaignManager campaignManager;
    [SerializeField] private UserRatingManager userRatingManager;

    [Header("UI")]
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private CanvasGroup menuGroup;
    [SerializeField] private CanvasGroup gameplayGroup;

    [Header("Win UI")]
    [SerializeField] private RectTransform winPanel;
    [SerializeField] private ParticleSystem winParticles;

    [Header("Ad Revive")]
    [SerializeField] private RectTransform adPanel;
    [SerializeField] private float adPanelTimer = 10f;
    [SerializeField] private TMP_Text adPanelTimerText;
    [SerializeField] private Image adPanelTimerBar;

    private Sequence _winTween;
    private Sequence _startGameSequence;

    private bool _sessionStarted;
    private bool _gameplayActive;
    private bool _adPanelVisible;
    private bool _adReviveUsed;
    private bool _isProcessingLose;

    private float _adTimer;
    private HexCell _pendingMineCell;
    private int _pendingLevel;

    private void Awake()
    {
        _adTimer = adPanelTimer;

        if (campaignManager != null)
        {
            campaignManager.Initialize(this);
        }

        if (userRatingManager != null)
        {
            userRatingManager.RankPromoted += HandleRankPromoted;
        }
    }

    private void Start()
    {
        BuildWinTween();
        BuildStartSequence();
        UpdateAdTimerUI();

        if (gameplayGroup != null)
        {
            gameplayGroup.alpha = 0f;
            gameplayGroup.gameObject.SetActive(false);
        }

        userRatingManager?.BroadcastState();
    }

    private void Update()
    {
        UpdateAdTimer();
    }

    public void StartGame()
    {
        if (_sessionStarted) return;

        _sessionStarted = true;
        _adReviveUsed = false;

        DisableGameplay();
        campaignManager?.BeginCampaign();

        if (gameplayGroup != null)
        {
            gameplayGroup.gameObject.SetActive(true);
            gameplayGroup.alpha = 0f;
            gameplayGroup.DOFade(1f, 1f).Play();
        }

        if (menuGroup != null)
        {
            menuGroup.gameObject.SetActive(true);
        }

        _startGameSequence?.Restart();
    }

    public void ShowAd()
    {
        if (_pendingMineCell == null || _isProcessingLose) return;

        _adReviveUsed = true;
        HideAdPanel();
        ResolveLevelCompletion(_pendingLevel);
    }

    public void SkipAd()
    {
        TriggerLose();
    }

    public void Lose()
    {
        TriggerLose();
    }

    public void ShowNewRankPanel(string previousRank, string newRank)
    {
        // Hook up dedicated UI panel here.
    }

    public void UpdateLevelText(int level)
    {
        if (levelText != null)
        {
            levelText.text = $"Уровень {level}";
        }
    }

    public void OnLevelStarted(int level, HexGrid grid)
    {
        UpdateLevelText(level);
        ResetAdPanelState();
        ClearPendingMine();
    }

    public void OnLevelCompleted(int level)
    {
        // Победа: звук перед показом анимации победы
        SoundManager.Instance?.Play(SfxType.Win);
        ResolveLevelCompletion(level);
    }

    public void OnMineTriggered(int level, HexCell mineCell)
    {
        if (_isProcessingLose) return;

        _pendingLevel = level;
        _pendingMineCell = mineCell;

    DisableGameplay();

    // Шанс дропа монеты из мины
    CoinsManager.Instance?.TryDropFromMine(mineCell != null ? mineCell.transform.position : Vector3.zero);

        if (!_adReviveUsed)
        {
            ShowAdPanel();
        }
        else
        {
            TriggerLose();
        }
    }

    private void ResolveLevelCompletion(int level)
    {
        DisableGameplay();

        _winTween?.Restart();

        if (userRatingManager != null)
        {
            userRatingManager.AddPoints(level * level);
        }

        ResetAdPanelState();
        ClearPendingMine();

        campaignManager?.AdvanceToNextLevel();
        EnableGameplay();
    }

    private void BuildWinTween()
    {
        if (winPanel == null) return;

        winPanel.gameObject.SetActive(false);

        _winTween = DOTween.Sequence()
            .AppendCallback(() => winPanel.gameObject.SetActive(true))
            .Append(winPanel.DOScale(1.4f, 0.7f).SetEase(Ease.OutBounce))
            .InsertCallback(0.2f, () => { winParticles?.Play(); SoundManager.Instance?.Play(SfxType.Win_Confetti); })
            .AppendInterval(1.5f)
            .Append(winPanel.DOScale(0.5f, 0.3f).SetEase(Ease.OutBounce))
            .SetAutoKill(false)
            .OnComplete(() =>
            {
                winPanel.gameObject.SetActive(false);
            });
    }

    private void BuildStartSequence()
    {
        if (menuGroup == null) return;

        _startGameSequence = DOTween.Sequence()
            .Append(menuGroup.DOFade(0f, 1f))
            .Join(menuGroup.transform.DOScale(2f, 1f))
            .SetAutoKill(false)
            .OnComplete(() =>
            {
                menuGroup.gameObject.SetActive(false);
                menuGroup.transform.localScale = Vector3.one;
                EnableGameplay();
            });
    }

    private void EnableGameplay()
    {
        if (!_sessionStarted) return;

        _gameplayActive = true;
        campaignManager?.EnableInput(true);
    }

    private void DisableGameplay()
    {
        _gameplayActive = false;
        campaignManager?.EnableInput(false);
    }

    private void ShowAdPanel()
    {
        if (adPanel == null) return;

        _adPanelVisible = true;
        _adTimer = adPanelTimer;
        UpdateAdTimerUI();

        adPanel.gameObject.SetActive(true);
        adPanel.anchoredPosition = new Vector2(0f, -1600f);

        adPanel
            .DOAnchorPosY(0f, 0.5f)
            .SetEase(Ease.OutSine)
            .Play();
    }

    private void HideAdPanel()
    {
        if (adPanel == null) return;

        _adPanelVisible = false;
        _adTimer = adPanelTimer;
        UpdateAdTimerUI();

        adPanel
            .DOAnchorPosY(-1600f, 0.8f)
            .SetEase(Ease.OutSine)
            .Play()
            .OnComplete(() => adPanel.gameObject.SetActive(false));
    }

    private void UpdateAdTimer()
    {
        if (!_adPanelVisible || adPanel == null || !adPanel.gameObject.activeInHierarchy) return;

        _adTimer -= Time.deltaTime;
        if (_adTimer <= 0f)
        {
            _adTimer = 0f;
            UpdateAdTimerUI();
            TriggerLose();
            return;
        }

        UpdateAdTimerUI();
    }

    private void UpdateAdTimerUI()
    {
        float fill = 1f;
        if (Math.Abs(adPanelTimer) > Mathf.Epsilon)
        {
            fill = Mathf.Clamp01(_adTimer / adPanelTimer);
        }

        if (adPanelTimerBar != null)
        {
            adPanelTimerBar.fillAmount = fill;
        }

        if (adPanelTimerText != null)
        {
            adPanelTimerText.text = $"{Mathf.Max(_adTimer, 0f):F1}s";
        }
    }

    private void TriggerLose()
    {
        if (_isProcessingLose) return;
        _isProcessingLose = true;

        HideAdPanel();
        ResetAdPanelState();

        if (userRatingManager != null)
        {
            userRatingManager.RemovePoints(5);
        }

        // Проигрышный звук
        SoundManager.Instance?.Play(SfxType.Lose);
        _ = ResolveLoseAsync();
    }

    private async UniTask ResolveLoseAsync()
    {
        if (campaignManager != null)
        {
            await campaignManager.PlayLoseSequenceAsync(_pendingMineCell);
        }

        ShowLosePanel();
        ClearPendingMine();
        _isProcessingLose = false;
    }

    private void ResetAdPanelState()
    {
        _adPanelVisible = false;
        _adTimer = adPanelTimer;
        UpdateAdTimerUI();
    }

    private void ClearPendingMine()
    {
        _pendingMineCell = null;
        _pendingLevel = 0;
    }

    private void HandleRankPromoted(string previousRank, string currentRank)
    {
        ShowNewRankPanel(previousRank, currentRank);
        SoundManager.Instance?.Play(SfxType.Rank_Up);
    }

    private void ShowLosePanel()
    {
        // Implement dedicated lose UI here.
    }

    private void OnDestroy()
    {
        if (userRatingManager != null)
        {
            userRatingManager.RankPromoted -= HandleRankPromoted;
        }
    }
}
