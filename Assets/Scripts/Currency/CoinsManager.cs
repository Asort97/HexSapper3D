using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Управляет монетами: выпадение с мины, полёт и анимация счётчика.
/// Разместите в сцене (желательно под Canvas) и задайте ссылки в инспекторе.
/// </summary>
public class CoinsManager : MonoBehaviour
{
    public static CoinsManager Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private Canvas canvas;
    [SerializeField] private RectTransform coinTarget; // иконка/слот в правом верхнем углу
    [SerializeField] private TMP_Text coinText;        // текст счётчика
    [SerializeField] private Image flyingCoinPrefab;   // префаб UI-иконки монеты (Image)
    [SerializeField] private Camera worldCamera;       // камера для конвертации (если Canvas не Overlay)

    [Header("Config")]
    [SerializeField, Range(0f, 1f)] private float dropChanceFromMine = 0.5f;
    [SerializeField] private int coinValuePerDrop = 10; // 1 выпавшая монета = +10 в счётчик

    private int _coins;
    private int _visibleCoins;
    private bool _playingTick;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (canvas == null) canvas = GetComponentInParent<Canvas>();
        _coins = PlayerPrefs.GetInt("coins", 0);
        _visibleCoins = _coins;
        UpdateText();
    }

    private void UpdateText()
    {
        if (coinText != null)
            coinText.text = _visibleCoins.ToString();
    }

    public bool TryDropFromMine(Vector3 worldPos)
    {
        if (UnityEngine.Random.value > dropChanceFromMine) return false;
        SpawnAndFlyCoin(worldPos, coinValuePerDrop);
        return true;
    }

    public void SpawnAndFlyCoin(Vector3 worldPos, int amount)
    {
        if (canvas == null || coinTarget == null || flyingCoinPrefab == null) return;

        // Создаём летящую иконку монеты
        var icon = Instantiate(flyingCoinPrefab, canvas.transform);
        var iconRt = icon.rectTransform;
        iconRt.sizeDelta = new Vector2(64, 64);
        icon.color = Color.yellow;

        var screen = Camera.main != null ? Camera.main.WorldToScreenPoint(worldPos) : worldPos;
        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform, screen, null, out var localPoint);
            iconRt.anchoredPosition = localPoint;
        }
        else
        {
            var cam = worldCamera != null ? worldCamera : Camera.main;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform, screen, cam, out var localPoint);
            iconRt.anchoredPosition = localPoint;
        }

        // Анимация полёта к целевой иконке
        SoundManager.Instance?.Play(SfxType.Coin_Spawn);
        iconRt
            .DOScale(1.0f, 0.15f)
            .From(0.2f)
            .SetEase(Ease.OutBack);

        var targetPos = coinTarget.anchoredPosition;
        iconRt
            .DOAnchorPos(targetPos, 0.8f)
            .SetEase(Ease.InCubic)
            .OnComplete(() =>
            {
                SoundManager.Instance?.Play(SfxType.Coin_Fly);
                Destroy(icon.gameObject);
                AddCoinsAnimated(amount);
            })
            .Play();
    }

    public void AddCoinsAnimated(int amount)
    {
        _coins += amount;
        PlayerPrefs.SetInt("coins", _coins);

        // Плавная анимация прироста цифры
        int start = _visibleCoins;
        int end = _coins;
        float duration = Mathf.Clamp(0.4f + (end - start) * 0.02f, 0.4f, 1.2f);

        // Звук тика во время повышения
        PlayTickLoop(duration).Forget();

        DOTween.To(() => start, v =>
        {
            _visibleCoins = v;
            UpdateText();
        }, end, duration)
        .SetEase(Ease.Linear)
        .OnComplete(() =>
        {
            _visibleCoins = end;
            UpdateText();
            _playingTick = false;
        })
        .Play();
    }

    private async UniTaskVoid PlayTickLoop(float duration)
    {
        if (_playingTick) return;
        _playingTick = true;

        float t = 0f;
        float interval = 0.08f;
        while (t < duration && _playingTick)
        {
            SoundManager.Instance?.Play(SfxType.Coin_Tick);
            await UniTask.Delay(TimeSpan.FromSeconds(interval));
            t += interval;
        }
    }
}
