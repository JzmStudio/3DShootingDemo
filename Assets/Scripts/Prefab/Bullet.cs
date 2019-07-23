using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float rotationSpeed;
    public Vector3 rotationAngle;

    // Update is called once per frame
    void Update()
    {
        transform.Rotate(rotationAngle * rotationSpeed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        //若是人
        if(other.gameObject.layer == 11)
        {
            if(other.gameObject.GetComponent<HumanAnimController>().OnGetBullet(100)) gameObject.SetActive(false);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    private void OnTriggerStay(Collider other) {
        //若是人
        if(other.gameObject.layer == 11)
        {
            if(other.gameObject.GetComponent<HumanAnimController>().OnGetBullet(100)) gameObject.SetActive(false);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
