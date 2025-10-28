using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using YG;

public interface ICampaignListener
{
    void OnLevelStarted(int level, HexGrid grid);
    void OnLevelCompleted(int level);
    void OnMineTriggered(int level, HexCell mineCell);
}

public class CampaignManager : MonoBehaviour
{
    [Header("Prefabs & Parents")]
    public HexGrid gridPrefab;
    public Transform gridParent;

    [Header("Camera")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float offset;
    [SerializeField] private CameraFieldFitter cameraFieldFitter;

    private HexGrid _currentGrid;
    private HexGrid _previousGrid;
    private Transform _currentContainer;
    private ICampaignListener _listener;

    private int _currentLevel = 1;
    private bool _campaignStarted;
    private Vector3 _initialCameraPosition;
    private int _lastRadius = -1;

    public HexGrid CurrentGrid => _currentGrid;
    public int CurrentLevel => _currentLevel;

    public void Initialize(ICampaignListener listener)
    {
        _listener = listener;
    }

    public void BeginCampaign()
    {
        if (_campaignStarted) return;
        _campaignStarted = true;
        _initialCameraPosition = cameraTransform.position;
        StartLevel(_currentLevel);
    }

    public void AdvanceToNextLevel()
    {
        _currentLevel++;
        StartLevel(_currentLevel);
    }

    public void EnableInput(bool enabled)
    {
        _currentGrid?.SetInteractionState(enabled);
    }

    public async UniTask PlayLoseSequenceAsync(HexCell sourceCell)
    {
        if (_currentGrid == null) return;

        var startCell = sourceCell != null ? sourceCell : _currentGrid.LastRevealedCell;
        if (startCell == null) return;

        await UniTask.Delay(TimeSpan.FromSeconds(1f));
        await _currentGrid.ExplodeChainAsync(startCell);
    }

    private void StartLevel(int level)
    {
        DetachCurrentGrid();

        _currentContainer = new GameObject($"GridContainer_{level}").transform;

        float newX = 0f;

        if (level > 1)
        {
            newX = offset * level;
            Debug.Log(newX);
            _currentContainer.position = new Vector3(newX, 0f, 0f);
            cameraTransform
                .DOLocalMoveX(newX, 0.8f)
                .SetEase(Ease.InOutCubic)
                .OnComplete(() =>
                {
                    if (_previousGrid != null)
                    {
                        Destroy(_previousGrid.gameObject);
                        _previousGrid = null;
                    }
                })
                .Play();
        }

        _currentContainer.SetParent(gridParent);
        _currentContainer.localScale = Vector3.one;

        _currentGrid = Instantiate(gridPrefab, _currentContainer);
        _currentGrid.transform.localScale = Vector3.one;

        _currentGrid.hexRadiusRings = GetRadiusForLevel(level);
        _currentGrid.mineRate = Mathf.Clamp01(0.08f + 0.02f * (level - 1));
        _currentGrid.GenerateEmptyGridHex();

        _currentGrid.OnGridCompleted += HandleLevelCompleted;
        _currentGrid.MineTriggered += HandleMineTriggered;
        _currentGrid.SetInteractionState(false);

        _listener?.OnLevelStarted(level, _currentGrid);
    }

    private void DetachCurrentGrid()
    {
        if (_currentGrid == null) return;

        _currentGrid.OnGridCompleted -= HandleLevelCompleted;
        _currentGrid.MineTriggered -= HandleMineTriggered;
        _previousGrid = _currentGrid;
        _currentGrid = null;
    }

    private void HandleLevelCompleted()
    {
        _listener?.OnLevelCompleted(_currentLevel);
    }

    private void HandleMineTriggered(HexCell cell)
    {
        _listener?.OnMineTriggered(_currentLevel, cell);
    }

    private int GetRadiusForLevel(int level)
    {
        int radius = 2 + (YandexGame.savesData.rank / 1);

        // int radius = 2 + (level / 1);
        
        // Применяем фит камеры только если радиус изменился
        if (radius != _lastRadius)
        {
            _lastRadius = radius;
            cameraFieldFitter.FitPerspectiveCameraToField(radius);
        }

        return radius;
    }
}
