using System.Collections;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public PlayerInput playerInput;
    public Rigidbody playerRigidbody;
    public CapsuleCollider playerCollider;
    public Animator playerAnimator;
    public float smoothTime;
    public float jumpForce;
    public float slideTime;
    public float groundCheckLength;
    public float groundCheckTime;
    public int minLane;
    public int maxLane;
    public int laneInterval;
    public int maxJumpCount;

    private Vector3 velocityRef;
    private float targetPosX;
    private float colliderHeight;
    private float lastGroundCheckTime;
    private int lane;
    private int jumpCount;
    private bool isSliding;

    void Start()
    {
        velocityRef = Vector3.zero;
        targetPosX = 0f;
        colliderHeight = playerCollider.height;
        lastGroundCheckTime = Time.time - groundCheckTime;
        lane = 0;
        jumpCount = 0;
        isSliding = false;
    }

    void Update()
    {
        GroundCheck();
        Movement();
        UpdatePos();
    }

    void GroundCheck()
    {
        if (Time.time > lastGroundCheckTime + groundCheckTime &&
            Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, 0.1f + groundCheckLength))
        {
            lastGroundCheckTime = Time.time;
            jumpCount = 0;
        }
    }

    void Movement()
    {
        if (playerInput.moveLeft && lane > minLane)
        {
            playerAnimator.SetTrigger("StrafeLeft");

            lane -= 1;
            targetPosX = (float)laneInterval * lane;
        }

        if (playerInput.moveRight && lane < maxLane)
        {
            playerAnimator.SetTrigger("StrafeRight");

            lane += 1;
            targetPosX = (float)laneInterval * lane;
        }

        if (playerInput.jump && jumpCount < maxJumpCount)
        {
            Jump();
        }

        if (playerInput.slide && !isSliding)
        {
            StartCoroutine(SlideRoutine());
        }
    }

    void Jump()
    {
        playerAnimator.SetTrigger("Jump");

        jumpCount += 1;
        playerRigidbody.velocity = new Vector3(playerRigidbody.velocity.x, 0f, 0f);
        playerRigidbody.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }

    IEnumerator SlideRoutine()
    {
        playerAnimator.SetTrigger("Slide");

        isSliding = true;
        playerCollider.height = colliderHeight / 4f;
        playerCollider.center = new Vector3(0f, colliderHeight / 8f, 0f);

        yield return new WaitForSeconds(slideTime);

        isSliding = false;
        playerCollider.height = colliderHeight;
        playerCollider.center = new Vector3(0f, colliderHeight / 2f, 0f);
    }

    void UpdatePos()
    {
        float newX = Mathf.SmoothDamp(transform.position.x, targetPosX, ref velocityRef.x, smoothTime);
        transform.position = new Vector3(newX, transform.position.y, transform.position.z);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;

        Gizmos.DrawLine(transform.position + Vector3.up * 0.1f, 
            transform.position + Vector3.up * 0.1f + Vector3.down * (0.1f + groundCheckLength));
    }
}
