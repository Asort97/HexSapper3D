using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ScoreDisplayer : MonoBehaviour
{
    private Image bar;
    [SerializeField] private TMP_Text currentRank;
    [SerializeField] private TMP_Text nextRank;
    [SerializeField] private TMP_Text scoreText;

    private void Start()
    {
        bar = GetComponent<Image>();
    }

    private void OnEnable()
    {
        UserRatingManager.OnChangeScore += UpdateBar;
        UserRatingManager.OnChangeRank += UpdateRank;
    }

    private void OnDisable()
    {
        UserRatingManager.OnChangeScore -= UpdateBar;
        UserRatingManager.OnChangeRank -= UpdateRank;
    }

    public void UpdateRank(string current, string next)
    {
        currentRank.text = current;
        nextRank.text = next;
    }   
     
    public void UpdateBar(float amount, int score, int needScore)
    {
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
