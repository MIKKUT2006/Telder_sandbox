
using UnityEngine;

using Game.Content;


public class GameBootstrap :
    MonoBehaviour
{

    private static GameBootstrap instance;


    private static bool contentInitialized;

    private static bool contentInitializing;


    public static bool IsContentInitialized =>
        contentInitialized;


    private void Awake()
    {

        if (
            instance != null
            &&
            instance != this
        )
        {

            Destroy(
                gameObject
            );


            return;

        }


        instance =
            this;


        DontDestroyOnLoad(
            gameObject
        );


        EnsureContentInitialized();

    }


    public static void EnsureContentInitialized()
    {

        if (
            contentInitialized
            ||
            contentInitializing
        )
        {

            return;

        }


        contentInitializing =
            true;


        try
        {

            ContentManager.Initialize();


            contentInitialized =
                true;


            Debug.Log(
                "CONTENT BOOTSTRAP: initialized exactly once."
            );

        }
        catch
        {

            // Keep the state retryable after a genuine loading error.
            contentInitialized =
                false;


            throw;

        }
        finally
        {

            contentInitializing =
                false;

        }

    }

}
