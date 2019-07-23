using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public GameObject[] showUnplay; //未进行游戏时应显示的物体
    public GameObject[] showPlay;   //进行游戏时应显示的物体
    public InputController inputController;   //未开始游戏时玩家不对输入做处理
    public float ConvertSpeed = 50f;   //镜头移动变换速度

    /*控制镜头在点击开始游戏后的移动 */
    private Vector3 startPointLocal = new Vector3(-0.24f, 8.9f, -39.1f); //主界面镜头所在点
    private Vector3 endPointLocal = new Vector3(-0.24f, 3.98f, -3.3f);   //游戏中镜头所在点
    private Quaternion startRotationLocal;

    private void Awake()
    {
        startRotationLocal = transform.localRotation;   //初始化正确镜头角度
    }

    // Start is called before the first frame update
    void Start()
    {
        ToMenu();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ToMenu()
    {
        Cursor.visible = true;
        StartCoroutine(MoveToPoint(startPointLocal, true));
    }

    public void StartGame()
    {
        Cursor.visible = false;
        StartCoroutine(MoveToPoint(endPointLocal, false));
    }

    IEnumerator MoveToPoint(Vector3 targetPoint, bool isToMenu)
    {
        //disable first
        if(isToMenu)
        {
            foreach(GameObject obj in showPlay)
            {
                obj.SetActive(false);
            }
            inputController.enabled = false;
        }
        else
        {
            foreach(GameObject obj in showUnplay)
            {
                obj.SetActive(false);
            }
        }

        float t = 0f;
        Vector3 startP = transform.localPosition;
        float convertTime = (startP - targetPoint).magnitude / ConvertSpeed;

        Quaternion startR = transform.localRotation;
        while(t < convertTime)
        {
            t += Time.deltaTime;
            if(t > convertTime) t = convertTime;
            transform.localPosition = Vector3.Slerp(startP, targetPoint, t / convertTime);
            transform.localRotation = Quaternion.Slerp(startR, startRotationLocal, t / convertTime);
            yield return null;
        }

        //enable Last
        if(isToMenu)
        {
            foreach(GameObject obj in showUnplay)
            {
                obj.SetActive(true);
            }
        }
        else
        {
            foreach(GameObject obj in showPlay)
            {
                obj.SetActive(true);
            }
            inputController.enabled = true;
            inputController.StartGame();
        }
    }

    //游戏结束时释放资源
    private void OnApplicationQuit()
    {
        Global.network.Close();
    }
}
