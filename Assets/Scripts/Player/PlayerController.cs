using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class PlayerController : MonoBehaviourPunCallbacks
{
    private PlayerInput playerInput;
    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction jumpAction;
    private InputAction pauseAction;

    [SerializeField] GameObject cameraContainer;
    [SerializeField] GameObject graphicsContainer;
    [SerializeField] float mouseSensitivity, walkSpeed, sprintSpeed, jumpSpeed, smoothTime;

    float verticalLookRotation;
    bool grounded;
    Vector3 smoothMoveVelocity;
    Vector3 moveAmount;

    private Joystick moveJoystick;
    private Joystick lookJoystick;
    private Button pauseButton;

    Rigidbody rb;
    PhotonView pv;
    PlayerManager pm;

    private void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        moveAction = playerInput.actions["Move"];
        lookAction = playerInput.actions["Look"];
        jumpAction = playerInput.actions["Jump"];
        pauseAction = playerInput.actions["Pause"];

        rb = GetComponent<Rigidbody>();
        pv = GetComponent<PhotonView>();
        pm = PhotonView.Find((int)pv.InstantiationData[0]).GetComponent<PlayerManager>();
        SetCharacter((string)pv.InstantiationData[1]);
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

        if (pv.IsMine && Application.isMobilePlatform)
        {
            TouchControls tc = GameObject.Find("TouchControls").GetComponent<TouchControls>();
            //tc.ActivateControls();
            moveJoystick = tc.MoveJoystick;
            lookJoystick = tc.LookJoystick;
            pauseButton = tc.pauseButton;
            pauseButton.onClick.AddListener(PauseGame);
        }

        //SetRole("robber");
    }

    private void Update()
    {
        if (!pv.IsMine || pm.IsPaused)
            return;

        if (pauseAction.ReadValue<float>() > 0)
            PauseGame();

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
        if (!pv.IsMine || pm.IsPaused)
            return;

        rb.MovePosition(rb.position + transform.TransformDirection(moveAmount) * Time.fixedDeltaTime);
    }

    private void PauseGame()
    {
        pm.IsPaused = true;
    }

    private void Look()
    {
        float horizontal = 0f;
        float vertical = 0f;

        if (!Application.isMobilePlatform)
        {
            float multiplier = 1f;
            if (Application.platform == RuntimePlatform.WebGLPlayer)
                multiplier = 0.3f;

            horizontal = lookAction.ReadValue<Vector2>().x * multiplier;
            vertical = lookAction.ReadValue<Vector2>().y * multiplier;
        }
        else
        {
            float treshold = 0.2f;
            if (lookJoystick.Horizontal >= treshold)
                horizontal = (lookJoystick.Horizontal - treshold) * 4;
            else if (lookJoystick.Horizontal <= -treshold)
                horizontal = (lookJoystick.Horizontal + treshold) * 4;
            if (lookJoystick.Vertical >= treshold)
                vertical = (lookJoystick.Vertical - treshold) * 2;
            else if (lookJoystick.Vertical <= -treshold)
                vertical = (lookJoystick.Vertical + treshold) * 2;
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
            horizontal = moveAction.ReadValue<Vector2>().x;
            vertical = moveAction.ReadValue<Vector2>().y;
            //horizontal = Input.GetAxisRaw("Horizontal");
            //vertical = Input.GetAxisRaw("Vertical");
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

        moveAmount = Vector3.SmoothDamp(moveAmount, moveDir * /*Input.GetKey(KeyCode.LeftShift) ? sprintSpeed :*/ walkSpeed, ref smoothMoveVelocity, smoothTime);
    }

    private void Jump()
    {
        if (jumpAction.ReadValue<float>() > 0 && grounded)
        {
            rb.AddForce(transform.up * jumpSpeed * 25);
            grounded = false;
        }
    }

    public void SetGrounded(bool _grounded)
    {
        grounded = _grounded;
    }

    private void Die()
    {
        pm.Die();
    }

    public void SetCharacter(string role)
    {
        Debug.Log("Setting role...");
        //Mesh mesh = GetComponentInChildren<MeshFilter>().mesh;
        //Renderer renderer = GetComponentInChildren<Renderer>();

        GameObject meshPrefab = Resources.Load(string.Format("Characters/PlayerGraphics{0}{1}", char.ToUpper(role[0]), role.Substring(1))) as GameObject;
        GameObject mesh = Instantiate(meshPrefab, graphicsContainer.transform, false);
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (!pv.IsMine && targetPlayer == pv.Owner)
        {
            // Do something
        }
    }
}
