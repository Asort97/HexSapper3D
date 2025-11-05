using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour, ICampaignListener
{
    [Header("Core Managers")]
    [SerializeField] private CampaignManager campaignManager;
    [SerializeField] private UserRatingManager userRatingManager;
    [SerializeField] private CameraZoomController cameraController;

    [Header("UI Panels")]
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private MainMenuPanel mainMenu;
    [SerializeField] private CanvasGroup gameplayGroup;
    [SerializeField] private WinPanel winPanel;
    [SerializeField] private AdPanel adPanel;
    [SerializeField] private SettingsPanel settingsPanel;

    private bool _sessionStarted;
    private bool _gameplayActive;
    private bool _adReviveUsed;
    private bool _isProcessingLose;
    private bool _settingsMenuOpen;

    private HexCell _pendingMineCell;
    private int _pendingLevel;

    private void Awake()
    {
        if (campaignManager != null)
        {
            campaignManager.Initialize(this);
        }

        if (campaignManager == null)
        {
            Debug.LogWarning("GameManager: CampaignManager not assigned in inspector. The level will not start until assigned.");
        }

            if (cameraController == null)
            {
                Debug.LogWarning("GameManager: CameraZoomController not assigned in inspector. Please assign it to enable camera input control.");
            }

        if (userRatingManager != null)
        {
            userRatingManager.RankPromoted += HandleRankPromoted;
        }

        // Подписываемся на истечение таймера ad панели
        if (adPanel != null)
        {
            adPanel.OnTimerExpired += TriggerLose;
        }
    }

    // Вызывается из UI кнопки "Start" — запускает ввод и скрывает меню
    public void StartGame()
    {
        if (!_sessionStarted)
        {
            _sessionStarted = true;
            _adReviveUsed = false;
            // Если кампания ещё не начала — запустить
            campaignManager?.BeginCampaign();
        }

        if (gameplayGroup != null)
        {
            gameplayGroup.gameObject.SetActive(true);
            gameplayGroup.alpha = 0f;
            gameplayGroup.DOFade(1f, 1f).Play();
        }

        EnableGameplay();

        if (mainMenu != null)
        {
            mainMenu.Hide();
        }
        else
        {
            Debug.LogWarning("GameManager: mainMenu not assigned in inspector. Cannot hide the main menu UI.");
        }
    }

    private void Start()
    {
        if (gameplayGroup != null)
        {
            gameplayGroup.alpha = 0f;
            gameplayGroup.gameObject.SetActive(false);
        }
        if (adPanel == null)
        {
            Debug.LogWarning("GameManager: AdPanel not assigned in inspector. Assign AdPanel to enable ad revive flow.");
        }

        // Запускаем кампанию при старте сцены, но не даём ввод до нажатия Start
        campaignManager?.BeginCampaign();

        if (_sessionStarted) return;

        _sessionStarted = true;
        _adReviveUsed = false;

        if (gameplayGroup != null)
        {
            gameplayGroup.gameObject.SetActive(true);
            gameplayGroup.alpha = 0f;
            gameplayGroup.DOFade(1f, 1f).Play();
        }

        // Отключаем ввод — игрок начнёт игру через StartGame()
        DisableGameplay();

        if (mainMenu != null)
        {
            // оставляем главное меню видимым; StartGame() скроет его
        }
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

        if (winPanel != null)
            winPanel.Show();

        if (userRatingManager != null)
        {
            userRatingManager.AddPoints(level * level);
        }

        ClearPendingMine();

        campaignManager?.AdvanceToNextLevel();
        EnableGameplay();
    }

    private void EnableGameplay()
    {
        if (!_sessionStarted) return;

        _gameplayActive = true;

        if (!_settingsMenuOpen)
        {
            campaignManager?.EnableInput(true);
            cameraController?.SetInputEnabled(true);
        }
    }

    private void DisableGameplay()
    {
        _gameplayActive = false;
        campaignManager?.EnableInput(false);
        cameraController?.SetInputEnabled(false);
    }

    private void ShowAdPanel()
    {
        adPanel?.Show();
    }

    private void HideAdPanel()
    {
        adPanel?.Hide();
    }

    private void TriggerLose()
    {
        if (_isProcessingLose) return;
        _isProcessingLose = true;

        HideAdPanel();

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

    public void OpenSettingsMenu(bool active)
    {
        if (settingsPanel == null) return;

        _settingsMenuOpen = active;

        if (active)
        {
            settingsPanel.Open();
            campaignManager?.EnableInput(false);
            cameraController?.SetInputEnabled(false);
        }
        else
        {
            settingsPanel.Close();

            if (_gameplayActive)
            {
                campaignManager?.EnableInput(true);
                cameraController?.SetInputEnabled(true);
            }
        }
    }

    private void OnDestroy()
    {
        if (userRatingManager != null)
        {
            userRatingManager.RankPromoted -= HandleRankPromoted;
        }
        
        if (adPanel != null)
        {
            adPanel.OnTimerExpired -= TriggerLose;
        }
    }
}
