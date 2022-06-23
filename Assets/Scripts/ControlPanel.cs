using UnityEngine;

[CreateAssetMenu(menuName = "Current Project/Control Panel")]
public class ControlPanel : ScriptableObject
{
    public const string PLAYER_MONEY = "PLAYER_MONEY";

    public enum CustomerWaitPlacementType { FreePosition, LimitedPosition }

    [Header("Main Configurations")]
    public bool useTouch;
    public bool useDragSelection;
    public LineRenderer dragLinePrefab;

    [Header("Customers Settings")]
    public Customer[] customers;
    public Vector2 customerGenerateTime = new Vector2(4, 13);
    [Range(0, 1)] public float tableNumberFilterChance = 0.3f;
    [Range(0, 1)] public float tableReserveFilterChance = 0.2f;
    [Range(0, 1)] public float tableFoodFilterChance = 0.2f;
    [Range(0, 1)] public float coupleFamilyChance = 0.5f;
    [Range(0, 1)] public float tripleFamilyChance = 0.25f;
    [Range(0, 1)] public float quadrupleFamilyChance = 0.1f;

    [Space(2)]
    [Header("Customer Wait Placement")]
    public CustomerWaitPlacementType customerWaitPlacementType;
    [Space(2)]
    public Vector2 freePositionOffsetX = new Vector2(-2.2f, 2.2f);
    public Vector2 freePositionOffsetZ = new Vector2(-0.8f, 0.8f);
    [Space(2)]
    public bool randomHordeCountInLimitedPositionType = false;

    [Space(2)]
    [Header("Filters Settings")]
    public FoodsFilterEnum[] foodFilters;

    [Space(2)]
    [Header("Colors Settings")]
    public Color okayOutlineColor;
    public Color errorOutlineColor;
    public Color tableTimeColor;
    public Gradient timerFillGradient;
    public Color canPurchaseTextColor;
    public Color cantPurchaseTextColor;

    #region Singleton
    private static ControlPanel instance;
    public static ControlPanel Instance
    {
        get => instance == null ? instance = Resources.Load("ControlPanel") as ControlPanel : instance;
    }
    #endregion
}