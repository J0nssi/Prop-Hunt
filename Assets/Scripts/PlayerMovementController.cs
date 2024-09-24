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
    CharacterController characterController;

    //Prop
    public float propSpeed = 6.5f;
    public float turnSmoothTime = 0.1f;
    float turnSmoothVelocity;
    public GameObject PlayerModel;
    public Transform TPScam;
    private PlayerObjectController playerObjectController;

    //Hunter
    public Transform FPScam;
    public float lookSpeed = 1.0f;
    public float lookXLimit = 60.0f;
    public float walkingSpeed = 7.5f;
    public float runningSpeed = 11.5f;
    public float jumpSpeed = 8.0f;
    public float gravity = 20.0f;
    public float propHitpoints = 50f;
    Vector3 moveDirection = Vector3.zero;
    float rotationX = 0;

    [HideInInspector]
    public bool canMove = true;

    private void Start()
    {
        PlayerModel.SetActive(false);
        playerObjectController = GetComponent<PlayerObjectController>();
        characterController = GetComponent<CharacterController>();
        FPScam = transform.Find("CameraHolder/First Person Camera");
        TPScam = transform.Find("CameraHolder/EmptyCam");
    }

    private void Update()
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
                if (Input.GetMouseButtonDown(0) && playerObjectController.Role == PlayerObjectController.PlayerRole.Hunter) // Left Mouse Button
                {
                    Shoot();
                }
            }
            

        }
    }

    private void SetPosition()
    {
        Vector3 newPosition = new Vector3(Random.Range(-5, 5), 10f, Random.Range(-15, 7));
        Debug.Log($"Setting position to {newPosition}");
        transform.position = newPosition;
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
        SetPosition();
        PlayerModel.SetActive(true); // Ensure the player model is active
    }

    public void PropMovement()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 direction = new Vector3(horizontal, 0f, vertical).normalized;

        if (characterController.isGrounded)
        {
            // Handle jumping
            if (Input.GetButtonDown("Jump")) // Use "Jump" button from Input settings
            {
                moveDirection.y = jumpSpeed; // Apply jump speed
            }
        }
        else
        {
            // Apply gravity when not grounded
            moveDirection.y -= gravity * Time.deltaTime;
        }

        if (direction.magnitude >= 0.1f)
        {
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + TPScam.eulerAngles.y;
            float angle = Mathf.SmoothDamp(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);

            Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            characterController.Move(moveDir.normalized * propSpeed * Time.deltaTime);
        }
        characterController.Move(new Vector3(0, moveDirection.y, 0) * Time.deltaTime);
    }

    private void DestroyProp()
    {
        // Optionally handle any effects or sounds before destroying
        Destroy(gameObject); // Destroy the prop itself
    }


    public void Shoot()
    {
        Debug.Log("Shooting triggered");

        Ray ray = new Ray(FPScam.position, FPScam.forward);
        RaycastHit hit;

        // Define a layer mask to ignore the player
        int layerMask = LayerMask.GetMask("Prop"); // Assuming your props are on a layer named "Prop"

        if (Physics.Raycast(ray, out hit, Mathf.Infinity, layerMask))
        {
            Debug.Log("Hit a prop: " + hit.collider.name);
            PropController prop = hit.collider.GetComponent<PropController>();
            if (prop != null)
            {
                prop.RpcTakeDamage(10f); // Deal damage to the specific prop
                Debug.Log("Hit a prop: " + hit.collider.name);
            }
        }
        else
        {
            Debug.Log("No hit detected");
        }
    }

    public void HunterMovement()
    {
        Vector3 forward = transform.TransformDirection(Vector3.forward);
        Vector3 right = transform.TransformDirection(Vector3.right);
        // Press Left Shift to run
        bool isRunning = Input.GetKey(KeyCode.LeftShift);
        float curSpeedX = canMove ? (isRunning ? runningSpeed : walkingSpeed) * Input.GetAxis("Vertical") : 0;
        float curSpeedY = canMove ? (isRunning ? runningSpeed : walkingSpeed) * Input.GetAxis("Horizontal") : 0;
        float movementDirectionY = moveDirection.y;
        moveDirection = (forward * curSpeedX) + (right * curSpeedY);

        if (Input.GetButton("Jump") && canMove && characterController.isGrounded)
        {
            moveDirection.y = jumpSpeed;
        }
        else
        {
            moveDirection.y = movementDirectionY;
        }
        if (!characterController.isGrounded)
        {
            moveDirection.y -= gravity * Time.deltaTime;
        }

        // Move the controller
        characterController.Move(moveDirection * Time.deltaTime);

        // Player and Camera rotation
        if (canMove)
        {
            rotationX += -Input.GetAxis("Mouse Y") * lookSpeed;
            rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);
            FPScam.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);
            transform.rotation *= Quaternion.Euler(0, Input.GetAxis("Mouse X") * lookSpeed, 0);
        }

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
