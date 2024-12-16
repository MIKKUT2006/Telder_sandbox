﻿using System.Collections;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

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

    public Tilemap[] testTilemap;
    public GameObject[] testTilemapGameobject;

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

    IEnumerator DigBlocks()
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
                int chunkCoord = x / chunkSize;   // Получаем координату чанка
                                                  //chunkCoord = chunkCoord * chunkSize;
                int ostatok = chunkCoord % 100;
                if (ostatok != 0)
                {
                    chunkCoord -= (chunkCoord - ostatok) + 1;
                }
                //Debug.Log($"позиция курсора: {x}, координата чанка: {chunkCoord}");
                //Debug.Log($"Чанк, где поставят блок: {HelperClass.ChunksGameobject[0].name} (тут должэен юыть 0)");
                Tilemap tilemap = HelperClass.ChunksGameobject[chunkCoord].GetComponent<Tilemap>();
                //Tilemap bgTilemap = HelperClass.ChunksGameobject[chunkCoord].GetComponent<Tilemap>();
                Tilemap lightTilemap = HelperClass.lightChunksGameobject[chunkCoord].GetComponent<Tilemap>();

                blockPosition[0] = new Vector3Int(x, y);
                // Устанавливаем блок в позици курсора

                // Самый крайний чанк слева
                if (chunkCoord == 0) {
                    if ((tilemap.GetTile(blockPosition[0]) == null) && ((HelperClass.Chunks[chunkCoord].GetTile(new Vector3Int(x, y - 1)) != null)
                    || (HelperClass.Chunks[chunkCoord].GetTile(new Vector3Int(x, y + 1)) != null)
                    || (HelperClass.Chunks[chunkCoord].GetTile(new Vector3Int(x - 1, y)) != null)
                    || (HelperClass.Chunks[chunkCoord].GetTile(new Vector3Int(x + 1, y)) != null)
                    || (HelperClass.Chunks[chunkCoord + 1].GetTile(new Vector3Int(x + 1, y)) != null)
                    || (HelperClass.Chunks[chunkCoord + 1].GetTile(new Vector3Int(x - 1, y)) != null)
                    || (HelperClass.bgChunks[chunkCoord].GetTile(new Vector3Int(x, y)) != null)
                    )
                    )
                    {
                        //Debug.Log("Крайний чанк");
                        placeTile(x, y, blockPosition, chunkCoord);
                        return;
                    }
                }
                // Самый крайний справа
                if (chunkCoord == HelperClass.Chunks.Count() - 1) {
                    if ((tilemap.GetTile(blockPosition[0]) == null) && ((HelperClass.Chunks[chunkCoord].GetTile(new Vector3Int(x, y - 1)) != null)
                    || (HelperClass.Chunks[chunkCoord].GetTile(new Vector3Int(x, y + 1)) != null)
                    || (HelperClass.Chunks[chunkCoord].GetTile(new Vector3Int(x - 1, y)) != null)
                    || (HelperClass.Chunks[chunkCoord].GetTile(new Vector3Int(x + 1, y)) != null)
                    || (HelperClass.Chunks[chunkCoord - 1].GetTile(new Vector3Int(x + 1, y)) != null)
                    || (HelperClass.Chunks[chunkCoord - 1].GetTile(new Vector3Int(x - 1, y)) != null)
                    || (HelperClass.bgChunks[chunkCoord].GetTile(new Vector3Int(x, y)) != null)
                    )
                    )
                    {
                        placeTile(x, y, blockPosition, chunkCoord);
                        return;
                    }
                        
                }
                // Остальные чанки
                if ((tilemap.GetTile(blockPosition[0]) == null) && ((HelperClass.Chunks[chunkCoord].GetTile(new Vector3Int(x, y - 1)) != null)
                    || (HelperClass.Chunks[chunkCoord].GetTile(new Vector3Int(x, y + 1)) != null)
                    || (HelperClass.Chunks[chunkCoord].GetTile(new Vector3Int(x - 1, y)) != null)
                    || (HelperClass.Chunks[chunkCoord].GetTile(new Vector3Int(x + 1, y)) != null)
                    || (HelperClass.Chunks[chunkCoord + 1].GetTile(new Vector3Int(x + 1, y)) != null)
                    || (HelperClass.Chunks[chunkCoord + 1].GetTile(new Vector3Int(x - 1, y)) != null)
                    || (HelperClass.Chunks[chunkCoord - 1].GetTile(new Vector3Int(x + 1, y)) != null)
                    || (HelperClass.Chunks[chunkCoord - 1].GetTile(new Vector3Int(x - 1, y)) != null)
                    || (HelperClass.bgChunks[chunkCoord].GetTile(new Vector3Int(x, y)) != null)
                    )
                    )
                {
                    placeTile(x, y, blockPosition, chunkCoord);
                    return;
                }
            }
        }
    }

    private void placeTile(int x, int y, Vector3Int[] blockPosition, int chunkCoord)
    {
        //ProceduralGeneration.map[x, y] = 2;
        if (HelperClass.playerInventory[HelperClass.selectedInventoryCell] != null)
        {
            if (HelperClass.playerInventory[HelperClass.selectedInventoryCell].count > 0 && HelperClass.playerInventory[HelperClass.selectedInventoryCell].isBlock == true)
            {
                // Уменьшаем количество блока в инвентаре
                HelperClass.playerInventory[HelperClass.selectedInventoryCell].count -= 1;
                // Устанавливаем тайл в карте тайлов
                TileBase[] tileBases = new TileBase[1];
                UnityEngine.Tilemaps.Tile tile = new UnityEngine.Tilemaps.Tile();
                tile.sprite = Inventory.transform.Find(HelperClass.selectedInventoryCell.ToString()).transform.Find("Image").GetComponent<Image>().sprite;
                tileBases[0] = tile;
                HelperClass.Chunks[chunkCoord].SetTiles(blockPosition, tileBases);
                // Устанавливаем в массиве блоков нужный айди блока из инвентаря
                ProceduralGeneration.map[x, y] = HelperClass.playerInventory[HelperClass.selectedInventoryCell].blockIndex;

                // Устанавливаем значение твердого блока для потоков воды
                HelperClass.Cells[x, y].SetType(CellType.Solid);

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
                }

                digSound.clip = place;
                digSound.Play();
            }
        }
    }

    void BreakTile()
    {
        Debug.Log("test!");
        Debug.Log("Оставшаяся прочность: " + blockSolid);

        // Позиция курсора
        Vector2 Tilepos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        int x = (int)Tilepos.x;
        int y = (int)Tilepos.y;
        Vector3Int blockPosition = new Vector3Int(x, y);

        // Получаем координату чанка
        int chunkCoord = x / chunkSize;   // Получаем координату чанка
        int ostatok = chunkCoord % 100;
        if (ostatok != 0)
        {
            chunkCoord -= (chunkCoord - ostatok) + 1;
        }

        Tilemap tilemap = HelperClass.ChunksGameobject[chunkCoord].GetComponent<Tilemap>();
        if (tilemap.GetTile(blockPosition) != null)
        {
            //Debug.Log("Блок существует");
            if (blockPos == new Vector2(x, y))
            {
                Debug.Log("Выбран этот же блок");
            }
            else
            {
                
                blockSolid = BlocksData.allBlocks[ProceduralGeneration.map[x, y]].blocksSolidity;          // Получаем прочность блока
                Debug.Log("Вы выбрали другой блок с прочностью: " + blockSolid);
                blockPos = new Vector2Int(x, y);
                blockId = BlocksData.allBlocks[ProceduralGeneration.map[x, y]].blockIndex;                 // Получаем айди блока
                isblockDig = true;
            }
            TileBase tile = tilemap.GetTile(blockPosition);
            if (blockSolid > 0)
            {
                blockSolid -= digLevel;
                Vector3 newpos = new Vector3(x + 0.5f, y + 0.5f);
                ParticleSystem newParticles = Instantiate(destroyParticles, newpos, Quaternion.identity);
                newParticles.gameObject.SetActive(true);
                Material destroyMaterial = newParticles.GetComponent<ParticleSystemRenderer>().material;

                // Получение его текстуры, если она есть
                if (tile is UnityEngine.Tilemaps.Tile)
                {
                    UnityEngine.Tilemaps.Tile tileScript = tile as UnityEngine.Tilemaps.Tile;
                    Texture2D texture = tileScript.sprite.texture;
                    destroyMaterial.mainTexture = texture;
                }
                else if (tile is RuleTile)
                {
                    RuleTile ruleTile = tile as RuleTile;
                    Sprite sprite = ruleTile.m_DefaultSprite;
                    Texture2D texture = sprite.texture;
                    destroyMaterial.mainTexture = texture;
                }

                digSound.clip = blockDigClip;
                digSound.Play();
                //Debug.Log(blockSolid);
            }
            else
            {
                // Если блок сломан

                // Устанавливаем значение пустого блока для потоков воды
                HelperClass.Cells[x, y].SetType(CellType.Blank);


                // Цикл для создания всех выпадающих предметов с блока
                foreach (int drop in BlocksData.allBlocks.Where(x => x.blockIndex == blockId).FirstOrDefault().dropId)
                {
                    Vector3 newpos = new Vector3(x + 0.5f, y + 0.5f);
                    AllItemsAndBlocks currentDrop = BlocksData.allBlocks.Where(x => x.blockIndex == drop).FirstOrDefault();
                    Debug.Log(currentDrop.name);
                    GameObject newBlock = Instantiate(BlockGameObject, newpos, Quaternion.identity);
                    newBlock.name = currentDrop.blockIndex.ToString();
                    Sprite sprite = null;
                    // Получение его текстуры, если она есть

                    // Загружаем изображение из файла
                    float pixelsPerUnit = 16;

                    byte[] imageData = File.ReadAllBytes(currentDrop.imagePath);
                    Texture2D texture = new Texture2D(16, 16);
                    texture.LoadImage(imageData); // ��������� ������ ����������� � ��������
                    texture.filterMode = FilterMode.Point;

                    // ������������ ������� ������� � ������ pixelsPerUnit
                    float width = texture.width / 16;
                    float height = texture.height / 16;

                    // �������� ������� �� ��������
                    Sprite newSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), pixelsPerUnit);

                    newBlock.GetComponent<SpriteRenderer>().sprite = newSprite;

                    ParticleSystem newParticles = Instantiate(destroyParticles, newpos, Quaternion.identity);
                    newParticles.gameObject.SetActive(true);
                    Material destroyMaterial = newParticles.GetComponent<ParticleSystemRenderer>().material;
                    destroyMaterial.mainTexture = texture;

                }
                // Конец цикла

                Debug.Log("Блок сломан");
                isblockDig = false;
                Tilemap lightTilemap = HelperClass.lightChunksGameobject[chunkCoord].GetComponent<Tilemap>();

                if (ProceduralGeneration.map[x, y + 1] == 0)                        // Проверка на освещенность солнцем блока
                {
                    ProceduralGeneration.map[x, y] = 4;
                    lightTilemap.SetTile(blockPosition, Light);
                    if (ProceduralGeneration.map[x, y - 1] == 4)
                    {
                        lightTilemap.SetTile(new Vector3Int(x, y - 1), Light);

                        for (int i = y; i > 0; i--)
                        {
                            if (ProceduralGeneration.map[x, i] == 4)
                            {
                                lightTilemap.SetTile(new Vector3Int(x, i), Light);
                                ProceduralGeneration.map[x, i] = 0;                 // Ставим освещенность сломанному блоку
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                    //ProceduralGeneration.map[x, y] = 4;                           // Ставим освещенность сломанному блоку
                    ProceduralGeneration.map[x, y] = 0;
                }
                else
                {
                    ProceduralGeneration.map[x, y] = 4;                             // Ставим освещенность сломанному блоку
                }

                digSound.clip = blockDestroy;
                digSound.Play();
                tilemap.SetTile(blockPosition, null);
                HelperClass.Chunks[chunkCoord] = tilemap;
                HelperClass.lightChunks[chunkCoord] = lightTilemap;
            }
            
        }
    }
}

