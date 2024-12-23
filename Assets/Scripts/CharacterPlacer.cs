using System.Collections;
using System.Collections.Generic;
using Inworld;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using EnhancedTouch = UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.EventSystems;
using Inworld.Sample.Innequin;

[RequireComponent(typeof(ARRaycastManager), typeof(ARPlaneManager))]
public class CharacterPlacer : MonoBehaviour
{
    [SerializeField] private GameObject characterPrefab;

    private GameObject characterInstance;
    private ARRaycastManager raycastManager;
    private ARPlaneManager planeManager;
    private List<ARRaycastHit> hits = new List<ARRaycastHit>();

    private void Awake()
    {
        raycastManager = GetComponent<ARRaycastManager>();
        planeManager = GetComponent<ARPlaneManager>();
    }

    private void OnEnable()
    {
        EnhancedTouch.TouchSimulation.Enable();
        EnhancedTouch.EnhancedTouchSupport.Enable();
        EnhancedTouch.Touch.onFingerDown += TouchedScreen;
        EnhancedTouch.Touch.onFingerMove += TouchedScreen;
    }

    private void OnDisable()
    {
        EnhancedTouch.TouchSimulation.Disable();
        EnhancedTouch.EnhancedTouchSupport.Disable();
        EnhancedTouch.Touch.onFingerDown -= TouchedScreen;
        EnhancedTouch.Touch.onFingerMove -= TouchedScreen;
    }

    private void TouchedScreen(EnhancedTouch.Finger finger)
    {
        if (finger.index != 0)
            return;

        // if (EventSystem.current.IsPointerOverGameObject(finger.currentTouch.touchId))
        //     return;
        

        if (raycastManager.Raycast(finger.currentTouch.screenPosition, hits, TrackableType.PlaneWithinPolygon))
        {
            foreach (var hit in hits)
            {
                var pose = hit.pose;

                // Make the character face the camera, but lock the Z rotation
                Vector3 direction = Camera.main.transform.position - pose.position;
                direction.y = 0; // Lock the Z rotation by setting Y component to 0
                pose.rotation = Quaternion.LookRotation(direction);

                if (characterInstance == null)
                {
                    characterInstance = Instantiate(characterPrefab, pose.position, pose.rotation);
                    InworldCharacter inworldCharacter = characterInstance.GetComponent<InworldCharacter>();

                    inworldCharacter.Event.onBeginSpeaking.AddListener(MainController.Instance.OnBeginSpeaking);
                    inworldCharacter.Event.onEndSpeaking.AddListener(MainController.Instance.OnEndSpeaking);
                    inworldCharacter.Event.onGoalCompleted.AddListener(MainController.Instance.OnGoalComplete);

                    StartCoroutine(GreetPlayer());
                } else {
                    characterInstance.transform.SetPositionAndRotation(pose.position, pose.rotation);
                }
            }
        }
    }

    private IEnumerator GreetPlayer()
    {
        Debug.Log("Finding Player...");
        while(InworldController.CurrentCharacter == null)
        {
            yield return null;
        }
        Debug.Log("Character Found");
        InworldController.CurrentCharacter.SendTrigger("greeting", false);
    }
}
