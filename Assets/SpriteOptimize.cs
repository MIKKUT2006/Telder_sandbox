using UnityEngine;
using UnityEngine.Tilemaps;

public class SpriteOptimize : MonoBehaviour
{
    //private TilemapRenderer _tilemapRenderer;
    //private Tilemap _tilemap;
    //private Bounds _tilemapBounds; // ������ ����������� �������
    //private Plane[] _frustumPlanes;

    //void Start()
    //{
    //    _tilemapRenderer = GetComponent<TilemapRenderer>();
    //    _tilemap = GetComponent<Tilemap>();
    //    if (_tilemapRenderer == null || _tilemap == null)
    //    {
    //        Debug.LogError("TilemapVisibilityOptimizer ������ ������ ���� �� ������� � TilemapRenderer � Tilemap!");
    //        enabled = false;
    //        return;
    //    }

    //    // ��������� ������� �������� ����� ��� ������
    //    CalculateTilemapBounds();
    //}

    //void Update()
    //{
    //    // �������� ��������� ��������� ������
    //    _frustumPlanes = GeometryUtility.CalculateFrustumPlanes(Camera.main);

    //    // ���������, ��������� �� �������������� ����� �������� ����� � �������� ��������� ������
    //    if (GeometryUtility.TestPlanesAABB(_frustumPlanes, _tilemapBounds))
    //    {
    //        // �������� ����� �����
    //        if (!_tilemapRenderer.enabled)
    //        {
    //            _tilemapRenderer.enabled = true;
    //            // Debug.Log($"{gameObject.name}: TilemapRenderer �������");
    //        }
    //    }
    //    else
    //    {
    //        // �������� ����� �� �����
    //        if (_tilemapRenderer.enabled)
    //        {
    //            _tilemapRenderer.enabled = false;
    //            // Debug.Log($"{gameObject.name}: TilemapRenderer ��������");
    //        }
    //    }
    //}

    //// ������������� ������� �������� �����
    //private void CalculateTilemapBounds()
    //{
    //    _tilemap.CompressBounds(); // ��������� bounds Tilemap

    //    _tilemapBounds = _tilemap.localBounds; // ����� localBounds ������ tilemapRenderer.bounds

    //    //�������� bounds � ������� ������� ���������
    //    _tilemapBounds.center = transform.TransformPoint(_tilemapBounds.center);

    //}
}
