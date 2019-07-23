using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RivalCreator : MonoBehaviour
{
    public GameObject rivalPrefab;
    public GameObject detectorPrefab; //用于检测周围有没有物体
    public float createRange = 35.0f;
    public float minDisToPlayer = 10.0f;    //创建时不能在玩家周围
    public GameObject player;   //玩家
    private HumanAnimController playerController;

    private LinkedList<GameObject> rivalList = new LinkedList<GameObject>();
    private LinkedList<GameObject> detectors = new LinkedList<GameObject>();    //仅用于CreateRival
    private LinkedList<GameObject> deletes = new LinkedList<GameObject>();
    private float doubleMinDisToPlayer;

    private int createRivalNum; //需创建的敌人数，从服务器获取
    private int requestNum = 0;    //请求创建数
    private int completeNum = 0;
    private bool isCreateOver = true;  //上次获取的敌人数是否创建完
    private bool isRecv = true; //上次询问是否收到回复
    private float outTime = 1.5f;   //超时时间
    private int idNext = 0;

    private ByteBuilder byteBuilder = new ByteBuilder();

    private void Awake()
    {
        doubleMinDisToPlayer = minDisToPlayer * 2;
        Global.network.DelegateNetMes(Network.RIVAL_NUM, new NetMes(Recv));
        Global.player = player;

        playerController = player.GetComponent<HumanAnimController>();
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        //创建敌人
        if(detectors.Count != 0)
        {
            foreach(GameObject obj in detectors)
            {
                if(!obj.activeSelf) deletes.AddLast(obj);
            }
            foreach(GameObject obj in deletes)
            {
                detectors.Remove(obj);
                GameObject.Destroy(obj);
            }
            foreach(GameObject obj in detectors)
            {
                GameObject rival = Instantiate(rivalPrefab, new Vector3(obj.transform.position.x, rivalPrefab.transform.position.y, obj.transform.position.z), Quaternion.identity);
                rival.transform.Rotate(0f, Random.Range(-180f, 180f), 0f, Space.Self);
                rivalList.AddLast(rival);

                RivalController controller = rival.GetComponent<RivalController>();
                controller.DelegateDie(new DieCallback(OnRivalDie));
                controller.id = idNext;
                ++idNext;

                GameObject.Destroy(obj);
                ++ completeNum;
            }
            detectors.Clear();
            deletes.Clear();
        }
        
        //询问服务器是否需新创建敌人
        if(isCreateOver && createRivalNum <= 0 && isRecv)
        {
            StartCoroutine("ResetRecv");
            byteBuilder.Add(System.BitConverter.GetBytes(Network.RIVAL_NUM));
            byteBuilder.Add(System.BitConverter.GetBytes(rivalList.Count));
            Global.network.Send(byteBuilder.GetByes());
            byteBuilder.Clear();
            isRecv = false;
        }
        else if(!isCreateOver && requestNum == createRivalNum) //检查是否创建成功
        {
            createRivalNum = requestNum - completeNum;
            //开始将未创建成功的创建
            for(int i = 0; i < createRivalNum; ++i)
            {
                CreateDetector();
            }
            requestNum = 0;
            completeNum = 0;
        }

        if(!isCreateOver && createRivalNum <= 0)    //检查是否全部创建完成
        {
            completeNum = 0;
            requestNum = 0;
            createRivalNum = 0;
            isCreateOver = true;
        }

        if(isCreateOver && createRivalNum > 0)  //检查是否有待创建的敌人
        {
            //开始将未创建成功的创建
            for(int i = 0; i < createRivalNum; ++i)
            {
                CreateDetector();
            }
            isCreateOver = false;
            requestNum = 0;
            completeNum = 0;
        }
    }

    /*若服务器超时未响应 */
    IEnumerator ResetRecv()
    {
        yield return new WaitForSeconds(outTime);
        isRecv = true;
    }

    void Recv(byte[] mes)
    {
        createRivalNum = System.BitConverter.ToInt32(mes, Network.DataStartIndex);
        // Debug.Log("Need Create "+createRivalNum);
        isRecv = true;
        StopCoroutine("ResetRecv");
    }

    void CreateDetector()
    {
        float x = Random.Range(-createRange, createRange);
        float z = Random.Range(-createRange, createRange);
        float dx = player.transform.position.x - x;
        float dz = player.transform.position.z - z;
        while(dx * dx + dz * dz < doubleMinDisToPlayer)
        {
            x = Random.Range(-createRange, createRange);
            z = Random.Range(-createRange, createRange);
            dx = player.transform.position.x - x;
            dz = player.transform.position.z - z;
        }
        StartCoroutine(AddDetector(Instantiate(detectorPrefab, new Vector3(x, detectorPrefab.transform.position.y, z), Quaternion.identity)));
    }

    IEnumerator AddDetector(GameObject detector)
    {
        //等待碰撞体检测
        yield return null;
        yield return null;
        detectors.AddLast(detector);
        ++requestNum;
    }

    public void OnRivalDie(GameObject obj)
    {
        Debug.Log("Remove");
        rivalList.Remove(obj);
    }

    private void OnDisable() {
        foreach(GameObject obj in rivalList) GameObject.Destroy(obj);
        rivalList.Clear();
    }
}