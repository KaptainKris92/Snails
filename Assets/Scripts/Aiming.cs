using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Aiming : MonoBehaviour
{   
    private Camera mainCam;
    private Vector3 mousePos; 
    public GameObject bullet;
    public Transform bulletTransform;
    public bool canFire = true;
    private float timer;
    public float timeBetweenFiring = 0.5f;


    // Start is called before the first frame update
    void Start()
    {
        mainCam = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
    }

    // Update is called once per frame
    void Update()
    {
        mousePos = mainCam.ScreenToWorldPoint(Input.mousePosition);

        Vector3 rotation = mousePos - transform.position;        

        float rotZ = Mathf.Atan2(rotation.y, rotation.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, rotZ);

        // If bullet collides with something, then set canFire to true. For now, this is just a timer.
        if(!canFire){
            timer += Time.deltaTime;
            if(timer > timeBetweenFiring){
                canFire = true;
            }

        }

        if (Input.GetMouseButton(0) && canFire){
            canFire = false;
            Instantiate(bullet, bulletTransform.position, Quaternion.identity);
        }
    }
}
