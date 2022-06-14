using UnityEngine;

public class GameManager : MonoBehaviour
{
    private Customer selectedTarget;
    private Camera _camera;
    private string hitTransformName;

    private void Start()
    {
        _camera = Camera.main;
    }

    private void Update()
    {
        if (selectedTarget)
        {
            if(Input.GetMouseButtonDown(1))
            {
                selectedTarget.UnSelectAsTarget();
                selectedTarget = null;
            }

            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (Physics.Raycast(_camera.ScreenPointToRay(Input.mousePosition), out var hit, Mathf.Infinity))
            {
                hitTransformName = hit.collider.name;

                if (hit.transform.GetComponentInParent<Customer>())
                {
                    selectedTarget = hit.collider.GetComponentInParent<Customer>();
                    selectedTarget.SelectAsTarget();
                }
            }
        }
    }
}