using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Класс, хранящй крепкость всех блоков в игре
public class BlocksData : MonoBehaviour
{
    public static List<int> blocksSolidity = new List<int> {
         0, //солнечный свет
         3, //трава
         3, //земля
         15, //камень
         0, //пустота
         3, //трава с деревьями
         1, //трава
         1, //трава
    };
}
