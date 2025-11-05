using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Управляет монетами: выпадение с раскрытых клеток, полёт и анимация счётчика.
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
    [SerializeField] private int coinValuePerDrop = 1; // 1 выпавшая монета = +N в счётчик

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

    public bool TryDropCoin(Vector3 worldPos)
    {
        if (UnityEngine.Random.value > dropChanceFromMine) return false;
        
        // Спавним несколько монеток
        int coinCount = coinValuePerDrop;
        for (int i = 0; i < coinCount; i++)
        {
            float delay = i * 0.08f; // задержка между спавном монеток
            SpawnSingleCoin(worldPos, 1, delay);
        }
        return true;
    }

    private async void SpawnSingleCoin(Vector3 worldPos, int amount, float delay)
    {
        if (canvas == null || coinTarget == null || flyingCoinPrefab == null)
        {
            Debug.LogWarning("[CoinsManager] Missing references: canvas, coinTarget or flyingCoinPrefab!");
            return;
        }

        // Ждём задержку
        if (delay > 0)
        {
            await UniTask.Delay((int)(delay * 1000));
        }

        // Создаём летящую иконку монеты
        var icon = Instantiate(flyingCoinPrefab, canvas.transform);
        icon.gameObject.SetActive(true);
        var iconRt = icon.rectTransform;
        icon.color = Color.yellow;
        icon.raycastTarget = false;

        // Добавляем случайный разброс к стартовой позиции
        Vector3 randomOffset = new Vector3(
            UnityEngine.Random.Range(-5f, 5f),
            UnityEngine.Random.Range(-5f, 5f),
            0f
        );

        // Конвертируем мировую позицию клетки в UI координаты
        Vector2 startPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.transform as RectTransform,
            RectTransformUtility.WorldToScreenPoint(Camera.main, worldPos + randomOffset),
            null,
            out startPos);
        iconRt.anchoredPosition = startPos;

        // Целевая позиция — просто localPosition цели
        Vector2 endPos = coinTarget.localPosition;

        // SEQUENCE: появление → полёт
        var seq = DOTween.Sequence();
        iconRt.localScale = Vector3.one * 0.1f;
        seq.Append(iconRt.DOScale(1.0f, 0.22f).SetEase(Ease.OutBack));
        seq.AppendInterval(0.05f);
        seq.Append(iconRt.DOAnchorPos(endPos, 0.8f).SetEase(Ease.InCubic));
        seq.OnStart(() => {
            SoundManager.Instance?.Play(SfxType.Coin_Spawn);
        });
        seq.OnStepComplete(() => {
            // ...existing code...
        });
        seq.OnComplete(() => {
            SoundManager.Instance?.Play(SfxType.Coin_Fly);
            Destroy(icon.gameObject);
            if (coinTarget != null)
            {
                coinTarget.DOKill();
                coinTarget.DOScale(1.15f, 0.05f).SetEase(Ease.OutQuad).OnComplete(() =>
                {
                    coinTarget.DOScale(1f, 0.1f).SetEase(Ease.InOutQuad).Play();
                }).Play();
            }
            AddCoinsAnimated(amount);
        });
        seq.Play();
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
