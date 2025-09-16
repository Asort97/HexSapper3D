using UnityEngine;
using System;
using TMPro;
using DG.Tweening;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

public class HexCell : MonoBehaviour
{
    [Header("Axial coords")]
    public int q; // column
    public int r; // row

    [Header("State")]
    public bool IsMine;
    public int AdjacentMines;
    public bool Revealed;
    public bool Flagged;

    public Action<HexCell> OnReveal; // событие для UI/логики

    // Визуализация (по желанию назначьте материалы/тексты в инспекторе)
    [Header("Visual")]
    public Renderer rend;
    public TextMeshPro textLabel;
    public Color hiddenColor = Color.gray;
    public Material revealedMaterial;
    public Material mineMaterial;
    [SerializeField] private GameObject bomb;
    [SerializeField] private GameObject flag;
    [SerializeField] private Renderer flagRenderer;
    [SerializeField] private ParticleSystem explosionParticle;
    private Tween revealTween;
    private Sequence flagDownSequence;
    private Sequence flagUpSequence;
    private Tween cellDown;
    private Tween cellUp;
    private HexGrid grid;
    public bool IsExploded { get; private set; }

    public void Init(int q, int r, HexGrid grid)
    {
        this.q = q; this.r = r;
        this.grid = grid;

        IsMine = false;

        AdjacentMines = 0;
        Revealed = false;
        Flagged = false;
        UpdateVisual();

        revealTween = transform.DOLocalRotate(new Vector3(0, 0, 180f), 0.5f).SetAutoKill(false);

        flag.SetActive(false);

        flagDownSequence = DOTween.Sequence()
            .Join(flagRenderer.materials[0].DOFade(1f, 0.3f))
            .Join(flagRenderer.materials[1].DOFade(1f, 0.3f))
            .Join(flag.transform.DOLocalMoveY(1.015f, 0.3f))
            .SetEase(Ease.InQuad)
            .SetAutoKill(false);

        flagUpSequence = DOTween.Sequence()
            .Join(flagRenderer.materials[0].DOFade(0f, 0.3f))
            .Join(flagRenderer.materials[1].DOFade(0f, 0.3f))
            .Join(flag.transform.DOLocalMoveY(3f, 0.3f))
            .SetEase(Ease.InQuad)
            .SetAutoKill(false);
    }

    public void SetMine(bool mine)
    {
        IsMine = mine;
    }

    public void SetAdjacent(int n)
    {
        AdjacentMines = n;
    }

    public void Reveal()
    {
        if (Revealed || Flagged) return;

        Revealed = true;
        UpdateVisual();

        revealTween.Play();

        OnReveal?.Invoke(this);
    }

    public void ToggleFlag()
    {
        if (Revealed) return;

        Flagged = !Flagged;

        UpdateVisual();
    }

    private void UpdateVisual()
    {
        if (Flagged)
        {
            flag.SetActive(true);
            flagDownSequence.Restart();
        }
        else
        {
            flagUpSequence.Restart();
            flagUpSequence.OnComplete(() => flag.SetActive(false));
        }
        
        if (rend != null)
        {
            if (!Revealed) rend.material.color = hiddenColor;
            else
            {
                var mats = rend.sharedMaterials;
                mats[1] = IsMine ? mineMaterial : revealedMaterial;
                bomb.SetActive(IsMine);
                rend.sharedMaterials = mats;
            }
        }

        if (textLabel != null)
        {
            if (!Revealed) textLabel.text = "";
            else textLabel.text = IsMine ? "X" : (AdjacentMines > 0 ? AdjacentMines.ToString() : "");
        }
    }

    public void ExplodeCell()
    {
        if (IsExploded) return;   // второй раз не тронем

        IsExploded = true;

        Debug.Log($"{gameObject.name} , spawned...");
        Instantiate(explosionParticle, transform.position, Quaternion.identity, null);

        gameObject.SetActive(false);
    }

    // Для быстрого теста кликом мыши
    public void OnMouseDown()
    {
        if (!grid.CanInteract) return;

        Reveal();
    }

    public void OnMouseOver()
    {
        if (!grid.CanInteract) return;
    
        if (Input.GetMouseButtonDown(1)) ToggleFlag();
        
        SetHighlight(true);

        if (grid != null)
        {
            foreach (var n in grid.GetNeighbors(q, r))
            {
                if (!n.Revealed)
                    n.SetHighlight(true);
            }
        }
    }

    public void OnMouseExit()
    {
        if (!grid.CanInteract) return;

        SetHighlight(false);

        if (grid != null)
        {
            foreach (var n in grid.GetNeighbors(q, r))
            {
                n.SetHighlight(false);
            }
        }
    }

    public void SetHighlight(bool state)
    {
        gameObject.layer = LayerMask.NameToLayer(state ? "Highlight" : "Default");
    }
}
