using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class PlayerController : MonoBehaviourPunCallbacks
{
    [SerializeField] GameObject cameraContainer;
    [SerializeField] float mouseSensitivity, walkSpeed, sprintSpeed, jumpSpeed, smoothTime;

    float verticalLookRotation;
    bool grounded;
    Vector3 smoothMoveVelocity;
    Vector3 moveAmount;

    private Joystick moveJoystick;
    private Joystick lookJoystick;

    Rigidbody rb;
    PhotonView pv;
    PlayerManager playerManager;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        pv = GetComponent<PhotonView>();

        if (pv.IsMine && Application.isMobilePlatform)
        {
            TouchControls tc = GameObject.Find("TouchControls").GetComponent<TouchControls>();
            tc.ActivateControls();
            moveJoystick = tc.MoveJoystick;
            lookJoystick = tc.LookJoystick;
        }

        playerManager = PhotonView.Find((int)pv.InstantiationData[0]).GetComponent<PlayerManager>();
    }

    private void Start()
    {
        Cursor.visible = false;

        if (!Application.isMobilePlatform)
            Cursor.lockState = CursorLockMode.Locked;
        else
            mouseSensitivity /= 2;

        if (pv.IsMine)
        {
            Debug.Log("Player created");
            Hashtable hash = new Hashtable();
            hash.Add("money", 0);
            PhotonNetwork.LocalPlayer.SetCustomProperties(hash);
        }
        else
        {
            Destroy(GetComponentInChildren<Camera>().gameObject);
            Destroy(rb);
        }
    }

    private void Update()
    {
        if (!pv.IsMine)
            return;

        Look();
        Move();
        Jump();

        // Respawn (die) when falling of the map
        if (transform.position.y < -10f)
        {
            Die();
        }
    }

    private void FixedUpdate()
    {
        if (!pv.IsMine)
            return;

        rb.MovePosition(rb.position + transform.TransformDirection(moveAmount) * Time.fixedDeltaTime);
    }

    private void Look()
    {
        float horizontal = 0f;
        float vertical = 0f;

        if (!Application.isMobilePlatform)
        {
            horizontal = Input.GetAxisRaw("Mouse X");
            vertical = Input.GetAxisRaw("Mouse Y");
        }
        else
        {
            float treshold = 0.2f;
            if (lookJoystick.Horizontal >= treshold)
                horizontal = 1f;
            else if (lookJoystick.Horizontal <= -treshold)
                horizontal = -1f;
            if (lookJoystick.Vertical >= treshold)
                vertical = 1f;
            else if (lookJoystick.Vertical <= -treshold)
                vertical = -1f;
        }

        transform.Rotate(Vector3.up * horizontal * mouseSensitivity);

        verticalLookRotation += vertical * mouseSensitivity;
        verticalLookRotation = Mathf.Clamp(verticalLookRotation, -90f, 90f);

        cameraContainer.transform.localEulerAngles = Vector3.left * verticalLookRotation;
    }

    private void Move()
    {
        float horizontal = 0f;
        float vertical = 0f;
        if (!Application.isMobilePlatform)
        {
            horizontal = Input.GetAxisRaw("Horizontal");
            vertical = Input.GetAxisRaw("Vertical");
        }
        else
        {
            float treshold = 0.2f;
            if (moveJoystick.Horizontal >= treshold)
                horizontal = 1f;
            else if (moveJoystick.Horizontal <= -treshold)
                horizontal = -1f;
            if (moveJoystick.Vertical >= treshold)
                vertical = 1f;
            else if (moveJoystick.Vertical <= -treshold)
                vertical = -1f;
        }

        Vector3 moveDir = new Vector3(horizontal, 0, vertical).normalized;

        moveAmount = Vector3.SmoothDamp(moveAmount, moveDir * (Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : walkSpeed), ref smoothMoveVelocity, smoothTime);
    }

    private void Jump()
    {
        if (Input.GetKeyDown(KeyCode.Space) && grounded)
        {
            rb.AddForce(transform.up * jumpSpeed * 100);
        }
    }

    public void SetGrounded(bool _grounded)
    {
        grounded = _grounded;
    }

    private void Die()
    {
        playerManager.Die();
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (!pv.IsMine && targetPlayer == pv.Owner)
        {
            // Do something
        }
    }
}
