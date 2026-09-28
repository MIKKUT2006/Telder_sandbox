#if UNITY_EDITOR

using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class WeaponSystemRecoveryTool
{
    private const string EditorDir =
        "Assets/GameData/Editor";

    private const string OldEditor =
        EditorDir +
        "/HeldItemPoseEditorWindow.cs";

    private const string BackupV41 =
        EditorDir +
        "/HeldItemPoseEditorWindow.before_weapons_v41.bak";

    private const string BackupV39 =
        EditorDir +
        "/HeldItemPoseEditorWindow.before_weapons_v39.bak";

    private const string OldExtension =
        EditorDir +
        "/HeldItemPoseEditorWindow.Weapons.cs";

    private const string OldTemplate =
        EditorDir +
        "/HeldItemPoseEditorWindow.Weapons.template.txt";

    private const string OldInstaller =
        EditorDir +
        "/HeldItemPoseWeaponEditorInstaller.cs";

    private const string OldV3Studio =
        EditorDir +
        "/HeldItemWeaponStudioWindow.cs";


    [Serializable]
    private sealed class JsonSyntaxProbe
    {
        public string ID;
    }


    [MenuItem(
        "Tools/Game/Weapon System/1. Restore old editor + repair item JSON"
    )]
    public static void RecoverProject()
    {
        int restored =
            RestoreOriginalHeldItemEditor();

        int deleted =
            DeleteOldGeneratedEditorFiles(
                restored > 0
            );

        int repaired =
            RepairAllBrokenItemJsons();

        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Weapon System Recovery",
            "Готово.\n\n" +
            "Восстановлен старый Held Item editor: " +
            restored +
            "\nУдалено старых generated-файлов: " +
            deleted +
            "\nИсправлено повреждённых JSON: " +
            repaired +
            "\n\nПосле компиляции открой:\n" +
            "Tools → Game → Held Item Weapon Studio V4",
            "OK"
        );
    }


    [MenuItem(
        "Tools/Game/Weapon System/Repair broken item JSON only"
    )]
    public static void RepairOnly()
    {
        int repaired =
            RepairAllBrokenItemJsons();

        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Item JSON Repair",
            "Исправлено файлов: " +
            repaired +
            "\n\nПеред изменением каждого повреждённого JSON создаётся .broken.bak.",
            "OK"
        );
    }


    private static int RestoreOriginalHeldItemEditor()
    {
        string backup =
            File.Exists(
                BackupV41
            )
                ? BackupV41
                : (
                    File.Exists(
                        BackupV39
                    )
                        ? BackupV39
                        : null
                );

        if (string.IsNullOrWhiteSpace(
                backup))
        {
            return 0;
        }

        try
        {
            File.Copy(
                backup,
                OldEditor,
                true
            );

            return 1;
        }
        catch (Exception e)
        {
            Debug.LogError(
                "WEAPON RECOVERY: failed to restore HeldItemPoseEditorWindow.cs\n" +
                e
            );

            return 0;
        }
    }


    private static int DeleteOldGeneratedEditorFiles(
        bool oldPoseEditorWasRestored)
    {
        string[] files =
            oldPoseEditorWasRestored
                ? new[]
                {
                    OldExtension,
                    OldTemplate,
                    OldInstaller,
                    OldV3Studio
                }
                : new[]
                {
                    // Without the backup we must NOT remove V39 extension:
                    // the current HeldItemPoseEditorWindow.cs may still
                    // contain injected V39 method calls.
                    OldV3Studio
                };

        int deleted =
            0;

        for (int i = 0;
             i < files.Length;
             i++)
        {
            string path =
                files[i];

            if (!File.Exists(
                    path))
            {
                continue;
            }

            try
            {
                File.Delete(
                    path
                );

                string meta =
                    path +
                    ".meta";

                if (File.Exists(
                        meta))
                {
                    File.Delete(
                        meta
                    );
                }

                deleted++;
            }
            catch (Exception e)
            {
                Debug.LogWarning(
                    "WEAPON RECOVERY: could not delete " +
                    path +
                    "\n" +
                    e.Message
                );
            }
        }

        return deleted;
    }


    private static int RepairAllBrokenItemJsons()
    {
        string folder =
            Path.Combine(
                Application.dataPath,
                "GameData",
                "Items"
            );

        if (!Directory.Exists(
                folder))
        {
            Debug.LogWarning(
                "WEAPON RECOVERY: Items folder not found: " +
                folder
            );

            return 0;
        }

        string[] files =
            Directory.GetFiles(
                folder,
                "*.json",
                SearchOption.AllDirectories
            );

        int repaired =
            0;

        for (int i = 0;
             i < files.Length;
             i++)
        {
            string path =
                files[i];

            string json;

            try
            {
                json =
                    File.ReadAllText(
                        path
                    );
            }
            catch
            {
                continue;
            }

            if (IsValidJson(
                    json))
            {
                continue;
            }

            string fixedJson =
                RepairLegacyCommaDamage(
                    json
                );

            if (!IsValidJson(
                    fixedJson))
            {
                Debug.LogError(
                    "WEAPON RECOVERY: JSON is still invalid and was NOT overwritten:\n" +
                    path
                );

                continue;
            }

            try
            {
                string backup =
                    path +
                    ".broken.bak";

                if (!File.Exists(
                        backup))
                {
                    File.Copy(
                        path,
                        backup
                    );
                }

                File.WriteAllText(
                    path,
                    fixedJson,
                    new UTF8Encoding(
                        false
                    )
                );

                Debug.Log(
                    "WEAPON RECOVERY: repaired JSON: " +
                    path
                );

                repaired++;
            }
            catch (Exception e)
            {
                Debug.LogError(
                    "WEAPON RECOVERY: could not write repaired JSON:\n" +
                    path +
                    "\n" +
                    e
                );
            }
        }

        return repaired;
    }


    private static bool IsValidJson(
        string json)
    {
        if (string.IsNullOrWhiteSpace(
                json))
        {
            return false;
        }

        try
        {
            JsonUtility.FromJson<
                JsonSyntaxProbe
            >(
                json
            );

            return true;
        }
        catch
        {
            return false;
        }
    }


    private static string RepairLegacyCommaDamage(
        string json)
    {
        StringBuilder output =
            new StringBuilder(
                json.Length
            );

        bool inString =
            false;

        bool escaped =
            false;

        for (int i = 0;
             i < json.Length;
             i++)
        {
            char c =
                json[i];

            if (inString)
            {
                output.Append(
                    c
                );

                if (escaped)
                {
                    escaped =
                        false;
                }
                else if (c ==
                         '\\')
                {
                    escaped =
                        true;
                }
                else if (c ==
                         '"')
                {
                    inString =
                        false;
                }

                continue;
            }

            if (c ==
                '"')
            {
                inString =
                    true;

                output.Append(
                    c
                );

                continue;
            }

            if (c !=
                ',')
            {
                output.Append(
                    c
                );

                continue;
            }

            char previous =
                FindPreviousNonWhitespace(
                    output
                );

            char next =
                FindNextNonWhitespace(
                    json,
                    i +
                    1
                );

            bool invalidComma =
                previous ==
                    '\0' ||
                previous ==
                    '{' ||
                previous ==
                    '[' ||
                previous ==
                    ',' ||
                next ==
                    '}' ||
                next ==
                    ']' ||
                next ==
                    ',';

            if (!invalidComma)
            {
                output.Append(
                    c
                );
            }
        }

        return
            output.ToString();
    }


    private static char FindPreviousNonWhitespace(
        StringBuilder text)
    {
        for (int i =
                 text.Length - 1;
             i >= 0;
             i--)
        {
            char c =
                text[i];

            if (!char.IsWhiteSpace(
                    c))
            {
                return c;
            }
        }

        return '\0';
    }


    private static char FindNextNonWhitespace(
        string text,
        int start)
    {
        for (int i = start;
             i < text.Length;
             i++)
        {
            char c =
                text[i];

            if (!char.IsWhiteSpace(
                    c))
            {
                return c;
            }
        }

        return '\0';
    }
}

#endif
