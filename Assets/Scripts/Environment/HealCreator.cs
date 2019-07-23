using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealCreator : MonoBehaviour
{
    public GameObject healPrefab;
    public int maxHealNum = 10;
    public int minHealNum = 1;
    public float range; //随机生成的范围
    public float refreshDelta = 4;  //刷新的间隔

    private LinkedList<GameObject> healList = new LinkedList<GameObject>();    //可用的治疗包
    private LinkedList<GameObject> des = new LinkedList<GameObject>();  //此链表仅做中间变量使用
    private int createNum = 0;  //此次更新需要生成治疗包个数

    // Start is called before the first frame update
    void Start()
    {
        int num = Random.Range(minHealNum, maxHealNum + 1);
        for(int i = 0; i < num; ++i)
        {
            healList.AddLast(Instantiate(healPrefab, new Vector3(Random.Range(-range, range), healPrefab.transform.position.y, Random.Range(-range, range)), healPrefab.transform.rotation));
        }
        StartCoroutine(CreateCheck());
    }

    IEnumerator CreateCheck()
    {
        while(enabled)
        {
            //生成此次刷新应生成的治疗包
            for(int i = 0; i < createNum; ++i)
            {
                healList.AddLast(Instantiate(healPrefab, new Vector3(Random.Range(-range, range), healPrefab.transform.position.y, Random.Range(-range, range)), healPrefab.transform.rotation));
            }
            createNum = 0;
            //计算下次刷新应生成的治疗包
            int num  = Random.Range(minHealNum, maxHealNum + 1);
            foreach(GameObject obj in healList) //寻找已失效的治疗包
            {
                if(!obj.activeSelf) des.AddLast(obj);
            }
            foreach(GameObject obj in des)
            {
                if(healList.Remove(obj)) Debug.Log("Suc");
                GameObject.Destroy(obj);
            }
            des.Clear();
            createNum = num - healList.Count;
            // Debug.Log("Create:"+createNum+" "+healList.Count+" "+num);
            yield return new WaitForSeconds(refreshDelta);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
