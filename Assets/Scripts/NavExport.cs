using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class NavExport : MonoBehaviour
{
    #region Public Attributes  
    public Vector3 leftUpStart = new Vector3(-40f, 0f, 40f);  
    public float accuracy = 1;  
    public int height = 80;  
    public int wide = 80;  
    #endregion  
 
    private void Start()
    {
        exportPoint(leftUpStart, height, wide, accuracy);
    }
 
 
    #region Public Methods  
  
    public void Exp()  
    {  
        exportPoint(leftUpStart, wide, height, accuracy);  
    }  
  
    public void exportPoint(Vector3 startPos, int x, int y, float accuracy)  
    {
        FileStream fs = new FileStream(@"layout.mesh", FileMode.Create);
        for (int i = 0; i < y; ++i)  // row, 即y值
        {  
            for (int j = 0; j < x; ++j)  // col, x value
            {  
                int res = 0;    //不可通过
                UnityEngine.AI.NavMeshHit hit;
                Vector3 pos = Vector3.zero;
                for (int k = -10; k < 30; ++k)  
                {
                    pos = startPos + new Vector3(j * accuracy, k, -i * accuracy);
                    if (UnityEngine.AI.NavMesh.SamplePosition(startPos + new Vector3(j * accuracy, k, -i * accuracy), out hit, 0.2f, UnityEngine.AI.NavMesh.AllAreas))  
                    {  
                        res = 1;    //可通过
                        break;
                    }
                }
                Debug.DrawRay(startPos + new Vector3(j * accuracy, 0, -i * accuracy), Vector3.up, res == 1 ? Color.green : Color.red, 100f);  
                fs.Write(System.BitConverter.GetBytes((int)pos.x), 0, 4);
                fs.Write(System.BitConverter.GetBytes((int)pos.y), 0, 4);
                fs.Write(System.BitConverter.GetBytes((int)pos.z), 0, 4);
                fs.Write(System.BitConverter.GetBytes(res), 0, 4);
                Debug.Log(pos.ToString() + " " + res + " " + fs.Position);
            }  
        }  
        fs.Flush();
        fs.Close();
        Debug.Log("file written!"); 
    }  
    #endregion  
}
