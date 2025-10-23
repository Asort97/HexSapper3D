using DG.Tweening;
using UnityEngine;

public class CameraFieldFitter : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private float cellSize = 1f;

    public void FitPerspectiveCameraToField(int radius)
    {
        float fieldDiameter = (radius * 2 + 1) * cellSize;
        float halfSize = fieldDiameter / 2f;
        float fovRad = mainCamera.fieldOfView * Mathf.Deg2Rad;

        // Вычисляем нужную дистанцию от центра, чтобы всё влезло по высоте
        float distance = halfSize / Mathf.Tan(fovRad / 2f);

        // Добавляем запас
        distance *= 1.1f;

        mainCamera.transform.DOMove(new Vector3(mainCamera.transform.position.x, distance, -distance), 1).SetEase(Ease.OutBack).Play();
        mainCamera.transform.LookAt(Vector3.zero);
    }
}
