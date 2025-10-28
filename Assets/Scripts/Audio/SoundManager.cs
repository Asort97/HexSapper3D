using System;
using System.Collections.Generic;
using UnityEngine;

public enum SfxType
{
    None = 0,
    UI_Click,
    Cell_Reveal,
    Cell_Flag,
    Mine_Explode,
    Lose,
    Win,
    Win_Confetti,
    Rank_Up,
    Coin_Spawn,
    Coin_Fly,
    Coin_Tick
}

/// <summary>
/// Простой менеджер звуков: Singleton + PlayOneShot.
/// Повесьте на отдельный GameObject в сцене, задайте клипы.
/// </summary>
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource uiSource;

    [Header("Clips Map")]
    [SerializeField] private List<SfxClip> clips = new();

    private readonly Dictionary<SfxType, AudioClip> _map = new();

    [Serializable]
    public struct SfxClip
    {
        public SfxType type;
        public AudioClip clip;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        _map.Clear();
        foreach (var c in clips)
        {
            if (c.clip != null && !_map.ContainsKey(c.type))
                _map.Add(c.type, c.clip);
        }

        // На всякий случай создадим источники, если не заданы
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
        }
        if (uiSource == null)
        {
            uiSource = gameObject.AddComponent<AudioSource>();
            uiSource.playOnAwake = false;
        }
    }

    public void Play(SfxType type)
    {
        if (!_map.TryGetValue(type, out var clip) || clip == null) return;

        var source = (type == SfxType.UI_Click) ? uiSource : sfxSource;
        source.PlayOneShot(clip);
    }

    public void PlayAt(SfxType type, Vector3 worldPos, float volume = 1f)
    {
        if (!_map.TryGetValue(type, out var clip) || clip == null) return;
        AudioSource.PlayClipAtPoint(clip, worldPos, Mathf.Clamp01(volume));
    }
}
