
#if UNITY_EDITOR

using System;
using System.IO;

using UnityEditor;


namespace Game.EditorTools
{

    public static class BlockCollisionShapeMigrationWizard
    {

        [MenuItem(
            "Tools/Game/Add CollisionShape To Block JSONs"
        )]
        public static void AddCollisionShape()
        {

            string folder =
                Path.Combine(
                    UnityEngine.Application.dataPath,
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
                    "Block Collision Shapes",
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


                if (
                    text.IndexOf(
                        "\"CollisionShape\"",
                        StringComparison.Ordinal
                    )
                    >=
                    0
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
                    text
                        .Substring(
                            0,
                            lastBrace
                        )
                        .TrimEnd();


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


                text =
                    before +
                    "\n" +
                    "  \"CollisionShape\": \"Full\",\n" +
                    "  \"CollisionRects\": []\n" +
                    "}";


                File.WriteAllText(
                    files[i],
                    text
                );


                changed++;

            }


            AssetDatabase.Refresh();


            EditorUtility.DisplayDialog(
                "Block Collision Shapes",
                "Готово.\n\nОбновлено JSON: " +
                changed +
                "\n\nСтарые блоки получили CollisionShape = Full.",
                "OK"
            );

        }

    }

}

#endif
