using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;
using UnityEngine.Video;

public class ScreenSystemController : MonoBehaviour
{
    public VideoClip[] videoClips;

    public Texture[] images;

    private List<GazeInteractionController> screens;
    private List<GazeInteractionController> spawnedChildren = new List<GazeInteractionController>();
    private Queue<GazeInteractionController> screenQueue = new Queue<GazeInteractionController>();
    private List<Tuple<Texture, VideoClip>> unassignedAssets = new List<Tuple<Texture, VideoClip>>();
    private bool allSpawned = false;
    private bool screenDisappeared = false;
    private int disappearedScreenCount = 0;
    public int nonExistentInstanceId = -99;
    public float screenDisappearThreshold = 500f;
    public float restartWaitThreshold = 5f;
    private float restartWaitTime = 0f;

    void Awake() {
        GroupImageWithVideoClips();
        screens = new List<GazeInteractionController>(this.GetComponentsInChildren<GazeInteractionController>());

        foreach (GazeInteractionController screen in screens) {
            screen.ReturnToOriginalPosition();
            screen.gameObject.SetActive(false);
        }

        Debug.Log("Found " + screens.Count.ToString() + " Screens");
        Debug.Log("Found " + unassignedAssets.Count.ToString() + " Episodes");

        AssignEpisodesToScreenAndShuffle();
    }

    void OnEnable() {
        GazeInteractionController firstScreen = screenQueue.Dequeue();
        firstScreen.gameObject.SetActive(true);
        firstScreen.SetFirstScreen(true);
        spawnedChildren.Add(firstScreen);
    }

    void Update()
    { 
        if (allSpawned && !screenDisappeared) {
            int selectedScreen = GetSelectedScreen();
            foreach (GazeInteractionController screen in spawnedChildren) {
                if (screen.isActiveAndEnabled && screen.GetInstanceID() != selectedScreen) {
                    if (screen.GetDistanceFromOriginalPos() < screenDisappearThreshold) {
                        screen.MoveAwayFromCamera();
                    } else {
                        screen.gameObject.SetActive(false);
                        disappearedScreenCount += 1;
                    }
                }
            }

            if (disappearedScreenCount == spawnedChildren.Count) {
                screenDisappeared = true;
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

        if (screenQueue.Count > 0) {
            GazeInteractionController nextScreen = screenQueue.Dequeue();
            nextScreen.gameObject.SetActive(true);
            spawnedChildren.Add(nextScreen);
        } else if (screenQueue.Count == 0) {
            allSpawned = true;
            foreach (GazeInteractionController screen in spawnedChildren)
            {
                screen.SetAllSpawned(true);
            }
        }
    }

    public void StartFocusedMode(int instanceId) {

        if (allSpawned) {
            foreach (GazeInteractionController screen in spawnedChildren) {
                if (screen.gameObject.GetInstanceID() != instanceId) {
                    StopCoroutine(screen.AudioVolumeFadeIn());
                    StartCoroutine(screen.AudioVolumeFadeOut());
                }
            }
        }
    }

    public void ExitFocusedMode(int instanceId) {
        if (allSpawned) {
            foreach (GazeInteractionController screen in spawnedChildren) {
                if (screen.gameObject.GetInstanceID() != instanceId) {
                    StopCoroutine(screen.AudioVolumeFadeOut());
                    StartCoroutine(screen.AudioVolumeFadeIn());
                }
            }
        }
    }

    private void AssignEpisodesToScreenAndShuffle() {
        while (unassignedAssets.Count > 0) {

            int episodeIdx = UnityEngine.Random.Range(0, unassignedAssets.Count);
            GazeInteractionController curScreen = screens[unassignedAssets.Count % screens.Count];
            Debug.Log("Episode [" + episodeIdx.ToString() + "] goes to Screen [" + (unassignedAssets.Count % screens.Count).ToString() + "].");
            VideoClip videoClip = unassignedAssets[episodeIdx].Item2;
            curScreen.AddVideoClip(videoClip);
            if (!curScreen.IsImageSet()) {
                Texture image = unassignedAssets[episodeIdx].Item1;
                curScreen.SetImage(image);
            }
            unassignedAssets.RemoveAt(episodeIdx);
        }

        while (screens.Count > 0) {
            int randomIdx = UnityEngine.Random.Range(0, screens.Count);
            GazeInteractionController screen = screens[randomIdx];
            screenQueue.Enqueue(screen);
            screens.RemoveAt(randomIdx);
        }
    }
    private int GetSelectedScreen() {
        foreach (GazeInteractionController screen in spawnedChildren) {
            if (screen.GetIsSelected()) {
                return screen.GetInstanceID();
            }
        }

        return nonExistentInstanceId;
    }

    private void GroupImageWithVideoClips() {
        if (videoClips.Length.Equals(images.Length)) {
            for (int i = 0; i < videoClips.Length; i++) {
                unassignedAssets.Add(new Tuple<Texture, VideoClip>(images[i], videoClips[i]));
            }
        } else {
            Debug.LogError("VideoClips and Images do not have the same size");
        }
    }

    private void Restart() {
        Debug.Log("Scene restarted!");
        Scene scene = SceneManager.GetActiveScene(); 
        SceneManager.LoadScene(scene.name);
    }
}
