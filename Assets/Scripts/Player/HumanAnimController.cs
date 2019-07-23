using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface CanGetSight {
    Vector3 GetSightPoint();
}

/**
 * 此类方法仅被InputController调用，移动方法仅对单帧有效
 */
public class HumanAnimController : MonoBehaviour
{
    public GameObject hand;
    public GameObject weaponNow = null;
    public float jumpHeight = 0.6f;
    /*事件监听 */
    public delegate void AttackOverListener();
    public delegate void ReloadOverListener();
    /*粒子 */
    public ParticleSystem dustPartical; //跑步或走路发出的尘土
    public GameObject damageParticalObject;   //被攻击时出现的效果烟雾
    /*人物血量 */
    public float MaxHealth = 100;
    public float health = 100;   //人物当前的血量

    private Animator anim;
    private CharacterController characterController;
    private bool isCrouching = false;
    private int upperLayerIndex;
    private const float JumpMinDelta = 0.75f;   //跳跃最短间隔
    private bool canJump = true;
    private CanGetSight sightGetter = null;    //从此对象中获取人物脸部与手部的朝向
    /*攻击相关 */
    private bool canAttack = true;  //表示单次攻击动作是否结束
    private WeaponController weaponController;  //武器控制器
    private bool canReload = true;  
    /*回调 */
    private AttackOverListener attackOverListeners; //攻击回调
    private ReloadOverListener reloadOverListeners; //换弹回调
    /*声音 */
    private AudioSource audioSource;

    private void Awake()
    {
        anim = GetComponent<Animator>();
        characterController = GetComponent<CharacterController>();
        upperLayerIndex = anim.GetLayerIndex("Upper");
        audioSource = GetComponent<AudioSource>();
    }

    // Start is called before the first frame update
    void Start()
    {
        health = MaxHealth;
    }

    // Update is called once per frame
    void Update()
    {
    }

    /*武器转换为刀 */
    public GameObject ToKnife(GameObject weapon)
    {
        weapon.transform.localPosition = Vector3.zero;
        weapon.transform.localRotation = Quaternion.identity;
        weapon.transform.SetParent(hand.transform, false);
        
        weaponController = weapon.GetComponent<WeaponController>();

        GameObject weaponLast = weaponNow;
        weaponNow = weapon;
        anim.SetTrigger("ToKnife");
        return weaponLast;
    }

    /*武器转换为手枪 */
    public GameObject ToHandgun(GameObject weapon)
    {
        weapon.transform.localPosition = Vector3.zero;
        weapon.transform.localRotation = Quaternion.identity;
        weapon.transform.SetParent(hand.transform, false);

        weaponController = weapon.GetComponent<WeaponController>();

        GameObject weaponLast = weaponNow;
        weaponNow = weapon;
        anim.SetTrigger("ToHandgun");
        return weaponLast;
    }

    /*武器转换为重型武器（加特林） */
    public GameObject ToHeavy(GameObject weapon)
    {
        weapon.transform.localPosition = Vector3.zero;
        weapon.transform.localRotation = Quaternion.identity;
        weapon.transform.SetParent(hand.transform, false);

        weaponController = weapon.GetComponent<WeaponController>();

        GameObject weaponLast = weaponNow;
        weaponNow = weapon;
        anim.SetTrigger("ToHeavy");
        return weaponLast;
    }

    /*武器转换为步枪 */
    public GameObject ToInfantry(GameObject weapon)
    {
        weapon.transform.localPosition = Vector3.zero;
        weapon.transform.localRotation = Quaternion.identity;
        weapon.transform.SetParent(hand.transform, false);

        weaponController = weapon.GetComponent<WeaponController>();

        GameObject weaponLast = weaponNow;
        weaponNow = weapon;
        anim.SetTrigger("ToInfantry");
        return weaponLast;
    }

    /*设置人物状态为蹲姿 */
    public void SetCrouching()
    {
        isCrouching = true;
        anim.SetBool("IsCrouching", true);
    }

    /*设置人物状态为站姿 */
    public void SetStanding()
    {
        isCrouching = false;
        anim.SetBool("IsCrouching", false);
    }

    /*verticalSpeed > 0 前，horizonSpeed > 0 右 */
    public void Walk(float verticalSpeed, float horizontalSpeed)
    {
        //尘土
        if(!isCrouching) dustPartical.Play();

        characterController.SimpleMove(transform.forward * verticalSpeed + transform.right * horizontalSpeed);
        if(isCrouching)
        {
            anim.SetBool("IsWalking", false);
            anim.SetBool("IsRunning", false);
            anim.SetBool("IsCrouchMoving", true);
            // anim.SetBool("IsCrouching", false);
        }
        else
        {
            anim.SetBool("IsWalking", true);
            anim.SetBool("IsRunning", false);
            anim.SetBool("IsCrouchMoving", false);
            // anim.SetBool("IsCrouching", false);
        }

        if(verticalSpeed < 0) anim.SetInteger("Vertical", -1);
        else if(verticalSpeed > 0) anim.SetInteger("Vertical", 1);
        else anim.SetInteger("Vertical", 0);

        if(horizontalSpeed > 0) anim.SetInteger("Horizontal", 1);
        else if(horizontalSpeed < 0) anim.SetInteger("Horizontal", -1);
        else anim.SetInteger("Horizontal", 0);
    }

    public void PlayRunAnim()
    {
        anim.SetBool("IsRunning", true);
        anim.SetInteger("Vertical", 1);
    }

    public void Run(float verticalSpeed, float horizontalSpeed)
    {
        //尘土
        if(!isCrouching) dustPartical.Play();

        characterController.SimpleMove(transform.forward * verticalSpeed + transform.right * horizontalSpeed);
        if(isCrouching)
        {
            anim.SetBool("IsWalking", false);
            anim.SetBool("IsRunning", false);
            anim.SetBool("IsCrouchMoving", true);
            // anim.SetBool("IsCrouching", false);
        }
        else
        {
            anim.SetBool("IsWalking", false);
            anim.SetBool("IsRunning", true);
            anim.SetBool("IsCrouchMoving", false);
            // anim.SetBool("IsCrouching", false);
        }

        if(verticalSpeed < 0) anim.SetInteger("Vertical", -1);
        else if(verticalSpeed > 0) anim.SetInteger("Vertical", 1);
        else anim.SetInteger("Vertical", 0);

        if(horizontalSpeed > 0) anim.SetInteger("Horizontal", 1);
        else if(horizontalSpeed < 0) anim.SetInteger("Horizontal", -1);
        else anim.SetInteger("Horizontal", 0);
    }

    public void Idle()
    {
        //尘土
        dustPartical.Stop();

        anim.SetBool("IsWalking", false);
        anim.SetBool("IsRunning", false);
        anim.SetBool("IsCrouchMoving", false);
        anim.SetInteger("Vertical", 0);
        anim.SetInteger("Horizontal", 0);
    }

    public void Jump()
    {
        //尘土
        dustPartical.Stop();

        if(Input.GetAxisRaw("Jump") == 1 && canJump)
        {
            anim.SetTrigger("ToJump");
            anim.SetBool("IsJumping", true);
            canJump = false;
            StartCoroutine(resetJump());
        }
    }

    IEnumerator resetJump()
    {
        float startY = transform.position.y;
        float delta = 0f;
        float halfMin = JumpMinDelta / 2;
        float speed = (jumpHeight - startY) / halfMin;
        //向上跳
        while(delta < halfMin)
        {
            delta += Time.deltaTime;
            characterController.Move(new Vector3(0f, speed * Time.deltaTime, 0f));
            yield return null;
        }
        //向下跳
        delta = 0f;
        while(delta < halfMin)
        {
            delta += Time.deltaTime;
            characterController.SimpleMove(new Vector3(0f, -(speed * Time.deltaTime), 0f));
            yield return null;
        }

        transform.position.Set(0f, startY, 0f);
        // yield return new WaitForSeconds(JumpMinDelta);
        canJump = true;
        anim.SetBool("IsJumping", false);
    }

    private void OnAnimatorIK(int layerIndex) {
        if(sightGetter != null)
        {
            Vector3 point = sightGetter.GetSightPoint();
            anim.SetLookAtPosition(point);
            if(health > 0) anim.SetLookAtWeight(0.9f, 1f);
            else anim.SetLookAtWeight(0f);
        }
    }

    /*设置人物面部与手部的朝向 */
    public void SetSightGetter(CanGetSight sightGetter) {
        this.sightGetter = sightGetter;
    }

    /*可以攻击返回ture */
    public bool Attack1()
    {
        if(weaponNow != null && canAttack && canReload) //不在换弹时间才能攻击
        {
            if(weaponController.bulletNumNow != 0)
            {
                anim.SetTrigger("Attack1");
                canAttack = false;
            }
            return true;
        }
        return false;
    }

    /*可以攻击返回ture */
    public bool Attack2()
    {
        if(weaponNow != null && canAttack && canReload) //不在换弹时间才能攻击
        {
            if(weaponController.bulletNumNow != 0)
            {
                anim.SetTrigger("Attack2");
                canAttack = false;
            }
            return true;
        }
        return false;
    }

    /*攻击动画结束回调 */
    public void AttackAnimOver()
    {
        //进行攻击结束回调
        if(attackOverListeners != null) attackOverListeners();

        float minDelta = weaponController.GetShootMinDelta();
        if(minDelta == 0f)
        {
            canAttack = true;
        }
        else
        {
            StartCoroutine(SetAttack(minDelta));
        }
    }

    IEnumerator SetAttack(float deltaTime)
    {
        yield return new WaitForSeconds(deltaTime);
        canAttack = true;
    }

    public void DelegateAttackOver(AttackOverListener listener)
    {
        attackOverListeners += listener;
    }

    /*换弹，若可换弹则true */
    public bool Reload()
    {
        if(weaponController.category != WeaponCategory.Knife && canReload && weaponController.bulletSum != 0)
        {
            anim.SetTrigger("Reload");
            canReload = false;
            return true;
        }
        return false;
    }

    /*添加换弹结束回调 */
    public void DelegateReloadOver(ReloadOverListener listener)
    {
        reloadOverListeners += listener;
    }

    /*装弹动画结束回调 */
    public void ReloadOver()
    {
        // Debug.Log("human Reload Over");
        canReload = true;
        //换弹结束回调
        if(reloadOverListeners != null) reloadOverListeners();
    }

    /*被攻击动画 */
    public void Damaged(float damageValue, Vector3 hitPoint)
    {
        if(health == 0) return;
        
        if(hitPoint.y - transform.position.y > 1.95) health = 0;  //爆头
        else health -= damageValue; //普通伤害
        if(health <= 0f)
        {
            Idle();
            health = 0f;
            //Audio
            audioSource.Play();
            //播放死亡动画
            anim.SetBool("IsDead", true);
            Vector3 p = transform.InverseTransformPoint(hitPoint);
            if(p.z >= 0)    //从前方来的子弹则向后倒下
            {
                anim.SetTrigger("DieFromFront");
                anim.Play("infantry_death_front", 0);
                anim.Play("infantry_death_front", 1);
            }
            else
            {
                anim.SetTrigger("DieFromBack");
                anim.Play("infantry_death_back", 0);
                anim.Play("infantry_death_back", 1);
            }

            // anim.SetLookAtWeight(0f);   //取消视线显示
        }
        else
        {
            anim.SetTrigger("TakeDamage");
        }
        // Debug.Log(health);
        
        //受伤显示
        GameObject damage = Instantiate(damageParticalObject, hitPoint, Quaternion.identity);
        damage.GetComponent<ParticleSystem>().Play();
        GameObject.Destroy(damage, 0.5f);
    }

    /*受伤动画结束回调 */
    public void DamageOver()
    {
        /*重置攻击与换弹 */
        canAttack = true;
        canReload = true;
    }

    /*被治愈回调，若治愈成功则返回true */
    public bool OnHeal(float healValue)
    {
        if(health >= MaxHealth) return false;
        if(health <MaxHealth) health += healValue;
        if(healValue > MaxHealth) health = MaxHealth;
        if(healValue <= 0) health = 0;
        return true;
    }

    /*补充子弹 */
    public bool OnGetBullet(int bulletNum)
    {
        if(weaponController.category != WeaponCategory.Knife && weaponController.bulletSum < weaponController.bulletMaxHold)
        {
            weaponController.bulletSum += bulletNum;
            return true;
        }
        else
        {
            return false;
        }
    }

    public void Restart()
    {
        health = MaxHealth;
        anim.SetBool("IsDead", false);
        Idle();
        anim.Play("knife_combat_idle", 0);
        anim.Play("knife_combat_idle", 1);
        // anim.SetLookAtWeight(0.9f, 1f);
    }
}
