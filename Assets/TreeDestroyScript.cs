using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor.Localization.Plugins.XLIFF.V12;
using UnityEngine;
using UnityEngine.Audio;
using static Unity.Collections.AllocatorManager;

public class TreeDestroyScript : MonoBehaviour
{
    public float maxHealth = 5f;
    public float damagePerSecond = 1f;
    private float currentHealth;
    private bool isChopping = false;
    public int typeId;
    public AllItemsAndBlocks treeType;
    private List<AudioClip> chopSounds = new List<AudioClip>();
    private AudioSource audioSource;

    // Падение срубленного дерева
    public float fallForce = 5f;
    public float rotationForce = 2f;

    private Rigidbody2D rb;
    GameObject leavesParticles;

    private void Start()
    {
        treeType = BlocksData.allBlocks[typeId];
        currentHealth = maxHealth;
        audioSource = GetComponent<AudioSource>();
        LoadChopSounds();

        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0; //отключаем гравитацию
        }
        leavesParticles = Resources.Load<GameObject>("Prefabs/Creak Leaves");
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
        if (isChopping && currentHealth > 0)
        {
            if (HelperClass.eguipmentItem != null)
            {
                if (HelperClass.eguipmentItem.toolType == treeType.needsToolType)
                {
                    currentHealth -= HelperClass.eguipmentItem.toolPower * Time.deltaTime;
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
            if (chopSounds.Count > 0 && audioSource.isPlaying == false)
            {
                int randomIndex = Random.Range(0, chopSounds.Count);
                audioSource.clip = chopSounds[randomIndex];
                audioSource.Play();
            }

            if (currentHealth <= 0)
            {
                AudioClip woodCreak = Resources.Load<AudioClip>("Sounds/Block/WoodCreak");
                audioSource.clip = woodCreak;
                audioSource.Play();
                Animator animator = GetComponent<Animator>();
                //animator.Play("Creak");
                FallDown();
                StartCoroutine(WaitSoundEnd());
            }
        }

    }
    public void FallDown()
    {
        
        Vector3 spawnPosition = transform.position;
        Instantiate(leavesParticles, spawnPosition, Quaternion.identity);
        if (rb == null) return;

        rb.isKinematic = false; //Разрешаем физике управлять объектом
                                //Применяем силу по оси X для падения
        rb.AddForce(Vector2.left * fallForce, ForceMode2D.Impulse); // Или Vector2.right в зависимости от желаемого направления падения.
        //Применяем момент для вращения. Random.value чтобы добавить немного разнообразия
        rb.AddTorque(rotationForce * Random.value, ForceMode2D.Impulse);
        rb.angularDamping = 1; //угловое сопротивление, чтобы дерево замедлялось при вращении
        rb.gravityScale = 1; //Включаем гравитацию.

        CapsuleCollider2D capsuleCollider2D = GetComponent<CapsuleCollider2D>();
        capsuleCollider2D.isTrigger = false;
    }
    private IEnumerator WaitSoundEnd()
    {
        if (audioSource.isPlaying == true)
        {
            yield return new WaitForSeconds(1f);
            StartCoroutine(WaitSoundEnd());
        }
        else{
            ChopDown();
        }
    }

    private void LoadChopSounds()
    {
        string soundsPath = "Sounds/Block/Wood"; // Путь к папке с ресурсами относительно папки "Resources". Если папка вне ресурсов, используйте Addressables.

        // Используем Resources.LoadAll для загрузки всех клипов в папке
        AudioClip[] clips = Resources.LoadAll<AudioClip>(soundsPath);
        chopSounds.AddRange(clips);

        if (chopSounds.Count == 0)
        {
            Debug.LogWarning($"No chop sounds found in: {soundsPath} ");
        }
    }

    void ChopDown()
    {
        // Цикл для создания всех выпадающих предметов с блока
        foreach (int drop in treeType.dropId)
        {
            Vector3 newpos = new Vector3(transform.position.x + 0.5f, transform.position.y + 0.5f);
            AllItemsAndBlocks currentDrop = BlocksData.allBlocks.Where(x => x.blockIndex == drop).FirstOrDefault();
            Debug.Log(currentDrop.name);
            GameObject newBlock = Instantiate(HelperClass.BlockGameObject, newpos, Quaternion.identity);
            newBlock.name = currentDrop.blockIndex.ToString();
            Sprite sprite = null;
            // Получение его текстуры, если она есть
            Sprite newSprite = (Sprite)Resources.Load(currentDrop.imagePath, typeof(Sprite));

            newBlock.GetComponent<SpriteRenderer>().sprite = newSprite;
        }
        // Конец цикла
        Destroy(gameObject);
        Debug.Log("Дерево срублено!");
    }
}
