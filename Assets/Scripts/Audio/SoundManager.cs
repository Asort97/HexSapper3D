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
    [SerializeField] private AudioSource ambientSource;

    [Header("Clips Map")]
    [SerializeField] private List<SfxClip> clips = new();

    [Header("Volumes")]
    [Range(0f,1f)] [SerializeField] private float sfxVolume = 1f;   // для всех SFX и UI
    [Range(0f,1f)] [SerializeField] private float ambientVolume = 1f;

    [Header("Sound Limiting")]
    [SerializeField] private int maxCellRevealSounds = 3; // Максимум одновременных звуков раскрытия
    [SerializeField] private float cellRevealSoundInterval = 0.05f; // Минимальный интервал между звуками

    private readonly Dictionary<SfxType, AudioClip> _map = new();
    private int _currentCellRevealCount = 0;
    private float _lastCellRevealTime = 0f;

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
        if (ambientSource == null)
        {
            ambientSource = gameObject.AddComponent<AudioSource>();
            ambientSource.loop = true;
            ambientSource.playOnAwake = false;
        }

        // Применяем громкости из YandexGame.savesData
        if (YG.YandexGame.savesData != null)
        {
            if (YG.YandexGame.savesData.GetType().GetField("sfxVolume") != null)
                sfxVolume = (float)YG.YandexGame.savesData.GetType().GetField("sfxVolume").GetValue(YG.YandexGame.savesData);
            if (YG.YandexGame.savesData.GetType().GetField("ambientVolume") != null)
                ambientVolume = (float)YG.YandexGame.savesData.GetType().GetField("ambientVolume").GetValue(YG.YandexGame.savesData);
        }
        ApplyVolumes();
    }

    private void ApplyVolumes()
    {
        if (sfxSource != null) sfxSource.volume = sfxVolume;
        if (uiSource != null) uiSource.volume = sfxVolume;
        if (ambientSource != null) ambientSource.volume = ambientVolume;
    }

    public void Play(SfxType type)
    {
        if (!_map.TryGetValue(type, out var clip) || clip == null) return;

        // Ограничение для звука раскрытия клеток
        if (type == SfxType.Cell_Reveal)
        {
            float currentTime = Time.time;
            
            // Сбрасываем счётчик, если прошло достаточно времени
            if (currentTime - _lastCellRevealTime > cellRevealSoundInterval * 2f)
            {
                _currentCellRevealCount = 0;
            }

            // Если уже играет максимум звуков, пропускаем
            if (_currentCellRevealCount >= maxCellRevealSounds)
            {
                return;
            }

            // Если слишком быстро после предыдущего звука, пропускаем
            if (currentTime - _lastCellRevealTime < cellRevealSoundInterval)
            {
                return;
            }

            _currentCellRevealCount++;
            _lastCellRevealTime = currentTime;
        }

        var source = (type == SfxType.UI_Click) ? uiSource : sfxSource;
        source.PlayOneShot(clip);
    }

    public void PlayAt(SfxType type, Vector3 worldPos, float volume = 1f)
    {
        if (!_map.TryGetValue(type, out var clip) || clip == null) return;
        AudioSource.PlayClipAtPoint(clip, worldPos, Mathf.Clamp01(volume));
    }

    // ===== Public API: Volumes =====
    public void SetSfxVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        if (sfxSource != null) sfxSource.volume = sfxVolume;
        if (uiSource != null) uiSource.volume = sfxVolume;
        if (YG.YandexGame.savesData != null)
        {
            var f = YG.YandexGame.savesData.GetType().GetField("sfxVolume");
            if (f != null) f.SetValue(YG.YandexGame.savesData, sfxVolume);
            YG.YandexGame.SaveProgress();
        }
    }

    public float GetSfxVolume() => sfxVolume;

    public void SetAmbientVolume(float volume)
    {
        ambientVolume = Mathf.Clamp01(volume);
        if (ambientSource != null) ambientSource.volume = ambientVolume;
        if (YG.YandexGame.savesData != null)
        {
            var f = YG.YandexGame.savesData.GetType().GetField("ambientVolume");
            if (f != null) f.SetValue(YG.YandexGame.savesData, ambientVolume);
            YG.YandexGame.SaveProgress();
        }
    }

    public float GetAmbientVolume() => ambientVolume;

    // Опционально: запустить амбиент
    public void PlayAmbient(AudioClip clip, bool loop = true)
    {
        if (ambientSource == null || clip == null) return;
        ambientSource.loop = loop;
        if (ambientSource.clip != clip)
            ambientSource.clip = clip;
        ambientSource.volume = ambientVolume;
        ambientSource.Play();
    }

    public void StopAmbient()
    {
        if (ambientSource != null) ambientSource.Stop();
    }
}
