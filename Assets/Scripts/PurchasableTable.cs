using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PurchasableTable : MonoBehaviour
{
    public string id = "TABLE_LEVEL1_1";
    public int price;
    public GameObject tableSpawnPoint;
    public GameObject tablePrefab;

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
        CheckIfPurchased();
        UpdateMoneyText();

        unlockByMoneyButton.onClick.AddListener(PurchaseTableByMoney);
        unlockByAdButton.onClick.AddListener(PurchaseTableByAd);
    }

    public void UpdateMoneyText()
    {
        moneyText.text = "<sprite index=0>" + price;

        if (_gameManager.HasMoney(price))
            moneyText.color = canPurchaseMoneyTextColor;
        else
            moneyText.color = cantPurchaseMoneyTextColor;
    }

    private void CheckIfPurchased()
    {
        if (SaveManager.instance.Get<bool>(id) == true)
            InstantiateTable();
    }

    private void PurchaseTableByMoney()
    {
        if (!_gameManager.HasMoney(price))
            return;

        InstantiateTable();
        SaveManager.instance.Set(id, true);
    }

    private void PurchaseTableByAd()
    {
        // TODO: check if player saw ad 
        return;

        InstantiateTable();
        SaveManager.instance.Set(id, true);
    }

    private void InstantiateTable()
    {
        purchasableTableUI.gameObject.SetActive(false);
        var table = Instantiate(tablePrefab, tableSpawnPoint.transform.position, tableSpawnPoint.transform.rotation, _gameManager.tablesParent.transform);
        table.transform.SetAsLastSibling();
        _gameManager.GetTables();
    }
}