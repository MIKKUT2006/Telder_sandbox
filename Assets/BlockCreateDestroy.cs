using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using UnityEngine.WSA;

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
                Debug.Log($"позиция курсора: {x}, координата чанка: {chunkCoord}");
                Debug.Log($"Чанк, где поставят блок: {HelperClass.ChunksGameobject[chunkCoord].name}");
                Tilemap tilemap = HelperClass.ChunksGameobject[chunkCoord].GetComponent<Tilemap>();
                //Tilemap bgTilemap = HelperClass.ChunksGameobject[chunkCoord].GetComponent<Tilemap>();
                Tilemap lightTilemap = HelperClass.lightChunksGameobject[chunkCoord].GetComponent<Tilemap>();

                blockPosition[0] = new Vector3Int(x, y);
                // Устанавливаем блок в позици курсора

                // Вывод блока в позиции курсора
                //Debug.Log((tilemap.GetTile(blockPosition[0])));

                // Проверка на то, что нет блока
                if ((tilemap.GetTile(blockPosition[0]) == null) && ((HelperClass.Chunks[chunkCoord].GetTile(new Vector3Int(x, y - 1)) != null)
                    || (HelperClass.Chunks[chunkCoord].GetTile(new Vector3Int(x, y + 1)) != null)
                    || (HelperClass.Chunks[chunkCoord].GetTile(new Vector3Int(x - 1, y)) != null)
                    || (HelperClass.Chunks[chunkCoord].GetTile(new Vector3Int(x + 1, y)) != null)
                    || (HelperClass.Chunks[chunkCoord + 1].GetTile(new Vector3Int(x + 1, y)) != null)
                    || (HelperClass.Chunks[chunkCoord + 1].GetTile(new Vector3Int(x - 1, y)) != null)
                    || (HelperClass.Chunks[chunkCoord - 1].GetTile(new Vector3Int(x + 1, y)) != null)
                    || (HelperClass.Chunks[chunkCoord - 1].GetTile(new Vector3Int(x - 1, y)) != null)
                    || (HelperClass.bgChunksGameobject[chunkCoord].GetComponent<Tilemap>().GetTile(new Vector3Int(x, y)) != null)
                    )
                    )
                {
                    ProceduralGeneration.map[x, y] = 2;
                    if (HelperClass.playerInventory[HelperClass.selectedInventoryCell] != null)
                    {
                        if (HelperClass.playerInventory[HelperClass.selectedInventoryCell].count > 0 && HelperClass.playerInventory[HelperClass.selectedInventoryCell].isBlock == true)
                        {
                            // Устанавливаем тайл в карте тайлов
                            TileBase[] tileBases = new TileBase[1];
                            UnityEngine.Tilemaps.Tile tile = new UnityEngine.Tilemaps.Tile();
                            tile.sprite = Inventory.transform.Find(HelperClass.selectedInventoryCell.ToString()).transform.Find("Image").GetComponent<Image>().sprite;
                            tileBases[0] = tile;
                            tilemap.SetTiles(blockPosition, tileBases);
                            // Устанавливаем в массиве блоков нужный айди блока из инвентаря
                            ProceduralGeneration.map[x,y] = HelperClass.playerInventory[HelperClass.selectedInventoryCell].blockIndex;
                            // Проигрываем анимацию использования предмета в инвентаре
                            //HelperClass.equippedCellAnimator.Play("UseItem");
                            // Уменьшаем количество блока в инвентаре
                            HelperClass.playerInventory[HelperClass.selectedInventoryCell].count -= 1;
                            Inventory.transform.Find(HelperClass.selectedInventoryCell.ToString()).transform.Find("Count").GetComponent<TextMeshProUGUI>().text = HelperClass.playerInventory[HelperClass.selectedInventoryCell].count.ToString();
                            // Очищаем всё, если больше нет блоков
                            if (HelperClass.playerInventory[HelperClass.selectedInventoryCell].count == 0)
                            {
                                Inventory.transform.Find(HelperClass.selectedInventoryCell.ToString()).transform.Find("Count").GetComponent<TextMeshProUGUI>().enabled = false;
                                Inventory.transform.Find(HelperClass.selectedInventoryCell.ToString()).transform.Find("Image").GetComponent<Image>().enabled = false;
                                HelperClass.playerInventoryGameObject.transform.Find(HelperClass.selectedInventoryCell.ToString()).transform.Find("Image").GetComponent<Image>().sprite = null;
                                HelperClass.playerInventory[HelperClass.selectedInventoryCell] = null;
                                HelperClass.equippedItem.GetComponent<SpriteRenderer>().enabled = false;
                                HelperClass.Cursor.SetActive(false);
                                HelperClass.itemName.text = "";
                            }

                            digSound.clip = place;
                            digSound.Play();
                            HelperClass.Chunks[chunkCoord] = tilemap;
                        }
                    }
                }
            }
        }
    }

    void BreakTile()
    {
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
                // Условие закомментировано по причине ненужности на данный момент
                //if (isblockDig == false)                                                                
                //{
                //    // верхнее условие
                //}
                // События с верхнего условия
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
                //GameObject.FindGameObjectWithTag("Player").GetComponent<Animator>().SetBool("Attack", true);
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
                Vector3 newpos = new Vector3(x + 0.5f, y + 0.5f);
                GameObject newBlock = Instantiate(BlockGameObject, newpos, Quaternion.identity);
                newBlock.name = blockId.ToString();
                Sprite sprite = null;
                // Получение его текстуры, если она есть
                if (tile is UnityEngine.Tilemaps.Tile)
                {
                    Debug.Log("Проходим по условию");
                    UnityEngine.Tilemaps.Tile tileScript = tile as UnityEngine.Tilemaps.Tile;
                    sprite = tileScript.sprite;
                    newBlock.GetComponent<SpriteRenderer>().sprite = tileScript.sprite;
                    //BlockGameObjectSprite.sprite = tileScript.sprite;
                }
                else if (tile is RuleTile)
                {
                    RuleTile ruleTile = tile as RuleTile;
                    sprite = ruleTile.m_DefaultSprite;
                    //BlockGameObjectSprite.sprite = sprite;
                    newBlock.GetComponent<SpriteRenderer>().sprite = sprite;
                }

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

                //// Добавляем блок в инвентарь
                //for (int i = 0; i < HelperClass.playerInventory.GetLength(0); i++)
                //{
                //    if (HelperClass.playerInventory[i] != null && HelperClass.playerInventory[i] == BlocksData.allBlocks.Find(x => x.blockIndex == blockId))
                //    {
                //        HelperClass.playerInventory[i].count++;
                //        Debug.Log($"В инвентаре {HelperClass.playerInventory[i].count} предмета {HelperClass.playerInventory[i].name}");

                //        digSound.clip = blockDestroy;
                //        digSound.Play();
                //        tilemap.SetTile(blockPosition, null);
                //        HelperClass.Chunks[chunkCoord] = tilemap;
                //        HelperClass.lightChunks[chunkCoord] = lightTilemap;
                //        Inventory.transform.Find(i.ToString()).transform.Find("Count").GetComponent<TextMeshProUGUI>().text = HelperClass.playerInventory[i].count.ToString();

                //        return;
                //    }
                //    else if (HelperClass.playerInventory[i] == null && blockId != 0)
                //    {
                //        HelperClass.playerInventory[i] = BlocksData.allBlocks.Find(x => x.blockIndex == blockId);
                //        HelperClass.playerInventory[i].count = 1;
                //        Debug.Log("В инвентарь был добавлен: " + HelperClass.playerInventory[i].name);
                //        Inventory.transform.Find(i.ToString()).transform.Find("Image").GetComponent<Image>().sprite = sprite;
                //        digSound.clip = blockDestroy;
                //        digSound.Play();
                //        tilemap.SetTile(blockPosition, null);
                //        HelperClass.Chunks[chunkCoord] = tilemap;
                //        HelperClass.lightChunks[chunkCoord] = lightTilemap;
                //        Inventory.transform.Find(i.ToString()).transform.Find("Count").GetComponent<TextMeshProUGUI>().text = HelperClass.playerInventory[i].count.ToString();

                //        return;
                //    }
                //    else
                //    {
                //        Debug.Log("Не прошло по условию");
                //    }
                //}
            }
            
        }
    }
}

