using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;

public class InputController : MonoBehaviour, CanGetSight
{
    public float viewMaxAngle = 30f;  //垂直视角改变的最大角度（角度制）
    public float viewHorizontalSpeed = 1.5f;
    public float viewVerticalSpeed = 2.3f;
    public float walkSpeed = 2f;
    public float runSpeed = 4.5f;
    public GameObject knifePrefab;
    public Text prompt; //在准星下的提示字符
    public GameObject shootSmokePrefab;    //射击到环境的反馈粒子显示
    public SightController sightController;    //准星控制器
    public float MaxPickDistance = 4f;  //最大捡东西距离
    public float shootAngleChangeSpeed = 5f;    //每次射击后坐力偏移的角度
    /*UI相关 */    
    public Transform healthTransform;   //控制血条显示，通过调整Scale中X轴的缩小来模拟血量
    public Text bulletNumNowText;   //现在的弹药显示
    public Text bulletNumTotalText; //总弹药显示
    public Image circleBulletImage;  //弹药的圆环显示
    public Text weaponInfoText; //武器提示文字
    public Text weaponShootInfoText;    //武器射击类型提示
    public Text weaponBulletSum;    //总共弹药显示
    public Text shootModePromptText;    //提示是否能切换武器状态
    public Text scoreText;  //分数（歼敌数）
    public Text timeText;   //生存时间显示
    public GameObject hitShow;  //击中显示

    public GameObject EscCanvas;    //退出界面

    private float aliveTime = 0f;

    public Text promptText;

    // public GameObject rival;    //test

    private HumanAnimController playerController;
    /*行走输入相关 */
    private float horizontal;
    private float vertical;
    private bool isCrouching = false;
    /*视角相关 */
    private float viewAngle = 0f;   //视角目前垂直活动的角度
    private float mouseX = 0f;
    private float mouseY = 0f;
    private Vector3 sightTargetPoint;
    private float shootAngle = 0f;  //射击后坐力偏移角度
    /*武器相关 */
    // private float shootBias = 0f;   //射击偏移量，在连续射击时会使得射击不准确
    private GameObject knife;   //主角的刀不能丢弃
    private GameObject gun; //主角只能拿一把枪，可丢弃
    private GameObject weaponNow;   //当前手中拿的武器
    private WeaponController weaponController;  //当前枪械的控制脚本
    /*射击相关 */
    private RaycastHit hit; //准星瞄准的相关信息
    /*血量信息 */
    private float health;   //人物的血量
    private float MaxHealth;
    /*死亡处理 */
    private bool isDead = false;
    private bool syncOver = false;
    private float timeOut = 5;

    private Vector3 startPositon;
    private Quaternion startRotation;
    private int bulletSumLast;  //用于同步子弹剩余量显示

    public void RecvMes(byte[] mes)
    {
        Debug.Log("REC:"+System.BitConverter.ToInt32(mes, Network.DataStartIndex));
        if(System.BitConverter.ToInt32(mes, Network.DataStartIndex) == Network.ACK)
        {
            Debug.Log("ACK");
            syncOver = true;
        }
    }

    private void Awake()
    {
        playerController = GetComponent<HumanAnimController>();
        //设置提示字段
        prompt.text = "按F换枪";
        prompt.gameObject.SetActive(false);

        startPositon = transform.position;
        startRotation = transform.rotation;

        Global.network.DelegateNetMes(Network.MATCH_INFO, new NetMes(RecvMes));
    }

    // Start is called before the first frame update
    void Start()
    {
        knife = Instantiate(knifePrefab, new Vector3(0f, 0f, 0f), Quaternion.identity);
        weaponNow = knife;
        weaponController = weaponNow.GetComponent<WeaponController>();
        playerController.ToKnife(knife);

        /*初始化准星目标点，设置视线 */
        // Ray ray = Camera.main.ScreenPointToRay(new Vector3(Camera.main.pixelWidth / 2, Camera.main.pixelHeight / 2, 0f));
        // if(Physics.Raycast(ray, out hit))
        // {
        //     sightTargetPoint = hit.point;
        // }
        sightTargetPoint = transform.forward * 5;
        playerController.SetSightGetter(this);  //视线设置以便于人物面向准星的位置

        /*设置血量、子弹等 */
        MaxHealth = playerController.MaxHealth;
        health = MaxHealth;
        healthTransform.localScale = new Vector3(1f, 1f, 1f);
        SetBulletUI();
        SetWeaponInfoUI();
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyUp(KeyCode.Escape))
        {
            if(EscCanvas.activeSelf)
            {
                EscCanvas.SetActive(false);
                Time.timeScale = 1;
                Cursor.visible = false;
            }
            else
            {
                EscCanvas.SetActive(true);
                Time.timeScale = 0;
                Cursor.visible = true;
            }
        }


        if(isDead) return;
        /*设置视线关注点 */
        Ray sightRay= Camera.main.ScreenPointToRay(new Vector3(Camera.main.pixelWidth / 2, Camera.main.pixelHeight / 2, 0f));
        RaycastHit sightHit;
        if(Physics.Raycast(sightRay, out sightHit))
        {
            sightTargetPoint = sightHit.point;
        }

        /*更新血量 */
        if(health != playerController.health)
        {
            health = playerController.health;
            healthTransform.localScale = new Vector3(health / MaxHealth, 1f, 1f);
        }
        if(health == 0)
        {
            //死亡处理
            StopCoroutine("StartTime");
            OverGame();
            return;
        }

        /*准星相关处理 */
        float bias = sightController.GetBias(); //射击加偏置
        Ray ray = Camera.main.ScreenPointToRay(new Vector3(Camera.main.pixelWidth / 2 + UnityEngine.Random.Range(-bias, bias), Camera.main.pixelHeight / 2 + UnityEngine.Random.Range(-bias, bias), 0f));
        bool isRaycast = Physics.Raycast(ray, out hit); //标记准星是否有目标点
        if(isRaycast)
        {
            //若为武器且在捡取范围内
            if(hit.collider.gameObject.tag.Equals("Weapon") && (hit.point - transform.position).magnitude <= MaxPickDistance)
            {
                prompt.gameObject.SetActive(true);
                //换枪
                if(Input.GetKey("f"))
                {
                    knife.SetActive(false);
                    //若有武器则显示丢弃武器动画
                    if(gun != null && gun.GetComponent<WeaponController>().category != WeaponCategory.Knife)
                    {
                        DropGun();
                    }

                    gun = hit.collider.gameObject;
                    weaponNow = gun;
                    weaponController = weaponNow.GetComponent<WeaponController>();
                    //去掉碰撞与重力
                    Collider collider = gun.GetComponent<MeshCollider>();
                    if(collider != null) Destroy(collider);
                    Rigidbody rigidbody = gun.GetComponent<Rigidbody>();
                    if(rigidbody != null) Destroy(rigidbody);
                    //正确显示动画
                    switch(weaponController.category)
                    {
                        case WeaponCategory.Handgun:
                            playerController.ToHandgun(gun);
                            break;
                        case WeaponCategory.Heavy:
                            playerController.ToHeavy(gun);
                            break;
                        case WeaponCategory.Infantry:
                            playerController.ToInfantry(gun);
                            break;
                    }
                    //正确显示UI
                    SetBulletUI();
                    SetWeaponInfoUI();
                }
            }
            else
            {
                prompt.gameObject.SetActive(false);
            }
        }

        /*以下所有处理同一时间只能触发一个 */
        //丢弃主武器
        if(Input.GetKey("g") && gun != null)
        {
            DropGun();
            //切换为刀
            knife.SetActive(true);
            playerController.ToKnife(knife);
            weaponNow = knife;
            weaponController = weaponNow.GetComponent<WeaponController>();
            //正确显示UI
            SetBulletUI();
            SetWeaponInfoUI();
        }
        //攻击，不能同时用两种攻击方式
        else if(Input.GetAxisRaw("Fire1") == 1 && ((weaponController.shootMode == ShootMode.Single && playerController.Attack1())/*单发模式 */ || (weaponController.shootMode == ShootMode.Auto && playerController.Attack2())/*连发模式 */))
        {
            // Debug.Log((hit.point - transform.position).magnitude);
            //判断是否能击中
            if(weaponController.Shoot() && isRaycast && (hit.point - transform.position).magnitude <= weaponController.maxAttackDis)
            {
                shootAngle += shootAngleChangeSpeed;    //后坐力
                //除刀外改变准星准度
                if(weaponController.category != WeaponCategory.Knife)
                {
                    sightController.Shoot();
                }
                //改变弹药显示
                SetBulletUI();
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
                    HumanAnimController rivalController = hit.collider.gameObject.GetComponent<HumanAnimController>();
                    rivalController.Damaged(weaponController.damage, hit.point);
                    //若敌人被打死则加分数
                    if(rivalController.health <= 0 && !hit.collider.gameObject.GetComponent<RivalController>().isDead)
                    {
                        scoreText.text = (int.Parse(scoreText.text) + 1).ToString();
                    }
                    //击中显示
                    StopCoroutine("ShowHitUI");
                    StartCoroutine("ShowHitUI");
                }
            }
        }
        else if(Input.GetAxisRaw("Fire2") == 1 && weaponController.category == WeaponCategory.Knife && playerController.Attack2())
        {
            // (hit.collider.transform.position - transform.position).magnitude < weaponController.maxAttackDis || 
            if(isRaycast && (hit.point - transform.position).magnitude <= weaponController.maxAttackDis)
            {
                //若击中人
                if(hit.collider.gameObject.layer == 11)
                {
                    hit.collider.gameObject.GetComponent<HumanAnimController>().Damaged(weaponController.damage, hit.point);
                    //击中显示
                    StopCoroutine("ShowHitUI");
                    StartCoroutine("ShowHitUI");
                }
            }
        }
        else if(Input.GetKey("r") && weaponController.category != WeaponCategory.Knife && playerController.Reload())
        {
        }
        else if(Input.GetKey("y") && weaponController.ChangeShootMode())
        {
            SetWeaponInfoUI();
        }
        //切换武器
        else if(Input.GetKeyUp("q") && gun != null)
        {
            if(weaponNow == gun)
            {
                gun.SetActive(false);
                knife.SetActive(true);
                weaponNow = knife;
                weaponController = weaponNow.GetComponent<WeaponController>();
                playerController.ToKnife(knife);
            }
            else
            {
                knife.SetActive(false);
                gun.SetActive(true);
                weaponNow = gun;
                weaponController = weaponNow.GetComponent<WeaponController>();
                switch(weaponController.category)
                {
                    case WeaponCategory.Handgun:
                        playerController.ToHandgun(gun);
                        break;
                    case WeaponCategory.Heavy:
                        playerController.ToHeavy(gun);
                        break;
                    case WeaponCategory.Infantry:
                        playerController.ToInfantry(gun);
                        break;
                }
            }
            //正确显示UI
            SetBulletUI();
            SetWeaponInfoUI();
        }

        if(bulletSumLast != weaponController.bulletSum)
        {
            if(weaponController.category == WeaponCategory.Knife)
            {
                weaponBulletSum.text = "";
            }
            else
            {
                weaponBulletSum.text = "剩余弹药：" + weaponController.bulletSum;
            }
            bulletSumLast = weaponController.bulletSum;
        }
    }

    IEnumerator ShowHitUI()
    {
        hitShow.SetActive(true);
        yield return new WaitForSeconds(0.3f);
        hitShow.SetActive(false);
    }

    /*丢弃主武器 */
    private void DropGun()
    {
        gun.transform.SetParent(transform.parent, true);
        MeshCollider collider = gun.AddComponent<MeshCollider>() as MeshCollider;
        collider.convex = true;
        Rigidbody rigidbody = gun.AddComponent<Rigidbody>() as Rigidbody;
        rigidbody.useGravity = true;
        rigidbody.AddForce((transform.forward + transform.up) * 100, ForceMode.Acceleration);
        gun = null; //重置主武器为null
    }

    private void FixedUpdate() {
        //移动视角
        mouseX = Input.GetAxis("Mouse X");
        if(mouseX != 0f)
        {
            if(!isDead)
            {
                transform.Rotate(0, mouseX * viewHorizontalSpeed, 0, Space.World);
            }
            else
            {
                Camera.main.transform.RotateAround(transform.position, Vector3.up, mouseX * viewHorizontalSpeed);
            }
        }

        if(isDead) return;

        mouseY = Input.GetAxis("Mouse Y");
        if(mouseY != 0f || shootAngle != 0f)
        {
            float angle = mouseY * viewVerticalSpeed + shootAngle;
            float targetAngle = viewAngle + angle;
            if(targetAngle > 0 && targetAngle > viewMaxAngle)
            {
                angle = viewMaxAngle - viewAngle;
                viewAngle = viewMaxAngle;
            }
            else if(targetAngle < 0 && targetAngle < - viewMaxAngle)
            {
                angle = - viewMaxAngle - viewAngle;
                viewAngle = - viewMaxAngle;
            }
            else
            {
                viewAngle = targetAngle;
            }
            Camera.main.transform.RotateAround(transform.position, transform.right, - angle);
            shootAngle = 0f;    //移动摄像头后清空射击后坐力偏移
        }

        //蹲下
        if(Input.GetKey("left ctrl"))
        {
            if(!isCrouching)
            {
                playerController.SetCrouching();
                isCrouching = true;
            }
        }
        else
        {
            if(isCrouching) 
            {
                isCrouching = false;
                playerController.SetStanding();
            }
        }

        //移动与奔跑
        vertical = Input.GetAxisRaw("Vertical");
        horizontal = Input.GetAxisRaw("Horizontal");
        if(vertical != 0f || horizontal != 0f)
        {
            //限制蹲下时不能跑
            if(Input.GetKey("left shift") && !isCrouching)
            {
                playerController.Run(vertical * runSpeed, horizontal * runSpeed);
            }
            else
            {
                playerController.Walk(vertical * walkSpeed, horizontal * walkSpeed);
            }
        }
        else
        {
            playerController.Idle();
        }

        //跳
        if(Input.GetAxisRaw("Jump") == 1)
        {
            playerController.Jump();
        }
    }

    public Vector3 GetSightPoint()
    {
        return sightTargetPoint;
    }

    /*监听换弹完成事件，重置弹药显示 */
    public void ReloadOver()
    {
        weaponController.Reload();
        SetBulletUI();
    }

    /*设置武器弹药显示 */
    private void SetBulletUI()
    {
        if(weaponController.category == WeaponCategory.Knife)
        {
            bulletNumNowText.text = "∞";
            bulletNumTotalText.text = "∞";
            circleBulletImage.fillAmount = 1.0f;
        }
        else
        {
            bulletNumTotalText.text = weaponController.bulletNum.ToString();
            bulletNumNowText.text = weaponController.bulletNumNow.ToString();
            circleBulletImage.fillAmount = (float)weaponController.bulletNumNow / weaponController.bulletNum;
        }
    }

    /*设置武器信息显示 */
    private void SetWeaponInfoUI()
    {
        switch(weaponController.category)
        {
            case WeaponCategory.Knife:
                weaponInfoText.text = "刀子";
                break;
            case WeaponCategory.Infantry:
                weaponInfoText.text = "步枪";
                break;
            case WeaponCategory.Handgun:
                weaponInfoText.text = "手枪";
                break;
            case WeaponCategory.Heavy:
                weaponInfoText.text = "重武器";
                break;
        }
        switch(weaponController.shootMode)
        {
            case ShootMode.Auto:
                weaponShootInfoText.text = "自动";
                break;
            case ShootMode.Single:
                if(weaponController.category != WeaponCategory.Knife) weaponShootInfoText.text = "单发";
                else weaponShootInfoText.text = "";
                break;
        }
        if(weaponController.canChangeMode) shootModePromptText.gameObject.SetActive(true);
        else shootModePromptText.gameObject.SetActive(false);
        if(weaponController.category == WeaponCategory.Knife)
        {
            weaponBulletSum.text = "";
        }
        else
        {
            weaponBulletSum.text = "剩余弹药：" + weaponController.bulletSum;
        }
        bulletSumLast = weaponController.bulletSum;
    }

    /*结束游戏 */
    public void OverGame()
    {
        Debug.Log("OVer");
        isDead = true;
        promptText.gameObject.SetActive(true);
        promptText.text = "同步中";
        int score = int.Parse(scoreText.text);
        Global.killSum += score;
        if(aliveTime > Global.maxAliveTime) Global.maxAliveTime = aliveTime;
        ByteBuilder builder = new ByteBuilder();
        builder.Add(System.BitConverter.GetBytes(Network.MATCH_INFO));
        builder.Add(System.BitConverter.GetBytes(Global.killSum));
        builder.Add(System.BitConverter.GetBytes((double)aliveTime));   //double
        builder.Add(System.BitConverter.GetBytes(Global.userName.Length));
        builder.Add(System.Text.Encoding.ASCII.GetBytes(Global.userName));
        Global.network.Send(builder.GetByes());
        StartCoroutine(TimeOut());
    }

    IEnumerator TimeOut()
    {
        float t = 0f;
        while(!syncOver)
        {
            t += Time.deltaTime;
            if(t >= timeOut) break;
            yield return null;
        }
        if(syncOver)
        {
            promptText.text = "同步完成，即将返回主菜单";
        }
        else
        {
            promptText.text = "网络超时，即将返回主菜单";
        }
        yield return new WaitForSeconds(5);

        promptText.text = "";

        transform.position = startPositon;
        transform.rotation = startRotation;
        sightTargetPoint = transform.forward * 5;
        // playerController.health = playerController.MaxHealth;
        gun = null;
        knife.SetActive(true);
        weaponNow = knife;
        weaponController = weaponNow.GetComponent<WeaponController>();

        playerController.Restart();
        healthTransform.localScale = Vector3.one;
        isDead = false;
        isCrouching = false;

        scoreText.text = "0";
        Camera.main.GetComponent<CameraController>().ToMenu();
    }

    public void StartGame()
    {
        StartCoroutine("StartTime");
    }

    IEnumerator StartTime()
    {
        aliveTime = 0f;
        while(true)
        {
            aliveTime += Time.deltaTime;
            string t = TimeSpan.FromSeconds(aliveTime).ToString();
            if(t.Length > 10) timeText.text = t.Substring(0, 10);
            else timeText.text = t;
            // timeText.text = TimeSpan.FromSeconds(aliveTime).ToString();
            yield return null;
        }
    }

    public void OnGameExit()
    {
        Application.Quit();
    }

    public void ResumeGame()
    {
        EscCanvas.SetActive(false);
        Time.timeScale = 1;
        Cursor.visible = false;
    }
}
