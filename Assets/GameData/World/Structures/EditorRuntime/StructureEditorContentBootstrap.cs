
using System;
using System.Reflection;

using UnityEngine;


namespace Game.World.Structures.EditorRuntime
{

    [DefaultExecutionOrder(-10000)]
    public class StructureEditorContentBootstrap :
        MonoBehaviour
    {

        private void Awake()
        {

            TryInitializeContent();

        }


        private void TryInitializeContent()
        {

            string[] typeNames =
            {
                "Game.Content.ContentLoader",
                "Game.Content.ContentManager"
            };


            Assembly[] assemblies =
                AppDomain.CurrentDomain
                    .GetAssemblies();


            for (
                int t = 0;
                t < typeNames.Length;
                t++
            )
            {

                for (
                    int a = 0;
                    a < assemblies.Length;
                    a++
                )
                {

                    Type type =
                        assemblies[a]
                            .GetType(
                                typeNames[t],
                                false
                            );


                    if (
                        type ==
                        null
                    )
                    {

                        continue;

                    }


                    MethodInfo initialize =
                        type.GetMethod(
                            "Initialize",
                            BindingFlags.Public |
                            BindingFlags.Static
                        );


                    if (
                        initialize ==
                        null
                    )
                    {

                        continue;

                    }


                    try
                    {

                        initialize.Invoke(
                            null,
                            null
                        );


                        Debug.Log(
                            "STRUCTURE EDITOR: Content initialized through " +
                            typeNames[t]
                        );


                        return;

                    }
                    catch (
                        Exception exception
                    )
                    {

                        Debug.LogWarning(
                            "STRUCTURE EDITOR: Failed to initialize content through " +
                            typeNames[t] +
                            "\n" +
                            exception
                        );

                    }

                }

            }

        }

    }

}
