
#if UNITY_EDITOR

using System;
using System.IO;
using System.Text;

using UnityEditor;
using UnityEngine;


namespace Game.EditorTools
{

    public static class GameplayContentV21Wizard
    {

        [MenuItem(
            "Tools/Game/Create Torch + Workbench Content"
        )]
        public static void CreateContent()
        {

            string root =
                Path.Combine(
                    Application.dataPath,
                    "GameData"
                );


            string blockFolder =
                Path.Combine(
                    root,
                    "Blocks"
                );


            string itemFolder =
                Path.Combine(
                    root,
                    "Items"
                );


            Directory.CreateDirectory(
                blockFolder
            );


            Directory.CreateDirectory(
                itemFolder
            );


            WriteIfMissing(
                Path.Combine(
                    blockFolder,
                    "torch.json"
                ),
                TorchBlockJson()
            );


            WriteIfMissing(
                Path.Combine(
                    itemFolder,
                    "torch.json"
                ),
                TorchItemJson()
            );


            WriteIfMissing(
                Path.Combine(
                    blockFolder,
                    "workbench.json"
                ),
                WorkbenchBlockJson()
            );


            WriteIfMissing(
                Path.Combine(
                    itemFolder,
                    "workbench.json"
                ),
                WorkbenchItemJson()
            );


            AssetDatabase.Refresh();


            EditorUtility.DisplayDialog(
                "Gameplay Content V21",
                "Созданы JSON для game:torch и game:workbench, если файлов ещё не было.\n\nВАЖНО: положи свои текстуры с ключами `torch` и `workbench` туда же, откуда твой BlockTexture loader уже берёт текстуры блоков.\n\nТестовые рецепты используют game:stone, чтобы система сразу работала. Потом замени ингредиенты на дерево/уголь.",
                "OK"
            );

        }


        [MenuItem(
            "Tools/Game/Add Crafting Fields To Block JSONs"
        )]
        public static void AddCraftingFields()
        {

            string folder =
                Path.Combine(
                    Application.dataPath,
                    "GameData",
                    "Blocks"
                );


            if (
                !Directory.Exists(
                    folder
                )
            )
            {

                EditorUtility.DisplayDialog(
                    "Craft JSON Migration",
                    "Assets/GameData/Blocks не найден.",
                    "OK"
                );


                return;

            }


            string[] files =
                Directory.GetFiles(
                    folder,
                    "*.json",
                    SearchOption.AllDirectories
                );


            int changed =
                0;


            for (
                int i = 0;
                i < files.Length;
                i++
            )
            {

                string text =
                    File.ReadAllText(
                        files[i]
                    );


                bool hasIngredients =
                    text.IndexOf(
                        "\"CraftIngredients\"",
                        StringComparison.Ordinal
                    )
                    >=
                    0;


                bool hasWithoutWorkbench =
                    text.IndexOf(
                        "\"CraftWithoutWorkbench\"",
                        StringComparison.Ordinal
                    )
                    >=
                    0;


                bool hasResultCount =
                    text.IndexOf(
                        "\"CraftResultCount\"",
                        StringComparison.Ordinal
                    )
                    >=
                    0;


                if (
                    hasIngredients
                    &&
                    hasWithoutWorkbench
                    &&
                    hasResultCount
                )
                {

                    continue;

                }


                int lastBrace =
                    text.LastIndexOf(
                        '}'
                    );


                if (
                    lastBrace <
                    0
                )
                {

                    continue;

                }


                string before =
                    text.Substring(
                        0,
                        lastBrace
                    )
                    .TrimEnd();


                string after =
                    text.Substring(
                        lastBrace
                    );


                StringBuilder addition =
                    new StringBuilder();


                if (
                    before.Length >
                    0
                    &&
                    before[
                        before.Length -
                        1
                    ]
                    !=
                    ','
                )
                {

                    addition.Append(
                        ","
                    );

                }


                addition.AppendLine();


                bool first =
                    true;


                if (
                    !hasIngredients
                )
                {

                    addition.Append(
                        "  \"CraftIngredients\": []"
                    );


                    first =
                        false;

                }


                if (
                    !hasWithoutWorkbench
                )
                {

                    if (
                        !first
                    )
                    {

                        addition.AppendLine(
                            ","
                        );

                    }


                    addition.Append(
                        "  \"CraftWithoutWorkbench\": false"
                    );


                    first =
                        false;

                }


                if (
                    !hasResultCount
                )
                {

                    if (
                        !first
                    )
                    {

                        addition.AppendLine(
                            ","
                        );

                    }


                    addition.Append(
                        "  \"CraftResultCount\": 1"
                    );

                }


                addition.AppendLine();


                File.WriteAllText(
                    files[i],
                    before +
                    addition +
                    after
                );


                changed++;

            }


            AssetDatabase.Refresh();


            EditorUtility.DisplayDialog(
                "Craft JSON Migration",
                "Обновлено block JSON: " +
                changed +
                "\n\nСуществующие поля и Drop/Tags не перезаписывались — wizard только дописал отсутствующие CraftIngredients, CraftWithoutWorkbench и CraftResultCount.",
                "OK"
            );

        }


        private static void WriteIfMissing(
            string path,
            string content
        )
        {

            if (
                File.Exists(
                    path
                )
            )
            {

                return;

            }


            File.WriteAllText(
                path,
                content
            );

        }


        private static string TorchBlockJson()
        {

            return
@"{
  ""ID"": ""game:torch"",
  ""Name"": ""Torch"",
  ""Texture"": ""torch"",

  ""Hardness"": 0.25,
  ""ExplosionResistance"": 0.25,

  ""Solid"": false,
  ""Transparent"": true,
  ""BlocksLight"": false,

  ""Material"": 0,

  ""Drop"": ""game:torch"",
  ""DropCount"": 1,

  ""PlaceSound"": """",
  ""BreakSound"": """",
  ""RequiredTool"": """",

  ""Tags"": [
    ""torch"",
    ""light""
  ],

  ""closed"": false,

  ""LightOpacity"": 0,
  ""LightEmissionR"": 15,
  ""LightEmissionG"": 11,
  ""LightEmissionB"": 6,

  ""CraftIngredients"": [
    {
      ""ItemId"": ""game:stone"",
      ""Count"": 1
    }
  ],
  ""CraftWithoutWorkbench"": true,
  ""CraftResultCount"": 4
}";
        }


        private static string TorchItemJson()
        {

            return
@"{
  ""ID"": ""game:torch"",
  ""Name"": ""Torch"",
  ""Texture"": ""torch"",
  ""Type"": ""Block"",
  ""MaxStack"": 100
}";
        }


        private static string WorkbenchBlockJson()
        {

            return
@"{
  ""ID"": ""game:workbench"",
  ""Name"": ""Workbench"",
  ""Texture"": ""workbench"",

  ""Hardness"": 1.0,
  ""ExplosionResistance"": 1.0,

  ""Solid"": true,
  ""Transparent"": false,
  ""BlocksLight"": true,

  ""Material"": 0,

  ""Drop"": ""game:workbench"",
  ""DropCount"": 1,

  ""PlaceSound"": """",
  ""BreakSound"": """",
  ""RequiredTool"": """",

  ""Tags"": [
    ""workbench"",
    ""crafting_station""
  ],

  ""closed"": false,

  ""LightOpacity"": 15,
  ""LightEmissionR"": 0,
  ""LightEmissionG"": 0,
  ""LightEmissionB"": 0,

  ""CraftIngredients"": [
    {
      ""ItemId"": ""game:stone"",
      ""Count"": 8
    }
  ],
  ""CraftWithoutWorkbench"": true,
  ""CraftResultCount"": 1
}";
        }


        private static string WorkbenchItemJson()
        {

            return
@"{
  ""ID"": ""game:workbench"",
  ""Name"": ""Workbench"",
  ""Texture"": ""workbench"",
  ""Type"": ""Block"",
  ""MaxStack"": 100
}";
        }

    }

}

#endif
