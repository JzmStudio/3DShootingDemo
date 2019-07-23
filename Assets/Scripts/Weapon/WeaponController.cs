using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//武器类别
public enum WeaponCategory
{
    Handgun, Heavy, Infantry, Knife
}

//武器的发射模式
public enum ShootMode
{
    //单发模式与连发模式
    Single, Auto
}

public class WeaponController : MonoBehaviour
{
    public WeaponCategory category;
    public int bulletNum = 30;   //每个弹夹的子弹容量
    public int bulletNumNow;
    public int bulletSum = 240;   //总共的子弹数量
    public int bulletMaxHold = 1000;    //最多的子弹数量
    public int damage = 25;  //每颗子弹的伤害
    public bool canChangeMode = false;  //是否能切换武器发射模式
    public ShootMode shootMode = ShootMode.Single; //武器的发射模式
    public float shootMinDeltaInSingle = 1f;    //单发模式时从一次射击到下次射击的最短间隔（s）
    public float shootMinDeltaInAuto = 0f;  //连发模式下每次射击的最短间隔（s），无此模式的枪械可不管
    public float maxAttackDis = Mathf.Infinity; //最大射击距离
    public ParticleSystem shootPartical;    //射击枪口的效果
    public Light shootLight;   //射击时发出的光节点
    /*音效相关 */
    public AudioClip shootClip = null;
    public AudioClip reloadClip = null;
    public AudioClip dryfireClip = null;    //空弹夹音效
    public AudioClip changeModeClip = null;    //武器换攻击模式的音效

    private float shootMinDelta;    //在当前模式下，从一次射击到下次射击的最短间隔（s）
    private AudioSource audioSource;    //音效播放组件

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        bulletNumNow = bulletNum;   //重置武器的初始子弹为弹夹最大容量
    }

    /*获取当前模式下的最短射击时间 */
    public float GetShootMinDelta()
    {
        if(shootMode == ShootMode.Single) return shootMinDeltaInSingle;
        else return shootMinDeltaInAuto;
    }

    /*播放一次开火动画，若能发出子弹为true，不能为false */
    public bool Shoot()
    {
        //若无弹药
        if(bulletNumNow == 0)
        {
            if(dryfireClip != null && !audioSource.isPlaying)
            {
                audioSource.clip = dryfireClip;
                audioSource.Play();
            }
            return false;
        }
        //音效
        if(shootClip != null)
        {
            audioSource.clip = shootClip;
            audioSource.Play();
        }
        //枪口火焰
        if(category != WeaponCategory.Knife)
        {
            //播放射击枪口效果
            StopAllCoroutines();
            shootPartical.Play();
            shootLight.enabled = true;
            StartCoroutine(ResetShoot());
        }
        //弹药数
        if(category != WeaponCategory.Knife) bulletNumNow -= 1;
        if(bulletNumNow < 0) bulletNumNow = 0;

        // if((shootPoint - shootStart).magnitude <= maxAttackDis)
        // {
        //     return true;
        // }
        // return false;
        return true;
    }

    IEnumerator ResetShoot()
    {
        yield return new WaitForSeconds(0.1f);
        shootLight.enabled = false;
    }

    /*此函数应在装弹动作接近完成时调用，将播放装弹动画并将弹药补满 */
    public void Reload()
    {
        int left = bulletSum + bulletNumNow - bulletNum;
        if(left > 0)
        {
            bulletNumNow = bulletNum;
            bulletSum = left;
        }
        else
        {
            bulletNumNow = bulletSum;
            bulletSum = 0;
        }
        //播放换弹音效
        if(reloadClip != null)
        {
            audioSource.clip = reloadClip;
            audioSource.Play();
        }
    }

    public bool ChangeShootMode()
    {
        if(!canChangeMode) return false;
        if(!audioSource.isPlaying)
        {
            audioSource.clip = changeModeClip;
            audioSource.Play();
            shootMode = shootMode == ShootMode.Single? ShootMode.Auto : ShootMode.Single;
            return true;
        }
        return false;
    }
}
