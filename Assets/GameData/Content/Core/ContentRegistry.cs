using System;
using System.Collections.Generic;


namespace Game.Content
{

    public class ContentRegistry<T>
        where T : class
    {


        private readonly Dictionary<ContentID, T> contents;


        private readonly Dictionary<T, ContentID> reverseLookup;



        public ContentRegistry()
        {

            contents = new Dictionary<ContentID, T>();

            reverseLookup = new Dictionary<T, ContentID>();

        }



        /// <summary>
        /// Добавить новый объект в реестр
        /// </summary>
        public void Register(
            ContentID id,
            T content
        )
        {


            if (contents.ContainsKey(id))
            {
                throw new Exception(
                    $"Content with ID {id} already exists!"
                );
            }



            contents.Add(
                id,
                content
            );


            reverseLookup.Add(
                content,
                id
            );

        }




        /// <summary>
        /// Получить объект по ID
        /// </summary>
        public T Get(
            ContentID id
        )
        {

            if (contents.TryGetValue(
                id,
                out T result
            ))
            {
                return result;
            }


            throw new Exception(
                $"Unknown content ID: {id}"
            );

        }




        /// <summary>
        /// Проверка существования
        /// </summary>
        public bool Contains(
            ContentID id
        )
        {

            return contents.ContainsKey(id);

        }




        /// <summary>
        /// Получить ID объекта
        /// </summary>
        public ContentID GetID(
            T content
        )
        {


            if (reverseLookup.TryGetValue(
                content,
                out ContentID id
            ))
            {
                return id;
            }



            throw new Exception(
                "Content is not registered!"
            );

        }





        /// <summary>
        /// Получить все зарегистрированные объекты
        /// </summary>
        public IEnumerable<T> GetAll()
        {

            return contents.Values;

        }




        /// <summary>
        /// Количество элементов
        /// </summary>
        public int Count
        {

            get
            {
                return contents.Count;
            }

        }



        /// <summary>
        /// Очистка реестра
        /// Используется при перезагрузке модов
        /// </summary>
        public void Clear()
        {

            contents.Clear();

            reverseLookup.Clear();

        }


    }

}
