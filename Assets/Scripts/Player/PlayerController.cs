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
    public PlayerControls playerControls;
    private InputAction moveAction;
    private InputAction lookAction;

    public GameObject cameraContainer;
    public GameObject graphicsContainer;
    [SerializeField] float walkSpeed, sprintSpeed, smoothTime;

    private float verticalLookRotation;
    private float interactionDistancePlayer = 3f;
    private bool isCrouched = false;
    private bool isNightVision = true;
    private bool hasLooked = false;
    Vector3 smoothMoveVelocity;
    Vector3 moveAmount;

    RaycastHit raycasthit;
    public LayerMask EnvironmentLayer;
    //private LayerMask EnvironmentLayer = 1 << 6;

    private Joystick moveJoystick;
    private Joystick lookJoystick;

    Rigidbody rb;
    BoxCollider boxCollider;
    public Light fl;
    public DeferredNightVisionEffect nv;
    public PhotonView pv;
    public PlayerManager pm;

    private void Awake()
    {
        EnhancedTouchSupport.Enable();
        playerInput = GetComponent<PlayerInput>();
        playerControls = new PlayerControls();
        moveAction = playerInput.actions["Move"];
        lookAction = playerInput.actions["Look"];

        rb = GetComponent<Rigidbody>();
        boxCollider = GetComponent<BoxCollider>();
        pv = GetComponent<PhotonView>();
        pm = PhotonView.Find((int)pv.InstantiationData[0]).GetComponent<PlayerManager>();
        nv = cameraContainer.GetComponentInChildren<DeferredNightVisionEffect>();
        nv.enabled = false;
        fl = cameraContainer.GetComponentInChildren<Light>();
        SetCharacter((string)pv.InstantiationData[1]);
    }

    private void Start()
    {
        Cursor.visible = false;

        if (!Application.isMobilePlatform)
            Cursor.lockState = CursorLockMode.Locked;

        if (pv.IsMine)
        {
            Debug.Log("Player created");
            Hashtable hash = new Hashtable();
            hash.Add("money", 0);
            PhotonNetwork.LocalPlayer.SetCustomProperties(hash);
            GameplayManager.Instance.SetPlayerController(pv.ViewID);
            playerControls.Actions.Interact.started += OnInteractInput;
            playerControls.Actions.Pause.started += PauseGameInput;

            if (Application.isMobilePlatform)
            {
                moveJoystick = TouchControls.Instance.MoveJoystick;
                lookJoystick = TouchControls.Instance.LookJoystick;
                TouchControls.Instance.interactButton.onClick.AddListener(OnInteractButton);
                TouchControls.Instance.pauseButton.onClick.AddListener(PauseGame);
                Touch.onFingerUp += OnInteractTouch;
            }
        }
        else
        {
            Destroy(rb);
        }
    }

    public override void OnEnable()
    {
        //playerInput.actions.Enable();
        playerControls.Enable();
        base.OnEnable();
    }

    public override void OnDisable()
    {
        Destroy(nv);
        playerControls.Disable();
        DisableControls();
        base.OnDisable();
    }

    public void DisableControls()
    {
        if (pv.IsMine)
        {
            playerControls.Actions.Interact.started -= OnInteractInput;
            playerControls.Actions.Pause.started -= PauseGameInput;

            if (Application.isMobilePlatform)
            {
                TouchControls.Instance.interactButton.onClick.RemoveListener(OnInteractButton);
                TouchControls.Instance.pauseButton.onClick.RemoveListener(PauseGame);
                Touch.onFingerUp -= OnInteractTouch;

                if (pm.role == "robber")
                {
                    TouchControls.Instance.crouchButton.onClick.RemoveListener(Crouch);
                    TouchControls.Instance.nightvisionButton.onClick.RemoveListener(NightVision);
                }
            }
            else
            {
                if (pm.role == "robber")
                {
                    playerControls.Actions.Special01.started -= CrouchInput;
                    playerControls.Actions.Special02.started -= NightVisionInput;
                }
            }
        }
    }

    private void Update()
    {
        if (!pv.IsMine)
            return;

        float pastTime = (float) PhotonNetwork.Time - GameplayManager.Instance.startTime;
        if (pastTime >= GameplayManager.Instance.maxTime)
            GameplayManager.Instance.EndMatch();

        GameplayManager.Instance.SetTime(GameplayManager.Instance.maxTime - pastTime);

        if (pm.IsPaused || pm.hasEscaped)
            return;

        // Respawn (die) when falling of the map
        if (transform.position.y < -10f)
            Die();

        Look();

        if (pm.isArrested && pm.role == "robber")
        {
            moveAmount = Vector3.zero;
            return;
        }

        Move();
        CheckInteraction();
    }

    private void FixedUpdate()
    {
        if (!pv.IsMine || pm.IsPaused)
            return;

        rb.MovePosition(rb.position + transform.TransformDirection(moveAmount) * Time.fixedDeltaTime);
    }

    private void PauseGameInput(InputAction.CallbackContext context)
    {
        PauseGame();
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

        if (hasLooked)
        {
            transform.Rotate(Vector3.up * horizontal * GameplayManager.Instance.sensitivity);

            verticalLookRotation += vertical * GameplayManager.Instance.sensitivity;
            verticalLookRotation = Mathf.Clamp(verticalLookRotation, -90f, 90f);

            cameraContainer.transform.localEulerAngles = Vector3.left * verticalLookRotation;
        }

        if (!hasLooked && (horizontal != 0 || vertical != 0))
            hasLooked = true;
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

    private void CrouchInput(InputAction.CallbackContext context)
    {
        Crouch();
    }

    private void Crouch()
    {
        pv.RPC("RPC_Crouch", RpcTarget.All, !isCrouched);
    }

    [PunRPC]
    private void RPC_Crouch(bool _isCrouched)
    {
        isCrouched = _isCrouched;
        if (Application.isMobilePlatform)
            TouchControls.Instance.CrouchButtonToggle(isCrouched);

        if (!isCrouched)
        {
            boxCollider.center = new Vector3(0f, 0.9f, 0f);
            boxCollider.size = new Vector3(0.8f, 1.8f, 0.5f);
            graphicsContainer.transform.localPosition = Vector3.zero;
            graphicsContainer.transform.localRotation = Quaternion.identity;
            graphicsContainer.transform.localScale = Vector3.one;
            cameraContainer.transform.localPosition = new Vector3(0, 1.65f, 0.1f);
        }
        else
        {
            //boxCollider.center = new Vector3(0f, 0.25f, 0.4f);
            //boxCollider.size = new Vector3(1f, 0.3f, 1f);
            boxCollider.center = new Vector3(0f, 0.25f, 0f);
            boxCollider.size = new Vector3(1.1f, 0.3f, 1.1f);
            //boxCollider.size = new Vector3(boxCollider.size.x, boxCollider.size.y / 2f, boxCollider.size.z);
            //graphicsContainer.transform.localPosition += -transform.up * boxCollider.size.y / 2f;
            //graphicsContainer.transform.localPosition += -transform.up * (boxCollider.size.y - boxCollider.size.z);
            //graphicsContainer.transform.localPosition = new Vector3(0, 0.2f, -0.5f);
            graphicsContainer.transform.localPosition = new Vector3(0, 0.2f, -1f);
            graphicsContainer.transform.localRotation = Quaternion.Euler(90, 0, 0);
            graphicsContainer.transform.localScale = new Vector3(1.1f, 0.9f, 1f);
            //cameraContainer.transform.localPosition = new Vector3(0, 0.3f, 0.8f);
            cameraContainer.transform.localPosition = new Vector3(0, 0.3f, 0.4f);
        }
    }

    private void NightVisionInput(InputAction.CallbackContext context)
    {
        NightVision();
    }

    private void NightVision()
    {
        isNightVision = !isNightVision;
        nv.enabled = isNightVision;
        if (Application.isMobilePlatform)
            TouchControls.Instance.NightVisionButtonToggle(isNightVision);
    }

    private void CheckInteraction(bool isInteracting = false)
    {
        GameplayManager.Instance.HideDescription();
        TouchControls.Instance.interactButton.gameObject.SetActive(false);

        if (Camera.main != null)
        {
            Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f));
            ray.origin = Camera.main.transform.position;
            if (Physics.Raycast(ray, out raycasthit, 10f, ~EnvironmentLayer))
            {
                GameObject obj = raycasthit.collider.gameObject;
                while (obj.GetComponent<IInteractable>() == null && obj.transform.parent != null)
                    obj = obj.transform.parent.gameObject;

                if (obj.GetComponent<IInteractable>() != null &&
                !(pm.role == "agent" && obj.GetComponent<ItemPickupMoney>()))
                {
                    if (obj.GetComponent<IInteractable>().Interact(raycasthit, isInteracting))
                        TouchControls.Instance.interactButton.gameObject.SetActive(true);
                }
            }
        }
    }

    private void OnInteractInput(InputAction.CallbackContext context)
    {
        CheckInteraction(true);
    }

    private void OnInteractButton()
    {
        CheckInteraction(true);
    }

    private void OnInteractTouch(Finger finger)
    {
        RaycastHit hitTouch;
        Ray ray = Camera.main.ScreenPointToRay(finger.screenPosition); // position in px
        ray.origin = Camera.main.transform.position;
        if (Physics.Raycast(ray, out hitTouch, 10f, ~EnvironmentLayer)
            && hitTouch.collider.gameObject == raycasthit.collider.gameObject)
        {
            CheckInteraction(true);
        }
    }

    public bool Interact(RaycastHit hit, bool isInteracting)
    {
        if (hit.distance <= interactionDistancePlayer && pm.pv.Owner != GameplayManager.Instance.pm.pv.Owner)
        {
            PlayerController pc = hit.collider.gameObject.GetComponent<PlayerController>();

            if (GameplayManager.Instance.pm.role == "agent" && pc.pm.role == "robber" && !pc.pm.isArrested)
            {
                if (isInteracting)
                {
                    int arrests = (int)PhotonNetwork.LocalPlayer.CustomProperties["arrests"];
                    arrests++;
                    Hashtable hash = new Hashtable();
                    hash.Add("arrests", arrests);

                    if (arrests == 1)
                        hash.Add("firstarrest", (float)PhotonNetwork.Time - GameplayManager.Instance.startTime);

                    PhotonNetwork.LocalPlayer.SetCustomProperties(hash);
                    pc.pm.SetArrested(true);
                }
                else
                    GameplayManager.Instance.ShowDescription("Arrest");
            }
            else if (GameplayManager.Instance.pm.role == "robber" && pc.pm.role == "robber" && pc.pm.isArrested)
            {
                if (isInteracting)
                {
                    pc.pm.SetArrested(false);
                }
                else
                    GameplayManager.Instance.ShowDescription("Free");
            }

            return true;
        }
        else
            return false;
    }

    private void Die()
    {
        pm.Die();
    }

    private void SetCharacter(string role)
    {
        if (role == "robber")
        {
            fl.transform.localPosition = Vector3.zero;
            fl.intensity = 0.1f;
            fl.range = 20;

            nv.enabled = pv.IsMine;
            fl.enabled = pv.IsMine;

            if (pv.IsMine)
            {
                playerControls.Actions.Special01.started += CrouchInput;
                playerControls.Actions.Special02.started += NightVisionInput;

                if (Application.isMobilePlatform)
                {
                    TouchControls.Instance.crouchButton.onClick.AddListener(Crouch);
                    TouchControls.Instance.nightvisionButton.onClick.AddListener(NightVision);
                }
            }
        }
        else
        {
            Destroy(nv);
        }

        if (!pv.IsMine || pv.IsMine)
        {
            GameObject meshPrefab = Resources.Load(string.Format("Characters/PlayerGraphics{0}{1}", char.ToUpper(role[0]), role.Substring(1))) as GameObject;
            Instantiate(meshPrefab, graphicsContainer.transform, false);
        }

        cameraContainer.GetComponentInChildren<Camera>().enabled = pv.IsMine;
        cameraContainer.GetComponentInChildren<AudioListener>().enabled = pv.IsMine;
    }
}
