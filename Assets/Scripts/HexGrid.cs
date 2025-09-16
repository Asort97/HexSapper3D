using UnityEngine;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using DG.Tweening;

/// Генерирует pointy-top гекс-сетку в форме ПРАВИЛЬНОГО ШЕСТИУГОЛЬНИКА (радиус R колец).
public class HexGrid : MonoBehaviour
{
    [Header("Hex shape (rings)")]
    [Min(0)]
    public int hexRadiusRings = 3;    // R: 0=1 клетка, 1=7, 2=19, 3=37, ...
    public bool centerGrid = true;

    [Header("Mines")]
    [Range(0f, 1f)]
    public float mineRate = 0.16f;     // 12–18% для казуала
    public int explicitMineCount = -1; // если >=0, используем точное кол-во
    public int randomSeed = 12345;
    public bool firstClickSafe = true; // не ставим мину на первую открытую клетку (и окрестность)
    public int safeRadius = 1;

    [Header("Visual layout")]
    public float hexWorldRadius = 0.8f; // «размер» гекса в мире (радиус)
    public HexCell cellPrefab;

    // ───────── internal ─────────
    private Dictionary<(int q, int r), HexCell> cells = new();
    private bool minesPlaced = false;
    private System.Random rng;
    public bool IsGridDone { get; private set; }
    private int totalSafeCells;   // сколько всего клеток без мин
    private int revealedSafeCells; // сколько уже открыто
    private bool _isDestroying;
    public Action OnGridCompleted; // событие для CampaignManager
    private bool _isCompleting;                 // защита от повторов
    public IEnumerable<HexCell> GetAllCells() => cells.Values;
    public HexCell LastRevealedCell;
    public bool CanInteract = true;
    
    // 6 направлений (pointy-top axial)
    private static readonly (int dq, int dr)[] DIRS = new (int, int)[]
    {
        (+1, 0), (+1,-1), (0,-1),
        (-1, 0), (-1,+1), (0,+1)
    };

    void Start()
    {
        GenerateEmptyGridHex();
        // PlaceMines(null);        // или отложить до 1-го клика, если хотите более мягкий firstClick
        ComputeAdjacency();
    }

    [ContextMenu("Generate grid")]
    public void GenerateEmptyGridHex()
    {
        ClearGrid();
        rng = new System.Random(randomSeed);

        // 1) Собираем аксиальные координаты
        var coords = new List<(int q, int r)>();
        int R = Mathf.Max(0, hexRadiusRings);
        for (int q = -R; q <= R; q++)
        {
            int rMin = Mathf.Max(-R, -q - R);
            int rMax = Mathf.Min(R, -q + R);
            for (int r = rMin; r <= rMax; r++) coords.Add((q, r));
        }

        // 2) Локальные позиции и локальный bbox
        Vector3 min = new Vector3(float.PositiveInfinity, 0, float.PositiveInfinity);
        Vector3 max = new Vector3(float.NegativeInfinity, 0, float.NegativeInfinity);
        var localPos = new Dictionary<(int, int), Vector3>(coords.Count);

        foreach (var (q, r) in coords)
        {
            var p = AxialToLocal(q, r);     // LOCAL
            localPos[(q, r)] = p;
            if (p.x < min.x) min.x = p.x;
            if (p.z < min.z) min.z = p.z;
            if (p.x > max.x) max.x = p.x;
            if (p.z > max.z) max.z = p.z;
        }
        Vector3 offsetLocal = -0.5f * (min + max); // смещение, чтобы грид был по центру родителя

        // 3) Инстанс клеток: строго локально
        foreach (var (q, r) in coords)
        {
            var cell = Instantiate(cellPrefab, transform); // parent = HexGrid
            cell.transform.localPosition = localPos[(q, r)] + offsetLocal; // LOCAL!
            cell.transform.localRotation = Quaternion.identity;
            cell.transform.localScale = Vector3.one;

            cell.Init(q, r, this);
            cell.OnReveal += HandleReveal;

            cells[(q, r)] = cell;
        }

        minesPlaced = false;
    }


    public void ClearGrid()
    {
        foreach (Transform ch in transform) Destroy(ch.gameObject);
        cells.Clear();
    }

    // локальные координаты (pointy-top axial → local)
    public Vector3 AxialToLocal(int q, int r)
    {
        float s = Mathf.Max(0.0001f, hexWorldRadius);
        float x = s * Mathf.Sqrt(3f) * (q + r * 0.5f);
        float z = s * 1.5f * r;
        return new Vector3(x, 0f, z); // LOCAL, не world!
    }

    // генерация правильного шестигранника в ЛОКАЛЕ, с центрированием внутри родителя
    /// Аксиальные → мировые (pointy-top).
    public Vector3 AxialToWorld(int q, int r)
    {
        // pointy-top:
        // x = size * sqrt(3) * (q + r/2)
        // z = size * (3/2) * r
        float size = hexWorldRadius;
        float x = size * Mathf.Sqrt(3f) * (q + r * 0.5f);
        float z = size * 1.5f * r;
        return new Vector3(x, 0f, z);
    }

    public IEnumerable<HexCell> GetNeighbors(int q, int r)
    {
        foreach (var (dq, dr) in DIRS)
        {
            var key = (q + dq, r + dr);
            if (cells.TryGetValue(key, out var n)) yield return n;
        }
    }

    /// Расстановка мин; exclude — координаты, куда мину ставить нельзя.
    public void PlaceMines(HashSet<(int q, int r)> exclude)
    {
        // пул координат
        List<(int q, int r)> pool = new(cells.Keys);
        if (exclude != null && exclude.Count > 0)
            pool.RemoveAll(c => exclude.Contains(c));

        int total = pool.Count;
        int toPlace = explicitMineCount >= 0
            ? Mathf.Clamp(explicitMineCount, 0, total)
            : Mathf.RoundToInt(total * mineRate);

        Shuffle(pool);
        for (int i = 0; i < toPlace; i++)
        {
            var (q, r) = pool[i];
            cells[(q, r)].SetMine(true);
        }

        minesPlaced = true;
    }

    public void ComputeAdjacency()
    {
        totalSafeCells = 0;
        revealedSafeCells = 0;

        foreach (var kv in cells)
        {
            var c = kv.Value;
            if (c.IsMine)
            {
                c.SetAdjacent(0);
            }
            else
            {
                int count = 0;
                foreach (var n in GetNeighbors(c.q, c.r))
                    if (n.IsMine) count++;
                c.SetAdjacent(count);

                totalSafeCells++;
            }

        }

        IsGridDone = false;
    }


    private void HandleReveal(HexCell c)
    {
        if (!minesPlaced)
        {
            HashSet<(int q, int r)> exclude = null;
            if (firstClickSafe)
            {
                exclude = new HashSet<(int, int)>();
                // исключаем саму клетку и окрестность
                foreach (var safeCell in GetRadius(c.q, c.r, safeRadius))
                exclude.Add((safeCell.q, safeCell.r));
            }

            PlaceMines(exclude);
            ComputeAdjacency();
        }


        if (c.IsMine)
        {
            // await UniTask.Delay(TimeSpan.FromSeconds(5));
            LastRevealedCell = c;
            CanInteract = false;

            if (!GameManager.Instance.AdReviveUsed && !_isDestroying)
            {
                GameManager.Instance.ShowAdPanel(true);
            }
            else
            {
                // await UniTask.Delay(TimeSpan.FromSeconds(1f));
                if (!_isDestroying)
                {
                    GameManager.Instance.Lose();
                }

                // await ExplodeChainAsync(c, ringDelay: 0.3f, jitterPerCell: 0.015f);
            }

            Debug.Log("BOOM! Mine clicked. Game over.");
            return;
        }

        // если клетка безопасная и только что открыта → увеличить счётчик
        // if (!c.IsMine && !c.Flagged && !c.Revealed)
        if (c.Revealed)
        {
            c.Reveal();
            revealedSafeCells++;
            CheckIfGridDone();
        }

        if (c.AdjacentMines == 0)
            FloodRevealZeros(c);
    }

    private async void CheckIfGridDone()
    {
        if (IsGridDone || _isCompleting) return;
        if (revealedSafeCells < totalSafeCells) return;

        _isCompleting = true; // ставим защиту сразу
        await UniTask.Delay(TimeSpan.FromSeconds(2));

        if (IsGridDone) return;    // на случай внешней пометки
        IsGridDone = true;
        OnGridCompleted?.Invoke();
    }

    public IEnumerable<HexCell> GetRadius(int q0, int r0, int R)
    {
        for (int dq = -R; dq <= R; dq++)
        {
            for (int dr = Mathf.Max(-R, -dq - R); dr <= Mathf.Min(R, -dq + R); dr++)
            {
                int q = q0 + dq;
                int r = r0 + dr;
                if (cells.TryGetValue((q, r), out var c))
                    yield return c;
            }
        }
    }

    public void FloodRevealZeros(HexCell start)
    {
        Queue<HexCell> q = new();
        HashSet<HexCell> vis = new();

        start.Reveal();
        q.Enqueue(start);
        vis.Add(start);

        while (q.Count > 0)
        {
            var cur = q.Dequeue();
            foreach (var n in GetNeighbors(cur.q, cur.r))
            {
                if (n.Revealed || n.Flagged) continue;
                if (n.IsMine) continue;

                n.Reveal();
                if (n.AdjacentMines == 0 && !vis.Contains(n))
                {
                    vis.Add(n);
                    q.Enqueue(n);
                }
            }
        }
    }

    public HexCell PickRandomClosedSafeCell()
    {
        List<HexCell> pool = new();
        foreach (var c in cells.Values)
            if (!c.Revealed && !c.IsMine && !c.Flagged) pool.Add(c);

        if (pool.Count == 0) return null;
        int idx = rng.Next(pool.Count);
        return pool[idx];
    }

    private void Shuffle<T>(IList<T> a)
    {
        for (int i = a.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (a[i], a[j]) = (a[j], a[i]);
        }
    }
    

    // куб-метрика для гексов (axial → cube)
    public static int HexDistance(int q1, int r1, int q2, int r2)
    {
        int x1 = q1, z1 = r1, y1 = -x1 - z1;
        int x2 = q2, z2 = r2, y2 = -x2 - z2;
        return Mathf.Max(Mathf.Abs(x1 - x2), Mathf.Abs(y1 - y2), Mathf.Abs(z1 - z2));
    }

    /// Взрывная цепочка: идём по «кольцам» от стартовой клетки.
    public async UniTask ExplodeChainAsync(
        HexCell start,
        float ringDelay = 0.08f,     // пауза между кольцами
        float jitterPerCell = 0.015f // маленькая случайная задержка между клетками в кольце
    )
    {

        _isDestroying = true;

        // группируем клетки по расстоянию от старта
        var rings = new SortedDictionary<int, List<HexCell>>();
        int maxD = 0;

        foreach (var c in GetAllCells())
        {
            int d = HexDistance(start.q, start.r, c.q, c.r);
            maxD = Mathf.Max(maxD, d);
            if (!rings.TryGetValue(d, out var list))
                rings[d] = list = new List<HexCell>();
            list.Add(c);
        }

        // анимация кольцо за кольцом
        for (int d = 0; d <= maxD; d++)
        {
            if (!rings.TryGetValue(d, out var list)) continue;

            // чуть рандомизируем порядок, чтобы выглядело живее
            Shuffle(list);

            // анимируем клетки этого кольца
            foreach (var cell in list)
            {
                // выключаем взаимодействие/коллайдер (если есть)
                var col = cell.GetComponent<Collider>();
                if (col) col.enabled = false;

                // маркируем как раскрытую, чтобы логика не дергалась
                if (!cell.Revealed) cell.Reveal();

                // визуальный «взрыв» через DOTween
                // cell.gameObject.SetActive(false);
                cell.ExplodeCell();

                // лёгкий джиттер, чтобы не синхронно
                if (jitterPerCell > 0f)
                    await UniTask.Delay(TimeSpan.FromSeconds(UnityEngine.Random.Range(0f, jitterPerCell)));
            }

            // пауза между кольцами
            if (ringDelay > 0f)
                await UniTask.Delay(TimeSpan.FromSeconds(ringDelay));
        }
    }
}
