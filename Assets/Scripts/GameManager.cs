using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Main Config")]
    public GameObject spawnPoint;
    public GameObject waitPosition;
    public Vector2 waitPositionOffsetX;
    public Vector2 waitPositionOffsetZ;

    [Header("Customers Config")]
    public Customer[] customers;
    public Vector2 customerGenerateTime = new Vector2(4, 13);
    private float _currentGeneratorTimer;
    private float _currentGeneratorDelay;

    [Space(2)]
    [Header("Outlines Config")]
    public Color okayOutlineColor;
    public Color errorOutlineColor;

    private Customer selectedTarget;
    private Camera _camera;

    private void Start()
    {
        _camera = Camera.main;
    }

    private void Update()
    {
        CustomerGenerator();

        if (selectedTarget)
        {
            if (Input.GetMouseButtonDown(1))
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
                var customer = hit.transform.GetComponentInParent<Customer>();

                if (customer)
                {
                    if (!customer.IsSelectable)
                        return;

                    if (customer.IsBusy)
                    {
                        customer.BusyWarn();
                        return;
                    }

                    selectedTarget = customer;
                    selectedTarget.SelectAsTarget();
                }
            }
        }
    }

    private void CustomerGenerator()
    {
        _currentGeneratorTimer += Time.deltaTime;

        if (_currentGeneratorTimer > _currentGeneratorDelay)
        {
            _currentGeneratorDelay = Random.Range(customerGenerateTime.x, customerGenerateTime.y);
            _currentGeneratorTimer = 0;

            var initPosition = spawnPoint.transform.position + new Vector3(Random.Range(waitPositionOffsetX.x, waitPositionOffsetX.y), 0, 0);

            var customer = Instantiate(customers[Random.Range(0, customers.Length)], initPosition, Quaternion.identity, transform);
            var destinationPos = new Vector3(initPosition.x, waitPosition.transform.position.y, waitPosition.transform.position.z + Random.Range(waitPositionOffsetZ.x, waitPositionOffsetZ.y));

            customer.Init();
            customer.MoveToLocation(destinationPos);
            customer.SetExitOnBoredomPosition(initPosition);
        }
    }
}