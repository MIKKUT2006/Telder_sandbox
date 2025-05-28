using UnityEngine;

public class CellularLiquidSimulation : MonoBehaviour
{
    public static float[,] liquidMap;
    public static int width, height;

    public static float maxLiquid = 1f;
    public static float minFlow = 0.005f;
    public static float maxCompress = 0.25f;
    public static int simulationSteps = 1;

    public Texture2D liquidTexture;
    public Color liquidColor = new Color(0, 0.5f, 1f, 0.8f);
    public SpriteRenderer renderer;

    public static int[,] solidMap; // подключаетс€ извне из ProceduralGeneration.map

    void Start()
    {
        //CellularLiquidSimulation.solidMap = ProceduralGeneration.map;

        //width = ProceduralGeneration.mapWidth;
        //height = ProceduralGeneration.mapHeight;
        liquidMap = new float[width, height];

        liquidTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        liquidTexture.filterMode = FilterMode.Point;
        renderer = GetComponent<SpriteRenderer>();
        renderer.sprite = Sprite.Create(liquidTexture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 1);

        for (int i = 64; i < 68; i++)
            AddLiquid(i, 120, 1f);
    }

    void FixedUpdate()
    {
        Simulate();
        UpdateTexture();
    }

    public static void Simulate()
    {
        for (int step = 0; step < simulationSteps; step++)
        {
            float[,] newMap = (float[,])liquidMap.Clone();

            for (int x = 0; x < width; x++)
            {
                for (int y = 1; y < height - 1; y++)
                {
                    if (IsSolid(x, y)) continue;

                    float remaining = liquidMap[x, y];
                    if (remaining <= 0f) continue;

                    // ¬низ
                    if (!IsSolid(x, y - 1))
                    {
                        float below = liquidMap[x, y - 1];
                        float flow = GetVerticalFlow(remaining, below);
                        if (flow > minFlow)
                        {
                            newMap[x, y] -= flow;
                            newMap[x, y - 1] += flow;
                            remaining -= flow;
                        }
                    }

                    // ¬бок
                    float sideFlow = remaining / 2f;
                    if (x > 0 && !IsSolid(x - 1, y))
                    {
                        float left = liquidMap[x - 1, y];
                        float lFlow = (sideFlow - left) / 4f;
                        lFlow = Mathf.Clamp(lFlow, 0, remaining);
                        if (lFlow > minFlow)
                        {
                            newMap[x, y] -= lFlow;
                            newMap[x - 1, y] += lFlow;
                            remaining -= lFlow;
                        }
                    }
                    if (x < width - 1 && !IsSolid(x + 1, y))
                    {
                        float right = liquidMap[x + 1, y];
                        float rFlow = (sideFlow - right) / 4f;
                        rFlow = Mathf.Clamp(rFlow, 0, remaining);
                        if (rFlow > minFlow)
                        {
                            newMap[x, y] -= rFlow;
                            newMap[x + 1, y] += rFlow;
                            remaining -= rFlow;
                        }
                    }

                    // ¬верх под давлением
                    if (remaining > maxLiquid && !IsSolid(x, y + 1))
                    {
                        float above = liquidMap[x, y + 1];
                        float flow = (remaining - above) / 4f;
                        flow = Mathf.Clamp(flow, 0, remaining);
                        if (flow > minFlow)
                        {
                            newMap[x, y] -= flow;
                            newMap[x, y + 1] += flow;
                        }
                    }
                }
            }
            liquidMap = newMap;
        }
    }

    private static bool IsSolid(int x, int y)
    {
        if (x < 0 || x >= width || y < 0 || y >= height)
            return true;

        int value = solidMap[x, y]; // можно прив€зать напр€мую к ProceduralGeneration.map
        return value != 0 && value != 4;
    }

    private static float GetVerticalFlow(float source, float destination)
    {
        float total = source + destination;
        if (total <= maxLiquid)
            return Mathf.Min(source, maxLiquid - destination);
        else if (destination < maxLiquid)
            return (total - maxLiquid) * 0.5f;
        else
            return 0f;
    }

    public static void AddLiquid(int x, int y, float amount)
    {
        if (x < 0 || x >= width || y < 0 || y >= height) return;
        liquidMap[x, y] = Mathf.Clamp(liquidMap[x, y] + amount, 0f, maxLiquid + maxCompress);
    }

    void UpdateTexture()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float amt = Mathf.Clamp01(liquidMap[x, y]);
                Color c = liquidColor;
                c.a *= amt;
                liquidTexture.SetPixel(x, y, c);
            }
        }
        liquidTexture.Apply();
    }
}
