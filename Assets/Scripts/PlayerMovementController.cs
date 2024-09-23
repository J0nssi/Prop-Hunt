using Mirror;
using Steamworks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerMovemenController : NetworkBehaviour
{
    //Prop
    public float speed = 6f;
    public float turnSmoothTime = 0.1f;
    float turnSmoothVelocity;
    public GameObject PlayerModel;
    public CharacterController controller;
    public Transform cam;
    private PlayerObjectController playerObjectController;

    //Hunter
    public float mouseSensitivity = 100f;
    float xRotation = 0f;

    private void Start()
    {
        PlayerModel.SetActive(false);
        playerObjectController = GetComponent<PlayerObjectController>();

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
            }
            
        }
    }

    private void SetPosition()
    {
        Vector3 newPosition = new Vector3(Random.Range(-5, 5), 0.8f, Random.Range(-15, 7));
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
        float xDirection = Input.GetAxisRaw("Horizontal");
        float zDirection = Input.GetAxisRaw("Vertical");

        Vector3 moveDirection = new Vector3(xDirection, 0f, zDirection).normalized;

        if (moveDirection.magnitude >= 0.1f)
        {
            float targetAngle = Mathf.Atan2(moveDirection.x, moveDirection.z) * Mathf.Rad2Deg + cam.eulerAngles.y;
            float angle = Mathf.SmoothDamp(transform.eulerAngles.y, targetAngle, ref turnSmoothVelocity, turnSmoothTime);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);

            Vector3 moveDir = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            controller.Move(moveDir.normalized * speed * Time.deltaTime);
        }
    }

    public void HunterMovement()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        cam.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        playerObjectController.transform.Rotate(Vector3.up * mouseX);
        
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
