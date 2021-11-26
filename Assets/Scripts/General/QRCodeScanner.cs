using System.Collections;
using System.Collections.Generic;
using System.Timers;
using UnityEngine;
using ZXing;
using UnityEngine.UI;
using TMPro;

public class QRCodeScanner : MonoBehaviour
{
    [SerializeField] private RawImage cameraStream;
    [SerializeField] private Button joinRoomButton;

    private bool isCameraAvailable = false;
    private WebCamTexture cameraTexture;
    private BarcodeReader barcodeReader;
    private string code;
    private bool isScanning = false;
    private float time = 1f;
    private float timer = 0;

    void Start()
    {
        //joinRoomButton.gameObject.SetActive(false);
        //SetupCamera();
    }

    void Update()
    {
        if (!Application.isMobilePlatform)
            return;

        timer += Time.deltaTime;
        UpdateCameraRender();

        if (timer >= time && !isScanning)
        {
            StartCoroutine("ScanInterval");
            isScanning = true;
        }
    }

    private void UpdateCameraRender()
    {
        if (!isCameraAvailable)
            return;

        int orientation = -cameraTexture.videoRotationAngle;
        cameraStream.rectTransform.localEulerAngles = new Vector3(0, 0, orientation);
    }

    private void OnEnable()
    {
        if (!Application.isMobilePlatform)
            return;

        timer = 0;
        isScanning = false;
        if (!isCameraAvailable)
            SetupCamera();
        joinRoomButton.gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        timer = 0;
        code = "";
        if (cameraTexture != null && cameraTexture.isPlaying)
            cameraTexture.Stop();
        joinRoomButton.gameObject.SetActive(false);
        isCameraAvailable = false;
        isScanning = false;
        StopCoroutine("ScanInterval");
    }

    private void SetupCamera()
    {
        WebCamDevice[] devices = WebCamTexture.devices;

        if (devices.Length == 0)
        {
            isCameraAvailable = false;
            return;
        }

        for (int i = 0; i < devices.Length; i++)
        {
            if (!devices[i].isFrontFacing)
            {
                cameraTexture = new WebCamTexture(devices[i].name, 960, 540);//(int) scanZone.rect.width,(int) scanZone.rect.height);
            }
        }

        if (cameraTexture != null)
        {
            cameraTexture.Play();
            cameraStream.texture = cameraTexture;
            barcodeReader = new BarcodeReader();
            isCameraAvailable = true;
        }
    }

    public void JoinRoom()
    {
        Launcher.Instance.JoinRoom(code);
        StopCoroutine("ScanInterval");
    }

    private void Scan()
    {
        if (isCameraAvailable)
        {
            try
            {
                Color32LuminanceSource colLumSource = new Color32LuminanceSource(cameraTexture.GetPixels32(), cameraTexture.width, cameraTexture.height);
                Result result = barcodeReader.Decode(colLumSource);
                if (result != null)
                {
                    code = result.Text;
                    joinRoomButton.gameObject.GetComponentInChildren<TMP_Text>().text = string.Format("Join room {0}", code);
                    joinRoomButton.gameObject.SetActive(true);
                    joinRoomButton.Select();
                }
            }
            catch (System.Exception ex)
            {
                Debug.Log(ex);
            }
        }
    }

    public IEnumerator ScanInterval()
    {
        while (true)
        {
            Scan();

            yield return new WaitForSeconds(1f);
        }
    }
}
