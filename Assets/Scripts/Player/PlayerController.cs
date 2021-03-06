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
using System.Linq;
using System;

public class PlayerController : MonoBehaviourPunCallbacks, IInteractable
{
    public PlayerControls playerControls;
    public GameObject cameraContainer;
    public GameObject graphicsContainer;
    [SerializeField] float walkSpeed, sprintSpeed, smoothTime;

    private float verticalLookRotation;
    private float interactionDistancePlayer = 3f;
    private bool isCrouched = false;
    public bool IsCrouched
    {
        get { return isCrouched; }
    }
    private bool isNightVision = true;
    private bool hasLooked = false;
    Vector3 smoothMoveVelocity;
    Vector3 moveAmount;
    Vector3 previousPosition;
    string previousState = "Idle";

    RaycastHit raycasthit;
    public LayerMask EnvironmentLayer;
    //private LayerMask EnvironmentLayer = 1 << 6;

    private Joystick moveJoystick;
    private Joystick lookJoystick;

    [SerializeField] AudioClip arrestAudioClip;
    [SerializeField] AudioClip freeAudioClip;
    [SerializeField] AudioClip nightvisionAudioClip;

    Rigidbody rb;
    public CapsuleCollider cc;
    public AudioSource audioSource;
    public Light fl;
    public DeferredNightVisionEffect nv;
    public PhotonView pv;
    public PlayerManager pm;
    private Animator anim;
    private SkinnedMeshRenderer[] meshes;

    private void Awake()
    {
        EnhancedTouchSupport.Enable();
        playerControls = new PlayerControls();

        rb = GetComponent<Rigidbody>();
        cc = GetComponent<CapsuleCollider>();
        pv = GetComponent<PhotonView>();
        pm = PhotonView.Find((int)pv.InstantiationData[0]).GetComponent<PlayerManager>();
        pm.SetController(gameObject);
        nv = cameraContainer.GetComponentInChildren<DeferredNightVisionEffect>();
        nv.enabled = false;
        fl = cameraContainer.GetComponentInChildren<Light>();
        audioSource = GetComponent<AudioSource>();
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
        playerControls.Enable();
        base.OnEnable();
    }

    public override void OnDisable()
    {
        playerControls.Disable();
        DisableControls();
        if (pv.IsMine)
        {
            playerControls.Actions.Pause.started -= PauseGameInput;

            if (Application.isMobilePlatform)
                TouchControls.Instance.pauseButton.onClick.RemoveListener(PauseGame);
        }
        base.OnDisable();
    }

    public void DisableControls()
    {
        if (pv.IsMine)
        {
            playerControls.Actions.Interact.started -= OnInteractInput;

            if (Application.isMobilePlatform)
            {
                TouchControls.Instance.interactButton.onClick.RemoveListener(OnInteractButton);
                Touch.onFingerUp -= OnInteractTouch;

                if (pm.role == "robber")
                {
                    TouchControls.Instance.crouchButton.onClick.RemoveListener(Crouch);
                    TouchControls.Instance.nightvisionButton.onClick.RemoveListener(NightVision);
                    Destroy(nv);
                }
            }
            else
            {
                if (pm.role == "robber")
                {
                    playerControls.Actions.Special01.started -= CrouchInput;
                    playerControls.Actions.Special02.started -= NightVisionInput;
                    Destroy(nv);
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

        // Respawn when falling of the map
        if (transform.position.y < -10f)
            pm.Respawn();

        Look();
        CheckInteraction();

        if (pm.isArrested && pm.role == "robber")
        {
            moveAmount = Vector3.zero;
            return;
        }

        Move();
    }

    private void FixedUpdate()
    {
        if (!pv.IsMine)
            return;

        CheckState();

        if (pm.IsPaused || pm.isArrested)
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
            if (Application.platform == RuntimePlatform.WebGLPlayer
                || Application.platform == RuntimePlatform.WindowsPlayer)
                multiplier = 0.3f;

            horizontal = playerControls.Actions.Look.ReadValue<Vector2>().x * multiplier;
            vertical = playerControls.Actions.Look.ReadValue<Vector2>().y * multiplier;
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
        float multiplier = isCrouched ? 0.4f : 1.2f;

        if (!Application.isMobilePlatform)
        {
            horizontal = playerControls.Actions.Move.ReadValue<Vector2>().x;
            vertical = playerControls.Actions.Move.ReadValue<Vector2>().y;
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

        Vector3 moveDir = new Vector3(horizontal, 0, vertical) * multiplier;

        moveAmount = Vector3.SmoothDamp(moveAmount, moveDir * /*Input.GetKey(KeyCode.LeftShift) ? sprintSpeed :*/ walkSpeed, ref smoothMoveVelocity, smoothTime);
    }

    private void CheckState()
    {
        float distance = Mathf.Abs((graphicsContainer.transform.position - previousPosition).magnitude);
        string state;

        if (distance < 0.01)
        {
            if (pm.isArrested)
                state = "Arrested";
            else if (isCrouched)
                state = "IdleCrouch";
            else
                state = "Idle";
        }
        else
        {
            if (isCrouched)
                state = "WalkingCrouch";
            else
                state = "Walking";
        }

        if (state != previousState)
        {
            pv.RPC("RPC_ChangedState", RpcTarget.All, state);
        }

        previousPosition = graphicsContainer.transform.position;
    }

    [PunRPC]
    private void RPC_ChangedState(string state)
    {
        if (gameObject.activeSelf)
        {
            switch (state)
            {
                case "Idle":
                    anim.Play("Idle", 0, 0.0f);
                    break;
                case "IdleCrouch":
                    anim.Play("IdleCrouch", 0, 0.0f);
                    break;
                case "Walking":
                    anim.Play("Walking", 0, 0.0f);
                    break;
                case "WalkingCrouch":
                    anim.Play("WalkingCrouch", 0, 0.0f);
                    break;
                case "Arrested":
                    anim.Play("Arrested", 0, 0.0f);
                    break;
            }
        }

        previousState = state;
    }

    public void Arrested(bool isArrested)
    {
        if (isArrested)
        {
            audioSource.clip = arrestAudioClip;
            audioSource.Play();

            Debug.Log("Arrested");

            if (!isCrouched)
                ToggleCrouch(!isCrouched);

            if (pv.IsMine && isNightVision)
                ToggleNightVision();
        }
        else
        {
            audioSource.clip = freeAudioClip;
            audioSource.Play();
        }
    }

    private void CrouchInput(InputAction.CallbackContext context)
    {
        Crouch();
    }

    private void Crouch()
    {
        if (!pm.isArrested)
            pv.RPC("RPC_Crouch", RpcTarget.All, !isCrouched);
    }

    [PunRPC]
    private void RPC_Crouch(bool _isCrouched)
    {
        ToggleCrouch(_isCrouched);
    }

    public void ToggleCrouch(bool _isCrouched)
    {
        isCrouched = _isCrouched;

        if (Application.isMobilePlatform)
            TouchControls.Instance.CrouchButtonToggle(isCrouched);

        if (!isCrouched)
        {
            cc.center = new Vector3(0f, 0.9f, 0.1f);
            cc.radius = 0.5f;
            cc.height = 1.8f;
            cameraContainer.transform.localPosition = new Vector3(0, 1.65f, 0.1f);
        }
        else
        {
            cc.center = new Vector3(0f, 0.65f, 0.1f);
            cc.radius = 0.5f;
            cc.height = 1.3f;
            cameraContainer.transform.localPosition = new Vector3(0, 0.9f, 0.1f);
        }
    }

    public void NightVisionInput(InputAction.CallbackContext context)
    {
        NightVision();
    }

    public void NightVision()
    {
        if (!pm.isArrested)
            ToggleNightVision();
    }

    public void ToggleNightVision()
    {
        isNightVision = !isNightVision;
        nv.enabled = isNightVision;

        if (isNightVision)
        {
            audioSource.clip = nightvisionAudioClip;
            audioSource.Play();
        }
        else
        {
            if (audioSource.clip == nightvisionAudioClip && audioSource.isPlaying)
                audioSource.Stop();
        }

        if (Application.isMobilePlatform)
            TouchControls.Instance.NightVisionButtonToggle(isNightVision);
    }

    private void CheckInteraction(bool isInteracting = false)
    {
        GameplayManager.Instance.HideDescription();
        TouchControls.Instance.interactButton.gameObject.SetActive(false);

        if (pm.isArrested && pm.role == "robber")
        {
            GameplayManager.Instance.ShowDescription("You have been arrested");
            return;
        }

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
                !(pm.role == "agent" && obj.GetComponent<ItemPickup>()) &&
                !(pm.role == "robber" && obj.GetComponent<PlayerController>() != null && obj.GetComponent<PlayerController>().pm.role == "agent"))
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
            //PlayerController pc = hit.collider.gameObject.GetComponent<PlayerController>();

            if (GameplayManager.Instance.pm.role == "agent" && pm.role == "robber" && !pm.isArrested)
            {
                if (isInteracting)
                {
                    string arrests = (string) PhotonNetwork.LocalPlayer.CustomProperties["arrests"];
                    List<string> arrestsList = new List<string> (arrests.Split(';').ToList());

                    if (!arrestsList.Contains(pv.Owner.ActorNumber.ToString()))
                        arrestsList.Add(pv.Owner.ActorNumber.ToString());

                    arrests = string.Join(";", arrestsList);
                    Hashtable hash = new Hashtable();
                    hash.Add("arrests", arrests);

                    //if (arrests == 1)
                    //    hash.Add("firstarrest", (float)PhotonNetwork.Time - GameplayManager.Instance.startTime);

                    PhotonNetwork.LocalPlayer.SetCustomProperties(hash);
                    pm.SetArrested(true);
                }
                else
                    GameplayManager.Instance.ShowDescription("Arrest");
            }
            else if (GameplayManager.Instance.pm.role == "robber" && pm.role == "robber" && pm.isArrested)
            {
                if (isInteracting)
                {
                    pm.SetArrested(false);
                }
                else
                    GameplayManager.Instance.ShowDescription("Free");
            }

            return true;
        }
        else
            return false;
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
                    TouchControls.Instance.NightVisionButtonToggle(true);
                }
            }
        }
        else
        {
            Destroy(nv);
        }

        GameObject meshPrefab = Resources.Load(string.Format("Characters/PlayerGraphics{0}{1}", char.ToUpper(role[0]), role.Substring(1))) as GameObject;
        GameObject obj = Instantiate(meshPrefab, graphicsContainer.transform, false);
        anim = obj.GetComponent<Animator>();
        meshes = gameObject.GetComponentsInChildren<SkinnedMeshRenderer>();

        if (pv.IsMine)
            ShowGraphics(false);

        cameraContainer.GetComponentInChildren<Camera>().enabled = pv.IsMine;
        cameraContainer.GetComponentInChildren<AudioListener>().enabled = pv.IsMine;
    }

    public void ShowGraphics(bool enable)
    {
        foreach (SkinnedMeshRenderer mesh in meshes)
            mesh.enabled = enable;
    }
}
