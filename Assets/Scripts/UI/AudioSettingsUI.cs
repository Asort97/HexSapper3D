using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Привяжите к Canvas-объекту с двумя слайдерами. 
/// SFX регулирует все звуковые эффекты (включая UI), Ambient — громкость фонового амбиента.
/// Значения сохраняются в YandexGame.savesData и восстанавливаются при запуске.
/// </summary>
public class AudioSettingsUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Slider ambientSlider;

    private SoundManager SM => SoundManager.Instance;


    private void Start()
    {
        float defaultSfx = SM != null ? SM.GetSfxVolume() : 1f;
        float defaultAmbient = SM != null ? SM.GetAmbientVolume() : 1f;

        float sfx = defaultSfx;
        float ambient = defaultAmbient;
        // Чтение из YandexGame.savesData (напрямую, без reflection)
        if (YG.YandexGame.savesData != null)
        {
            sfx = YG.YandexGame.savesData.sfxVolume;
            ambient = YG.YandexGame.savesData.ambientVolume;
        }

        if (sfxSlider != null) sfxSlider.value = sfx;
        if (ambientSlider != null) ambientSlider.value = ambient;

        ApplyVolumes(sfx, ambient);

        if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(OnSfxChanged);
        if (ambientSlider != null) ambientSlider.onValueChanged.AddListener(OnAmbientChanged);
    }

    private void OnDestroy()
    {
        if (sfxSlider != null) sfxSlider.onValueChanged.RemoveListener(OnSfxChanged);
        if (ambientSlider != null) ambientSlider.onValueChanged.RemoveListener(OnAmbientChanged);
    }


    private void OnSfxChanged(float value)
    {
        if (SM != null) SM.SetSfxVolume(value);
        if (YG.YandexGame.savesData != null)
        {
            YG.YandexGame.savesData.sfxVolume = value;
            YG.YandexGame.SaveProgress();
        }
    }

    private void OnAmbientChanged(float value)
    {
        if (SM != null) SM.SetAmbientVolume(value);
        if (YG.YandexGame.savesData != null)
        {
            YG.YandexGame.savesData.ambientVolume = value;
            YG.YandexGame.SaveProgress();
        }
    }

    private void ApplyVolumes(float sfx, float ambient)
    {
        if (SM != null)
        {
            SM.SetSfxVolume(sfx);
            SM.SetAmbientVolume(ambient);
        }
    }
}
