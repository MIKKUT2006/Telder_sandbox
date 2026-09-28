using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Game.Combat
{
    public static class ExplicitWeaponAttackGuard
    {
        public static bool AllowsAttack(
            string itemId,
            WeaponKind kind
        )
        {
            if (kind == WeaponKind.None)
                return false;

            if (string.IsNullOrWhiteSpace(itemId))
                return false;

            string itemsFolder =
                Path.Combine(
                    Application.dataPath,
                    "GameData",
                    "Items"
                );

            if (!Directory.Exists(itemsFolder))
                return false;

            string json =
                TryReadItemJson(
                    itemsFolder,
                    itemId
                );

            if (string.IsNullOrWhiteSpace(json))
                return false;

            string kindName =
                kind.ToString();

            // A weapon attack must be explicitly declared in item JSON.
            // Supported keys cover current/older Telder metadata variants.
            string pattern =
                "\"(Kind|WeaponKind|WeaponType|AttackType|Type)\"\\s*:\\s*\"" +
                Regex.Escape(kindName) +
                "\"";

            return Regex.IsMatch(
                json,
                pattern,
                RegexOptions.IgnoreCase
            );
        }

        private static string TryReadItemJson(
            string folder,
            string itemId
        )
        {
            string shortId =
                itemId;

            int separator =
                shortId.LastIndexOf(':');

            if (separator >= 0 &&
                separator < shortId.Length - 1)
            {
                shortId =
                    shortId.Substring(
                        separator + 1
                    );
            }

            string direct =
                Path.Combine(
                    folder,
                    shortId + ".json"
                );

            if (File.Exists(direct))
            {
                try
                {
                    return File.ReadAllText(direct);
                }
                catch
                {
                }
            }

            string escapedId =
                Regex.Escape(itemId);

            string idPattern =
                "\"ID\"\\s*:\\s*\"" +
                escapedId +
                "\"";

            foreach (
                string file
                in Directory.GetFiles(
                    folder,
                    "*.json",
                    SearchOption.AllDirectories
                )
            )
            {
                try
                {
                    string json =
                        File.ReadAllText(file);

                    if (
                        Regex.IsMatch(
                            json,
                            idPattern,
                            RegexOptions.IgnoreCase
                        )
                    )
                    {
                        return json;
                    }
                }
                catch
                {
                }
            }

            return null;
        }
    }
}
