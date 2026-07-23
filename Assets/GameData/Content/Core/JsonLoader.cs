using System.IO;
using UnityEngine;


namespace Game.Content
{

    public static class JsonLoader
    {

        public static T Load<T>(
            string path
        )
        {

            if (!File.Exists(path))
            {
                Debug.LogError(
                    "JSON file not found: " + path
                );

                return default;
            }



            string json =
                File.ReadAllText(path);



            T data =
                JsonUtility.FromJson<T>(json);



            return data;

        }





        public static T[] LoadArray<T>(
            string path
        )
        {

            if (!File.Exists(path))
            {
                Debug.LogError(
                    "JSON file not found: " + path
                );

                return null;
            }



            string json =
                File.ReadAllText(path);



            JsonArrayWrapper<T> wrapper =
                JsonUtility.FromJson<JsonArrayWrapper<T>>(json);



            return wrapper.items;

        }


    }



    [System.Serializable]
    public class JsonArrayWrapper<T>
    {

        public T[] items;

    }

}