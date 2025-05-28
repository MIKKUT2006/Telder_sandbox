using System.Collections.Generic;
using UnityEngine;

public class LiquidSimulation : MonoBehaviour
{
    public static float[,] liquidMap;
    public static bool[,] activeLiquidMap;
    public static Queue<Vector2Int> liquidQueue = new Queue<Vector2Int>();

    public static int width, height;
    public static float updateInterval = 0.1f; // �������� ����������
    private static float timer = 0f;

    public static Transform player;
    public static float simulationRadius = 50f;

    public static Texture2D liquidTexture;
    public static SpriteRenderer liquidRenderer;

    // ������������� �������
    public static void Initialize(int w, int h, Transform playerTransform, SpriteRenderer renderer)
    {
        width = w;
        height = h;
        player = playerTransform;
        liquidRenderer = renderer;

        liquidMap = new float[width, height];
        activeLiquidMap = new bool[width, height];

        liquidTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        liquidTexture.filterMode = FilterMode.Point;

        liquidRenderer.sprite = Sprite.Create(
            liquidTexture,
            new Rect(0, 0, width, height),
            new Vector2(0.5f, 0.5f),
            1f
        );
    }

    // ���������� ���������
    void Update()
    {
        timer += Time.deltaTime;
        if (timer < updateInterval) return;
        timer = 0f;

        SimulateLiquids();
        UpdateLiquidTexture();
    }

    // �������� ����� ���������
    private static void SimulateLiquids()
    {
        int count = liquidQueue.Count;
        for (int i = 0; i < count; i++)
        {
            Vector2Int pos = liquidQueue.Dequeue();
            activeLiquidMap[pos.x, pos.y] = false;

            if (!IsNearPlayer(pos)) continue;

            if (SimulateAt(pos.x, pos.y))
                Schedule(pos.x, pos.y);
        }
    }

    // ��������� �������� � ����������� ������
    private static bool SimulateAt(int x, int y)
    {
        float amount = liquidMap[x, y];
        if (amount <= 0f) return false;

        bool moved = false;

        // ���������� ��������� ���� (����������)
        moved |= TryFlow(x, y, x, y - 1); // ����

        // ���������� ��������� ���� (�� �����������)
        moved |= TryFlow(x, y, x - 1, y); // �����
        moved |= TryFlow(x, y, x + 1, y); // ������

        // ���������� ��������� ����� (���� ���� �����)
        moved |= TryFlow(x, y, x, y + 1); // �����

        return moved;
    }

    private static bool TryFlow(int xFrom, int yFrom, int xTo, int yTo)
    {
        if (!InBounds(xTo, yTo)) return false;
        if (!IsAir(xTo, yTo) && !IsLiquid(xTo, yTo)) return false;  // ���� ��� �� ������ ��� ��������, �� �������������� ����

        float from = liquidMap[xFrom, yFrom];
        float to = liquidMap[xTo, yTo];
        float flow = Mathf.Min(0.25f, from);  // ����� ��������, ������������ ������������ ���������

        if (to >= 1f || flow <= 0f) return false;

        // ����������� ��������
        liquidMap[xFrom, yFrom] -= flow;
        liquidMap[xTo, yTo] += flow;

        // ������������� ���������� ��� ���� ������
        Schedule(xTo, yTo);
        return true;
    }

    private static bool IsAir(int x, int y)
    {
        //int tile = ProceduralGeneration.map[x, y];
        //return tile == 0 || tile == 4;  // ������ ������ ��� �����, ������� �� ������������ ��������

        // ������ ��� ������������������
        return true;
    }

    private static bool IsLiquid(int x, int y)
    {
        // �������� �� �������� � �������� ������ (���� ���� ��� ���)
        return liquidMap[x, y] > 0f;
    }

    private static bool InBounds(int x, int y)
    {
        return x >= 0 && y >= 0 && x < width && y < height;
    }
    // ��������, ����� �� �����
    private static bool IsNearPlayer(Vector2Int pos)
    {
        if (player == null) return false;
        return Vector2.Distance(new Vector2(pos.x, pos.y), player.position) < simulationRadius;
    }

    // ������������ ���������� �����
    public static void Schedule(int x, int y)
    {
        if (!InBounds(x, y)) return;
        if (activeLiquidMap[x, y]) return;

        activeLiquidMap[x, y] = true;
        liquidQueue.Enqueue(new Vector2Int(x, y));
    }

    // ���������� �������� ����
    public static void UpdateLiquidTexture()
    {
        if (liquidTexture == null) return;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float amount = Mathf.Clamp01(liquidMap[x, y]);

                if (amount > 0f)
                {
                    // ������� ������������� ���� ����� ����������, �������� ������� � ������������ � ����������� ��������
                    Color color = new Color(0.1f, 0.5f, 1f, Mathf.Lerp(0.3f, 1f, amount)); // ����� ���� � ������������ �������������
                    liquidTexture.SetPixel(x, y, color);
                }
                else
                {
                    // ���������� �������, ���� ���� ���
                    liquidTexture.SetPixel(x, y, Color.clear);
                }
            }
        }

        liquidTexture.Apply();
    }

    // ���������� �������� � ������
    public static void AddLiquid(int x, int y, float amount)
    {
        if (!InBounds(x, y)) return;

        liquidMap[x, y] = Mathf.Clamp01(liquidMap[x, y] + amount);
        Schedule(x, y);
    }

    // �������� �������� �� ������
    public static void RemoveLiquid(int x, int y)
    {
        if (!InBounds(x, y)) return;

        liquidMap[x, y] = 0f;
        Schedule(x, y);
    }
}