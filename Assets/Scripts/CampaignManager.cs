using UnityEngine;
using DG.Tweening;
using Cysharp.Threading.Tasks;
using System;
using YG;

public class CampaignManager : MonoBehaviour
{
    public HexGrid gridPrefab;
    public Transform gridParent;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float offset;
    [SerializeField] private TimerManager timerManager;
    [SerializeField] private UserRatingManager userRatingManager;
    [SerializeField] private CameraFieldFitter cameraFieldFitter;
    private HexGrid _currentGrid;
    private HexGrid _previousGrid;
    private int currentLevel = 1;
    private Transform currentContainer;

    private void Start() => StartLevel(currentLevel);

    private void StartLevel(int level)
    {
        currentContainer = new GameObject($"GridContainer_{level}").transform;

        float newX = 0;
        if (_currentGrid) _previousGrid = _currentGrid;

        if (level > 1)
        {
            newX = offset * level;
            currentContainer.position = new Vector3(newX, 0, 0);
            cameraTransform.DOLocalMoveX(newX, 0.8f)
                .SetEase(Ease.InOutCubic)
                .OnComplete(() => { if (_previousGrid) Destroy(_previousGrid.gameObject); })
                .Play();
        }

        currentContainer.SetParent(gridParent);
        currentContainer.localScale = Vector3.one;

        _currentGrid = Instantiate(gridPrefab, currentContainer);
        _currentGrid.transform.localScale = Vector3.one;

        // Настройка параметров
        _currentGrid.hexRadiusRings = GetRadiusForLevel(level);
        _currentGrid.mineRate = Mathf.Clamp01(0.08f + 0.02f * (level - 1));  // плавный рост сложности

        // 🔹 ТОЛЬКО пустая сетка (мины появятся после клика)
        _currentGrid.GenerateEmptyGridHex();

        // подписка на событие победы
        _currentGrid.OnGridCompleted += () => OnLevelWin();
    }

    private void OnEnable()
    {
        GameManager.Instance.WinEvent  += OnLevelWin;
        GameManager.Instance.LoseEvent += OnLevelLose;
    }
    
    private void OnDisable()
    {
        GameManager.Instance.WinEvent -= OnLevelWin;
        GameManager.Instance.LoseEvent -= OnLevelLose;
    }

    private int GetRadiusForLevel(int level)
    {
        int radius = 2 + (level / 1);
        if (level % 1 == 0)
        {
            float newY = cameraTransform.position.y + 2.3F * level;
            float newZ = cameraTransform.position.z - 2.3F * level;


            cameraFieldFitter.FitPerspectiveCameraToField(radius);
            // cameraTransform.position = new Vector3(
            //     cameraTransform.position.x,
            //     newY,
            //     newZ
            // );
        }
        return radius;
    }

    public void OnLevelWin()
    {
        GameManager.Instance.ShowWinPanel();
        userRatingManager.AddPoints(currentLevel*currentLevel);
        NextLevelRoutine();
    }

    public async void OnLevelLose()
    {
        userRatingManager.RemovePoints(5);
        await UniTask.Delay(TimeSpan.FromSeconds(1f));
        await _currentGrid.ExplodeChainAsync(_currentGrid.LastRevealedCell);
        GameManager.Instance.ShowLosePanel();
    }

    private void NextLevelRoutine()
    {
        currentLevel++;
        GameManager.Instance.UpdateLevelText(currentLevel);
        // timerManager.AddTime(currentLevel*currentLevel);
        StartLevel(currentLevel);
    }
}
