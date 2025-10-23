using System;
using TMPro;
using UnityEngine;
using YG;

public class UserRatingManager : MonoBehaviour
{
    // [Serializable]
    // public struct Rank
    // {
    //     public string rankName;
    //     public int score;
    // }

    // [SerializeField] private Rank[] ranks;
    [SerializeField] private RankHierarchy rankHierarchy;
    public static Action<float, int, int> OnChangeScore;
    public static Action<string, string> OnChangeRank;
    public int Score;
    private int _rank;

    private void Start()
    {
        Score = YandexGame.savesData.score;
        _rank = YandexGame.savesData.rank;
        
        UpdateBars();
    }

    private void UpdateBars()
    {
        float barAmount = GetNormalizedRankProgress();

        Debug.Log(barAmount);

        OnChangeScore?.Invoke(barAmount, Score, GetNextRank().score);
        OnChangeRank?.Invoke(GetCurrentRank().rankName, GetNextRank().rankName);
    }

    [ContextMenu("Clear saves")]
    public void RemoveSaves()
    {
        YandexGame.ResetSaveProgress();
        YandexGame.SaveProgress();
    }

    public void AddPoints(int amount)
    {
        Score += amount;


        YandexGame.savesData.score = Score;
        YandexGame.SaveProgress();

        CheckRank();

        UpdateBars();
    }

    public void RemovePoints(int amount)
    {
        Score = Mathf.Max(Score - amount, GetCurrentRank().score);

        UpdateBars();
        
        YandexGame.savesData.score = Score;
        YandexGame.SaveProgress();
    }
    
    private float GetNormalizedRankProgress()
    {
        if (rankHierarchy.ranks == null || rankHierarchy.ranks.Length == 0) return 0f;

        if (_rank >= rankHierarchy.ranks.Length - 1)
            return 1f;

        int currentRankPoints = GetCurrentRank().score;
        int nextRankPoints = GetNextRank().score;

        // нормализуем от 0 до 1
        Debug.Log($"CURR: {currentRankPoints} NEXT: {nextRankPoints}");
        return Mathf.InverseLerp(currentRankPoints, nextRankPoints, Score);
    }

    private void CheckRank()
    {
        if (GetCurrentRank().rankName == GetNextRank().rankName) return;

        if (Score >= GetNextRank().score)
        {
            _rank++;

            YandexGame.savesData.rank = _rank;
            YandexGame.SaveProgress();


            int previousRank = Mathf.Clamp(_rank - 1, 0, rankHierarchy.ranks.Length - 1);

            OnChangeRank?.Invoke(GetCurrentRank().rankName, GetNextRank().rankName);

            GameManager.Instance.ShowNewRankPanel(GetPreviousRank().rankName, GetCurrentRank().rankName);
        }
    }

    private Rank GetCurrentRank()
    {
        return rankHierarchy.ranks[Mathf.Clamp(_rank, 0, rankHierarchy.ranks.Length - 1)];
    }

    private Rank GetNextRank()
    {
        return rankHierarchy.ranks[Mathf.Clamp(_rank+1, 0, rankHierarchy.ranks.Length - 1)];
    }
    
    private Rank GetPreviousRank()
    {
        return rankHierarchy.ranks[Mathf.Clamp(_rank-1, 0, rankHierarchy.ranks.Length - 1)];
    }
}