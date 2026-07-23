using Newtonsoft.Json;
using Game.Content;


namespace Game.Serialization
{

    public class ContentIDConverter : JsonConverter<ContentID>
    {

        public override ContentID ReadJson(
            JsonReader reader,
            System.Type objectType,
            ContentID existingValue,
            bool hasExistingValue,
            JsonSerializer serializer
        )
        {

            string value =
                reader.Value as string;


            if (string.IsNullOrEmpty(value))
            {
                return default(ContentID);
            }


            string[] parts =
                value.Split(':');


            return new ContentID(
                parts[0],
                parts[1]
            );

        }





        public override void WriteJson(
            JsonWriter writer,
            ContentID value,
            JsonSerializer serializer
        )
        {

            writer.WriteValue(
                value.Namespace +
                ":" +
                value.Name
            );

        }

    }

}