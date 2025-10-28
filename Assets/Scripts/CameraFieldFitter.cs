using DG.Tweening;
using UnityEngine;

public class CameraFieldFitter : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private float cellSize = 1f;
    
    private Vector3 _initialCameraPosition;
    private bool _initialized;

    private void Awake()
    {
        if (mainCamera != null && !_initialized)
        {
            _initialCameraPosition = mainCamera.transform.position;
            _initialized = true;
        }
    }

    public void FitPerspectiveCameraToField(int radius)
    {
        if (!_initialized && mainCamera != null)
        {
            _initialCameraPosition = mainCamera.transform.position;
            _initialized = true;
        }

        float fieldDiameter = (radius * 2 + 1) * cellSize;
        float halfSize = fieldDiameter / 2f;
        float fovRad = mainCamera.fieldOfView * Mathf.Deg2Rad;

        // Вычисляем нужную дистанцию от центра, чтобы всё влезло по высоте
        float distance = halfSize / Mathf.Tan(fovRad / 2f);

        // Добавляем запас
        distance *= 1.1f;

        // Смещение относительно начальной позиции
        float radiusOffset = radius - 2; // 2 - начальный радиус
        float offsetY = _initialCameraPosition.y + distance + radiusOffset * 2.3f;
        float offsetZ = _initialCameraPosition.z - distance - radiusOffset * 2.3f;

        DOTween.Sequence()
        .Append(mainCamera.transform.DOMoveY(offsetY, 1))
        .Join(mainCamera.transform.DOMoveZ(offsetZ, 1))
        .SetEase(Ease.OutBack).Play();
        // mainCamera.transform.LookAt(Vector3.zero);
    }
}
