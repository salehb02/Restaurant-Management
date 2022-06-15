using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AI;
using EPOOutline;
using TMPro;

public class Customer : MonoBehaviour
{
    public enum CustomerType { Single, Couple, Triple, VIP }
    public CustomerType customerType;

    [Header("Boredom settings")]
    public float idleTime = 2f;
    public float leaveTime = 10f;
    public Gradient waitFillGradient;

    [Space(2)]
    [Header("Animation")]
    public int numberOfAngryModes;
    public float movementLerpMuliplier = 1.5f;

    [Space(2)]
    [Header("UI")]
    public GameObject waitTimerUI;
    public Image waitFill;
    public TextMeshProUGUI moneyAmountText;

    private Coroutine _waitCoroutine;
    private int _moneyAmount;
    private Vector3 _exitOnBoredomPosition;

    // propeties
    public bool IsSelected { get; private set; }
    public bool IsSelectable { get; private set; }
    public bool IsBusy { get; private set; }
    public bool IsVIP { get; private set; }

    // components
    private NavMeshAgent _agent;
    private Camera _camera;
    private Outlinable _outlinable;
    private Animator _animator;
    private GameManager _gameManager;

    private void Start()
    {
        //Init();
    }

    public void Init()
    {
        // get components
        _animator = GetComponent<Animator>();
        _camera = Camera.main;
        _agent = GetComponent<NavMeshAgent>();
        _gameManager = FindObjectOfType<GameManager>();

        // add outline
        _outlinable = gameObject.AddComponent<Outlinable>();
        _outlinable.AddAllChildRenderersToRenderingList();

        IsSelectable = true;
        UnSelectAsTarget();
        HideTimer();
        SetCustomerType();
        _waitCoroutine = EnterWaitMode();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (IsSelected)
            {
                if (Physics.Raycast(_camera.ScreenPointToRay(Input.mousePosition), out var hit, Mathf.Infinity))
                {
                    _agent.SetDestination(hit.point);
                    EndWaitMode();
                }
            }
        }

        _animator.SetFloat("Speed", Mathf.Lerp(_animator.GetFloat("Speed"), _agent.velocity.magnitude, Time.deltaTime * movementLerpMuliplier));
        PlaySitDownAnimation();
        PlayStandUpAnimation();

        if (Input.GetKeyDown(KeyCode.V))
            BusyWarn();
    }

    private void SetCustomerType()
    {
        switch (customerType)
        {
            case CustomerType.Single:
                _moneyAmount = Random.Range(40, 75);
                break;
            case CustomerType.Couple:
                _moneyAmount = Random.Range(65, 99);
                break;
            case CustomerType.Triple:
                _moneyAmount = Random.Range(79, 110);
                break;
            case CustomerType.VIP:
                _moneyAmount = Random.Range(250, 500);
                IsVIP = true;
                break;
            default:
                break;
        }

        moneyAmountText.text = "$" + _moneyAmount;
    }

    public void SelectAsTarget()
    {
        IsSelected = true;
        _outlinable.OutlineParameters.Color = _gameManager.okayOutlineColor;
        _outlinable.enabled = true;
    }

    public void UnSelectAsTarget()
    {
        IsSelected = false;
        _outlinable.enabled = false;
    }

    public void MoveToLocation(Vector3 position)
    {
        _agent.SetDestination(position);
    }

    public void SetExitOnBoredomPosition(Vector3 pos) => _exitOnBoredomPosition = pos;

    public void BusyWarn() => StartCoroutine(BusyWarnCoroutine());

    private IEnumerator BusyWarnCoroutine()
    {
        _outlinable.enabled = true;

        var lerpSpeed = 4f;
        var errorColor = _gameManager.errorOutlineColor;
        errorColor.a = 0;

        while (errorColor.a < 1)
        {
            errorColor.a = Mathf.MoveTowards(errorColor.a, 1, Time.deltaTime * lerpSpeed);
            _outlinable.OutlineParameters.Color = errorColor;
            yield return null;
        }

        while (errorColor.a > 0)
        {
            errorColor.a = Mathf.MoveTowards(errorColor.a, 0, Time.deltaTime * lerpSpeed);
            _outlinable.OutlineParameters.Color = errorColor;
            yield return null;
        }

        _outlinable.enabled = false;
    }

    public void PlaySitDownAnimation()
    {
        if (!IsSelected)
            return;

        if (Input.GetKeyDown(KeyCode.S))
            _animator.SetTrigger("Sit Down");
    }

    public void PlayStandUpAnimation()
    {
        if (!IsSelected)
            return;

        if (Input.GetKeyDown(KeyCode.F))
            _animator.SetTrigger("Stand Up");
    }

    public void PlayAngryAnimation()
    {
        _animator.SetFloat("Angry Mode", Random.Range(0, numberOfAngryModes + 1));
        _animator.SetTrigger("Be Angry");
    }

    public void EndWaitMode() => StopCoroutine(_waitCoroutine);

    public Coroutine EnterWaitMode() => StartCoroutine(EnterWaitModeCoroutine());

    bool playedAngryAnimation = false;

    private IEnumerator EnterWaitModeCoroutine()
    {
        // idle wait
        yield return new WaitForSeconds(idleTime);

        var timer = 0f;
        var angryAnimationTime = Random.Range(leaveTime / 3f, leaveTime / 1.5f);

        // get bored
        while (timer < leaveTime)
        {
            timer += Time.deltaTime;

            if (timer >= angryAnimationTime && !playedAngryAnimation)
            {
                playedAngryAnimation = true;
                PlayAngryAnimation();
            }

            ShowTimer(timer / leaveTime, waitFillGradient.Evaluate(timer / leaveTime));

            yield return null;
        }

        // leave place
        LeaveOnBored();
        HideTimer();
        UnSelectAsTarget();
        IsSelectable = false;

        // wait until reach door
        while (Vector3.Distance(transform.position, _exitOnBoredomPosition) > 0.5f)
            yield return null;

        // destroy object when reached the exit door
        Destroy(gameObject);
    }

    private void LeaveOnBored()
    {
        _agent.SetDestination(_exitOnBoredomPosition);
    }

    private void ShowTimer(float fillAmount, Color color)
    {
        waitTimerUI.SetActive(true);
        waitFill.fillAmount = fillAmount;
        waitFill.color = color;
    }

    private void HideTimer()
    {
        waitTimerUI.SetActive(false);
    }
}