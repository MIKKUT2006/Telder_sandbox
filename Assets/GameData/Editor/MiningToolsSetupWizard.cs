
#if UNITY_EDITOR

using System;
using System.IO;
using System.Text.RegularExpressions;

using UnityEditor;
using UnityEngine;


namespace Game.EditorTools
{

    public static class MiningToolsSetupWizard
    {

        [MenuItem(
            "Tools/Game/Setup Pickaxe Mining"
        )]
        public static void Setup()
        {

            string itemFolder =
                Path.Combine(
                    Application.dataPath,
                    "GameData",
                    "Items"
                );


            string blockFolder =
                Path.Combine(
                    Application.dataPath,
                    "GameData",
                    "Blocks"
                );


            Directory.CreateDirectory(
                itemFolder
            );


            CreateItemIfMissing(
                itemFolder,
                "wood_pickaxe.json",
                WoodPickaxeJson()
            );


            CreateItemIfMissing(
                itemFolder,
                "iron_pickaxe.json",
                IronPickaxeJson()
            );


            CreateItemIfMissing(
                itemFolder,
                "teleportium_pickaxe.json",
                TeleportiumPickaxeJson()
            );


            int patched =
                PatchBlockMiningRules(
                    blockFolder
                );


            AssetDatabase.Refresh();


            EditorUtility.DisplayDialog(
                "Pickaxe Mining",
                "Готово.\n\n" +
                "Созданы кирки уровней 1/2/3 (если файлов ещё не было).\n" +
                "Stone и ore JSON обновлено: " +
                patched +
                "\n\n" +
                "По умолчанию:\n" +
                "stone / coal_ore = 1\n" +
                "iron_ore = 2\n" +
                "teleportium_ore = 3\n" +
                "остальные *_ore без уровня = 1\n\n" +
                "Добавь textures: wood_pickaxe, iron_pickaxe, teleportium_pickaxe.",
                "OK"
            );

        }


        private static void CreateItemIfMissing(
            string folder,
            string fileName,
            string json
        )
        {

            string path =
                Path.Combine(
                    folder,
                    fileName
                );


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
                json
            );

        }


        private static int PatchBlockMiningRules(
            string folder
        )
        {

            if (
                !Directory.Exists(
                    folder
                )
            )
            {

                return 0;

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

                string path =
                    files[i];


                string text =
                    File.ReadAllText(
                        path
                    );


                string id =
                    ReadStringField(
                        text,
                        "ID"
                    );


                if (
                    string.IsNullOrWhiteSpace(
                        id
                    )
                )
                {

                    continue;

                }


                bool stone =
                    string.Equals(
                        id,
                        "game:stone",
                        StringComparison.OrdinalIgnoreCase
                    );


                bool ore =
                    id.EndsWith(
                        "_ore",
                        StringComparison.OrdinalIgnoreCase
                    )
                    ||
                    text.IndexOf(
                        "\"ore\"",
                        StringComparison.OrdinalIgnoreCase
                    )
                    >=
                    0;


                if (
                    !stone
                    &&
                    !ore
                )
                {

                    continue;

                }


                int level =
                    GetDefaultLevel(
                        id
                    );


                string updated =
                    SetStringField(
                        text,
                        "RequiredTool",
                        "pickaxe"
                    );


                if (
                    !HasField(
                        updated,
                        "RequiredToolLevel"
                    )
                )
                {

                    updated =
                        InsertBeforeLastBrace(
                            updated,
                            "\"RequiredToolLevel\": " +
                            level
                        );

                }


                if (
                    updated !=
                    text
                )
                {

                    File.WriteAllText(
                        path,
                        updated
                    );


                    changed++;

                }

            }


            return changed;

        }


        private static int GetDefaultLevel(
            string id
        )
        {

            if (
                id.IndexOf(
                    "teleportium",
                    StringComparison.OrdinalIgnoreCase
                )
                >=
                0
            )
            {

                return 3;

            }


            if (
                id.IndexOf(
                    "iron",
                    StringComparison.OrdinalIgnoreCase
                )
                >=
                0
            )
            {

                return 2;

            }


            return 1;

        }


        private static string ReadStringField(
            string json,
            string field
        )
        {

            Match match =
                Regex.Match(
                    json,
                    "\"" +
                    Regex.Escape(
                        field
                    ) +
                    "\"\\s*:\\s*\"([^\"]*)\""
                );


            return
                match.Success
                    ? match.Groups[
                        1
                    ].Value
                    : null;

        }


        private static bool HasField(
            string json,
            string field
        )
        {

            return
                Regex.IsMatch(
                    json,
                    "\"" +
                    Regex.Escape(
                        field
                    ) +
                    "\"\\s*:"
                );

        }


        private static string SetStringField(
            string json,
            string field,
            string value
        )
        {

            string pattern =
                "\"" +
                Regex.Escape(
                    field
                ) +
                "\"\\s*:\\s*\"[^\"]*\"";


            string replacement =
                "\"" +
                field +
                "\": \"" +
                value +
                "\"";


            if (
                Regex.IsMatch(
                    json,
                    pattern
                )
            )
            {

                Regex regex =
                    new Regex(
                        pattern
                    );


                return
                    regex.Replace(
                        json,
                        replacement,
                        1
                    );

            }


            return
                InsertBeforeLastBrace(
                    json,
                    replacement
                );

        }


        private static string InsertBeforeLastBrace(
            string json,
            string fieldLine
        )
        {

            int index =
                json.LastIndexOf(
                    '}'
                );


            if (
                index <
                0
            )
            {

                return json;

            }


            string before =
                json.Substring(
                    0,
                    index
                ).TrimEnd();


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

                before +=
                    ",";

            }


            return
                before +
                "\n  " +
                fieldLine +
                "\n}";

        }


        private static string WoodPickaxeJson()
        {

            return
@"{
  ""ID"": ""game:wood_pickaxe"",
  ""Name"": ""Wood Pickaxe"",
  ""Texture"": ""wood_pickaxe"",
  ""MaxStack"": 1,

  ""ToolType"": ""pickaxe"",
  ""ToolLevel"": 1,
  ""MiningSpeed"": 1.0,

  ""HeldScale"": 1.0,
  ""HeldRotation"": 0.0,
  ""HeldOffsetX"": 0.0,
  ""HeldOffsetY"": 0.0
}";

        }


        private static string IronPickaxeJson()
        {

            return
@"{
  ""ID"": ""game:iron_pickaxe"",
  ""Name"": ""Iron Pickaxe"",
  ""Texture"": ""iron_pickaxe"",
  ""MaxStack"": 1,

  ""ToolType"": ""pickaxe"",
  ""ToolLevel"": 2,
  ""MiningSpeed"": 1.35,

  ""HeldScale"": 1.0,
  ""HeldRotation"": 0.0,
  ""HeldOffsetX"": 0.0,
  ""HeldOffsetY"": 0.0
}";

        }


        private static string TeleportiumPickaxeJson()
        {

            return
@"{
  ""ID"": ""game:teleportium_pickaxe"",
  ""Name"": ""Teleportium Pickaxe"",
  ""Texture"": ""teleportium_pickaxe"",
  ""MaxStack"": 1,

  ""ToolType"": ""pickaxe"",
  ""ToolLevel"": 3,
  ""MiningSpeed"": 1.75,

  ""HeldScale"": 1.0,
  ""HeldRotation"": 0.0,
  ""HeldOffsetX"": 0.0,
  ""HeldOffsetY"": 0.0
}";

        }

    }

}

#endif
