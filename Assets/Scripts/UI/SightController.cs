using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SightController : MonoBehaviour
{
    public GameObject sightUp;
    public GameObject sightBottom;
    public GameObject sightLeft;
    public GameObject sightRight;
    public float sightExpandSpeed = 25f;    //准星扩大时间（unit/s）
    public float sightReduceSpeed = 25f;    //准星缩小时间（unit/s）
    public float expandTime = 0.3f; //一次射击准星扩大时间
    public float MaxDistance = 18f;
    private float MinDistance = 10f;

    private void Awake()
    {
    }

    // Start is called before the first frame update
    void Start()
    {
        // Shoot();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Shoot()
    {
        StopAllCoroutines();
        StartCoroutine(ExpandSight());
    }

    IEnumerator ExpandSight()
    {
        float expandDelta = 0f;
        float dis = sightUp.transform.localPosition.y;
        while(dis < MaxDistance)
        {
            expandDelta += Time.deltaTime;
            if(expandDelta > expandTime) break;

            float delta = Time.deltaTime * sightExpandSpeed;
            dis += delta;
            sightUp.transform.Translate(new Vector3(0f, delta, 0f), Space.Self);
            sightBottom.transform.Translate(new Vector3(0f, -delta, 0f), Space.Self);
            sightLeft.transform.Translate(new Vector3(-delta, 0f, 0f), Space.Self);
            sightRight.transform.Translate(new Vector3(delta, 0f, 0f), Space.Self);
            yield return null;
        }
        yield return StartCoroutine(ReduceSight());
    }

    IEnumerator ReduceSight()
    {
        float dis = this.sightUp.transform.localPosition.y;
        while(dis > MinDistance)
        {
            float delta = Time.deltaTime * sightReduceSpeed;
            dis -= delta;
            if(dis < MinDistance) delta -= MinDistance - dis;
            sightUp.transform.Translate(new Vector3(0f, -delta, 0f), Space.Self);
            sightBottom.transform.Translate(new Vector3(0f, delta, 0f), Space.Self);
            sightLeft.transform.Translate(new Vector3(delta, 0f, 0f), Space.Self);
            sightRight.transform.Translate(new Vector3(-delta, 0f, 0f), Space.Self);
            yield return null;
        }
        //归位
        sightUp.transform.position.Set(0f, MinDistance, 0f);
        sightBottom.transform.position.Set(0f, -MinDistance, 0f);
        sightLeft.transform.position.Set(-MinDistance, 0f, 0f);
        sightRight.transform.position.Set(MinDistance, 0f, 0f);
    }

    /*获取射击的偏移 */
    public float GetBias()
    {
        return this.sightUp.transform.localPosition.y - MinDistance;
    }
}
