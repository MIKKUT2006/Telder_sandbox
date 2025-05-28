using UnityEngine;
using UnityEngine.Tilemaps;

public class SpriteOptimize : MonoBehaviour
{
    //private TilemapRenderer _tilemapRenderer;
    //private Tilemap _tilemap;
    //private Bounds _tilemapBounds; // Храним вычисленные границы
    //private Plane[] _frustumPlanes;

    //void Start()
    //{
    //    _tilemapRenderer = GetComponent<TilemapRenderer>();
    //    _tilemap = GetComponent<Tilemap>();
    //    if (_tilemapRenderer == null || _tilemap == null)
    //    {
    //        Debug.LogError("TilemapVisibilityOptimizer скрипт должен быть на объекте с TilemapRenderer и Tilemap!");
    //        enabled = false;
    //        return;
    //    }

    //    // Вычисляем границы тайловой карты при старте
    //    CalculateTilemapBounds();
    //}

    //void Update()
    //{
    //    // Получаем плоскости отсечения камеры
    //    _frustumPlanes = GeometryUtility.CalculateFrustumPlanes(Camera.main);

    //    // Проверяем, находится ли ограничивающий объем тайловой карты в пределах видимости камеры
    //    if (GeometryUtility.TestPlanesAABB(_frustumPlanes, _tilemapBounds))
    //    {
    //        // Тайловая карта видна
    //        if (!_tilemapRenderer.enabled)
    //        {
    //            _tilemapRenderer.enabled = true;
    //            // Debug.Log($"{gameObject.name}: TilemapRenderer включен");
    //        }
    //    }
    //    else
    //    {
    //        // Тайловая карта не видна
    //        if (_tilemapRenderer.enabled)
    //        {
    //            _tilemapRenderer.enabled = false;
    //            // Debug.Log($"{gameObject.name}: TilemapRenderer выключен");
    //        }
    //    }
    //}

    //// Пересчитываем границы тайловой карты
    //private void CalculateTilemapBounds()
    //{
    //    _tilemap.CompressBounds(); // Обновляем bounds Tilemap

    //    _tilemapBounds = _tilemap.localBounds; // Берем localBounds вместо tilemapRenderer.bounds

    //    //Сдвигаем bounds в мировую систему координат
    //    _tilemapBounds.center = transform.TransformPoint(_tilemapBounds.center);

    //}
}
