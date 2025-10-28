using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Навесьте на любую UI-кнопку (или другой кликабельный UI),
/// чтобы проигрывался звук клика через SoundManager.
/// </summary>
public class UIButtonSfx : MonoBehaviour, IPointerClickHandler
{
    public void OnPointerClick(PointerEventData eventData)
    {
        SoundManager.Instance?.Play(SfxType.UI_Click);
    }
}
