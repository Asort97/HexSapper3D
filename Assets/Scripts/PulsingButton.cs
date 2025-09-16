using DG.Tweening;
using UnityEngine;

public class PulsingButton : MonoBehaviour
{
    [SerializeField] private float scale;
    [SerializeField] private float duration;

    private void Start()
    {
        transform.DOScale(scale, duration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetAutoKill(false)
            .Play();
    }
}
