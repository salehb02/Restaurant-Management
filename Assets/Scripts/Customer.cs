using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AI;
using EPOOutline;
using TMPro;
using DG.Tweening;

public class Customer : MonoBehaviour
{
    public enum CustomerType { Single = 1, Couple = 2, Triple = 3, Quadruple = 4, VIP }
    public CustomerType customerType;

    public GameObject pivot;

    [Header("Using Table Mode")]
    public float eatTime = 15f;

    [Space(2)]
    [Header("Boredom settings")]
    public float idleTime = 2f;
    public float leaveTime = 10f;

    [Space(2)]
    [Header("Animation")]
    public int numberOfAngryModes;
    public float movementLerpMuliplier = 1.5f;
    public float sitDownAnimationLength;
    public float standUpAnimationLength;
    public float startEatDelay;

    [Space(2)]
    [Header("UI")]
    public GameObject customerCanvas;
    public GameObject waitTimerUI;
    public Image waitFill;
    public TextMeshProUGUI moneyAmountText;

    private Coroutine _waitCoroutine;
    private int _prizeAmount;
    private Vector3 _exitPosition;
    private GameObject _sitPosition;
    private GameObject _standPosition;
    private bool _init = false;
    private bool _leaving = false;
    private bool _goingToSit = false;

    // propeties
    public bool IsSelected { get; private set; }
    public bool IsSelectable { get; private set; }
    public bool IsBusy { get; private set; }
    public bool IsVIP { get; private set; }
    public Table TargetTable { get; private set; }

    // components
    private NavMeshAgent _agent;
    private Camera _camera;
    private Outlinable _outlinable;
    private Animator _animator;
    private GameManager _gameManager;
    private Table _currentTable;

    private void Start()
    {
        Init();
    }

    public void Init()
    {
        if (_init)
            return;

        // get components
        _animator = GetComponent<Animator>();
        _camera = Camera.main;
        _agent = GetComponent<NavMeshAgent>();
        _gameManager = FindObjectOfType<GameManager>();

        // add outline
        _outlinable = gameObject.AddComponent<Outlinable>();
        _outlinable.AddAllChildRenderersToRenderingList();

        customerCanvas.gameObject.SetActive(true);
        IsSelectable = true;
        UnSelect();
        HideTimer();
        GetCustomerPrize();
        _waitCoroutine = StartCoroutine(EnterWaitModeCoroutine());

        _init = true;
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (IsSelected)
            {
                if (Physics.Raycast(_camera.ScreenPointToRay(Input.mousePosition), out var hit, Mathf.Infinity))
                {
                    var table = hit.transform.GetComponentInParent<Table>();

                    if (table && table.Select((int)customerType) != null)
                    {
                        _currentTable = table;
                        var sitPostion = table.GetAvailableSitPosition();
                        _sitPosition = sitPostion.sitPos;
                        _standPosition = sitPostion.standPos;

                        _agent.SetDestination(_standPosition.transform.position);
                        EndWaitMode();
                        table.SetBusyMode(this);
                        UnSelect();

                        if(_gameManager.selectedTarget == this)
                            _gameManager.selectedTarget = null;

                        _goingToSit = true;
                    }
                }
            }
        }

        // set animation blend value by agent speed
        _animator.SetFloat("Speed", Mathf.Lerp(_animator.GetFloat("Speed"), _agent.velocity.magnitude, Time.deltaTime * movementLerpMuliplier));

        // check distance to exit 
        if (_leaving)
        {
            if (Vector3.Distance(transform.position, _exitPosition) <= 0.5f)
                Destroy(gameObject);
        }

        if(_goingToSit)
        {
            if (Vector3.Distance(transform.position, _standPosition.transform.position) <= 0.2f)
            {
                SitDown();
                _goingToSit = false;
            }
        }
    }

    private void GetCustomerPrize()
    {
        switch (customerType)
        {
            case CustomerType.Single:
                _prizeAmount = Random.Range(5, 20);
                break;
            case CustomerType.Couple:
                _prizeAmount = Random.Range(5, 20);
                break;
            case CustomerType.Triple:
                _prizeAmount = Random.Range(7, 15);
                break;
            case CustomerType.Quadruple:
                _prizeAmount = Random.Range(10, 25);
                break;
            case CustomerType.VIP:
                _prizeAmount = Random.Range(40, 50);
                IsVIP = true;
                break;
            default:
                break;
        }

        moneyAmountText.text = "$" + _prizeAmount;
    }

    public void Select()
    {
        IsSelected = true;
        _outlinable.OutlineParameters.Color = _gameManager.okayOutlineColor;
        _outlinable.enabled = true;
    }

    public void UnSelect()
    {
        IsSelected = false;
        _outlinable.enabled = false;
    }

    public void MoveToLocation(Vector3 pos)
    {
        _agent.SetDestination(pos);
    }

    public void SetExitPosition(Vector3 pos) => _exitPosition = pos;

    private void SitDown()
    {
        pivot.transform.SetParent(_sitPosition.transform);
        _agent.enabled = false;
        transform.rotation = _standPosition.transform.rotation;
        transform.position = _standPosition.transform.position;

        _animator.SetTrigger("Sit Down");

        pivot.transform.DOLocalMove(Vector3.zero, sitDownAnimationLength).OnComplete(() =>
        {
            _currentTable.StartTimer(eatTime);
            StartCoroutine(EatAnimationCoroutine());
        });

        pivot.transform.DOLocalRotateQuaternion(Quaternion.identity, sitDownAnimationLength);
    }

    public void StandUp()
    {
        _currentTable.LeaveTable();
        pivot.transform.SetParent(transform);
        _animator.SetTrigger("Stand Up");

        pivot.transform.DOLocalMove(Vector3.zero, standUpAnimationLength).OnComplete(() =>
        {
            _agent.enabled = true;
            Leave();
        });

        pivot.transform.DOLocalRotateQuaternion(Quaternion.identity, standUpAnimationLength);
    }

    private IEnumerator EatAnimationCoroutine()
    {
        yield return new WaitForSeconds(startEatDelay);

        _animator.SetTrigger("Eat");

        yield return new WaitForSeconds(eatTime - startEatDelay - 2f);

        _animator.SetTrigger("Give Up Eating");
    }

    public void PlayAngryAnimation()
    {
        _animator.SetFloat("Angry Mode", Random.Range(0, numberOfAngryModes + 1));
        _animator.SetTrigger("Be Angry");
    }

    public void EndWaitMode()
    {
        StopCoroutine(_waitCoroutine);
        HideTimer();
    }

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

            ShowTimer(timer / leaveTime, _gameManager.timerFillGradient.Evaluate(timer / leaveTime));

            yield return null;
        }

        // leave place
        HideTimer();
        UnSelect();
        Leave();
    }

    public void Leave()
    {
        IsSelectable = false;
        customerCanvas.gameObject.SetActive(false);
        _agent.SetDestination(_exitPosition);
        _leaving = true;
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