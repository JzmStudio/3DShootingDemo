using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScoreShowMenu : MonoBehaviour
{
    private Text score;
    private int numNow;

    private void Awake()
    {
        score = GetComponent<Text>();
    }

    private void Start() {
        numNow = Global.killSum;
    }

    // Update is called once per frame
    void Update()
    {
        if(Global.killSum != numNow)
        {
            score.text = Global.killSum.ToString();
            numNow = Global.killSum;
        }
    }
}
