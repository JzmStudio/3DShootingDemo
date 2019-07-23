using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public delegate void DieCallback(GameObject gameObject);

public class RivalController : MonoBehaviour, CanGetSight
{
    public GameObject[] weapons;    //AI所可能持有的枪
    public float runSpeed = 4.5f;  //跑步速度
    public float walkSpeed = 2.3f;
    public float shootBias = 2f;    //射击偏移，降低AI射击准确率
    public float rotateSpeed = 90f; //旋转速度
    public float maxRotateImmeAngle = 30f; //能立即转身的角度
    public GameObject shootSmokePrefab;
    public float sightAngle = 80f;  //视角一半

    public int id = 0;

    private DieCallback dieCallbacks = null;    //死亡回调

    private GameObject weapon;  //AI手中持有的枪
    private WeaponController weaponController;  //此时手中武器对应的控制脚本
    private HumanAnimController humanController;
    public bool isDead = false;    //标记是否已执行死亡
    /*动作 */
    private int vertical = 0;   //前后移动标定
    private int horizontal = 0;   //左右移动标定
    private bool isPatrol = false;  //是否在巡逻状态
    private Vector3 patrolForward = Vector3.forward;
    private bool isTrace = false;   //是否追击玩家
    private bool isInRotate = false;    //正否在旋转过程中

    private bool isShoot = false;   //标定是否进行射击
    private Vector3 lookAtPoint;    //视线目标
    private Vector3 shootRectify = new Vector3(0f, 1.5f, 0f); //射击高度矫正
    private RaycastHit sightHit;    //视线目标
    private RaycastHit hit; //攻击目标
    private NavMeshAgent nav;
    private ByteBuilder byteBuilder = new ByteBuilder();

    private Collider lastCollider = null;   //用于巡逻，使转身不碰撞到同一物体上

    private LinkedList<Vector3> path = new LinkedList<Vector3>();   //用于追踪玩家的路径
    private Vector3 playerLastPosition = Vector3.zero; //若玩家和上次相距较近则不再进行寻路

    private NetMes netMesDelegate;

    private void Awake()
    {
        nav = GetComponent<NavMeshAgent>();
        humanController = GetComponent<HumanAnimController>();

        int index = Random.Range(0, weapons.Length);
        weapon = Instantiate(weapons[index], weapons[index].transform.position, weapons[index].transform.rotation);
        weapon.transform.SetParent(transform, false);
        weaponController = weapon.GetComponent<WeaponController>();

        netMesDelegate = new NetMes(DealMes);
        Global.network.DelegateNetMes(Network.RIVAL_REQUEST, netMesDelegate);
    }

    public void DealMes(byte[] mes)
    {
        // Debug.Log("Re1"+System.BitConverter.ToInt32(mes, Network.DataStartIndex));
        if(System.BitConverter.ToInt32(mes, Network.DataStartIndex) != id) return;
        int actionCode = System.BitConverter.ToInt32(mes, Network.DataStartIndex + 4);
        // Debug.Log("Re" + actionCode);
        switch(actionCode)
        {
            case Network.TRACE:
                Debug.Log("TRACE");
                isTrace = true;
                isPatrol = false;
                isShoot = false;
                break;
            case Network.SHOOT:
                Debug.Log("SHOOT");
                isTrace = false;
                isShoot = true;
                isPatrol = false;
                break;
            case Network.SEARCH_ROAD:
                path.Clear();
                int startIndex= Network.DataStartIndex + 8;
                while(startIndex < mes.Length)
                {
                    Vector3 v = new Vector3(System.BitConverter.ToInt32(mes, startIndex), transform.position.y, System.BitConverter.ToInt32(mes, startIndex + 4));
                    //每次读取一次x、z坐标
                    path.AddLast(v);
                    // Debug.DrawLine(v, v + new Vector3(0, 3f, 0f), Color.black, 1000);
                    startIndex += 8;
                }
                if(isTrace && !isDead) //重置追踪路径并开始追踪
                {
                    Debug.Log("TRACE");
                    StopAllCoroutines();
                    isInRotate = false;    //重置
                    StartCoroutine("Trace", path);
                    humanController.PlayRunAnim();
                }
                // Debug.Log("DRAW");
                // UnityEditor.EditorApplication.isPaused = true;
                break;
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        //初始化视线
        Ray tar = new Ray(transform.position + shootRectify, transform.forward);
        if(Physics.Raycast(tar, out sightHit))
        {
            lookAtPoint = sightHit.point;
        }

        //初始化动作
        switch(weaponController.category)
        {
            case WeaponCategory.Handgun:
                humanController.ToHandgun(weapon);
                break;
            case WeaponCategory.Heavy:
                humanController.ToHeavy(weapon);
                break;
            case WeaponCategory.Infantry:
                humanController.ToInfantry(weapon);
                break;
            case WeaponCategory.Knife:
                humanController.ToKnife(weapon);
                break;
        }

        SetPatrol();
    }

    // Update is called once per frame
    void Update()
    {
        //死亡则慢慢下沉消失
        if(humanController.health <= 0 && !isDead)
        {
            if(dieCallbacks != null) dieCallbacks(gameObject);   //死亡回调

            isDead = true;
            Destroy(GetComponent<CharacterController>());
            Destroy(GetComponent<Rigidbody>());
            Destroy(GetComponent<SphereCollider>());
            StopAllCoroutines();
            StartCoroutine(Die());
            return;
        }
        if(humanController.health != humanController.MaxHealth && humanController.health > 0 && !isTrace && !isShoot)
        {
            isTrace = true;
            isPatrol = false;
            isShoot = false;
        }

        if(isInRotate) return;
        // Debug.Log("Next");
        if(!isDead)
        {
            //移动
            if(isPatrol)
            {
                // Debug.Log("WALK");
                // nav.enabled = false;
                // StopCoroutine("Trace");
                humanController.Idle();
                if(Vector3.Angle(patrolForward, transform.forward) > 5.0f)  //判断是否需要转向
                {
                    // Debug.Log("ChangeTo:"+patrolForward + transform.forward + (patrolForward == transform.forward) +" "+);
                    // StopCoroutine("RotateByFrame");
                    RotateTowards(patrolForward);
                    return;
                }
                humanController.Walk(walkSpeed, 0f);
            }
            else if(isTrace)    //追踪
            {
                if(playerLastPosition == Vector3.zero || (Global.player.transform.position - playerLastPosition).magnitude > 3f)
                {   
                    /*向服务器发送路径计算请求 */
                    byteBuilder.Add(System.BitConverter.GetBytes(Network.RIVAL_REQUEST));
                    byteBuilder.Add(System.BitConverter.GetBytes(id));
                    byteBuilder.Add(System.BitConverter.GetBytes(Network.SEARCH_ROAD));
                    byteBuilder.Add(System.BitConverter.GetBytes((int)(transform.position.x + 0.5)));
                    byteBuilder.Add(System.BitConverter.GetBytes((int)(transform.position.z + 0.5)));
                    byteBuilder.Add(System.BitConverter.GetBytes((int)(Global.player.transform.position.x + 0.5)));
                    byteBuilder.Add(System.BitConverter.GetBytes((int)(Global.player.transform.position.z + 0.5)));
                    Global.network.Send(byteBuilder.GetByes());
                    byteBuilder.Clear();
                    // nav.enabled = true;
                    // nav.SetDestination(Global.player.transform.position);
                    // humanController.Run(0, 0);
                    playerLastPosition = Global.player.transform.position;
                }
            }
            else if(vertical != 0 || horizontal != 0)
            {
                humanController.Run(vertical * runSpeed, horizontal * runSpeed);
                // nav.enabled = false;
                StopCoroutine("Trace");
                humanController.Idle();
            }
            else
            {
                // humanController.Idle();
                // nav.enabled = false;
                // StopCoroutine("Trace");
                StopAllCoroutines();
                humanController.Idle();
            }

            //设置视线
            Ray tar = new Ray(transform.position + shootRectify, transform.forward);
            if(Physics.Raycast(tar, out sightHit))
            {
                lookAtPoint = sightHit.point;
            }

            //检测是否玩家在视线内
            Vector3 targetV = Global.player.transform.position - transform.position;
            // Debug.Log(Vector3.Angle(transform.forward, targetV));
            if(Vector3.Angle(transform.forward, targetV) <= sightAngle)
            {
                Ray detect = new Ray(transform.position + shootRectify, targetV);
                RaycastHit h;
                if(Physics.Raycast(detect, out h))
                {
                    // Debug.Log("Hit:"+h.collider.gameObject.layer+" "+h.collider.name);
                    // Debug.DrawRay(transform.position, targetV, Color.black);
                    if(h.collider.gameObject.layer == 11)
                    {
                        // Debug.Log("Human");
                        float dis = (transform.position - Global.player.transform.position).magnitude;
                        if(!isTrace && !isShoot)    //若在巡逻状态
                        {
                            Debug.Log("In Sight");
                            //在视线内
                            byteBuilder.Add(System.BitConverter.GetBytes(Network.RIVAL_REQUEST));
                            byteBuilder.Add(System.BitConverter.GetBytes(id));
                            byteBuilder.Add(System.BitConverter.GetBytes(Network.PLAYER_IN_SIGHT));
                            Global.network.Send(byteBuilder.GetByes());
                            byteBuilder.Clear();
                        }
                        else if(!isShoot && dis < weaponController.maxAttackDis)    //若不在射击状态
                        {
                            byteBuilder.Add(System.BitConverter.GetBytes(Network.RIVAL_REQUEST));
                            byteBuilder.Add(System.BitConverter.GetBytes(id));
                            byteBuilder.Add(System.BitConverter.GetBytes(Network.PLAYER_IN_SHOOT));
                            Global.network.Send(byteBuilder.GetByes());
                            byteBuilder.Clear();
                        }
                        else if(isShoot && dis > weaponController.maxAttackDis) //若在射击状态但不在攻击距离内
                        {
                            byteBuilder.Add(System.BitConverter.GetBytes(Network.RIVAL_REQUEST));
                            byteBuilder.Add(System.BitConverter.GetBytes(id));
                            byteBuilder.Add(System.BitConverter.GetBytes(Network.PLAYER_OUT_SHOOT));
                            Global.network.Send(byteBuilder.GetByes());
                            byteBuilder.Clear();
                        }
                    }
                    else if(isShoot)    //出视线外
                    {
                        byteBuilder.Add(System.BitConverter.GetBytes(Network.RIVAL_REQUEST));
                        byteBuilder.Add(System.BitConverter.GetBytes(id));
                        byteBuilder.Add(System.BitConverter.GetBytes(Network.PLAYER_OUT_SHOOT));
                        Global.network.Send(byteBuilder.GetByes());
                        byteBuilder.Clear();
                    }
                }
            }

            //shoot
            if(isShoot)
            {
                Debug.Log("SHOOT");
                //若无子弹则换弹
                if(weaponController.bulletNumNow <= 0)
                {
                    weaponController.bulletSum = weaponController.bulletMaxHold + 100;  //设置子弹无限
                    humanController.Reload();
                }
                else
                {
                    transform.LookAt(Global.player.transform, transform.up);
                    if(humanController.Attack1() && weaponController.Shoot())
                    {
                        Ray ray = new Ray(transform.position + shootRectify, transform.forward + transform.right * Random.Range(-shootBias, shootBias) + transform.up * Random.Range(-shootBias, shootBias));
                        if(Physics.Raycast(ray, out hit))
                        {
                            //若击中环境，刀子无击中效果
                            if(hit.collider.gameObject.layer == 9 && weaponController.category != WeaponCategory.Knife)
                            {
                                GameObject shootSmoke = Instantiate(shootSmokePrefab, hit.point, Quaternion.identity);
                                shootSmoke.transform.localRotation.SetLookRotation(hit.normal);
                                //获取击中物体的颜色
                                Color color = hit.collider.gameObject.GetComponent<MeshRenderer>().material.color;
                                ParticleSystem particleSystem = shootSmoke.GetComponent<ParticleSystem>();
                                //设置起始颜色
                                ParticleSystem.MainModule main = particleSystem.main;
                                main.startColor = color;
                                //设置颜色渐变
                                ParticleSystem.ColorOverLifetimeModule col = particleSystem.colorOverLifetime;
                                col.enabled = true;
                                Gradient grad = new Gradient();
                                grad.SetKeys(new GradientColorKey[] { new GradientColorKey(color, 0.4f), new GradientColorKey(color, 1.0f) }, new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) });
                                col.color = grad;

                                GameObject.Destroy(shootSmoke, 0.5f);   //设置0.2s后销毁烟雾效果
                            }
                            //若击中人
                            else if(hit.collider.gameObject.layer == 11)
                            {
                                hit.collider.gameObject.GetComponent<HumanAnimController>().Damaged(weaponController.damage, hit.point);
                            }
                        }
                    }
                }
            }
        }
    }

    public void RotateTowards(Vector3 targetForword)
    {
        // Debug.Log("Rotate");
        float angle = Vector3.Angle(targetForword, transform.forward);
        if(angle < maxRotateImmeAngle) transform.forward = targetForword;
        else
        {
            // StartCoroutine(RotateByFrame(targetForword));
            StartCoroutine("RotateByFrame", targetForword);
        }
    }

    IEnumerator RotateByFrame(Vector3 targetForword)
    {
        Debug.Log("RotateIn");
        isInRotate = true;
        // Vector3 tarInLocal = transform.InverseTransformVector(targetForword);
        // float angle = Vector3.Angle(targetForword, Vector3.forward);
        // if(tarInLocal.x < 0) angle = -angle;
        float sumT = Vector3.Angle(targetForword, transform.forward) / rotateSpeed;
        float t = 0;
        Quaternion start = transform.rotation;
        // Debug.Log("SR:"+start.ToString());
        transform.LookAt(targetForword + transform.position, transform.up);
        Quaternion end = transform.rotation;
        // Debug.Log("ER:"+end.ToString());
        while(true)
        {
            t += Time.deltaTime;
            float k = t / sumT;
            if(k >= 1) break;
            transform.rotation = Quaternion.Slerp(start, end, k);
            yield return null;
        }
        // transform.rotation = end;
        transform.forward = targetForword;
        isInRotate = false;
        Debug.Log("RotateOUT");
    }

    IEnumerator Trace(LinkedList<Vector3> path)
    {
        Vector3 target;
        while(path.Count != 0)
        {
            target = path.First.Value;
            path.RemoveFirst();
            if(target != transform.position)
            {
                Vector3 targetForward = target - transform.position;
                float angle = Vector3.Angle(transform.forward, targetForward);
                //先转身
                if(angle < maxRotateImmeAngle) transform.forward = targetForward;
                else
                {
                    yield return StartCoroutine(RotateByFrame(targetForward));
                }
            }
            //再走过去
            yield return StartCoroutine(GoTo(target));
        }
    }

    IEnumerator GoTo(Vector3 target)
    {
        // Debug.Log("GoTo");
        humanController.PlayRunAnim();
        float dis = (target - transform.position).magnitude;
        float tSum = dis / runSpeed;
        Vector3 startPosition = transform.position;
        float t = 0f;
        while(transform.position != target)
        {
            t += Time.deltaTime;
            transform.position = Vector3.Lerp(startPosition, target, t / tSum);
            yield return null;
        }
        // Debug.Log("GoOver");
    }

    IEnumerator Die()
    {
        // Debug.Log("StartDie");
        yield return new WaitForSeconds(3);
        while(transform.position.y >= -2)
        {
            // Debug.Log("In Die"+transform.position);
            transform.Translate(0f, -Time.deltaTime, 0f, Space.World);
            // transform.position -= new Vector3(0f, -Time.deltaTime, 0f);
            yield return null;
        }
        Object.Destroy(gameObject);
        // Debug.Log("EndDie");
    }

    public void GoLeft()
    {
        horizontal = -1;
    }

    public void GoRight()
    {
        horizontal = 1;
    }

    public void GoFront()
    {
        vertical = 1;
    }

    public void GoBack()
    {
        vertical = -1;
    }

    public Vector3 GetSightPoint()
    {
        return lookAtPoint;
    }

    /*监听换弹完成事件 */
    public void ReloadOver()
    {
        weaponController.Reload();
    }

    /*设置人物巡逻 */
    public void SetPatrol()
    {
        isPatrol = true;
        patrolForward = transform.forward;
    }

    public void LookAt(Vector3 point)
    {
        transform.LookAt(point, transform.up);
    }

    public void DelegateDie(DieCallback callback)
    {
        if(dieCallbacks == null) dieCallbacks = callback;
        else dieCallbacks += callback;
    }

    private void OnTriggerEnter(Collider other)
    {
        if(lastCollider && other.gameObject == lastCollider.gameObject) return;
        // Debug.Log("ENTER:"+isInRotate);
        // Debug.DrawLine(other.transform.position, other.transform.position + new Vector3(0f, 5f, 0f), Color.green, 1000);
        if(other.gameObject.layer == 9 && other.tag != "Ground")
        {
            // Debug.Log("Set:"+other.name);
            if(isPatrol && !isInRotate)    //转移巡逻方向
            {
                patrolForward = transform.position - other.gameObject.transform.position;
                patrolForward.Normalize();
            }
        }

        lastCollider = other;
    }

    private void OnTriggerStay(Collider other) {
        if(lastCollider && other.gameObject == lastCollider.gameObject) return;
        if(other.gameObject.layer == 9 && other.tag != "Ground")
        {
            // Debug.Log("Set2:"+other.name+" "+other.transform.position+" "+(other.gameObject == lastCollider));
            if(isPatrol && !isInRotate)    //转移巡逻方向
            {
                patrolForward = transform.position - other.gameObject.transform.position;
                patrolForward.Normalize();
            }
        }

        lastCollider = other;
    }

    // private void OnControllerColliderHit(ControllerColliderHit hit)
    // {
    //     // Debug.Log("Hit:"+hit.gameObject.name);
    //     if(hit.gameObject.layer == 9 && hit.gameObject.name != "Ground")
    //     {
    //         if(isPatrol && !isInRotate)    //转移巡逻方向
    //         {
    //             patrolForward = transform.position - hit.gameObject.transform.position;
    //             patrolForward.Normalize();
    //         }
    //     }
    // }

    private void OnDestroy() {
        Global.network.ReleaseNetMes(Network.RIVAL_REQUEST, netMesDelegate);    //取消监听
    }
}
