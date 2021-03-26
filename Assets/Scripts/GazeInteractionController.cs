using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.Video;


public class GazeInteractionController : MonoBehaviour
{
    public XRSimpleInteractable interactable;
    public float hoverToSelectThreshold = 3;
    public float hoverToActivateThreshold = 6;
    public bool isFirstScreen = false;

    private Queue<VideoClip> videoClips = new Queue<VideoClip>();
    private VideoPlayer videoPlayer;
    private AudioSource audioSource;
    private float hoverTimer;
    private bool spawnedNext = false;
    private bool allSpawned = false;
    private bool hasBeenHovered = false;
    private bool videoAssigned = false;
    private Vector3 originalPosition;
    
    void Awake() {
        videoPlayer = this.GetComponent<VideoPlayer>();
        audioSource = videoPlayer.GetComponentInChildren<AudioSource>();
        videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
        videoPlayer.EnableAudioTrack(0, true);
        videoPlayer.SetTargetAudioSource(0, audioSource);
        videoPlayer.controlledAudioTrackCount = 1;
        audioSource.volume = 1.0f;

        originalPosition = this.gameObject.transform.position;
    }

    void OnEnable() {
        StartCoroutine(PrepareVideoClips());
    }

    void OnDisable() {
        videoPlayer.Stop();
        Debug.Log("Script disabled");
    }

    // Start is called before the first frame update
    void Start()
    {
        hoverTimer = 0;
        if (videoAssigned) {
            if (videoPlayer.isActiveAndEnabled) {
                videoPlayer.Prepare();
                if (isFirstScreen) {
                    Debug.Log("Show first frame");
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

                    double spawnThreshold = Math.Max(
                        this.videoPlayer.clip.length - this.videoPlayer.time - 2, 
                        hoverToActivateThreshold
                    );

                    Debug.Log("SpawnThreshold is " + spawnThreshold.ToString());

                    if (hoverTimer > hoverToSelectThreshold && hoverTimer <= spawnThreshold) {
                        PlayVideo();

                    } else if (hoverTimer > spawnThreshold) {
                        PlayVideoAndSpawnNext();
                    }
                } else {
                    if (hasBeenHovered) {
                        hoverTimer = 0;
                        PauseVideo();
                    }
                }
            } else {
                PlayVideo();

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
            Debug.Log("Videos not yet loaded");
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
            videoClips.Enqueue(videoPlayer.clip);
            LoadVideoClip();
            videoPlayer.time = 0;
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
            new Vector3(0f, this.transform.position.y, 0f),
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
        float step = GetDistanceFromCamera() * 0.01f;
        this.transform.position = Vector3.MoveTowards(
            this.transform.position,
            originalPosition,
            step
        );
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
        videoClips = new Queue<VideoClip>();

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
    }

    private void ShowFirstFrame() {
        videoPlayer.playOnAwake = true;
    }

    IEnumerator PrepareVideoClips() 
    {
        while (videoClips.Count == 0) {
            yield return null;
        }

        LoadVideoClip();
        Debug.Log("Start(): Screen has " + this.videoClips.Count + " videos.");
        this.videoAssigned = true;
    }
}