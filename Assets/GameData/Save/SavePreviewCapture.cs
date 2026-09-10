using System.IO;
using UnityEngine;

namespace Game.Save
{
    public static class SavePreviewCapture
    {
        public static bool CaptureCurrentScreen(
            string saveId,
            int width = 640,
            int height = 360
        )
        {
            if (
                string.IsNullOrWhiteSpace(
                    saveId
                )
            )
            {
                return false;
            }


            Texture2D source =
                ScreenCapture
                    .CaptureScreenshotAsTexture();


            if (
                source == null
            )
            {
                return false;
            }


            RenderTexture temporary =
                RenderTexture.GetTemporary(
                    width,
                    height,
                    0,
                    RenderTextureFormat.ARGB32
                );


            RenderTexture previous =
                RenderTexture.active;


            Graphics.Blit(
                source,
                temporary
            );


            RenderTexture.active =
                temporary;


            Texture2D scaled =
                new Texture2D(
                    width,
                    height,
                    TextureFormat.RGB24,
                    false
                );


            scaled.ReadPixels(
                new Rect(
                    0,
                    0,
                    width,
                    height
                ),
                0,
                0
            );


            scaled.Apply();


            byte[] png =
                scaled.EncodeToPNG();


            string path =
                SavePaths.GetPreviewFile(
                    saveId
                );


            Directory.CreateDirectory(
                Path.GetDirectoryName(
                    path
                )
            );


            File.WriteAllBytes(
                path,
                png
            );


            RenderTexture.active =
                previous;


            RenderTexture.ReleaseTemporary(
                temporary
            );


            Object.Destroy(
                source
            );


            Object.Destroy(
                scaled
            );


            return true;
        }
    }
}
