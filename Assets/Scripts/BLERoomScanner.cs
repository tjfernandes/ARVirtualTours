using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using System.Linq;
using System;
using System.ComponentModel;
using Inworld;
using UnityEngine.SceneManagement;

[Serializable]
public class SceneObject
{   
    [Tooltip("Unique Identifier for the Scene Object")]
    public string UUID;
    
    [Tooltip("Name of the Room")]
    public string roomName;
    
    [Tooltip("Sprite representing the Room")]
    public Sprite roomSprite;
    
    [Tooltip("Inworld Game Data associated with the Scene Object")]
    public ScriptableObject sceneGameData;
}

public class BLERoomScanner : MonoBehaviour
{
    public SceneObject[] sceneObjects;
    #region UI Elements
        public Sprite magnifientGlassSprite;
        public Sprite TapSprite;
        private GameObject scanPanel;
        private TextMeshProUGUI scanPanelText;
        private Image scanPanelImage;
        private Button repeatScanButton;
    #endregion
    private Vector3 originalScaleDefaultPanelText;
    private string closestBeaconUUID;
    private int closestBeaconRSSI = int.MinValue;
    private SceneObject closestBeacon;


    // Dictionary of beacon UUIDs and their RSSI values within 5 second scan
    private Dictionary<string, List<int>> beaconRSSIs = new Dictionary<string, List<int>>();
    private float scanDuration = 3f;
    private bool isScanning = false;

    void Awake()
    {
        scanPanel = GameObject.Find("Canvas").transform.Find("ScanPanel").gameObject;
        scanPanel.GetComponent<Button>().onClick.AddListener(OnScanButtonClicked);
        scanPanelText = scanPanel.transform.Find("Info").gameObject.GetComponent<TextMeshProUGUI>();
        scanPanelImage = scanPanel.transform.Find("Image").GetComponent<Image>();
        repeatScanButton = scanPanel.transform.Find("RepeatScanButton").GetComponent<Button>();
        repeatScanButton.onClick.AddListener(OnScanButtonClicked);
    }

    void Start()
    {
        // Store original defaultPanelText size
        originalScaleDefaultPanelText = scanPanelImage.transform.localScale;
    }

    void Update()
    {
        float scaleFactor = 1 + Mathf.Sin(Time.time * 2f) * 0.1f; // Sin wave for smooth breathing
        scanPanelImage.transform.localScale = originalScaleDefaultPanelText * scaleFactor; // Apply scaling
    }

    void OnScanButtonClicked()
    {
        Debug.Log("Scan button clicked");
        closestBeacon = null;
        scanPanel.GetComponent<Button>().onClick.RemoveAllListeners();
        scanPanel.SetActive(true);

        repeatScanButton.gameObject.SetActive(false);
        scanPanelImage.GetComponent<Image>().sprite = magnifientGlassSprite;
        scanPanelText.GetComponent<TextMeshProUGUI>().text = "Getting the room ready...";

        Debug.Log("Requesting BLE scan...");

        RequestBLEScan();
    }

    private void RequestBLEScan()
    {
        // Start the BLE scan
        StartScanning();

        // Wait until closestBeacon is not null
        StartCoroutine(WaitForClosestBeacon());
    }

    private IEnumerator WaitForClosestBeacon()
    {
        Debug.Log("Waiting for closest beacon...");
        while (closestBeacon == null)
        {
            yield return null; // Wait for the next frame
        }

        // set the room name and room name
        scanPanelImage.GetComponent<Image>().sprite = closestBeacon.roomSprite;
        scanPanelText.GetComponent<TextMeshProUGUI>().text = $"Tap to enter {closestBeacon.roomName}";

        // Activate "Scan Again" button
        repeatScanButton.gameObject.SetActive(true);

        // Add a click listener to the scan panel
        scanPanel.GetComponent<Button>().onClick.AddListener(() => OnRoomSelected(closestBeacon));
    }

    private void OnRoomSelected(SceneObject sceneInfo)
    {
        Debug.Log("Room selected: " + sceneInfo.roomName);

        // Select the room in the unity scenes
        SceneManager.LoadScene(sceneInfo.roomName);
    }

    public void StartScanning()
    {
        if (isScanning) return;
        isScanning = true;

        beaconRSSIs.Clear();
        Debug.Log("Starting BLE scan for " + scanDuration + " seconds...");

        List<string> serviceUUIDs = new List<string>();

        if (sceneObjects.Length > 0)
        {
            // Logs beacon information and adds to UUID list
            foreach (SceneObject sceneObject in sceneObjects)
            {
                Debug.Log($"SceneObject UUID: {sceneObject.UUID}");
                Debug.Log($"SceneObject Room: {sceneObject.roomName}");
                serviceUUIDs.Add(sceneObject.UUID);
            }
        }
        else
        {
            Debug.Log("SceneObject's are missing!");
            return;
        }

        // Initialize Bluetooth
        BluetoothLEHardwareInterface.Initialize(true, false, () =>
        {
            Debug.Log("Bluetooth Initialized");
            // Scan for any peripherals
            BluetoothLEHardwareInterface.ScanForPeripheralsWithServices(null, null, (deviceUUID, deviceName, rssi, _) =>
            { 
                if (serviceUUIDs.Contains(deviceUUID))
                {
                    if (!beaconRSSIs.ContainsKey(deviceUUID))
                    {
                        beaconRSSIs[deviceUUID] = new List<int>();
                    }
                    beaconRSSIs[deviceUUID].Add(rssi);
                }
                
            }, true);
            Invoke("StopScanning", scanDuration);
        }, (error) => { Debug.Log("Bluetooth Error: " + error); });
    }

    void StopScanning()
    {
        BluetoothLEHardwareInterface.StopScan();
        isScanning = false;
        Debug.Log("BLE scan stopped");

        ProcessBeaconRSSIs();
    }

    private void ProcessBeaconRSSIs()
    {
        if (beaconRSSIs.Count == 0)
        {
            Debug.Log("No beacons detected");
            return;
        }

        string closestBeaconUUID = "";
        int highestAvgRSSI = int.MinValue;

        foreach (var entry in beaconRSSIs)
        {
            int avgRSSI = (int) entry.Value.Average();
            Debug.Log($"Beacon {entry.Key} -> Avg RSSI: {avgRSSI}");

            if (avgRSSI > highestAvgRSSI) // Check if it's the strongest
            {
                highestAvgRSSI = avgRSSI;
                closestBeaconUUID = entry.Key;
            }
        }

        closestBeacon = GetSceneObjectByUUID(closestBeaconUUID);

        Debug.Log($"Closest beacon: Device: {closestBeacon.roomName} | UUID: {closestBeaconUUID} | Avg RSSI: {highestAvgRSSI}");


    }

    void OnDisable()
    {
        BluetoothLEHardwareInterface.StopScan();
    }

    private SceneObject GetSceneObjectByUUID(string UUID)
    {
        return sceneObjects.FirstOrDefault(x => x.UUID == UUID);
    }

}

