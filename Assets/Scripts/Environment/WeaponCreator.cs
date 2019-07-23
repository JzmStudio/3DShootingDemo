using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponCreator : MonoBehaviour
{
    public GameObject [] handguns;
    public GameObject [] heavies;
    public GameObject [] infantries;
    public float randomRange = 15f;
    public int minHandgunNum = 2;
    public int minHeavyNum = 1;
    public int minInfantryNum = 2;
    public int maxHandgunNum = 5;
    public int maxHeavyNum = 2;
    public int maxInfantryNum = 8;

    private int handgunNum;
    private int heavyNum;
    private int infantryNum;

    private LinkedList<GameObject> weapons = new LinkedList<GameObject>();

    private HumanAnimController playerController;

    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("STRAT");
        playerController = Global.player.GetComponent<HumanAnimController>();

        CreateWeapon();
    }

    private void OnDisable() {
        Debug.Log("OnDis");
        foreach (var obj in weapons)
        {
            GameObject.Destroy(obj);
        }
        weapons.Clear();
    }

    void CreateWeapon()
    {
        if(weapons.Count != 0) return;

        handgunNum = Random.Range(minHandgunNum, maxHandgunNum + 1);
        heavyNum = Random.Range(minHeavyNum, maxHeavyNum + 1);
        infantryNum = Random.Range(minInfantryNum, maxInfantryNum + 1);

        //随机手枪分布
        for(int n = 0; n < handgunNum; ++n)
        {
            GameObject weapon = Instantiate(
                handguns[Random.Range(0, handguns.Length)], 
                new Vector3(Random.Range(-randomRange, randomRange), transform.position.y, Random.Range(-randomRange, randomRange)),
                Quaternion.identity
            );
            weapon.transform.SetParent(transform, false);
            MeshCollider collider = weapon.AddComponent<MeshCollider>() as MeshCollider;
            collider.convex = true;
            Rigidbody rigidbody = weapon.AddComponent<Rigidbody>() as Rigidbody;
            rigidbody.useGravity = true;
            // collider.isTrigger = true;  //设置武器可穿过
            weapons.AddLast(weapon);
        }
        //随机重武器分布
        for(int n = 0; n < handgunNum; ++n)
        {
            GameObject weapon = Instantiate(
                heavies[Random.Range(0, heavies.Length)], 
                new Vector3(Random.Range(-randomRange, randomRange), transform.position.y, Random.Range(-randomRange, randomRange)),
                Quaternion.identity
            );
            weapon.transform.SetParent(transform, false);
            MeshCollider collider = weapon.AddComponent<MeshCollider>() as MeshCollider;
            collider.convex = true;
            Rigidbody rigidbody = weapon.AddComponent<Rigidbody>() as Rigidbody;
            rigidbody.useGravity = true;
            // collider.isTrigger = true;  //设置武器可穿过
            weapons.AddLast(weapon);
        }
        //随机步枪分布
        for(int n = 0; n < handgunNum; ++n)
        {
            GameObject weapon = Instantiate(
                infantries[Random.Range(0, infantries.Length)], 
                new Vector3(Random.Range(-randomRange, randomRange), transform.position.y, Random.Range(-randomRange, randomRange)),
                Quaternion.identity
            );
            weapon.transform.SetParent(transform, false);
            MeshCollider collider = weapon.AddComponent<MeshCollider>() as MeshCollider;
            collider.convex = true;
            Rigidbody rigidbody = weapon.AddComponent<Rigidbody>() as Rigidbody;
            rigidbody.useGravity = true;
            // collider.isTrigger = true;  //设置武器可穿过
            weapons.AddLast(weapon);
        }
    }

    private void OnEnable() {
        Debug.Log("OnEn");
        CreateWeapon();
    }
}
