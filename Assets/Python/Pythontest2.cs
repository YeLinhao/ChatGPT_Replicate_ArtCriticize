using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//注意  一定要引用下面这个命名空间
using System.Diagnostics;

public class Pythontest2 : MonoBehaviour
{
    private void Start()
    {
        RunPythonScript(new string[0]);
    }
    private void Update()
    {
    }
    private static void RunPythonScript(string[] argvs)
    {
        Process p = new Process();
        string path = @"C:\Users\ylh\Desktop\Unity\technical creativity\Assets\Python\main.py";
        foreach (string temp in argvs)
        {
            path += " " + temp;
        }
        p.StartInfo.FileName = @"python.exe";

        p.StartInfo.UseShellExecute = false;
        p.StartInfo.Arguments = path;
        p.StartInfo.RedirectStandardOutput = true;
        p.StartInfo.RedirectStandardError = true;
        p.StartInfo.RedirectStandardInput = true;
        //p.StartInfo.CreateNoWindow = true;
        UnityEngine.Debug.Log("Successful1");
        p.Start();
        p.BeginOutputReadLine();
        p.OutputDataReceived += new DataReceivedEventHandler(Get_data);
        UnityEngine.Debug.Log("Successful2");
        p.WaitForExit();
        UnityEngine.Debug.Log("Successful3");
    }
    private static void Get_data(object sender, DataReceivedEventArgs eventArgs)
    {
        UnityEngine.Debug.Log("Successful4");
        if (!string.IsNullOrEmpty(eventArgs.Data))
        {
            UnityEngine.Debug.Log("Successful");
            print(eventArgs.Data);
        }
    }
}