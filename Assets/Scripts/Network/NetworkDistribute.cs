using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkDistribute : MonoBehaviour
{
    public int MaxDistributePerFrame = 100;  //设此值防止分发时影响游戏卡顿

    private void Awake()
    {
        StartCoroutine(Distribute());
    }

    IEnumerator Distribute()
    {
        byte[] mes;
        int num = 0;
        while(true)
        {
            while(Global.network.recvMes.TryDequeue(out mes))
            {
                ++num;
                int mesCode = System.BitConverter.ToInt32(mes, 0);
                // Debug.Log(mesCode.ToString());
                Global.network.CallWithMesCode(mesCode, mes);
                if(num > MaxDistributePerFrame) break;
            }
            num = 0;
            yield return null;
        }
    }
}
