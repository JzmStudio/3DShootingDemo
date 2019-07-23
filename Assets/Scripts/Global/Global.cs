using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Global
{
    public static Network network = new Network(888, "127.0.0.1", 999); 
    public static string userName = "";
    public static int killSum = 0;
    public static float maxAliveTime = 0;

    public static GameObject player = null;

    /*startIndex is inclusive, endIndex is exclusive */
    public static byte[] GetSub(byte[] data, int startIndex, int endIndex)
    {
        if(startIndex >= data.Length || endIndex > data.Length) return null;
        int num = endIndex - startIndex;
        byte[] res = new byte[num];
        System.Buffer.BlockCopy(data, startIndex, res, 0, num);
        return res;
    }
}
