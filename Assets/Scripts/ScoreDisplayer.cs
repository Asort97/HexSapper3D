using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ScoreDisplayer : MonoBehaviour
{
    private Image bar;
    [SerializeField] private UserRatingManager ratingManager;
    [SerializeField] private TMP_Text currentRank;
    [SerializeField] private TMP_Text nextRank;
    [SerializeField] private TMP_Text scoreText;

    private void Start()
    {
        bar = GetComponent<Image>();
    }

    private void OnEnable()
    {
        if (ratingManager == null) return;

        ratingManager.ScoreChanged += UpdateBar;
        ratingManager.RankChanged += UpdateRank;
        ratingManager.BroadcastState();
    }

    private void OnDisable()
    {
        if (ratingManager == null) return;

        ratingManager.ScoreChanged -= UpdateBar;
        ratingManager.RankChanged -= UpdateRank;
    }

    public void UpdateRank(string current, string next)
    {
        if (currentRank != null)
        {
            currentRank.text = current;
        }

        if (nextRank != null)
        {
            nextRank.text = next;
        }
    }   
     
    public void UpdateBar(float amount, int score, int needScore)
    {
        if (bar == null)
        {
            bar = GetComponent<Image>();
        }

        if (gameObject.activeInHierarchy)
        {
            bar.DOFillAmount(amount, 0.8f).SetEase(Ease.OutSine).Play();
        }
        else
        {
            bar.fillAmount = amount;
        }

        if(scoreText != null)
        {
            scoreText.text = $"{score}/{needScore}";
        }
    }
}
