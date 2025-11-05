using UnityEngine;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using System;
using System.Linq;

public class HexGrid : MonoBehaviour
{
    [Header("Hex shape (rings)")]
    [Min(0)] public int hexRadiusRings = 3;

    [Header("Mines (доля поля)")]
    [Range(0f, 1f)] public float mineRate = 0.1f;
    public int randomSeed = 12345;

    [Header("Visual")]
    public float hexWorldRadius = 0.8f;
    public HexCell cellPrefab;

    // ─ Internal
    private readonly Dictionary<(int q, int r), HexCell> cells = new();
    private System.Random rng;
    public bool IsGridDone { get; private set; }
    private int totalSafeCells;
    private int revealedSafeCells;
    private bool _isDestroying;
   private bool firstClickDone = false;
   private bool _bulkRevealing = false;
    private bool _canInteract = false;  // Начинаем с выключенным, включим явно через SetInteractionState

    public IEnumerable<HexCell> GetAllCells() => cells.Values;
    public HexCell LastRevealedCell;
    public bool CanInteract => _canInteract && !_isDestroying;
    public bool IsFirstClickDone => firstClickDone;
    public Action OnGridCompleted;
    public event Action<HexCell> MineTriggered;

    public void SetInteractionState(bool enabled)
    {
        _canInteract = enabled;
        
        // Отключаем/включаем коллайдеры всех ячеек, чтобы Unity не вызывал OnMouse* методы
        foreach (var cell in cells.Values)
        {
            var collider = cell.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = enabled;
            }
        }
    }

    private static readonly (int dq, int dr)[] DIRS = {
        (+1, 0), (+1,-1), (0,-1),
        (-1, 0), (-1,+1), (0,+1)
    };

    private void Start()
    {
        randomSeed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
        // НЕ генерируем здесь - это делает CampaignManager
    }

    [ContextMenu("Generate grid")]
    public void GenerateEmptyGridHex()
    {
        ClearGrid();
        rng = new System.Random(randomSeed);

        // Генерация координат правильного шестиугольника радиуса R
        var coords = new List<(int q, int r)>();
        int R = Mathf.Max(0, hexRadiusRings);
        for (int q = -R; q <= R; q++)
        {
            int rMin = Mathf.Max(-R, -q - R);
            int rMax = Mathf.Min(R, -q + R);
            for (int r = rMin; r <= rMax; r++) coords.Add((q, r));
        }

        // Центрирование
        Vector3 min = new(float.PositiveInfinity, 0, float.PositiveInfinity);
        Vector3 max = new(float.NegativeInfinity, 0, float.NegativeInfinity);
        var localPos = new Dictionary<(int, int), Vector3>(coords.Count);

        foreach (var (q, r) in coords)
        {
            var p = AxialToLocal(q, r);
            localPos[(q, r)] = p;
            if (p.x < min.x) min.x = p.x;
            if (p.z < min.z) min.z = p.z;
            if (p.x > max.x) max.x = p.x;
            if (p.z > max.z) max.z = p.z;
        }
        Vector3 offsetLocal = -0.5f * (min + max);

        // Создание клеток
        foreach (var (q, r) in coords)
        {
            var cell = Instantiate(cellPrefab, transform);
            cell.transform.localPosition = localPos[(q, r)] + offsetLocal;
            cell.transform.localRotation = Quaternion.identity;
            cell.transform.localScale = Vector3.one;

            cell.Init(q, r, this);
            cell.OnReveal += HandleReveal;
            cells[(q, r)] = cell;
        }
    }

    public void ClearGrid()
    {
        foreach (Transform ch in transform) Destroy(ch.gameObject);
        cells.Clear();
        IsGridDone = false;
        _canInteract = true;
        totalSafeCells = 0;
        revealedSafeCells = 0;
        firstClickDone = false;
        _isDestroying = false;
    }

    // axial → local (pointy-top)
    public Vector3 AxialToLocal(int q, int r)
    {
        float s = Mathf.Max(0.0001f, hexWorldRadius);
        float x = s * Mathf.Sqrt(3f) * (q + r * 0.5f);
        float z = s * 1.5f * r;
        return new Vector3(x, 0f, z);
    }

    public IEnumerable<HexCell> GetNeighbors(int q, int r)
    {
        foreach (var (dq, dr) in DIRS)
        {
            if (cells.TryGetValue((q + dq, r + dr), out var n))
                yield return n;
        }
    }

    // ───────── Генерация мин после первого клика ─────────
    private void GenerateMinesAfterFirstClick(HexCell clickedCell)
    {
        foreach (var c in GetAllCells())
            c.IsMine = false;

        // исключаем только клик и соседей
        var safeZone = new HashSet<HexCell> { clickedCell };
        foreach (var n in GetNeighbors(clickedCell.q, clickedCell.r))
            safeZone.Add(n);

        var candidates = new List<HexCell>();
        foreach (var c in GetAllCells())
            if (!safeZone.Contains(c))
                candidates.Add(c);

        int totalCells = cells.Count;
        int mineCount = Mathf.Clamp(
            Mathf.RoundToInt(mineRate * totalCells),
            1,
            candidates.Count - 1
        );

        FisherYatesShuffle(candidates, randomSeed);
        for (int i = 0; i < mineCount; i++)
            candidates[i].IsMine = true;

        ComputeAdjacency();
    }

    private static void FisherYatesShuffle<T>(IList<T> list, int seed)
    {
        var rnd = new System.Random(seed);
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rnd.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    public void ComputeAdjacency()
    {
        totalSafeCells = 0;
        revealedSafeCells = 0;

        foreach (var c in GetAllCells())
        {
            if (c.IsMine)
            {
                c.SetAdjacent(0);
            }
            else
            {
                int cnt = 0;
                foreach (var n in GetNeighbors(c.q, c.r))
                    if (n.IsMine) cnt++;
                c.SetAdjacent(cnt);
                totalSafeCells++;
            }
        }

        IsGridDone = false;
    }

    private void HandleReveal(HexCell c)
    {
        if (_bulkRevealing) return;

        // Первый клик — генерация мин
        if (!firstClickDone)
        {
            firstClickDone = true;
            GenerateMinesAfterFirstClick(c);
        }

        if (c.IsMine)
        {
            LastRevealedCell = c;
            SetInteractionState(false);

            if (!_isDestroying)
                MineTriggered?.Invoke(c);
            return;
        }

        // if (c.Revealed) return;

        c.Reveal(false);
        revealedSafeCells++;
        CheckIfGridDone();

        // 💡 Главное отличие:
        // если это "0" → flood-fill
        if (c.AdjacentMines == 0)
            FloodRevealZeros(c);
    }

    public void FloodRevealZeros(HexCell start)
    {
        var q = new Queue<HexCell>();
        var vis = new HashSet<HexCell>();

            start.Reveal(false);
        q.Enqueue(start);
        vis.Add(start);

        while (q.Count > 0)
        {
            var cur = q.Dequeue();
            foreach (var n in GetNeighbors(cur.q, cur.r))
            {
                if (n.Revealed || n.Flagged || n.IsMine) continue;
                    n.Reveal(false);
                if (n.AdjacentMines == 0 && vis.Add(n))
                    q.Enqueue(n);
            }
        }
    }

    private async void CheckIfGridDone()
    {
        if (IsGridDone) return;
        if (revealedSafeCells < totalSafeCells) return;

        await UniTask.Delay(TimeSpan.FromSeconds(0.8f));
        if (IsGridDone) return;

        IsGridDone = true;
        OnGridCompleted?.Invoke();
    }
    
    [ContextMenu("DONE")]
    public void DONE()
    {
        IsGridDone = true;
        OnGridCompleted?.Invoke();
    }

    public static int HexDistance(int q1, int r1, int q2, int r2)
    {
        int x1 = q1, z1 = r1, y1 = -x1 - z1;
        int x2 = q2, z2 = r2, y2 = -x2 - z2;
        return Mathf.Max(Mathf.Abs(x1 - x2), Mathf.Abs(y1 - y2), Mathf.Abs(z1 - z2));
    }

    public async UniTask ExplodeChainAsync(HexCell start, float ringDelay = 0.08f, float jitterPerCell = 0.015f)
    {
        _isDestroying = true;

        var rings = new SortedDictionary<int, List<HexCell>>();
        int maxD = 0;

        foreach (var c in GetAllCells())
        {
            int d = HexDistance(start.q, start.r, c.q, c.r);
            maxD = Mathf.Max(maxD, d);
            if (!rings.TryGetValue(d, out var list)) rings[d] = list = new List<HexCell>();
            list.Add(c);
        }

        for (int d = 0; d <= maxD; d++)
        {
            if (!rings.TryGetValue(d, out var list)) continue;
            Shuffle(list);

            foreach (var cell in list)
            {
                var col = cell.GetComponent<Collider>();
                if (col) col.enabled = false;
                    if (!cell.Revealed) cell.Reveal(false);
                cell.ExplodeCell();

                if (jitterPerCell > 0f)
                    await UniTask.Delay(TimeSpan.FromSeconds(UnityEngine.Random.Range(0f, jitterPerCell)));
            }

            if (ringDelay > 0f)
                await UniTask.Delay(TimeSpan.FromSeconds(ringDelay));
        }
    }

    private void Shuffle<T>(IList<T> a)
    {
        for (int i = a.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (a[i], a[j]) = (a[j], a[i]);
        }
    }
}
