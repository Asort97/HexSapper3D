using UnityEngine;
using DG.Tweening;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using System;

public class CampaignManager : MonoBehaviour
{
    public HexGrid gridPrefab;   // твой HexGrid префаб
    public Transform gridParent;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float offset;
    [SerializeField] private TimerManager timerManager;
    [SerializeField] private UserRatingManager userRatingManager;

    private HexGrid _currentGrid;
    private HexGrid _previousGrid;
    private int currentLevel = 1;
    private Transform currentContainer;

    private void Start()
    {
        StartLevel(currentLevel);
    }

    private void StartLevel(int level)
    {
        // контейнер
        currentContainer = new GameObject($"GridContainer_{level}").transform;

        float newX = 0;

        if(_currentGrid)
            _previousGrid = _currentGrid;

        if (level > 1)
        {
            newX = offset * level;
            currentContainer.position = new Vector3(newX, 0, 0);
            cameraTransform.DOLocalMoveX(newX, 0.8f)
            .SetEase(Ease.InOutCubic)
            .Play()
            .OnComplete(() =>
            {
                if (_previousGrid != null)
                {
                    Destroy(_previousGrid.gameObject);
                }
            });
        }

        currentContainer.SetParent(gridParent);

        currentContainer.localScale = Vector3.one;           // 1 во время генерации!

        // грид
        _currentGrid = Instantiate(gridPrefab, currentContainer);
        _currentGrid.transform.localScale = Vector3.one;

        // генерим поле
        _currentGrid.hexRadiusRings = GetRadiusForLevel(level);
        _currentGrid.hexWorldRadius = Mathf.Max(0.1f, _currentGrid.hexWorldRadius); // страхуемся
        _currentGrid.explicitMineCount = GetMinesForLevel(level);
        _currentGrid.GenerateEmptyGridHex();
        _currentGrid.PlaceMines(null);
        _currentGrid.ComputeAdjacency();

        _currentGrid.OnGridCompleted += () =>
        {
            Debug.Log("Level complete → запускаем переход");
            OnLevelWin(); // твой метод перехода к следующему уровню
        };

    }

    private void OnEnable()
    {
        GameManager.Instance.WinEvent += OnLevelWin;
        GameManager.Instance.LoseEvent += OnLevelLose;
    }

    private void OnDisable()
    {
        GameManager.Instance.WinEvent -= OnLevelWin; 
        GameManager.Instance.LoseEvent -= OnLevelLose;    
    }

    private int GetRadiusForLevel(int level)
    {
        // каждые 4 уровня +1 радиус
        return 2 + (level / 4);
    }

    private int GetMinesForLevel(int level)
    {
        // базовое количество + рост
        return 1 + level * 2;
    }

    public void OnLevelWin()
    {
        userRatingManager.AddPoints(30);

        NextLevelRoutine();
    }

    public async void OnLevelLose()
    {
        userRatingManager.RemovePoints(5);

        await UniTask.Delay(TimeSpan.FromSeconds(1f));
        await _currentGrid.ExplodeChainAsync(_currentGrid.LastRevealedCell);    
    }

    private void NextLevelRoutine()
    {
        currentLevel++;

        GameManager.Instance.UpdateLevelText(currentLevel);
        timerManager.AddTime(30);
        
        StartLevel(currentLevel);
    }
}
