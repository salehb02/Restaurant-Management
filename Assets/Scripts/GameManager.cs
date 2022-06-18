using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    [Header("Main Config")]
    public GameObject spawnPoint;
    public GameObject waitPosition;
    public GameObject[] exitPoints;
    public Vector2 waitPositionOffsetX;
    public Vector2 waitPositionOffsetZ;

    [Header("Customers Config")]
    public Customer[] customers;
    public Vector2 customerGenerateTime = new Vector2(4, 13);
    [Range(0, 1)] public float tableNumberFilterChance = 0.3f;
    [Range(0, 1)] public float coupleFamilyChance = 0.5f;
    [Range(0, 1)] public float tripleFamilyChance = 0.25f;
    [Range(0, 1)] public float quadrupleFamilyChance = 0.1f;
    private float _currentGeneratorTimer;
    private float _currentGeneratorDelay;

    [Space(2)]
    [Header("Table")]
    public GameObject tablesParent;
    public GameObject[] foodPrefabs;
    private List<Table> _currentTables = new List<Table>();
    private List<PurchasableTable> _purchasableTables = new List<PurchasableTable>();

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
    private Camera _camera;
    private List<Customer> _currentWaiters = new List<Customer>();

    public const string PLAYER_MONEY = "PLAYER_MONEY";

    private void Start()
    {
        _camera = Camera.main;

        GetTables();
        GetPurchasableTables();

        UpdateMoneyText();
    }

    private void Update()
    {
        CustomerGeneratorTimer();

        if (Input.GetMouseButtonDown(0))
        {
            if (Physics.Raycast(_camera.ScreenPointToRay(Input.mousePosition), out var hit, Mathf.Infinity))
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

        var initPosition = spawnPoint.transform.position + new Vector3(Random.Range(waitPositionOffsetX.x, waitPositionOffsetX.y), 0, 0);

        foreach (var waiter in _currentWaiters)
        {
            if (Vector3.Distance(initPosition, waiter.transform.position) < 1f)
            {
                CustomerGenerator();
                return;
            }
        }

        var customer = Instantiate(customers[Random.Range(0, customers.Length)], initPosition, Quaternion.identity, transform);
        var destinationPos = new Vector3(initPosition.x, waitPosition.transform.position.y, waitPosition.transform.position.z + Random.Range(waitPositionOffsetZ.x, waitPositionOffsetZ.y));
        var exitPoint = GetExitPoint().transform.position;

        // Load and initialize followers
        var followersNumber = 0;

        if (Random.value <= coupleFamilyChance)
        {
            followersNumber = 1;
        }
        else if (Random.value <= tripleFamilyChance)
        {
            followersNumber = 2;
        }
        else if (Random.value <= quadrupleFamilyChance)
        {
            followersNumber = 3;
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

        // Table number filter
        if (Random.value <= tableNumberFilterChance)
            customer.SetTableNumberFilter(_currentTables[Random.Range(0, _currentTables.Count)].TableNumber);

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
        _currentTables = FindObjectsOfType<Table>().OrderBy(x => x.transform.GetSiblingIndex()).ToList();

        for (int i = 0; i < _currentTables.Count; i++)
        {
            _currentTables[i].SetTableNumber(i + 1);
        }
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