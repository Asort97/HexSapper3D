using System;
using UnityEngine;
using YG;

public class UserRatingManager : MonoBehaviour
{
    [SerializeField] private RankHierarchy rankHierarchy;

    public event Action<float, int, int> ScoreChanged;
    public event Action<string, string> RankChanged;
    public event Action<string, string> RankPromoted;

    public int Score { get; private set; }

    private int _rankIndex;

    private void Start()
    {
        Score = YandexGame.savesData.score;
        _rankIndex = YandexGame.savesData.rank;

        BroadcastState();
    }

    public void BroadcastState()
    {
        UpdateStateBroadcast();
    }

    [ContextMenu("Clear saves")]
    public void RemoveSaves()
    {
        YandexGame.ResetSaveProgress();
        YandexGame.SaveProgress();

        Score = YandexGame.savesData.score;
        _rankIndex = YandexGame.savesData.rank;

        BroadcastState();
    }

    public void AddPoints(int amount)
    {
        Score += amount;
        YandexGame.savesData.score = Score;
        YandexGame.SaveProgress();

        CheckRankPromotion();
        UpdateStateBroadcast();
    }

    public void RemovePoints(int amount)
    {
        Score = Mathf.Max(Score - amount, GetCurrentRank().score);
        YandexGame.savesData.score = Score;
        YandexGame.SaveProgress();

        UpdateStateBroadcast();
    }

    private void UpdateStateBroadcast()
    {
        float normalized = GetNormalizedRankProgress();

        ScoreChanged?.Invoke(normalized, Score, GetNextRank().score);
        RankChanged?.Invoke(GetCurrentRank().rankName, GetNextRank().rankName);
    }

    private float GetNormalizedRankProgress()
    {
        if (rankHierarchy == null || rankHierarchy.ranks == null || rankHierarchy.ranks.Length == 0)
            return 0f;

        if (_rankIndex >= rankHierarchy.ranks.Length - 1)
            return 1f;

        Rank current = GetCurrentRank();
        Rank next = GetNextRank();

        if (Mathf.Approximately(next.score, current.score))
            return 1f;

        return Mathf.InverseLerp(current.score, next.score, Score);
    }

    private void CheckRankPromotion()
    {
        if (rankHierarchy == null || rankHierarchy.ranks == null || rankHierarchy.ranks.Length == 0)
            return;

        if (_rankIndex >= rankHierarchy.ranks.Length - 1)
            return;

        Rank nextRank = GetNextRank();
        if (Score < nextRank.score)
            return;

        string previousRankName = GetCurrentRank().rankName;

        _rankIndex = Mathf.Min(_rankIndex + 1, rankHierarchy.ranks.Length - 1);
        YandexGame.savesData.rank = _rankIndex;
        YandexGame.SaveProgress();

        string currentRankName = GetCurrentRank().rankName;
        RankPromoted?.Invoke(previousRankName, currentRankName);
    }

    private Rank GetCurrentRank()
    {
        if (rankHierarchy == null || rankHierarchy.ranks == null || rankHierarchy.ranks.Length == 0)
            return default;

        int index = Mathf.Clamp(_rankIndex, 0, rankHierarchy.ranks.Length - 1);
        return rankHierarchy.ranks[index];
    }

    private Rank GetNextRank()
    {
        if (rankHierarchy == null || rankHierarchy.ranks == null || rankHierarchy.ranks.Length == 0)
            return default;

        int index = Mathf.Clamp(_rankIndex + 1, 0, rankHierarchy.ranks.Length - 1);
        return rankHierarchy.ranks[index];
    }
}

