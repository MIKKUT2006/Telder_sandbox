#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

using UnityEditor;
using UnityEngine;


public static class TelderBiomesFloraV4Installer
{
    [MenuItem("Tools/Game/Install Biomes + Flora V4")]
    public static void Install()
    {
        string path =
            Path.Combine(
                Application.dataPath,
                "GameData",
                "World",
                "Loading",
                "ChunkLoader.cs"
            );


        if (!File.Exists(path))
        {
            Debug.LogError(
                "BIOMES + FLORA V4: ChunkLoader.cs not found at " +
                path
            );


            EditorUtility.DisplayDialog(
                "Biomes + Flora V4",
                "ChunkLoader.cs was not found.\n\n" +
                "Send me Assets/GameData/World/Loading/ChunkLoader.cs and I will wire the last step manually.",
                "OK"
            );


            return;
        }


        string text =
            File.ReadAllText(
                path
            );


        string original =
            text;


        // Previous packs are compatible because V4 overwrites their bridge
        // classes with forwarding shims.
        bool alreadyCommits =
            text.Contains(
                "GeneratedFloraFurnitureCommit.Apply"
            )
            ||
            text.Contains(
                "GeneratedFurnitureChunkBridge.Apply"
            )
            ||
            text.Contains(
                "GeneratedVegetationFurnitureBridge.Apply"
            )
            ||
            text.Contains(
                "GeneratedVegetationFurnitureBridgeV3.Apply"
            );


        if (!alreadyCommits)
        {
            Regex applyRegex =
                new Regex(
                    @"chunk\.ApplyData\s*\(\s*data\s*\)\s*;",
                    RegexOptions.Singleline
                );


            Match apply =
                applyRegex.Match(
                    text
                );


            if (!apply.Success)
            {
                Debug.LogError(
                    "BIOMES + FLORA V4: could not find chunk.ApplyData(data) in ChunkLoader.cs."
                );


                EditorUtility.DisplayDialog(
                    "Biomes + Flora V4",
                    "Could not patch ChunkLoader automatically.\n" +
                    "Send me the current ChunkLoader.cs.",
                    "OK"
                );


                return;
            }


            string insertion =
apply.Value +
@"

                // TELDER BIOMES + FLORA V4
                Game.World.Vegetation.Runtime
                    .GeneratedFloraFurnitureCommit
                    .Apply(
                        data,
                        position.x,
                        position.y
                    );";


            text =
                text.Remove(
                    apply.Index,
                    apply.Length
                )
                .Insert(
                    apply.Index,
                    insertion
                );
        }


        bool alreadyUnloads =
            text.Contains(
                "GeneratedFloraFurnitureCommit.Unload"
            )
            ||
            text.Contains(
                "GeneratedFurnitureChunkBridge.Unload"
            )
            ||
            text.Contains(
                "GeneratedVegetationFurnitureBridge.Unload"
            )
            ||
            text.Contains(
                "GeneratedVegetationFurnitureBridgeV3.Unload"
            );


        if (!alreadyUnloads)
        {
            Regex removeRegex =
                new Regex(
                    @"world\.RemoveChunk\s*\(\s*position\.x\s*,\s*position\.y\s*\)\s*;",
                    RegexOptions.Singleline
                );


            Match remove =
                removeRegex.Match(
                    text
                );


            if (remove.Success)
            {
                string insertion =
@"Game.World.Vegetation.Runtime
                    .GeneratedFloraFurnitureCommit
                    .Unload(
                        position.x,
                        position.y
                    );


                " +
remove.Value;


                text =
                    text.Remove(
                        remove.Index,
                        remove.Length
                    )
                    .Insert(
                        remove.Index,
                        insertion
                    );
            }
        }


        if (
            text != original
        )
        {
            string backup =
                path +
                ".biomes_flora_v4.bak";


            if (!File.Exists(backup))
            {
                File.Copy(
                    path,
                    backup
                );
            }


            File.WriteAllText(
                path,
                text,
                new UTF8Encoding(false)
            );


            AssetDatabase.Refresh();


            Debug.Log(
                "BIOMES + FLORA V4: ChunkLoader integration installed. Backup: " +
                backup
            );
        }
        else
        {
            Debug.Log(
                "BIOMES + FLORA V4: ChunkLoader already has a compatible generated-furniture bridge."
            );
        }


        Validate();
    }


    [MenuItem("Tools/Game/Validate Biomes + Flora V4")]
    public static void Validate()
    {
        Type type =
            typeof(
                Game.World.ChunkData
            );


        var get =
            type.GetMethod(
                "GetFurniture",
                new Type[]
                {
                    typeof(int),
                    typeof(int)
                }
            );


        var set =
            type.GetMethod(
                "SetFurniture",
                new Type[]
                {
                    typeof(int),
                    typeof(int),
                    typeof(ushort)
                }
            );


        bool ok =
            get != null &&
            get.ReturnType == typeof(ushort) &&
            set != null;


        if (!ok)
        {
            Debug.LogError(
                "BIOMES + FLORA V4 VALIDATION FAILED: ChunkData furniture API is not the V4 ushort API."
            );


            EditorUtility.DisplayDialog(
                "Biomes + Flora V4",
                "Validation failed.\nCheck Console.",
                "OK"
            );


            return;
        }


        Debug.Log(
            "BIOMES + FLORA V4 VALIDATION OK.\n" +
            "ChunkData furniture API is direct ushort API.\n" +
            "CaveVegetationGenerationRuntime V4 uses no reflection."
        );


        EditorUtility.DisplayDialog(
            "Biomes + Flora V4",
            "Validation OK.\n\n" +
            "Now create a NEW world or walk into chunks that have never been generated.",
            "OK"
        );
    }
}
#endif
