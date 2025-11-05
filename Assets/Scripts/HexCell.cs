using UnityEngine;
using System;
using TMPro;
using DG.Tweening;

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

    [Header("Hover Lift")]
    [SerializeField] private float hoverHeightSelf = 0.35f;
    [SerializeField] private float hoverHeightNeighbor = 0.18f;
    [SerializeField] private float hoverDuration = 0.12f;
    [SerializeField] private Ease hoverEase = Ease.OutQuad;

    private float _baseLocalY;
    private Tween _hoverTween;
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
        cellDown = transform.DOLocalMoveY(-1f, 1f);

    _baseLocalY = transform.localPosition.y;

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

    public void Reveal(bool isPlayerClick = false)
    {
        if (Revealed || Flagged) return;

        Revealed = true;
        UpdateVisual();

        revealTween.Play();

        // Звук раскрытия клетки (без различия — взрыв обрабатывается отдельно)
        SoundManager.Instance?.Play(SfxType.Cell_Reveal);

        // На всякий случай опускаем клетку при раскрытии
        ResetHoverHeight(hoverDuration * 0.6f);

        // Шанс дропа монеты ТОЛЬКО от прямого клика игрока (не auto-reveal)
        if (isPlayerClick && !IsMine && grid != null && grid.IsFirstClickDone)
        {
            CoinsManager.Instance?.TryDropCoin(transform.position);

            ComboStrikeManager.Instance?.TryAddCombo();
        }

        OnReveal?.Invoke(this);
    }

    public void ToggleFlag()
    {
        if (Revealed) return;

        Flagged = !Flagged;

        UpdateVisual();

        // Звук установки/снятия флага
        SoundManager.Instance?.Play(SfxType.Cell_Flag);
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

        // Звук взрыва мины
        SoundManager.Instance?.PlayAt(SfxType.Mine_Explode, transform.position);

        gameObject.SetActive(false);
    }

    // Клик мышью
public void OnMouseDown()
{
    if (!grid.CanInteract) return;
    
    // ПКМ — постановка флага
    if (Input.GetMouseButtonDown(1))
    {
        ToggleFlag();
        return;
    }
    
    // ЛКМ — раскрытие клетки
    if (Input.GetMouseButtonDown(0))
    {
        Reveal(true);
    }
}

public void OnMouseOver()
    {
        if (!grid.CanInteract) return;

        // ПКМ — постановка флага
        if (Input.GetMouseButtonDown(1))
        {
            ToggleFlag();
            return;
        }

        SetHighlight(true);

        // Поднятие основной клетки — только если нет флага
        if (!Flagged)
            SetHoverHeight(hoverHeightSelf, hoverDuration);
        else
            SetHoverHeight(0f, hoverDuration);

        // Поднимаем только нераскрытых соседей
        if (grid != null)
        {
            foreach (var n in grid.GetNeighbors(q, r))
            {
                if (!n.Revealed)
                {
                    n.SetHighlight(true);
                    n.SetHoverHeight(n.Flagged ? 0f : hoverHeightNeighbor, hoverDuration);
                }
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
                if (!n.Revealed)
                    n.ResetHoverHeight(hoverDuration);
            }
        }

        ResetHoverHeight(hoverDuration);
    }

    public void SetHighlight(bool state)
    {
        gameObject.layer = LayerMask.NameToLayer(state ? "Highlight" : "Default");
    }

    public void SetHoverHeight(float relativeY, float duration)
    {
        // Если клетка взорвана — ничего не делаем
        if (!gameObject.activeInHierarchy) return;

        var pos = transform.localPosition;
        float targetY = _baseLocalY + relativeY;
        _hoverTween?.Kill(false);
        _hoverTween = transform.DOLocalMoveY(targetY, Mathf.Max(0.01f, duration)).SetEase(hoverEase);
        _hoverTween.Play();
    }

    public void ResetHoverHeight(float duration)
    {
        if (!gameObject.activeInHierarchy) return;
        _hoverTween?.Kill(false);
        _hoverTween = transform.DOLocalMoveY(_baseLocalY, Mathf.Max(0.01f, duration)).SetEase(hoverEase);
        _hoverTween.Play();
    }
}
