using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using DG.Tweening;
using System.IO.Ports;
using System;
using System.Threading;

public class DIP_CountPre : MonoBehaviour
{
    // Start is called before the first frame update

    public TMP_Text Number;
    public TMP_Text Number2;
    public Image fill;
    public Image fill2;

    private int timer = 0;
    public int tickGap;


    SerialPort sp = new SerialPort("COM3", 9600);
    public float p;

    void Start()
    {
        sp.Open();
        sp.ReadTimeout = 1;
    }



    private void Update()
    {

        if (Input.GetKeyDown(KeyCode.Space))
        {
            fill2.fillAmount += 0.01f;
        }


        timer++;
        if (timer == tickGap)
        {
            timer = 0;
            CheckInput();
        }

        try
        {
            string data = sp.ReadLine();
            p = float.Parse(data);
            print(p);
            fill.fillAmount = 0.9f - (p*1.70f -0.7f);
        }
        catch (System.Exception)
        {
            // 执行错误处理代码
        }





        Number.text = (fill.fillAmount * 100 / 1).ToString("#0");
        Number2.text = (fill2.fillAmount * 100 / 1).ToString("#0");
    }
    
    public void CheckInput()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
           
            DOTween.To(() => fill.fillAmount,x => fill.fillAmount = x,0.2f,1);
            fill.fillAmount += 0.01f;
        }

        if (Input.GetKeyDown(KeyCode.W))
        {
            DOTween.To(() => fill.fillAmount, x => fill.fillAmount = x, 0.4f, 1);
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            DOTween.To(() => fill.fillAmount, x => fill.fillAmount = x, 0.6f, 1);
        }

        if (Input.GetKeyDown(KeyCode.A))
        {
            DOTween.To(() => fill.fillAmount, x => fill.fillAmount = x, 0.8f, 1);
        }

        if (Input.GetKeyDown(KeyCode.D))
        {
            DOTween.To(() => fill.fillAmount, x => fill.fillAmount = x, 1.0f, 1);
        }

    }



    void OnApplicationQuit()
    {
        sp.Close();
    }



}
