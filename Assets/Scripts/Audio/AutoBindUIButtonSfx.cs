using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Повесьте на Canvas (или любой объект сцены), чтобы автоматически
/// добавить компонент UIButtonSfx всем Button в сцене при старте.
/// </summary>
public class AutoBindUIButtonSfx : MonoBehaviour
{
    private void Start()
    {
        var buttons = FindObjectsOfType<Button>(true);
        foreach (var b in buttons)
        {
            if (b.gameObject.GetComponent<UIButtonSfx>() == null)
            {
                b.gameObject.AddComponent<UIButtonSfx>();
            }
        }
    }
}
