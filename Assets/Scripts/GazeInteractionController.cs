using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.Video;


public class GazeInteractionController : MonoBehaviour
{
    public XRSimpleInteractable interactable;

    private Queue<VideoClip> videoClips = new Queue<VideoClip>();
    float hoverTimer;
    public float hoverToSelectThreshold = 2;
    public float hoverToActivateThreshold = 6;

    public bool isFirstScreen = false;

    private VideoPlayer videoPlayer;

    private AudioSource audioSource;

    private bool spawnedNext = false;

    private bool allSpawned = false;

    private bool hasBeenHovered = false;

    private bool videoAssigned = false;

    private double videoTime = 0;

    private Vector3 originalPosition;

    private Texture2D videoFrame;

    private Renderer rend;
    private Texture tex;

    void OnEnable() {
        videoPlayer = this.GetComponent<VideoPlayer>();
        audioSource = videoPlayer.GetComponentInChildren<AudioSource>();
        videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
        videoPlayer.EnableAudioTrack(0, true);
        videoPlayer.SetTargetAudioSource(0, audioSource);
        videoPlayer.controlledAudioTrackCount = 1;
        audioSource.volume = 1.0f;

        originalPosition = this.gameObject.transform.position;
    }

    void OnDisable() {
        videoPlayer.Stop();
        Debug.Log("Script disabled");
    }

    // Start is called before the first frame update
    void Start()
    {
        hoverTimer = 0;
        videoFrame = new Texture2D(2, 2);
        if (videoClips.Count > 0) {
            videoAssigned = true;
            LoadVideoClip();
            Debug.Log("Start(): Screen has " + this.videoClips.Count + " videos.");
            if (videoPlayer.isActiveAndEnabled) {
                videoPlayer.time = 0;
                videoPlayer.Prepare();
                if (isFirstScreen) {
                    Debug.Log("Show first frame");
                    ShowFirstFrame();
                } else {
                    PlayVideo();
                }
            } else {
                Debug.LogError("VideoPlayer is not active and enabled");
            }
        } else {
            Debug.Log("Videos not yet loaded");
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (videoAssigned) {
            if (!allSpawned) {
                if (interactable.isHovered) {

                    hasBeenHovered = true;
                    hoverTimer += Time.deltaTime;
                    double spawnThreshold = Math.Max(this.videoPlayer.clip.length - videoTime - 2, hoverToActivateThreshold);

                    if (hoverTimer > hoverToSelectThreshold && hoverTimer <= spawnThreshold) {
                        PlayVideo();

                    } else if (hoverTimer > spawnThreshold) {
                        PlayVideoAndSpawnNext();
                    }
                } else {
                    videoTime = videoPlayer.time;
                    if (hasBeenHovered) {
                        hoverTimer = 0;
                        PauseVideo();
                    }
                }
            } else {
                if (!videoPlayer.isPlaying) {
                    PlayVideo();
                }

                if (interactable.isHovered) {
                    hoverTimer += Time.deltaTime;

                    if (hoverTimer > hoverToSelectThreshold && hoverTimer < hoverToActivateThreshold) {
                        ReturnToOriginalState();
                        FocusOnCurrentVideo();

                    } else if (hoverTimer >= hoverToActivateThreshold) {
                        MoveTowardCamera();
                    }

                } else {
                    if (hoverTimer >= hoverToActivateThreshold) {
                        ReturnToOriginalState();
                    }
                    hoverTimer = 0;
                }
            }
        } else {
            Start();
        }
    }

    public void Dim() {

        // Material material = this.GetComponent<MeshRenderer>().material;
        // Color currentColor = material.GetColor("current_color");
        // Debug.Log("The current color is " + currentColor.ToString());
        // currentColor.a = 0.3f;
        // material.SetColor("current_color", currentColor);

        this.audioSource.volume = 0.2f;
    }

    private void PlayVideo() {

        if (!videoPlayer.isPlaying) {
            videoPlayer.Play();
            audioSource.Play();
        }

        if (videoPlayer.time >= videoPlayer.clip.length) {
            LoadVideoClip();
            videoPlayer.Play();
            videoPlayer.SetTargetAudioSource(0, audioSource);
            audioSource.Play();
        }
    }

    private void PlayVideoAndSpawnNext() {
        PlayVideo();

        if (!spawnedNext) {
            SendMessageUpwards("SpawnNext", SendMessageOptions.RequireReceiver);
            spawnedNext = true;
        }
    }
    private void PauseVideo() {
        videoPlayer.Pause();
        audioSource.Pause();
    }

    private void FocusOnCurrentVideo() {

        this.SendMessageUpwards(
            "StartFocusedMode", 
            this.gameObject.GetInstanceID(), 
            SendMessageOptions.RequireReceiver
        );
    }

    public void MoveTowardCamera() {
        float step = Time.deltaTime * 0.1f;

        this.transform.position = Vector3.MoveTowards(
            this.transform.position,
            new Vector3(0f, 0f, 0f),
            step
        );
    }

    public void MoveAwayFromCamera() {
        float step = GetDistanceFromCamera() * 0.001f;
        this.transform.position = Vector3.MoveTowards(
            this.transform.position,
            new Vector3(
                this.transform.position.x * 3, 
                this.transform.position.y, 
                this.transform.position.z * 3
            ),
            step
        );
    }

    public void ReturnToOriginalState() {
        this.transform.position = originalPosition;
        this.audioSource.volume = 1.0f;
    }
    public void SetFirstScreen(bool value) {
        this.isFirstScreen = value;
    }

    public void SetAllSpawned(bool value) {
        Debug.Log("SetAllSpawned method invoked for " + this.gameObject.name);
        this.allSpawned = value;
    }

    public bool isSelected() {
        return interactable.isHovered && hoverTimer > hoverToSelectThreshold;
    }

    public float GetDistanceFromCamera() {
        return Vector3.Distance(new Vector3(0f, 0f, 0f), this.transform.position);
    }

    public void Disappear() {
        this.videoPlayer.Stop();
        this.audioSource.Stop();
        this.videoPlayer.time = 0;
        this.audioSource.time = 0;
        this.videoPlayer.targetTexture.Release();

        spawnedNext = false;
        allSpawned = false;
        videoAssigned = false;
    }

    public void addVideoClip(VideoClip videoClip) {
        this.videoClips.Enqueue(videoClip);
    }

    private void LoadVideoClip() {
        VideoClip clip = videoClips.Dequeue();
        videoPlayer.clip = clip;
        videoClips.Enqueue(clip);
    }

    private void ShowFirstFrame() {
        videoPlayer.playOnAwake = true;
    }
}

// namespace CustomExtensions
// {
//     public static class VideoPlayerExtentions 
//     {
//         public static IEnumerator ShowFirstFrame(this VideoPlayer player)
//         {
//             VideoPlayer.FrameReadyEventHandler frameReadyHandler = null;
//             bool frameReady = false;
//             bool oldSendFrameReadyEvents = player.sendFrameReadyEvents;
            
//             frameReadyHandler =  (source,index)=>{
//                 frameReady = true;
//                 player.frameReady -= frameReadyHandler;
//                 player.sendFrameReadyEvents = oldSendFrameReadyEvents;

//                 RenderTexture renderTexture = source.texture as RenderTexture;


//             };

//             player.frameReady += frameReadyHandler;
//             player.sendFrameReadyEvents = true;

//             player.Prepare();
//             player.StepForward();

//             while(!frameReady){
//                 yield return null;
//             }    
//         }  
//     }
// }
