using UnityEngine;

public class LookAt : MonoBehaviour
{
    private Camera _cam;

    private void Start()
    {
        _cam = Camera.main;
    }

    private void Update()
    {
        var rot = Quaternion.LookRotation(_cam.transform.position - transform.position).eulerAngles;
        rot.y = rot.z = 0;
        rot.x *= -1;
        transform.rotation = Quaternion.Euler(rot);
    }
}