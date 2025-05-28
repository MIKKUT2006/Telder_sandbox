using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using static UnityEditor.Experimental.GraphView.GraphView;
using static UnityEditor.PlayerSettings;
using Color = UnityEngine.Color;

public class BlockCreateDestroy : MonoBehaviour
{
    // Карта тайлов и блок
    public Tilemap tilemap;
    public TileBase[] Block;
    public TileBase Light;
    public ParticleSystem destroyParticles;
    public Material destroyParticlesMaterial;
    public AudioSource digSound;
    public AudioClip blockDestroy;
    public AudioClip blockDigClip;
    public AudioClip place;
    public GameObject BlockGameObject;
    public GameObject CursorGameObject;
    public GameObject Inventory;
    private SpriteRenderer BlockGameObjectSprite;

    public Tilemap[,] testTilemap;
    public GameObject[,] testTilemapGameobject;

    // Объект для плавного анимирования установки блока
    public GameObject blockPlacementEffectPrefab; // настроенный спрайт с fade-in
    private HashSet<Vector3Int> placingTiles = new HashSet<Vector3Int>();

    int chunkSize;
    int blockSolid;
    int blockId;
    int digLevel = 1;
    bool isDig = false;
    bool isblockDig = false;
    bool placeBlock = false;
    Vector2 blockPos;
    void Start()
    {
        chunkSize = HelperClass.chunkSize;
        StartCoroutine(DigBlocks());
        testTilemapGameobject = HelperClass.bgChunksGameobject;
        testTilemap = HelperClass.bgChunks;
        BlockGameObjectSprite = BlockGameObject.GetComponent<SpriteRenderer>();
        Inventory = HelperClass.playerInventoryGameObject;
        HelperClass.BlockGameObject = BlockGameObject;
    }

    // Update is called once per frame
    void LateUpdate()
    {
        // При нажатии пкм устанавливаем блок
        if (Input.GetMouseButtonDown(1))
        {
            placeBlock = true;
        }

        if (Input.GetMouseButtonUp(1))
        {
            placeBlock = false;
        }

        if (Input.GetMouseButton(0))
        {
            isDig = true;
        }

        if (Input.GetMouseButtonUp(0))
        {
            isDig = false;
        }
    }

    private void FixedUpdate()
    {
        SetTile();
    }

    public IEnumerator DigBlocks()
    {
        if (isDig == true)
        {
            BreakTile();
        }
        yield return new WaitForSeconds(0.5f);
        StartCoroutine(DigBlocks());
    }

    void SetTile()
    {
        if (placeBlock == true)
        {
            if (HelperClass.barrierPlaceBlock == false)
            {
                // Позиция курсора
                Vector2 Tilepos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                Tilepos = CursorGameObject.transform.position;
                //Debug.Log(CursorGameObject.transform.position);

                int x = (int)Tilepos.x;
                int y = (int)Tilepos.y;
                Vector3Int[] blockPosition = new Vector3Int[1];

                // Получаем координату чанка
                //int chunkCoord = x / chunkSize;   // Получаем координату чанка
                //                                  //chunkCoord = chunkCoord * chunkSize;
                //int ostatok = chunkCoord % 100;
                //if (ostatok != 0)
                //{
                //    chunkCoord -= (chunkCoord - ostatok) + 1;
                //}
                //Debug.Log($"позиция курсора: {x}, координата чанка: {chunkCoord}");
                //Debug.Log($"Чанк, где поставят блок: {HelperClass.ChunksGameobject[0].name} (тут должэен юыть 0)");

                //int chunkCoord = HelperClass.chunkSize;
                //chunkCoord = Mathf.FloorToInt(x / (float)chunkSize);

                // Получаем координату чанка
                int chunkCoordX = ChunkHelper.GetChunkXCoordinate(x);
                int chunkCoordY = ChunkHelper.GetChunkYCoordinate(y);

                Tilemap tilemap = HelperClass.ChunksGameobject[chunkCoordX, chunkCoordY].GetComponent<Tilemap>();
                //Tilemap bgTilemap = HelperClass.ChunksGameobject[chunkCoord].GetComponent<Tilemap>();
                Tilemap lightTilemap = HelperClass.lightChunksGameobject[chunkCoordX, chunkCoordY].GetComponent<Tilemap>();

                blockPosition[0] = new Vector3Int(x, y);
                // Устанавливаем блок в позици курсора

                // Самый крайний чанк слева
                if (!placingTiles.Contains(blockPosition[0]) && tilemap.GetTile(blockPosition[0]) == null &&
                    chunkCoordX == 0) {
                    if ((tilemap.GetTile(blockPosition[0]) == null) && ((HelperClass.Chunks[chunkCoordX, chunkCoordY].GetTile(new Vector3Int(x, y - 1)) != null)
                    || (HelperClass.Chunks[chunkCoordX, chunkCoordY].GetTile(new Vector3Int(x, y + 1)) != null)
                    || (HelperClass.Chunks[chunkCoordX, chunkCoordY].GetTile(new Vector3Int(x - 1, y)) != null)
                    || (HelperClass.Chunks[chunkCoordX, chunkCoordY].GetTile(new Vector3Int(x + 1, y)) != null)
                    || (HelperClass.Chunks[chunkCoordX + 1, chunkCoordY].GetTile(new Vector3Int(x + 1, y)) != null)
                    || (HelperClass.Chunks[chunkCoordX + 1, chunkCoordY].GetTile(new Vector3Int(x - 1, y)) != null)
                    || (HelperClass.bgChunks[chunkCoordX, chunkCoordY].GetTile(new Vector3Int(x, y)) != null)
                    )
                    )
                    {
                        placingTiles.Add(blockPosition[0]); // пометить как "в процессе установки"
                        placeTile(x, y, blockPosition, chunkCoordX, chunkCoordY);
                        return;
                    }
                }
                // Самый крайний справа
                if (!placingTiles.Contains(blockPosition[0]) && tilemap.GetTile(blockPosition[0]) == null &&
                    chunkCoordX == HelperClass.Chunks.GetLength(0) - 1) {
                    if ((tilemap.GetTile(blockPosition[0]) == null) && ((HelperClass.Chunks[chunkCoordX, chunkCoordY].GetTile(new Vector3Int(x, y - 1)) != null)
                    || (HelperClass.Chunks[chunkCoordX, chunkCoordY].GetTile(new Vector3Int(x, y + 1)) != null)
                    || (HelperClass.Chunks[chunkCoordX, chunkCoordY].GetTile(new Vector3Int(x - 1, y)) != null)
                    || (HelperClass.Chunks[chunkCoordX, chunkCoordY].GetTile(new Vector3Int(x + 1, y)) != null)
                    || (HelperClass.Chunks[chunkCoordX - 1, chunkCoordY].GetTile(new Vector3Int(x + 1, y)) != null)
                    || (HelperClass.Chunks[chunkCoordX - 1, chunkCoordY].GetTile(new Vector3Int(x - 1, y)) != null)
                    || (HelperClass.bgChunks[chunkCoordX, chunkCoordY].GetTile(new Vector3Int(x, y)) != null)
                    )
                    )
                    {
                        placingTiles.Add(blockPosition[0]); // пометить как "в процессе установки"
                        placeTile(x, y, blockPosition, chunkCoordX, chunkCoordY);
                        return;
                    }
                        
                }
                // Остальные чанки
                if (!placingTiles.Contains(blockPosition[0]) && tilemap.GetTile(blockPosition[0]) == null &&
                    (tilemap.GetTile(blockPosition[0]) == null) && ((HelperClass.Chunks[chunkCoordX, chunkCoordY].GetTile(new Vector3Int(x, y - 1)) != null)
                    || (HelperClass.Chunks[chunkCoordX, chunkCoordY].GetTile(new Vector3Int(x, y + 1)) != null)
                    || (HelperClass.Chunks[chunkCoordX, chunkCoordY].GetTile(new Vector3Int(x - 1, y)) != null)
                    || (HelperClass.Chunks[chunkCoordX, chunkCoordY].GetTile(new Vector3Int(x + 1, y)) != null)
                    || (HelperClass.Chunks[chunkCoordX + 1, chunkCoordY].GetTile(new Vector3Int(x + 1, y)) != null)
                    || (HelperClass.Chunks[chunkCoordX + 1, chunkCoordY].GetTile(new Vector3Int(x - 1, y)) != null)
                    || (HelperClass.Chunks[chunkCoordX - 1, chunkCoordY].GetTile(new Vector3Int(x + 1, y)) != null)
                    || (HelperClass.Chunks[chunkCoordX - 1, chunkCoordY].GetTile(new Vector3Int(x - 1, y)) != null)
                    || (HelperClass.bgChunks[chunkCoordX, chunkCoordY].GetTile(new Vector3Int(x, y)) != null)
                    )
                    )
                {
                    placingTiles.Add(blockPosition[0]); // пометить как "в процессе установки"
                    placeTile(x, y, blockPosition, chunkCoordX, chunkCoordY);
                    return;
                }
            }
        }
    }

    private void placeTile(int x, int y, Vector3Int[] blockPosition, int chunkCoordX, int chunkCoordY)
    {
        //ProceduralGeneration.map[x, y] = 2;
        if (HelperClass.playerInventory[HelperClass.selectedInventoryCell] != null)
        {
            if (HelperClass.playerInventory[HelperClass.selectedInventoryCell].isObject == false)
            {
                if (HelperClass.playerInventory[HelperClass.selectedInventoryCell].count > 0 && HelperClass.playerInventory[HelperClass.selectedInventoryCell].isBlock == true)
                {

                    // Уменьшаем количество блока в инвентаре
                    HelperClass.playerInventory[HelperClass.selectedInventoryCell].count -= 1;
                    

                    // Устанавливаем тайл в карте тайлов
                    TileBase[] tileBases = new TileBase[1];
                    UnityEngine.Tilemaps.Tile tile = new UnityEngine.Tilemaps.Tile();
                    Sprite sprite = Inventory.transform.Find(HelperClass.selectedInventoryCell.ToString()).transform.Find("Image").GetComponent<Image>().sprite;
                    tile.sprite = sprite;
                    tileBases[0] = tile;
                    // Эффект установки блока
                    GameObject effect = Instantiate(blockPlacementEffectPrefab, new Vector2(x+0.5f,y + 0.5f), Quaternion.identity);
                    effect.GetComponent<SpriteRenderer>().sprite = sprite;
                    StartCoroutine(FinalizePlacement(effect, blockPosition, tileBases, HelperClass.Chunks[chunkCoordX, chunkCoordY]));

                    //ProceduralGeneration.worldTilesMap.Apply();

                    // Устанавливаем в массиве блоков нужный айди блока из инвентаря
                    //ProceduralGeneration.map[x, y] = HelperClass.playerInventory[HelperClass.selectedInventoryCell].blockIndex;

                    // Указываем количество
                    Inventory.transform.Find(HelperClass.selectedInventoryCell.ToString()).transform.Find("Count").GetComponent<TextMeshProUGUI>().text = HelperClass.playerInventory[HelperClass.selectedInventoryCell].count.ToString();
                    HelperClass.equippedItemCell.transform.Find("Count").GetComponent<TextMeshProUGUI>().text = HelperClass.playerInventory[HelperClass.selectedInventoryCell].count.ToString();

                    // Очищаем всё, если больше нет блоков
                    if (HelperClass.playerInventory[HelperClass.selectedInventoryCell].count == 0)
                    {
                        Inventory.transform.Find(HelperClass.selectedInventoryCell.ToString()).transform.Find("Count").GetComponent<TextMeshProUGUI>().enabled = false;
                        Inventory.transform.Find(HelperClass.selectedInventoryCell.ToString()).transform.Find("Image").GetComponent<Image>().enabled = false;
                        HelperClass.playerInventoryGameObject.transform.Find(HelperClass.selectedInventoryCell.ToString()).transform.Find("Image").GetComponent<Image>().sprite = null;
                        HelperClass.playerInventory[HelperClass.selectedInventoryCell] = null;
                        HelperClass.equippedItem.GetComponent<SpriteRenderer>().enabled = false;
                        HelperClass.Cursor.GetComponent<SpriteRenderer>().enabled = false;
                        HelperClass.itemName.text = "";

                        // Очищаем клетку "В руке"
                        HelperClass.equippedItemCell.transform.Find("Count").GetComponent<TextMeshProUGUI>().enabled = false;
                        HelperClass.equippedItemCell.transform.Find("Image").GetComponent<Image>().enabled = false;
                        HelperClass.equippedItemCell.transform.Find("Image").GetComponent<Image>().sprite = null;
                    }
                    // Пересчитываем освещение
                    //ProceduralGeneration.ApplySunlightColumn(x);
                    //ProceduralGeneration.RecalculateLightAround(x, y);
                    //ProceduralGeneration.UpdateLightTexture();


                    //ProceduralGeneration.worldTilesMap.Apply();

                    digSound.clip = place;
                    digSound.Play();
                }
            }
            else
            {
                if(HelperClass.playerInventory[HelperClass.selectedInventoryCell].count > 0)
                {

                    // Уменьшаем количество блока в инвентаре
                    // Уменьшаем количество блока в инвентаре
                    HelperClass.playerInventory[HelperClass.selectedInventoryCell].count -= 1;
                    // Устанавливаем в массиве блоков нужный айди блока из инвентаря
                    //ProceduralGeneration.map[x, y] = HelperClass.playerInventory[HelperClass.selectedInventoryCell].blockIndex;
                    // Устанавливаем тайл в карте тайлов
                    Debug.Log(HelperClass.playerInventory[HelperClass.selectedInventoryCell].prefab);
                    
                    Vector3 vector3 = new Vector3(blockPosition[0].x + 0.5f, blockPosition[0].y + 0.5f, blockPosition[0].z + 0.5f);
                    Debug.Log(HelperClass.playerInventory[HelperClass.selectedInventoryCell].prefab);
                    Debug.Log(BlocksData.allBlocks[20].prefab.name);
                    Instantiate(HelperClass.playerInventory[HelperClass.selectedInventoryCell].prefab, vector3, Quaternion.identity);

                    Debug.Log("");
                    // Устанавливаем в массиве блоков нужный айди блока из инвентаря
                    //ProceduralGeneration.map[x, y] = HelperClass.playerInventory[HelperClass.selectedInventoryCell].blockIndex;

                    // Устанавливаем значение твердого блока для потоков воды
                    //HelperClass.Cells[x, y].SetType(CellType.Solid);

                    Inventory.transform.Find(HelperClass.selectedInventoryCell.ToString()).transform.Find("Count").GetComponent<TextMeshProUGUI>().text = HelperClass.playerInventory[HelperClass.selectedInventoryCell].count.ToString();
                    // Очищаем всё, если больше нет блоков
                    if (HelperClass.playerInventory[HelperClass.selectedInventoryCell].count == 0)
                    {
                        Inventory.transform.Find(HelperClass.selectedInventoryCell.ToString()).transform.Find("Count").GetComponent<TextMeshProUGUI>().enabled = false;
                        Inventory.transform.Find(HelperClass.selectedInventoryCell.ToString()).transform.Find("Image").GetComponent<Image>().enabled = false;
                        HelperClass.playerInventoryGameObject.transform.Find(HelperClass.selectedInventoryCell.ToString()).transform.Find("Image").GetComponent<Image>().sprite = null;
                        HelperClass.playerInventory[HelperClass.selectedInventoryCell] = null;
                        HelperClass.equippedItem.GetComponent<SpriteRenderer>().enabled = false;
                        HelperClass.Cursor.GetComponent<SpriteRenderer>().enabled = false;
                        HelperClass.itemName.text = "";

                        // Очищаем клетку "В руке"
                        HelperClass.equippedItemCell.transform.Find("Count").GetComponent<TextMeshProUGUI>().enabled = false;
                        HelperClass.equippedItemCell.transform.Find("Image").GetComponent<Image>().enabled = false;
                        HelperClass.equippedItemCell.transform.Find("Image").GetComponent<Image>().sprite = null;
                    }

                    digSound.clip = place;
                    digSound.Play();
                }
            }
            
        }
    }

    void BreakTile()
    {
        //if (HelperClass.eguipmentItem != null)
        //{
        //    if (HelperClass.eguipmentItem.toolType == 4)
        //    {
        //        return;
        //    }
        //}
        
        ////Debug.Log("Оставшаяся прочность: " + blockSolid);

        //// Позиция курсора
        //Vector2 Tilepos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        //int x = (int)Tilepos.x;
        //int y = (int)Tilepos.y;
        //Vector3Int blockPosition = new Vector3Int(x, y);

        //// Получаем координату чанка
        //int chunkCoordX = ChunkHelper.GetChunkXCoordinate(x);
        //int chunkCoordY = ChunkHelper.GetChunkYCoordinate(y);

        //Tilemap tilemap = HelperClass.ChunksGameobject[chunkCoordX, chunkCoordY].GetComponent<Tilemap>();
        //Tilemap bgTilemap = HelperClass.bgChunksGameobject[chunkCoordX, chunkCoordY].GetComponent<Tilemap>();
        //if (tilemap.GetTile(blockPosition) != null)
        //{
        //    //Debug.Log("Блок существует");
        //    if (blockPos == new Vector2(x, y))
        //    {
        //        //Debug.Log("Выбран этот же блок");
        //    }
        //    else
        //    {
                
        //        blockSolid = BlocksData.allBlocks[ProceduralGeneration.map[x, y]].blocksSolidity;          // Получаем прочность блока
        //        //Debug.Log("Вы выбрали другой блок с прочностью: " + blockSolid);
        //        blockPos = new Vector2Int(x, y);
        //        blockId = BlocksData.allBlocks[ProceduralGeneration.map[x, y]].blockIndex;                 // Получаем айди блока
        //        isblockDig = true;
        //    }
        //    TileBase tile = tilemap.GetTile(blockPosition);
        //    if (blockSolid > 0)
        //    {
        //        Debug.Log($"Нужен инструмент: {BlocksData.allBlocks[ProceduralGeneration.map[x, y]].needsToolType}");
        //        //Debug.Log($"У нас: {HelperClass.eguipmentItem.name}");
        //        if (HelperClass.eguipmentItem != null && BlocksData.allBlocks[ProceduralGeneration.map[x, y]].needsToolType == HelperClass.eguipmentItem.toolType)
        //        {
        //            blockSolid -= HelperClass.eguipmentItem.toolPower;
        //        }
        //        else
        //        {
        //            blockSolid -= 1;
        //        }
        //        Vector3 newpos = new Vector3(x + 0.5f, y + 0.5f);
        //        ParticleSystem newParticles = Instantiate(destroyParticles, newpos, Quaternion.identity);
        //        newParticles.gameObject.SetActive(true);
        //        Material destroyMaterial = newParticles.GetComponent<ParticleSystemRenderer>().material;

        //        // Получение его текстуры, если она есть
        //        if (tile is UnityEngine.Tilemaps.Tile)
        //        {
        //            UnityEngine.Tilemaps.Tile tileScript = tile as UnityEngine.Tilemaps.Tile;
        //            Texture2D texture = tileScript.sprite.texture;
        //            destroyMaterial.mainTexture = texture;
        //        }
        //        else if (tile is RuleTile)
        //        {
        //            RuleTile ruleTile = tile as RuleTile;
        //            Sprite sprite = ruleTile.m_DefaultSprite;
        //            Texture2D texture = sprite.texture;
        //            destroyMaterial.mainTexture = texture;
        //        }

        //        digSound.clip = blockDigClip;
        //        digSound.Play();
        //        Debug.Log(blockSolid);
        //    }
        //    else
        //    {
        //        // Если блок сломан
        //        tilemap.SetTile(blockPosition, null);   // Ломаем блок
        //        blockSolid = BlocksData.allBlocks[ProceduralGeneration.bgMap[x, y]].blocksSolidity;

        //        // Устанавливаем значение пустого блока для потоков воды
        //        //HelperClass.Cells[x, y].SetType(CellType.Blank);

        //        // Устанавливаем освещение вокруг
        //        //int chunkCoordX = ChunkHelper.GetChunkXCoordinate(x);
        //        //int chunkCoordY = ChunkHelper.GetChunkXCoordinate(y);
        //        Tilemap lightTileMap = HelperClass.lightChunksGameobject[chunkCoordX, chunkCoordY].GetComponent<Tilemap>();

        //        // Устанавливаем тайлы освещения
        //        lightTileMap.SetTile(new Vector3Int(x, y, 0), ProceduralGeneration.lightTiles[0]);



        //        // Цикл для создания всех выпадающих предметов с блока
        //        foreach (int drop in BlocksData.allBlocks.Where(x => x.blockIndex == blockId).FirstOrDefault().dropId)
        //        {
        //            Vector3 newpos = new Vector3(x + 0.5f, y + 0.5f);
        //            AllItemsAndBlocks currentDrop = BlocksData.allBlocks.Where(x => x.blockIndex == drop).FirstOrDefault();
        //            Debug.Log(currentDrop.name);
        //            GameObject newBlock = Instantiate(BlockGameObject, newpos, Quaternion.identity);
        //            newBlock.name = currentDrop.blockIndex.ToString();
        //            Sprite sprite = null;
        //            // Получение его текстуры, если она есть

        //            newBlock.GetComponent<SpriteRenderer>().sprite = (Sprite)Resources.Load(currentDrop.imagePath, typeof(Sprite));
        //        }
        //        // Конец цикла
        //        Debug.Log("Блок сломан");
        //        isblockDig = false;
        //        Tilemap lightTilemap = HelperClass.lightChunksGameobject[chunkCoordX, chunkCoordY].GetComponent<Tilemap>();
        //        ProceduralGeneration.map[x, y] = 0;
        //        //for (int dx = -2; dx <= 2; dx++)
        //        //{
        //        //    ProceduralGeneration.ApplySunlightColumn(x + dx);
        //        //}
        //        ProceduralGeneration.UpdateLightingAfterBlockChange(x,y);
        //        ProceduralGeneration.DebugPrintLightMap(); // Выведет карту освещенности
        //        ProceduralGeneration.UpdateLightTexture();

                

        //        digSound.clip = blockDestroy;
        //        digSound.Play();

                
        //        HelperClass.Chunks[chunkCoordX, chunkCoordY] = tilemap;
        //        HelperClass.lightChunks[chunkCoordX, chunkCoordY] = lightTilemap;
        //    }
            
        //}
        //// БЛОК ЗАДНЕГО ФОНА
        //else if (bgTilemap.GetTile(blockPosition) != null)
        //{
        //    //Debug.Log("Блок существует");
        //    if (blockPos == new Vector2(x, y))
        //    {
        //        //Debug.Log("Выбран этот же блок");
        //    }
        //    else
        //    {

        //        blockSolid = BlocksData.allBlocks[ProceduralGeneration.bgMap[x, y]].blocksSolidity;          // Получаем прочность блока
        //        //Debug.Log("Вы выбрали другой блок с прочностью: " + blockSolid);
        //        blockPos = new Vector2Int(x, y);
        //        blockId = BlocksData.allBlocks[ProceduralGeneration.bgMap[x, y]].blockIndex;                 // Получаем айди блока
        //        isblockDig = true;
        //    }
        //    TileBase tile = bgTilemap.GetTile(blockPosition);
        //    if (blockSolid > 0)
        //    {
        //        Debug.Log($"Нужен инструмент: {BlocksData.allBlocks[ProceduralGeneration.bgMap[x, y]].needsToolType}");
        //        //Debug.Log($"У нас: {HelperClass.eguipmentItem.name}");
        //        if (HelperClass.eguipmentItem != null && BlocksData.allBlocks[ProceduralGeneration.bgMap[x, y]].needsToolType == HelperClass.eguipmentItem.toolType)
        //        {
        //            blockSolid -= HelperClass.eguipmentItem.toolPower;
        //        }
        //        else
        //        {
        //            blockSolid -= 1;
        //        }
        //        Vector3 newpos = new Vector3(x + 0.5f, y + 0.5f);
        //        ParticleSystem newParticles = Instantiate(destroyParticles, newpos, Quaternion.identity);
        //        newParticles.gameObject.SetActive(true);
        //        Material destroyMaterial = newParticles.GetComponent<ParticleSystemRenderer>().material;

        //        // Получение его текстуры, если она есть
        //        if (tile is UnityEngine.Tilemaps.Tile)
        //        {
        //            UnityEngine.Tilemaps.Tile tileScript = tile as UnityEngine.Tilemaps.Tile;
        //            Texture2D texture = tileScript.sprite.texture;
        //            destroyMaterial.mainTexture = texture;
        //        }
        //        else if (tile is RuleTile)
        //        {
        //            RuleTile ruleTile = tile as RuleTile;
        //            Sprite sprite = ruleTile.m_DefaultSprite;
        //            Texture2D texture = sprite.texture;
        //            destroyMaterial.mainTexture = texture;
        //        }

        //        digSound.clip = blockDigClip;
        //        digSound.Play();
        //        Debug.Log(blockSolid);
        //    }
        //    else
        //    {
        //        // Если блок сломан
        //        bgTilemap.SetTile(blockPosition, null);   // Ломаем блок
                
        //        ProceduralGeneration.bgMap[x, y] = 0;
        //        Tilemap lightTileMap = HelperClass.lightChunksGameobject[chunkCoordX, chunkCoordY].GetComponent<Tilemap>();

        //        // Устанавливаем тайлы освещения
        //        lightTileMap.SetTile(new Vector3Int(x, y, 0), ProceduralGeneration.lightTiles[0]);
        //        // Ставим источник освещения
        //        Debug.Log("Освещение");
        //        ProceduralGeneration.AddLightSource(x, y, 1, 5);
        //        ProceduralGeneration.UpdateLightTexture();
        //        //ProceduralGeneration.worldTilesMap.Apply();


        //        // Цикл для создания всех выпадающих предметов с блока
        //        foreach (int drop in BlocksData.allBlocks.Where(x => x.blockIndex == blockId).FirstOrDefault().dropId)
        //        {
        //            Vector3 newpos = new Vector3(x + 0.5f, y + 0.5f);
        //            AllItemsAndBlocks currentDrop = BlocksData.allBlocks.Where(x => x.blockIndex == drop).FirstOrDefault();
        //            Debug.Log(currentDrop.name);
        //            GameObject newBlock = Instantiate(BlockGameObject, newpos, Quaternion.identity);
        //            newBlock.name = currentDrop.blockIndex.ToString();
        //            Sprite sprite = null;
        //            // Получение его текстуры, если она есть

        //            // Загружаем изображение из файла
        //            float pixelsPerUnit = 16;

        //            //byte[] imageData = File.ReadAllBytes(currentDrop.imagePath);
        //            //Texture2D texture = new Texture2D(16, 16);
        //            //texture.LoadImage(imageData); // ��������� ������ ����������� � ��������
        //            //texture.filterMode = FilterMode.Point;

        //            //// ������������ ������� ������� � ������ pixelsPerUnit
        //            //float width = texture.width / 16;
        //            //float height = texture.height / 16;

        //            // �������� ������� �� ��������
        //            //Sprite newSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), pixelsPerUnit);
        //            Sprite newSprite = (Sprite)Resources.Load(currentDrop.imagePath, typeof(Sprite));

        //            newBlock.GetComponent<SpriteRenderer>().sprite = newSprite;

        //            //ParticleSystem newParticles = Instantiate(destroyParticles, newpos, Quaternion.identity);
        //            //newParticles.gameObject.SetActive(true);
        //            //Material destroyMaterial = newParticles.GetComponent<ParticleSystemRenderer>().material;
        //            //destroyMaterial.mainTexture = texture;
        //        }
        //        // Конец цикла

        //        Debug.Log("Блок сломан");
        //        isblockDig = false;
        //        Tilemap lightTilemap = HelperClass.lightChunksGameobject[chunkCoordX, chunkCoordY].GetComponent<Tilemap>();

        //        digSound.clip = blockDestroy;
        //        digSound.Play();


        //        HelperClass.Chunks[chunkCoordX, chunkCoordY] = tilemap;
        //        HelperClass.lightChunks[chunkCoordX, chunkCoordY] = lightTilemap;
        //    }
        //}
    }
    IEnumerator FinalizePlacement(GameObject visual, Vector3Int[] pos, TileBase[] tile, Tilemap tilemap)
    {
        yield return new WaitForSeconds(0.3f); // длительность анимации
        tilemap.SetTiles(pos, tile);
        Destroy(visual);
        foreach (var p in pos)
            placingTiles.Remove(p); // разрешить установку снова
    }
}

