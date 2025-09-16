using UnityEngine;
using TMPro;
using System;

public class TimerManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text timerText;

    [Header("Settings")]
    [SerializeField] private int startTimeSeconds = 30;

    private float remainingTime;
    private bool isRunning;

    // событие при завершении
    public Action OnTimerEnd;

    private void Start()
    {
        ResetTimer();
        StartTimer();
    }

    private void Update()
    {
        if (!isRunning) return;

        remainingTime -= Time.deltaTime;
        if (remainingTime <= 0f)
        {
            remainingTime = 0f;
            isRunning = false;
            UpdateText();
            OnTimerEnd?.Invoke(); // вызываем коллбек
        }
        else
        {
            UpdateText();
        }
    }

    public void ResetTimer()
    {
        remainingTime = startTimeSeconds;
        UpdateText();
    }

    public void StartTimer()
    {
        isRunning = true;
    }

    public void StopTimer()
    {
        isRunning = false;
    }

    public void AddTime(int amount)
    {
        remainingTime += amount;
        UpdateText();
    }

    private void UpdateText()
    {
        int minutes = Mathf.FloorToInt(remainingTime / 60f);
        int seconds = Mathf.FloorToInt(remainingTime % 60f);
        timerText.text = $"{minutes:00}:{seconds:00}";
    }
}
