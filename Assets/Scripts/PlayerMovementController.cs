using Mirror;
using Mirror.Examples.Common;
using Steamworks;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class PlayerMovemenController : NetworkBehaviour
{
    public Rigidbody rb;  // Rigidbody for physics-based movement

    public GameObject PlayerModel;
    public Transform TPScam;
    public Transform FPScam;

    public float propSpeed = 6.5f;
    public float turnSmoothTime = 0.1f;
    private float turnSmoothVelocity;

    public float walkingSpeed = 7.5f;
    public float runningSpeed = 11.5f;
    public float jumpForce = 8.0f;  // Force applied when jumping
    public float gravityMultiplier = 2.0f; // To adjust fall speed
    public float lookSpeed = 1.0f;
    public float lookXLimit = 60.0f;

    private bool isGrounded;
    private Vector3 moveDirection = Vector3.zero;
    private float rotationX = 0;

    [HideInInspector]
    public bool canMove = true;
    public bool isPropFrozen = false;

    private PlayerObjectController playerObjectController;
    private Vector3 frozenPosition;
    private Quaternion frozenRotation;

    // LayerMask for ground detection
    public LayerMask groundLayer;
    public Transform groundCheck;  // Empty game object to detect ground
    public float groundCheckDistance = 0.5f;  // Distance to detect ground

    private void Start()
    {

        rb = GetComponent<Rigidbody>();  // Assign Rigidbody component
        PlayerModel.SetActive(false);
        playerObjectController = GetComponent<PlayerObjectController>();
        FPScam = transform.Find("CameraHolder/First Person Camera");
        TPScam = transform.Find("CameraHolder/EmptyCam"); 
    }

    private void FixedUpdate()
    {
        if(SceneManager.GetActiveScene().name == "Game")
        {
            if (PlayerModel.activeSelf == false)
            {
                PlayerModel.SetActive(true);
                Cursor.lockState = CursorLockMode.Locked;
            }

            if (isOwned)
            {
                Movement();       
            }          
        }
    }

    private void Update()
    {
        if (isGrounded && Input.GetButtonDown("Jump"))
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);  // Apply jump force
        }
        if (playerObjectController.Role == PlayerObjectController.PlayerRole.Prop)
        {
            if (Input.GetKeyDown(KeyCode.R))  // Example to toggle freeze state
            {
                ToggleFreeze();
            }
        }
    }


    [Command]
    private void CmdSetPosition()
    {
        // Set a new random position on the server
        Vector3 newPosition = new Vector3(Random.Range(-5, 5), 10f, Random.Range(-15, 7));
        Debug.Log($"Server: Setting position to {newPosition}");

        // Set the position on the server, which will propagate to clients
        RpcSetPosition(newPosition);
    }

    [ClientRpc]
    private void RpcSetPosition(Vector3 newPosition)
    {
        if (rb != null)
        {
            rb.isKinematic = true; // Temporarily disable physics
        }

        transform.position = newPosition;

        if (rb != null)
        {
            rb.isKinematic = false; // Re-enable physics after moving
        }
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        if (isLocalPlayer)
        {
            CmdSetPosition(); // Ask the server to set the position
        }
    }


    public void PropMovement()
    {
        CheckGrounded();  // Check if the player is grounded

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 direction = new Vector3(horizontal, 0f, vertical).normalized;

        if (direction.magnitude >= 0.1f)
        {
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + TPScam.eulerAngles.y;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);

            Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            rb.MovePosition(rb.position + moveDir.normalized * propSpeed * Time.fixedDeltaTime);  // Move prop
        }

    }

    private void CheckGrounded()
    {
        // Raycast or SphereCast to check if the player is on the ground
        isGrounded = Physics.CheckSphere(groundCheck.position, groundCheckDistance, groundLayer, QueryTriggerInteraction.Ignore);
    }


    private void ToggleFreeze()
    {
        isPropFrozen = !isPropFrozen;

        if (isPropFrozen)
        {
            // Freeze position and rotation
            frozenPosition = transform.position;
            frozenRotation = transform.rotation;
            rb.isKinematic = true;  // Freeze physics
        }
        else
        {
            // Unfreeze: Movement resumes normally
            rb.isKinematic = false;  // Unfreeze physics
        }
    }

    public void HunterMovement()
    {
        CheckGrounded();  // Check if the player is grounded

        float moveSpeed = Input.GetKey(KeyCode.LeftShift) ? runningSpeed : walkingSpeed;

        // Calculate movement direction based on input
        float moveX = Input.GetAxis("Horizontal") * moveSpeed;
        float moveZ = Input.GetAxis("Vertical") * moveSpeed;

        Vector3 move = transform.right * moveX + transform.forward * moveZ;
        move.y = rb.velocity.y;  // Preserve the current Y velocity

        rb.velocity = move;  // Update the Rigidbody velocity

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);  // Apply jump force
        }

        // Look around
        rotationX += -Input.GetAxis("Mouse Y") * lookSpeed;
        rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);
        FPScam.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);
        transform.rotation *= Quaternion.Euler(0, Input.GetAxis("Mouse X") * lookSpeed, 0);
    }

    public void Movement()
    {
        // Ensure that playerObjectController is not null before accessing it
        if (playerObjectController == null)
        {
            Debug.LogError("PlayerObjectController reference is missing!");
            return;
        }

        // Check the player's role and apply the correct movement logic
        if (playerObjectController.Role == PlayerObjectController.PlayerRole.Prop)
        {
            PropMovement();
        }
        else if (playerObjectController.Role == PlayerObjectController.PlayerRole.Hunter)
        {
            HunterMovement();
        }
    }
}
