using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerMovemenController : NetworkBehaviour
{

    public float Speed = 0.1f;
    public GameObject PlayerModel;
    public Transform cameraTransform;  // The camera's transform
    public float rotationSpeed = 5f;

    private void Start()
    {
        PlayerModel.SetActive(false);
    }

    private void Update()
    {
        if(SceneManager.GetActiveScene().name == "Game")
        {
            if (PlayerModel.activeSelf == false)
            {
                SetPosition();
                PlayerModel.SetActive(true);
                Cursor.lockState = CursorLockMode.Locked;
            }

            if (isOwned)
            {
                Movement();
            }
            
        }
    }

    public void SetPosition()
    {
        transform.position = new Vector3(Random.Range(-5,5), 0.8f, Random.Range(-15,7));
    }

    public void Movement()
    {
        float xDirection = Input.GetAxis("Horizontal");
        float zDirection = Input.GetAxis("Vertical");

        Vector3 moveDirection = new Vector3(xDirection, 0.0f, zDirection).normalized;

        transform.position += moveDirection * Speed;
        Vector3 forwardDirection = cameraTransform.forward;
        forwardDirection.y = 0f; // Prevent tilting the player upwards/downwards

        Quaternion targetRotation = Quaternion.LookRotation(forwardDirection);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }
}
