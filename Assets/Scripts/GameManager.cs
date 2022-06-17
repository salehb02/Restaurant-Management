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
    private float _currentGeneratorTimer;
    private float _currentGeneratorDelay;

    [Space(2)]
    [Header("Table")]
    public GameObject[] foodPrefabs;
    private List<Table> _currentTables = new List<Table>();

    [Space(2)]
    [Header("Outlines Config")]
    public Color okayOutlineColor;
    public Color errorOutlineColor;

    public Gradient timerFillGradient;

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

        customer.Init();
        customer.MoveToLocation(destinationPos);
        customer.SetExitPosition(GetExitPoint().transform.position);
        _currentWaiters.Add(customer);
    }

    public void RemoveFromWaiters(Customer customer) => _currentWaiters.Remove(customer);

    public GameObject GetExitPoint() => exitPoints[Random.Range(0, exitPoints.Length)];

    private void GetTables()
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
        moneyText.text = "$" + playerMoney.ToString(playerMoney == 0 ? null : "#,#");
    }

    public void AddMoney(int amount)
    {
        SaveManager.instance.Set(PLAYER_MONEY, SaveManager.instance.Get<int>(PLAYER_MONEY) + amount);
        UpdateMoneyText();
    }

    public void UseMoney(int amount)
    {
        SaveManager.instance.Set(PLAYER_MONEY, SaveManager.instance.Get<int>(PLAYER_MONEY) - amount);
        UpdateMoneyText();
    }

    public bool HasMoney(int amount) => SaveManager.instance.Get<int>(PLAYER_MONEY) >= amount ? true : false;
}