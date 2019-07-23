using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;

public class AliveShowMenu : MonoBehaviour
{
    private Text aliveText;

    private float aliveTime;

    private void Awake()
    {
        aliveText = GetComponent<Text>();
    }

    // Start is called before the first frame update
    void Start()
    {
        aliveTime = Global.maxAliveTime;
        aliveText.text = TimeSpan.FromSeconds(Global.maxAliveTime).ToString();
    }

    // Update is called once per frame
    void Update()
    {
        if(aliveTime != Global.maxAliveTime)
        {
            aliveTime = Global.maxAliveTime;
            String t = TimeSpan.FromSeconds(aliveTime).ToString();
            if(t.Length > 10)   aliveText.text = t.Substring(0, 10);
            else aliveText.text = t;
        }
    }
}
