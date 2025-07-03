using System.Collections;
using System.Collections.Generic;
using Inworld;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using EnhancedTouch = UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.EventSystems;
using Inworld.Sample.Innequin;
using UnityEngine.InputSystem.EnhancedTouch;
using Inworld.Entities;

[RequireComponent(typeof(ARRaycastManager), typeof(ARPlaneManager))]
public class CharacterPlacer : MonoBehaviour
{
    [SerializeField] private GameObject characterPrefab;
    [SerializeField] private GameObject instructionPanel;

    private GameObject characterInstance;
    private ARRaycastManager raycastManager;
    private ARPlaneManager planeManager;
    private List<ARRaycastHit> hits = new List<ARRaycastHit>();
    private bool canPositionCharacter = true;

    private void Awake()
    {
        raycastManager = GetComponent<ARRaycastManager>();
        planeManager = GetComponent<ARPlaneManager>();

        // Show instructions when the app starts
        ShowInstructions();
    }

    private void OnEnable()
    {
        EnhancedTouch.TouchSimulation.Enable();
        EnhancedTouch.EnhancedTouchSupport.Enable();
        EnhancedTouch.Touch.onFingerDown += TouchedScreen;
        EnhancedTouch.Touch.onFingerMove += TouchedScreen;
        EnhancedTouch.Touch.onFingerUp += HidePlanes;
    }

    private void OnDisable()
    {
        EnhancedTouch.TouchSimulation.Disable();
        EnhancedTouch.EnhancedTouchSupport.Disable();
        EnhancedTouch.Touch.onFingerDown -= TouchedScreen;
        EnhancedTouch.Touch.onFingerMove -= TouchedScreen;
        EnhancedTouch.Touch.onFingerUp -= HidePlanes;
    }

    private void TouchedScreen(EnhancedTouch.Finger finger)
    {
        if (finger.index != 0)
            return;

        if (IsTouchOverUI(finger))
            return;

        if (raycastManager.Raycast(finger.currentTouch.screenPosition, hits, TrackableType.PlaneWithinPolygon))
        {
            ShowPlanes();
            HideInstructions();
            
            foreach (var hit in hits)
            {
                var pose = hit.pose;

                // Make the character face the camera, but lock the Z rotation
                Vector3 direction = Camera.main.transform.position - pose.position;
                direction.y = 0; // Lock the Z rotation by setting Y component to 0
                pose.rotation = Quaternion.LookRotation(direction);

                if (characterInstance == null)
                {
                    InstantiateCharacter(pose);
                }
                else
                {
                    characterInstance.transform.SetPositionAndRotation(pose.position, pose.rotation);
                }
            }
        }
    }

    private bool IsTouchOverUI(EnhancedTouch.Finger finger)
    {
        PointerEventData pointerEventData = new PointerEventData(EventSystem.current)
        {
            position = finger.currentTouch.screenPosition
        };

        List<RaycastResult> raycastResults = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerEventData, raycastResults);

        return raycastResults.Count > 0;
    }

    private void InstantiateCharacter(Pose pose)
    {
        characterInstance = Instantiate(characterPrefab, pose.position, pose.rotation);

        InworldCharacter character = characterInstance.GetComponent<InworldCharacter>();

        character.Event.onBeginSpeaking.AddListener(MainController.Instance.OnBeginSpeaking);
        character.Event.onEndSpeaking.AddListener(MainController.Instance.OnEndSpeaking);
        character.Event.onGoalCompleted.AddListener(MainController.Instance.OnGoalComplete);

        Debug.Log("Character Instance: " + characterInstance);

        // make the character canvas face the camera so that the user can always read the chat
        GameObject canvas = characterInstance.transform.Find("Canvas").gameObject;
        canvas.AddComponent<MaintainCanvasPosition>();

        Debug.Log("Loading Scene: " + InworldController.Instance.CurrentScene);
        StartCoroutine(GreetPlayer());
    }

    private void HidePlanes(EnhancedTouch.Finger finger)
    {
        foreach (var plane in planeManager.trackables)
        {
            plane.gameObject.SetActive(false);
        }
    }

    private void ShowPlanes()
    {
        foreach (var plane in planeManager.trackables)
        {
            plane.gameObject.SetActive(true);
        }
    }

    // Greet the player after the character is placed
    private IEnumerator GreetPlayer()
    {
        Debug.Log("Finding Player...");
        while (InworldController.CurrentCharacter == null || InworldController.Client == null)
        {
            yield return null;
        }
        Debug.Log("Character Found");
        InworldController.CurrentCharacter.SendTrigger("greeting", false);
    }
    
    private void ShowInstructions()
    {
        if (instructionPanel != null)
        {
            instructionPanel.SetActive(true);
        }
    }

    private void HideInstructions()
    {
        if (instructionPanel != null)
        {
            instructionPanel.SetActive(false);
        }
    }
}
