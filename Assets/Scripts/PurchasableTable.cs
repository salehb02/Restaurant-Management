using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PurchasableTable : MonoBehaviour
{
    public string id = "TABLE_LEVEL1_1";
    public int price;
    public GameObject table;
    public bool alreadyPurchased;
    public PurchasableTable[] nearbyPurchasableTable;
    public bool isLastPurchasableTable = false;
    public float maxFOVOnPurchase = 80;

    [Space(2)]
    [Header("UI")]
    public GameObject purchasableTableUI;
    public Button unlockByMoneyButton;
    public TextMeshProUGUI moneyText;
    public Button unlockByAdButton;
    [Space(2)]
    public Color canPurchaseMoneyTextColor;
    public Color cantPurchaseMoneyTextColor;

    private GameManager _gameManager;

    private void Start()
    {
        _gameManager = FindObjectOfType<GameManager>();
        
        unlockByMoneyButton.onClick.AddListener(PurchaseTableByMoney);
        unlockByAdButton.onClick.AddListener(PurchaseTableByAd);

        if(alreadyPurchased)
            SaveManager.instance.Set(id, true);

        CheckIfPurchased();
        UpdateMoneyText();
    }

    public void UpdateMoneyText()
    {
        moneyText.text = "<sprite index=0>" + price;

        if (_gameManager.HasMoney(price))
            moneyText.color = canPurchaseMoneyTextColor;
        else
            moneyText.color = cantPurchaseMoneyTextColor;
    }

    public bool IsPurchased() => SaveManager.instance.Get<bool>(id) == true;

    private void CheckIfPurchased()
    {
        if (IsPurchased())
        {
            ActiveTable();
        }
        else
        {
            table.SetActive(false);
            DeactiveNearbyPurchasableTables();
        }
    }

    private void PurchaseTableByMoney()
    {
        if (!_gameManager.HasMoney(price))
            return;

        ActiveTable();
        SaveManager.instance.Set(id, true);
        _gameManager.UseMoney(price);
    }

    private void PurchaseTableByAd()
    {
        // TODO: check if player saw ad 
        return;

        ActiveTable();
        SaveManager.instance.Set(id, true);
    }

    private void ActiveTable()
    {
        purchasableTableUI.gameObject.SetActive(false);
        table.SetActive(true);
        table.transform.SetParent(_gameManager.tablesParent.transform);
        table.transform.SetAsLastSibling();
        _gameManager.GetTables();
        ActiveNearbyPurchasableTables();
        _gameManager.SetMaximumFOV(maxFOVOnPurchase);
    }

    private void ActiveNearbyPurchasableTables()
    {
        if (nearbyPurchasableTable.Length == 0)
            return;

        foreach (var table in nearbyPurchasableTable)
        {
            table.gameObject.SetActive(true);

            if (!isLastPurchasableTable)
            {
                if (table.IsPurchased())
                    table.ActiveNearbyPurchasableTables();
            }
        }
    }

    public void DeactiveNearbyPurchasableTables()
    {
        if (nearbyPurchasableTable.Length == 0)
            return;

        foreach (var table in nearbyPurchasableTable)
        {
            table.gameObject.SetActive(false);

            if (!isLastPurchasableTable)
            {
                if (table.IsPurchased())
                    table.DeactiveNearbyPurchasableTables();
            }
        }
    }
}