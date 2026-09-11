
using System;
using System.Collections.Generic;
using System.IO;

using UnityEngine;


namespace Game.Items.Visual
{

    [Serializable]
    public class HeldItemPoseMetadata
    {

        public string ID;


        public float HeldScale =
            1f;


        public float HeldRotation;


        public float HeldOffsetX;


        public float HeldOffsetY;

    }


    public static class HeldItemPoseRegistry
    {

        private static readonly Dictionary<
            string,
            HeldItemPoseMetadata
        > poses =
            new Dictionary<
                string,
                HeldItemPoseMetadata
            >(
                StringComparer.OrdinalIgnoreCase
            );


        private static bool loaded;


        public static void Reload()
        {

            loaded =
                true;


            poses.Clear();


            string folder =
                Path.Combine(
                    Application.dataPath,
                    "GameData",
                    "Items"
                );


            if (
                !Directory.Exists(
                    folder
                )
            )
            {

                return;

            }


            string[] files =
                Directory.GetFiles(
                    folder,
                    "*.json",
                    SearchOption.AllDirectories
                );


            for (
                int i = 0;
                i < files.Length;
                i++
            )
            {

                try
                {

                    HeldItemPoseMetadata pose =
                        JsonUtility.FromJson<
                            HeldItemPoseMetadata
                        >(
                            File.ReadAllText(
                                files[i]
                            )
                        );


                    if (
                        pose ==
                        null
                        ||
                        string.IsNullOrWhiteSpace(
                            pose.ID
                        )
                    )
                    {

                        continue;

                    }


                    if (
                        pose.HeldScale <=
                        0f
                    )
                    {

                        pose.HeldScale =
                            1f;

                    }


                    poses[
                        pose.ID
                    ] =
                        pose;

                }
                catch (
                    Exception exception
                )
                {

                    Debug.LogWarning(
                        "HELD ITEM POSE: failed to read " +
                        files[i] +
                        "\n" +
                        exception.Message
                    );

                }

            }

        }


        public static bool TryGet(
            string itemId,
            out HeldItemPoseMetadata pose
        )
        {

            EnsureLoaded();


            pose =
                null;


            if (
                string.IsNullOrWhiteSpace(
                    itemId
                )
            )
            {

                return false;

            }


            return
                poses.TryGetValue(
                    itemId,
                    out pose
                )
                &&
                pose !=
                null;

        }


        private static void EnsureLoaded()
        {

            if (
                loaded
            )
            {

                return;

            }


            Reload();

        }

    }

}
