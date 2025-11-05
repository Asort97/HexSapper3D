using UnityEngine;

/// <summary>
/// Управление зумом и перемещением камеры:
/// - Зум: колесико мыши на ПК и pinch-to-zoom на мобильных
/// - Перемещение: средняя кнопка мыши (ПК) или свайп одним пальцем (мобильные)
/// Повесьте на объект с камерой или отдельный контроллер.
/// </summary>
public class CameraZoomController : MonoBehaviour
{
    [Header("Target Camera")]
    [SerializeField] private Camera targetCamera;

    [Header("Zoom Settings")]
    [SerializeField] private float zoomSpeed = 10f;
    [SerializeField] private float minZoom = 5f;
    [SerializeField] private float maxZoom = 50f;
    [SerializeField] private float smoothTime = 0.1f;

    [Header("Pan Settings")]
    [SerializeField] private float panSpeed = 0.5f;
    [SerializeField] private float touchPanSpeed = 0.02f;

    [Header("Touch Settings")]
    [SerializeField] private float touchZoomSpeed = 0.1f;

    [Header("Movement Limits (Optional)")]
    [SerializeField] private bool limitMovement = false;
    [SerializeField] private Vector2 minPosition = new Vector2(-50f, -50f);
    [SerializeField] private Vector2 maxPosition = new Vector2(50f, 50f);

    private float _currentZoom;
    private float _targetZoom;
    private float _zoomVelocity;
    private float _previousTouchDistance;
    private Vector3 _dragOriginWorld;
    private Vector2 _mouseDownScreen;
    private bool _isDragging;
    private const float DragThresholdPixels = 3f;
    public static bool IsPanning { get; private set; }

    private Plane _groundPlane = new Plane(Vector3.up, Vector3.zero);
    private bool _isInitialized;
    private bool _inputEnabled = false;

    private void Awake()
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (targetCamera != null)
        {
            if (targetCamera.orthographic)
                _currentZoom = targetCamera.orthographicSize;
            else
                _currentZoom = targetCamera.fieldOfView;

            _targetZoom = _currentZoom;
            _isInitialized = true;
        }
    }

    private void Update()
    {
        if (targetCamera == null || !_isInitialized) return;

        if (_inputEnabled)
        {
            HandleInput();
        }
        ApplyZoom();
    }

    public void SetInputEnabled(bool enabled)
    {
        _inputEnabled = enabled;
        
        // При отключении сбрасываем состояния
        if (!enabled)
        {
            _isDragging = false;
            IsPanning = false;
        }
    }

    private void HandleInput()
    {
    // Перемещение камеры правой кнопкой мыши
        HandleMousePan();
        
        // ПК: колесико мыши
        if (Input.mousePresent)
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.001f)
            {
                _targetZoom -= scroll * zoomSpeed;
                _targetZoom = Mathf.Clamp(_targetZoom, minZoom, maxZoom);
            }
        }

        // Мобильные: pinch-to-zoom или свайп одним пальцем
        if (Input.touchCount == 2)
        {
            // Два пальца - zoom
            Touch touch0 = Input.GetTouch(0);
            Touch touch1 = Input.GetTouch(1);
            
            float currentDistance = Vector2.Distance(touch0.position, touch1.position);

            if (touch0.phase == TouchPhase.Began || touch1.phase == TouchPhase.Began)
            {
                _previousTouchDistance = currentDistance;
            }
            else if (touch0.phase == TouchPhase.Moved || touch1.phase == TouchPhase.Moved)
            {
                float delta = _previousTouchDistance - currentDistance;
                _targetZoom += delta * touchZoomSpeed;
                _targetZoom = Mathf.Clamp(_targetZoom, minZoom, maxZoom);
                _previousTouchDistance = currentDistance;
            }
        }
        else if (Input.touchCount == 1)
        {
            // Один палец - перемещение камеры
            HandleTouchPan();
        }
    }

    private void HandleMousePan()
    {
        // Начало перетаскивания средней кнопкой (колесико мыши)
        if (Input.GetMouseButtonDown(2))
        {
            Ray ray = targetCamera.ScreenPointToRay(Input.mousePosition);
            if (_groundPlane.Raycast(ray, out float enter))
            {
                _dragOriginWorld = ray.GetPoint(enter);
                _mouseDownScreen = Input.mousePosition;
                _isDragging = true;
                IsPanning = false; // включим только после преодоления порога
            }
        }

        // Перетаскивание
        if (Input.GetMouseButton(2) && _isDragging)
        {
            // Активируем режим панорамирования после мини-порога, чтобы отличить от клика ПКМ
            if (!IsPanning)
            {
                Vector2 delta = (Vector2)Input.mousePosition - _mouseDownScreen;
                if (delta.magnitude >= DragThresholdPixels)
                {
                    IsPanning = true;
                }
            }

            Ray ray = targetCamera.ScreenPointToRay(Input.mousePosition);
            if (_groundPlane.Raycast(ray, out float enter))
            {
                Vector3 currentWorld = ray.GetPoint(enter);
                Vector3 move = (_dragOriginWorld - currentWorld); // перемещаемся по плоскости XZ

                Vector3 newPos = targetCamera.transform.position + move * panSpeed;
                ApplyPositionLimits(ref newPos);
                targetCamera.transform.position = newPos;

                // Обновляем origin, чтобы пан был релейтивным
                _dragOriginWorld = currentWorld;
            }
        }

        // Конец перетаскивания
        if (Input.GetMouseButtonUp(2))
        {
            _isDragging = false;
            IsPanning = false;
        }
    }

    private void HandleTouchPan()
    {
        Touch touch = Input.GetTouch(0);

        if (touch.phase == TouchPhase.Began)
        {
            Ray ray = targetCamera.ScreenPointToRay(touch.position);
            if (_groundPlane.Raycast(ray, out float enter))
            {
                _dragOriginWorld = ray.GetPoint(enter);
                IsPanning = false;
            }
        }
        else if (touch.phase == TouchPhase.Moved)
        {
            Ray ray = targetCamera.ScreenPointToRay(touch.position);
            if (_groundPlane.Raycast(ray, out float enter))
            {
                Vector3 currentWorld = ray.GetPoint(enter);
                Vector3 move = (_dragOriginWorld - currentWorld);
                Vector3 newPos = targetCamera.transform.position + move;
                ApplyPositionLimits(ref newPos);
                targetCamera.transform.position = Vector3.Lerp(targetCamera.transform.position, newPos, 0.8f);
                _dragOriginWorld = currentWorld;
                IsPanning = true;
            }
        }
        else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
        {
            IsPanning = false;
        }
    }

    private void ApplyPositionLimits(ref Vector3 position)
    {
        if (limitMovement)
        {
            position.x = Mathf.Clamp(position.x, minPosition.x, maxPosition.x);
            position.z = Mathf.Clamp(position.z, minPosition.y, maxPosition.y);
        }
    }

    private void ApplyZoom()
    {
        // Плавное приближение к целевому зуму
        _currentZoom = Mathf.SmoothDamp(_currentZoom, _targetZoom, ref _zoomVelocity, smoothTime);

        if (targetCamera.orthographic)
        {
            targetCamera.orthographicSize = _currentZoom;
        }
        else
        {
            targetCamera.fieldOfView = _currentZoom;
        }
    }

    public void SetZoom(float zoom)
    {
        _targetZoom = Mathf.Clamp(zoom, minZoom, maxZoom);
    }

    public void SetZoomImmediate(float zoom)
    {
        _targetZoom = Mathf.Clamp(zoom, minZoom, maxZoom);
        _currentZoom = _targetZoom;
        ApplyZoom();
    }

    public float GetCurrentZoom()
    {
        return _currentZoom;
    }
}
