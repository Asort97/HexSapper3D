using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public enum Rank
{
    Beginner = 0,
    Sapper   = 1,
    Veteran  = 2,
    Master   = 3,
    Legend   = 4
}

public class UserRatingManager : MonoBehaviour
{
    [Header("UI (optional)")]
    [SerializeField] private Image progressFill;     // type = Filled (Horizontal)
    [SerializeField] private TMP_Text rankText;      // например: "Veteran"
    [SerializeField] private TMP_Text remainText;    // например: "Осталось: 37"

    [Header("Config")]
    public const int POINTS_PER_RANK = 100;
    [SerializeField] private Rank startRank = Rank.Beginner;
    [SerializeField] private int startPointsInRank = 0; // 0..99

    // State
    public Rank CurrentRank { get; private set; }
    public int PointsInRank { get; private set; } // 0..POINTS_PER_RANK-1 (на максимальном ранге = cap)

    // Events
    public event Action<Rank> OnRankChanged;
    public event Action<int, int> OnPointsChanged; // (pointsInRank, pointsToNext)

    private int MaxRankIndex => Enum.GetValues(typeof(Rank)).Length - 1;
    private int CurrentRankIndex => (int)CurrentRank;

    void Awake()
    {
        CurrentRank = startRank;
        PointsInRank = Mathf.Clamp(startPointsInRank, 0, POINTS_PER_RANK - 1);
        UpdateUI();
    }

    // ====== Публичные методы ======

    /// Добавить очки. Прокачивает ранги, если перешли порог 100.
    public void AddPoints(int amount)
    {
        if (amount <= 0) return;

        int total = GetTotalPoints();
        total += amount;

        SetFromTotalPoints(total);
    }

    /// Отнять очки. Может понизить ранг (но не ниже нулевого).
    public void RemovePoints(int amount)
    {
        if (amount <= 0) return;

        int total = GetTotalPoints();
        total = Mathf.Max(0, total - amount);

        SetFromTotalPoints(total);
    }

    /// Прямое изменение ранга (например, +1/-1). Точки внутри ранга сохраняются/обрезаются.
    public void AddRank(int delta)
    {
        if (delta == 0) return;
        int idx = Mathf.Clamp(CurrentRankIndex + delta, 0, MaxRankIndex);
        var newRank = (Rank)idx;

        if (newRank != CurrentRank)
        {
            CurrentRank = newRank;
            if (IsAtMaxRank()) PointsInRank = POINTS_PER_RANK - 1; // заполнен до края
            OnRankChanged?.Invoke(CurrentRank);
            UpdateUI();
        }
    }

    // ====== Вспомогательные ======

    /// Сколько очков осталось до следующего ранга (на максимуме = 0).
    public int PointsToNext()
    {
        if (IsAtMaxRank()) return 0;
        return POINTS_PER_RANK - PointsInRank - 1;
    }

    /// Нормализованный прогресс текущего ранга [0..1].
    public float Progress01()
    {
        return IsAtMaxRank()
            ? 1f
            : Mathf.Clamp01((PointsInRank + 1) / (float)POINTS_PER_RANK);
    }

    /// Общая сумма очков по всем рангам.
    public int GetTotalPoints()
    {
        return CurrentRankIndex * POINTS_PER_RANK + PointsInRank;
    }

    /// Установить состояние из «общих очков».
    private void SetFromTotalPoints(int totalPoints)
    {
        int maxTotal = MaxRankIndex * POINTS_PER_RANK + (POINTS_PER_RANK - 1);
        totalPoints = Mathf.Clamp(totalPoints, 0, maxTotal);

        int newRankIdx = totalPoints / POINTS_PER_RANK;
        int newPointsInRank = totalPoints % POINTS_PER_RANK;

        bool rankChanged = (newRankIdx != CurrentRankIndex);
        CurrentRank = (Rank)newRankIdx;
        PointsInRank = newPointsInRank;

        if (IsAtMaxRank())
            PointsInRank = POINTS_PER_RANK - 1;

        OnPointsChanged?.Invoke(PointsInRank, PointsToNext());
        if (rankChanged) OnRankChanged?.Invoke(CurrentRank);

        UpdateUI();
    }

    private bool IsAtMaxRank() => CurrentRankIndex >= MaxRankIndex;

    private void UpdateUI()
    {
        if (progressFill != null)
        {
            progressFill.fillAmount = Progress01();
        }
        if (rankText != null)
        {
            rankText.text = CurrentRank.ToString();
        }
        if (remainText != null)
        {
            remainText.text = IsAtMaxRank()
                ? "Макс."
                : $"Осталось: {PointsToNext()}";
        }
    }
}
