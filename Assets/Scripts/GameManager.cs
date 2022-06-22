using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    [Header("Main Config")]
    public GameObject spawnPoint;
    public GameObject[] exitPoints;
    public Camera camera;
    public bool useTouch;
    private float _currentFOV;

    [Space(2)]
    [Header("Free Wait Position")]
    public bool useFreeWaitPosition = true;
    public GameObject waitPosition;
    public Vector2 waitPositionOffsetX;
    public Vector2 waitPositionOffsetZ;

    [Space(2)]
    [Header("Gate Wait Position")]
    public bool useGateWaitPosition = false;
    public Gate[] gateWaitPoints;
    private List<Gate> _availableGates = new List<Gate>();

    [Space(2)]
    [Header("Customers Config")]
    public Customer[] customers;
    public Vector2 customerGenerateTime = new Vector2(4, 13);
    [Range(0, 1)] public float tableNumberFilterChance = 0.3f;
    [Range(0, 1)] public float tableReserveFilterChance = 0.2f;
    [Range(0, 1)] public float tableFoodFilterChance = 0.2f;
    [Range(0, 1)] public float coupleFamilyChance = 0.5f;
    [Range(0, 1)] public float tripleFamilyChance = 0.25f;
    [Range(0, 1)] public float quadrupleFamilyChance = 0.1f;
    private float _currentGeneratorTimer;
    private float _currentGeneratorDelay;

    [Space(2)]
    [Header("Table")]
    public GameObject tablesParent;
    public List<Table> Tables { get; private set; } = new List<Table>();
    private List<PurchasableTable> _purchasableTables = new List<PurchasableTable>();

    [Space(2)]
    [Header("Global Filter Settings")]
    public FoodsFilterEnum[] foodFilters;

    [Space(2)]
    [Header("Outlines Config")]
    public Color okayOutlineColor;
    public Color errorOutlineColor;

    public Gradient timerFillGradient;
    public Color tableTimeColor;

    [Space(2)]
    [Header("UI")]
    public TextMeshProUGUI moneyText;

    public Customer SelectedTarget { get; set; }
    private List<Customer> _currentWaiters = new List<Customer>();

    public const string PLAYER_MONEY = "PLAYER_MONEY";

    [System.Serializable]
    public class Gate
    {
        public GameObject gatePosition;
        public int hordeCount;
    }

    private void Start()
    {
        camera = Camera.main;
        _currentFOV = camera.fieldOfView;

        if (useGateWaitPosition)
            _availableGates = gateWaitPoints.ToList();

        GetTables();
        GetPurchasableTables();

        UpdateMoneyText();
    }

    private void Update()
    {
        // Customer generator
        CustomerGeneratorTimer();

        // Select customer
        if (useTouch)
        {
            if (Input.touchCount > 0)
            {
                var touch = Input.touches[0];

                if (Physics.Raycast(camera.ScreenPointToRay(touch.position), out var hit, Mathf.Infinity))
                    SelectCustomer(hit);
            }
        }
        else
        {
            if (Input.GetMouseButtonDown(0))
                if (Physics.Raycast(camera.ScreenPointToRay(Input.mousePosition), out var hit, Mathf.Infinity))
                    SelectCustomer(hit);
        }

        camera.fieldOfView = Mathf.Lerp(camera.fieldOfView,_currentFOV,Time.deltaTime * 2f);
    }

    private void SelectCustomer(RaycastHit hit)
    {
        var customer = hit.transform.GetComponentInParent<Customer>();

        if (customer)
        {
            if (!customer.IsSelectable)
                return;

            if (SelectedTarget != null)
                SelectedTarget.UnSelect();

            if (customer.IsFollower)
                SelectedTarget = customer.ToFollow;
            else
                SelectedTarget = customer;

            SelectedTarget.Select();
        }
    }

    private void CustomerGeneratorTimer()
    {
        _currentGeneratorTimer += Time.deltaTime;

        if (_currentGeneratorTimer > _currentGeneratorDelay)
        {
            CustomerGenerator();
        }
    }

    private void CustomerGenerator()
    {
        _currentGeneratorDelay = Random.Range(customerGenerateTime.x, customerGenerateTime.y);
        _currentGeneratorTimer = 0;

        var maxSits = GetMaxTablesSpace();

        var initPosition = new Vector3();
        var destinationPos = new Vector3();
        Gate gate = null;

        if (useGateWaitPosition)
        {
            var sizeFilteredGates = _availableGates.Where(x => x.hordeCount <= maxSits).ToList();

            if (sizeFilteredGates.Count == 0)
                return;

            gate = sizeFilteredGates[Random.Range(0, sizeFilteredGates.Count)];
            initPosition = new Vector3(gate.gatePosition.transform.position.x, gate.gatePosition.transform.position.y, spawnPoint.transform.position.z);
            destinationPos = gate.gatePosition.transform.position;

            _availableGates.Remove(gate);
        }

        if (useFreeWaitPosition)
        {
            initPosition = spawnPoint.transform.position + new Vector3(Random.Range(waitPositionOffsetX.x, waitPositionOffsetX.y), 0, 0);
            destinationPos = new Vector3(initPosition.x, waitPosition.transform.position.y, waitPosition.transform.position.z + Random.Range(waitPositionOffsetZ.x, waitPositionOffsetZ.y));
        }

        foreach (var waiter in _currentWaiters)
        {
            if (Vector3.Distance(initPosition, waiter.transform.position) < 1f)
            {
                CustomerGenerator();
                return;
            }
        }

        var customer = Instantiate(customers[Random.Range(0, customers.Length)], initPosition, Quaternion.identity, transform);
        var exitPoint = GetExitPoint().transform.position;

        // Load and initialize followers
        var followersNumber = 0;

        if (useFreeWaitPosition)
        {
            if (Random.value <= coupleFamilyChance && maxSits >= 2)
            {
                followersNumber = 1;
            }
            else if (Random.value <= tripleFamilyChance && maxSits >= 3)
            {
                followersNumber = 2;
            }
            else if (Random.value <= quadrupleFamilyChance && maxSits >= 4)
            {
                followersNumber = 3;
            }
        }

        if (useGateWaitPosition)
        {
            followersNumber = gate.hordeCount - 1;
        }

        customer.customerType = (Customer.CustomerType)(followersNumber + 1);

        var isLeftSideFill = false;
        var followersDistance = 0.7f;

        for (int i = 0; i < followersNumber; i++)
        {
            var offset = new Vector3();

            switch (customer.Followers.Count)
            {
                case 0:
                    offset = new Vector3(followersDistance, 0, 0);
                    offset.x *= Random.value <= 0.5f ? 1f : -1f;

                    isLeftSideFill = offset.x < 0;
                    break;
                case 1:
                    offset = new Vector3(followersDistance, 0, 0);
                    offset.x = isLeftSideFill ? 1 : -1;
                    break;
                case 2:
                    offset = new Vector3(0, 0, -followersDistance);
                    break;
                default:
                    break;
            }

            var follower = Instantiate(customers[Random.Range(0, customers.Length)], initPosition + offset, Quaternion.identity, transform);
            follower.Init();
            follower.FollowCustomer(customer, new Customer.Follower(follower, offset));
        }

        // Initialize customer
        customer.Init();
        customer.MoveToLocation(destinationPos);
        customer.SetExitPosition(exitPoint);

        if (useGateWaitPosition)
            customer.FilledGate = gate;

        var reserveTable = false;
        var numberFilter = false;

        // Table reserve filter
        if (Tables.Where(x => x.isReserved).ToList().Count > 0)
        {
            if (Random.value <= tableReserveFilterChance)
            {
                var suitableTable = Tables.Where(x => x.sitPositions.Length >= customer.Followers.Count + 1 && x.isReserved).ToList();

                if (suitableTable.Count > 0)
                {
                    customer.ReserveTable();
                    reserveTable = true;
                }
            }
        }

        // Table number filter
        if (Tables.Where(x => x.isNumbered).ToList().Count > 0 && reserveTable == false)
        {
            if (Random.value <= tableNumberFilterChance)
            {
                var suitableTable = Tables.Where(x => x.sitPositions.Length >= customer.Followers.Count + 1 && x.isNumbered).ToList();

                if (suitableTable.Count > 0)
                {
                    customer.SetTableNumberFilter(suitableTable[Random.Range(0, suitableTable.Count)].tableNumber);
                    numberFilter = true;
                }
            }
        }

        // Specific food filter
        if (Tables.Where(x => x.isFoodFiltered).ToList().Count > 0 && !numberFilter && !reserveTable)
        {
            if (Random.value <= tableFoodFilterChance)
            {
                var suitableTable = Tables.Where(x => x.sitPositions.Length >= customer.Followers.Count + 1 && x.isFoodFiltered).ToList();

                if (suitableTable.Count > 0)
                    customer.SetFoodFilter(suitableTable[Random.Range(0, suitableTable.Count)].foodType);
            }
        }

        _currentWaiters.Add(customer);
    }

    public void RemoveFromWaiters(Customer customer) => _currentWaiters.Remove(customer);

    public GameObject GetExitPoint() => exitPoints[Random.Range(0, exitPoints.Length)];

    private void GetPurchasableTables()
    {
        _purchasableTables = FindObjectsOfType<PurchasableTable>().OrderBy(x => x.transform.GetSiblingIndex()).ToList();
    }

    public void GetTables()
    {
        Tables = FindObjectsOfType<Table>().OrderBy(x => x.transform.GetSiblingIndex()).ToList();
    }

    public void AddAvailableGate(Gate gate)
    {
        _availableGates.Add(gate);
    }

    private int GetMaxTablesSpace()
    {
        var maxSits = 0;

        foreach(var table in Tables)
        {
            if(table.sitPositions.Length > maxSits)
                maxSits = table.sitPositions.Length;
        }

        return maxSits;
    }

    public void SetMaximumFOV(float fov)
    {
        _currentFOV = fov;
    }

    private void UpdateMoneyText()
    {
        var playerMoney = SaveManager.instance.Get<int>(PLAYER_MONEY);
        moneyText.text = "<sprite index=0>" + playerMoney.ToString(playerMoney == 0 ? null : "#,#");
    }

    public void AddMoney(int amount)
    {
        SaveManager.instance.Set(PLAYER_MONEY, SaveManager.instance.Get<int>(PLAYER_MONEY) + amount);
        UpdateMoneyText();

        foreach (var purchasableTable in _purchasableTables)
            purchasableTable.UpdateMoneyText();
    }

    public void UseMoney(int amount)
    {
        if (!HasMoney(amount))
            return;

        SaveManager.instance.Set(PLAYER_MONEY, SaveManager.instance.Get<int>(PLAYER_MONEY) - amount);
        UpdateMoneyText();

        foreach (var purchasableTable in _purchasableTables)
            purchasableTable.UpdateMoneyText();
    }

    public bool HasMoney(int amount) => SaveManager.instance.Get<int>(PLAYER_MONEY) >= amount ? true : false;
}