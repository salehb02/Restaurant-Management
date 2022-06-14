using UnityEngine;
using UnityEngine.AI;

public class Customer : MonoBehaviour
{
    [Header("Animation")]
    public float movementLerpMuliplier = 1.5f;

    private NavMeshAgent _agent;
    private bool _isSelected;
    private Camera _camera;
    private EPOOutline.Outlinable _outlinable;
    private Animator _animator;

    private void Start()
    {
        _animator = GetComponent<Animator>();
        _camera = Camera.main;
        _agent = GetComponent<NavMeshAgent>();
        _outlinable = GetComponent<EPOOutline.Outlinable>();

        UnSelectAsTarget();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (_isSelected)
            {
                if (Physics.Raycast(_camera.ScreenPointToRay(Input.mousePosition), out var hit, Mathf.Infinity))
                {
                    _agent.SetDestination(hit.point);
                }
            }
        }

        _animator.SetFloat("Speed", Mathf.Lerp(_animator.GetFloat("Speed"), _agent.velocity.magnitude, Time.deltaTime * movementLerpMuliplier));
        SitDown();
        StandUp();
    }

    public void SelectAsTarget()
    {
        _isSelected = true;
        _outlinable.enabled = true;
    }

    public void UnSelectAsTarget()
    {
        _isSelected = false;
        _outlinable.enabled = false;
    }

    public void SitDown()
    {
        if (!_isSelected)
            return;

        if (Input.GetKeyDown(KeyCode.S))
            _animator.SetTrigger("Sit Down");
    }

    public void StandUp()
    {
        if (!_isSelected)
            return;

        if (Input.GetKeyDown(KeyCode.F))
            _animator.SetTrigger("Stand Up");
    }
}