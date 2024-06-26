using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class PlayerMove : NetworkBehaviour
{
    //public float speed = 6.0f;
    //public float jumpSpeed = 8.0f;
    public float gravity = 20.0f;
    private Vector3 moveDirection = Vector3.zero;
    private bool bFlying = false;
    [SyncVar] private bool bDead = false;
    private bool bAwake = false;
    private float tempGravity = 0;
    private int AnimMovingHash;
    Animator childAnimator;

    private void Awake()
    {
        AnimMovingHash = Animator.StringToHash("Moving");
        childAnimator = GetComponentInChildren<Animator>();
    }

    public void SetFlying(bool b)
    {
        if (b)
        {
            moveDirection.y = 0;
            bFlying = true;
            tempGravity = -1 * (2 * gravity);
        }
        else if (bFlying)
        {
            bFlying = false;
            tempGravity = 0;
        }
    }

    public void SetTempGravity(float val) { tempGravity = val; bFlying = false; }

    private void Start()
    {
        if (!isOwned || bDead) enabled = false;
        StartCoroutine(Delay());
    }

    private IEnumerator Delay()
    {
        yield return new WaitForSeconds(1);
        transform.position = new Vector3(Random.Range(-20f, 20f), 100, Random.Range(-20.0f, 20.0f));
        bAwake = true;
    }

    [ClientRpc]
    public void SetDead()
    {
        bDead = true;
    }

    void Update()
    {
        if (!isOwned || bDead || !bAwake) return;

        CharacterController controller = GetComponent<CharacterController>();
        if (!controller.enabled) return;
        StatManager manager = GetComponent<StatManager>();
        if (controller.isGrounded)
        {
            tempGravity = 0;
            moveDirection = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
            moveDirection.Normalize();
            moveDirection = transform.TransformDirection(moveDirection);
            moveDirection *= (float)manager.GetStat(NumericalStats.MovementSpeed);
            if (Input.GetButton("Jump"))
                moveDirection.y = (float)manager.GetStat(NumericalStats.JumpHeight);
        }
        moveDirection.y -= (gravity + tempGravity) * Time.deltaTime;
        controller.Move(moveDirection * Time.deltaTime);
        Vector3 tempDir = new Vector3(moveDirection.x, 0, moveDirection.z);
        if (tempDir != Vector3.zero && controller.isGrounded) { childAnimator.SetBool(AnimMovingHash, true); }
        else { childAnimator.SetBool("Moving", false); }
        if (isServer) UpdateTransform(transform.position);
    }

    [ClientRpc]
    void UpdateTransform(Vector3 transform)
    {
        this.transform.position = transform;
    }
}