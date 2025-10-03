using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class FadeTransition : MonoBehaviour
{
    private CanvasGroup canvasGroup;
    private Tween toFade;
    private Tween fromFade;

    private void Awake()
    {
        toFade = canvasGroup.DOFade(1, 1).SetAutoKill(false);
        fromFade = canvasGroup.DOFade(0, 1).SetAutoKill(false);
    }

    public void ToFade()
    {
        toFade.Restart();
    }
    
    public void FromFade()
    {
        fromFade.Restart();
    }
}
