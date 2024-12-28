using System.Collections;
using System.IO;
using System.Linq;
using UnityEditor.Localization.Plugins.XLIFF.V12;
using UnityEngine;

public class TreeDestroyScript : MonoBehaviour
{
    public float maxHealth = 5f;
    public float damagePerSecond = 1f;
    private float currentHealth;
    private bool isChopping = false;
    public int typeId;
    public AllItemsAndBlocks treeType;

    private void Start()
    {
        treeType = BlocksData.allBlocks[typeId];
        currentHealth = maxHealth;
    }

    public void StartChopping()
    {
        isChopping = true;
    }

    public void StopChopping()
    {
        isChopping = false;
    }

    void Update()
    {
        if (isChopping)
        {
            if (HelperClass.eguipmentItem != null)
            {
                if (HelperClass.eguipmentItem.toolType == treeType.needsToolType)
                {
                    currentHealth -= HelperClass.eguipmentItem.axePower * Time.deltaTime;
                }
                else
                {
                    currentHealth -= 1 * Time.deltaTime;
                }
            }
            else
            {
                currentHealth -= 1 * Time.deltaTime;
            }
            

            if (currentHealth <= 0)
            {
                ChopDown();
            }
        }
    }

    void ChopDown()
    {
        // Цикл для создания всех выпадающих предметов с блока
        foreach (int drop in treeType.dropId)
        {
            //Debug.Log(drop);
            Vector3 newpos = new Vector3(transform.position.x + 0.5f, transform.position.y + 0.5f);
            AllItemsAndBlocks currentDrop = BlocksData.allBlocks.Where(x => x.blockIndex == drop).FirstOrDefault();
            Debug.Log(currentDrop.name);
            GameObject newBlock = Instantiate(HelperClass.BlockGameObject, newpos, Quaternion.identity);
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

            //ParticleSystem newParticles = Instantiate(destroyParticles, newpos, Quaternion.identity);
            //newParticles.gameObject.SetActive(true);
            //Material destroyMaterial = newParticles.GetComponent<ParticleSystemRenderer>().material;
            //destroyMaterial.mainTexture = texture;
        }
        // Конец цикла
        Destroy(gameObject);
        Debug.Log("Дерево срублено!");
    }
}
