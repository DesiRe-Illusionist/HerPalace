using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

public class ScreenSystemController : MonoBehaviour
{
    public VideoClip[] videoClips;

    // Start is called before the first frame update
    private List<GazeInteractionController> spawnedChildren;
    private List<GazeInteractionController> screens;

    private Queue<GazeInteractionController> screenQueue;

    private List<VideoClip> unassignedVideoClip;

    private bool allSpawned = false;

    private bool screenDisappeared = false;

    public int nonExistentInstanceId = -99;

    public float screenDisappearThreshold = 50f;

    public float restartWaitThreshold = 5f;

    private float restartWaitTime = 0f;


    void Awake() {
        spawnedChildren = new List<GazeInteractionController>();
        screens = new List<GazeInteractionController>(this.GetComponentsInChildren<GazeInteractionController>());
        unassignedVideoClip = new List<VideoClip>(videoClips);
        screenQueue = new Queue<GazeInteractionController>();

        foreach (GazeInteractionController screen in screens) {
            screen.ReturnToOriginalState();
            screen.gameObject.SetActive(false);
        }

        Debug.Log("Found " + screens.Count.ToString() + " Screens");
        Debug.Log("Found " + unassignedVideoClip.Count.ToString() + " VideoClips");

        AssignVideoToScreenAndShuffle();
    }
    void Start()
    {
        GazeInteractionController firstScreen = screenQueue.Dequeue();
        firstScreen.gameObject.SetActive(true);
        firstScreen.SetFirstScreen(true);
        spawnedChildren.Add(firstScreen);
    }

    // Update is called once per frame
    void Update()
    {
        if (!allSpawned && screenQueue.Count == 0) {
            allSpawned = true;

            foreach (GazeInteractionController screen in spawnedChildren)
            {
                screen.SetAllSpawned(true);
            }
        }

        if (allSpawned && !screenDisappeared) {
            int selectedScreen = GetSelectedScreen();
            if (selectedScreen == nonExistentInstanceId) {
                if (spawnedChildren[0].GetDistanceFromCamera() < screenDisappearThreshold) {
                    foreach (GazeInteractionController screen in spawnedChildren) {
                        screen.MoveAwayFromCamera();
                    }
                } else {
                    foreach (GazeInteractionController screen in spawnedChildren) {
                        screen.Disappear();
                    }
                    screenDisappeared = true;
                }
            } else {
                foreach (GazeInteractionController screen in spawnedChildren)
                {
                    if (screen.GetInstanceID() != selectedScreen) {
                        screen.ReturnToOriginalState();
                    }
                }
            }
        }

        if (allSpawned && screenDisappeared) {
            restartWaitTime += Time.deltaTime;

            if (restartWaitTime > restartWaitThreshold) { 
                Restart();
            }
        }
    }

    public void SpawnNext() {
        Debug.Log("SpawnNext method called!");

        if (screenQueue.Count > 0) {
            GazeInteractionController nextScreen = screenQueue.Dequeue();
            nextScreen.gameObject.SetActive(true);
            spawnedChildren.Add(nextScreen);
        }
    }

    public void StartFocusedMode(int instanceId) {
        // Debug.Log("StartFocusMode method called!");

        if (allSpawned) {
            foreach (GazeInteractionController screen in spawnedChildren) {
                if (screen.gameObject.GetInstanceID() != instanceId) {
                    screen.Dim();
                }
            }
        }
    }

    private void AssignVideoToScreenAndShuffle() {
        while (unassignedVideoClip.Count > 0) {

            int videoClipIdx = Random.Range(0, unassignedVideoClip.Count);
            Debug.Log("videoClip [" + videoClipIdx.ToString() + "] goes to Screen [" + (unassignedVideoClip.Count % screens.Count).ToString() + "].");
            VideoClip videoClip = unassignedVideoClip[videoClipIdx];
            screens[unassignedVideoClip.Count % screens.Count].addVideoClip(videoClip);
            unassignedVideoClip.RemoveAt(videoClipIdx);
        }

        while (screens.Count > 0) {
            int randomIdx = Random.Range(0, screens.Count);
            GazeInteractionController screen = screens[randomIdx];
            screenQueue.Enqueue(screen);
            screens.RemoveAt(randomIdx);
        }
    }
    private int GetSelectedScreen() {
        foreach (GazeInteractionController screen in spawnedChildren) {
            if (screen.isSelected()) {
                return screen.GetInstanceID();
            }
        }

        return nonExistentInstanceId;
    }

    private void Restart() {
        Debug.Log("Scene restarted!");
        allSpawned = false;
        screenDisappeared = false;
        restartWaitTime = 0f;
        Awake();
        Start();
    }
}
