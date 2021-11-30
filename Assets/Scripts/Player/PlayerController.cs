using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class PlayerController : MonoBehaviourPunCallbacks, IInteractable
{
    private PlayerInput playerInput;
    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction jumpAction;
    private InputAction pauseAction;
    public InputAction interactAction;

    [SerializeField] GameObject cameraContainer;
    [SerializeField] GameObject graphicsContainer;
    [SerializeField] float mouseSensitivity, walkSpeed, sprintSpeed, jumpSpeed, smoothTime;

    private float verticalLookRotation;
    private bool grounded;
    private float interactionDistancePlayer = 3f;
    private bool isHolding = false;
    private bool isReleased = true;
    public bool hasFinishedSpree = false;
    Vector3 smoothMoveVelocity;
    Vector3 moveAmount;

    RaycastHit raycasthit;
    public LayerMask EnvironmentLayer;
    //private LayerMask EnvironmentLayer = 1 << 6;

    private Joystick moveJoystick;
    private Joystick lookJoystick;
    private Button pauseButton;

    Rigidbody rb;
    public PhotonView pv;
    public PlayerManager pm;

    private void Awake()
    {
        EnhancedTouchSupport.Enable();
        playerInput = GetComponent<PlayerInput>();
        moveAction = playerInput.actions["Move"];
        lookAction = playerInput.actions["Look"];
        jumpAction = playerInput.actions["Jump"];
        interactAction = playerInput.actions["Interact"];
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
            HUD.Instance.SetPlayerController(pv.ViewID);
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

        // Respawn (die) when falling of the map
        if (transform.position.y < -10f)
            Die();

        Look();

        if (hasFinishedSpree)
            return;

        CheckInteraction();

        if (pm.isArrested && pm.role == "robber")
            return;

        Move();
        Jump();
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

        Vector3 moveDir = new Vector3(horizontal, 0, vertical);

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

    private void CheckInteraction()
    {
        bool isLooking = CheckIsLookingAtObject();

        if (isLooking)
        {
            bool isInteracting = CheckIsInteractingWithObject(raycasthit);

            GameObject checkingObj = raycasthit.collider.gameObject;
            GameObject interactableObj = checkingObj.GetComponent<IInteractable>() != null ? checkingObj : null;

            while (interactableObj == null && checkingObj.transform.parent != null)
            {
                Debug.Log(checkingObj.name);
                checkingObj = checkingObj.transform.parent.gameObject;
                if (checkingObj.GetComponent<IInteractable>() != null)
                    interactableObj = checkingObj;
            }

            //do
            //{
            //    Debug.Log(checkingObj.name);
            //    if (checkingObj.GetComponent<IInteractable>() != null)
            //        interactableObj = checkingObj;
            //    else
            //        checkingObj = checkingObj.transform.parent.gameObject;
            //}
            //while (interactableObj == null && checkingObj.transform.parent != null);



            if (interactableObj != null)
            {
                Debug.Log(interactableObj.name);
                interactableObj.GetComponent<IInteractable>().Interact(raycasthit, isInteracting);
            }
            else
                HUD.Instance.HideDescription();

            //(ItemManager.Instance.CheckInteraction(hit))
        }
        else
        {
            HUD.Instance.HideDescription();
        }
    }

    private bool CheckIsInteractingWithObject(RaycastHit hit)
    {
        RaycastHit hitTouch;

        isHolding = false;

        if (HUD.Instance.pc.interactAction.ReadValue<float>() > 0 && !Application.isMobilePlatform)
        {
            isHolding = true;
        }
        else if (Application.isMobilePlatform && Touch.activeFingers.Count > 0)
        {
            foreach (Finger finger in Touch.activeFingers)
            {
                Ray ray = Camera.main.ScreenPointToRay(finger.screenPosition); // position in px
                ray.origin = Camera.main.transform.position;
                if (Physics.Raycast(ray, out hitTouch, 10f, ~EnvironmentLayer))
                {
                    if (hitTouch.collider.gameObject == hit.collider.gameObject)
                    {
                        isHolding = true;
                    }
                    break;
                }
            }
        }

        if (!isHolding)
        {
            if (!isReleased)
            {
                isReleased = true;
                return true;
            }
        }
        else
        {
            isReleased = false;
        }

        return false;
    }

    public void Interact(RaycastHit hit, bool isInteracting)
    {
        if (hit.distance <= interactionDistancePlayer)
        {
            PlayerController pc = hit.collider.gameObject.GetComponent<PlayerController>();

            if (pm.role == "agent" && pc.pm.role == "robber" && !pc.pm.isArrested)
            {
                if (isInteracting)
                {
                    pc.pm.SetArrested(true);
                    HUD.Instance.HideDescription();
                }
                else
                    HUD.Instance.ShowDescription("Arrest");
            }
            else if (pm.role == "robber" && pc.pm.role == "robber" && pc.pm.isArrested)
            {
                if (isInteracting)
                {
                    pc.pm.SetArrested(false);
                    HUD.Instance.HideDescription();
                }
                else
                    HUD.Instance.ShowDescription("Free");
            }
        }
    }

    protected bool CheckIsLookingAtObject()
    {
        if (Camera.main != null)
        {
            Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f));
            ray.origin = Camera.main.transform.position;
            return Physics.Raycast(ray, out raycasthit, 10f, ~EnvironmentLayer);
        }
        else
            return false;
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
        Instantiate(meshPrefab, graphicsContainer.transform, false);
    }

    public void FinishSpree()
    {
        hasFinishedSpree = true;

        int moneyCollected = (int) PhotonNetwork.LocalPlayer.CustomProperties["money"];
        int previousTotal = (int)PhotonNetwork.CurrentRoom.CustomProperties["totalmoney"];
        int totalMoney = previousTotal + moneyCollected;

        Hashtable hash = new Hashtable();
        hash.Add("totalmoney", totalMoney);
        PhotonNetwork.CurrentRoom.SetCustomProperties(hash);

        Debug.Log(string.Format("Total money collected: {0}", totalMoney));
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (!pv.IsMine && targetPlayer == pv.Owner)
        {
            // Do something
        }
    }
}
