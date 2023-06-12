using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Scripting.Python;
using TMPro;
public class PythonManager : MonoBehaviour
{


    // Start is called before the first frame update

    public void ScreenShot()
    {
        StartCoroutine(CoroutineScreenShot());

    }


    private IEnumerator CoroutineScreenShot()
    {
        yield return new WaitForEndOfFrame();

        int width = Screen.width;
        int height = Screen.height;
        Texture2D screenshotTexture = new Texture2D(width / 2, height / 2, TextureFormat.ARGB32, false);
        Rect rect = new Rect(width / 4, height / 4, width / 2, height / 2);
        screenshotTexture.ReadPixels(rect, 0, 0);
        screenshotTexture.Apply();

        byte[] byteArray = screenshotTexture.EncodeToPNG();
        System.IO.File.WriteAllBytes(Application.dataPath + "/CameraScreenshot.png", byteArray);


    }

    public void Criticize_Positive()
    {
        string path = Application.dataPath + "/Python/main_positive.py";
        PythonRunner.RunFile(path);

    }

    public void Criticize_Neutral()
    {
        string path = Application.dataPath + "/Python/main_neutral.py";
        PythonRunner.RunFile(path);

    }
    public void Criticize_Negative()
    {
        string path = Application.dataPath + "/Python/main_negative.py";
        PythonRunner.RunFile(path);

    }
}
