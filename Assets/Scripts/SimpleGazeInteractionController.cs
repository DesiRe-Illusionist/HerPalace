using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.Video;

public class SimpleGazeInteractionController : MonoBehaviour
{
    public XRSimpleInteractable interactable;

    public VideoClip videoClip;
    float hoverTimer;
    public float hoverToSelectThreshold = 2;
    public float hoverToActivateThreshold = 6;

    private VideoPlayer videoPlayer;

    private AudioSource audioSource;

    private bool spawnedNext = false;

    private bool allSpawned = false;

    private bool hasBeenHovered = false;

    private bool videoAssigned = false;

    private double videoTime = 0;

    private Vector3 originalPosition;

    void OnEnable() {
        videoPlayer = this.GetComponent<VideoPlayer>();
        videoPlayer.clip = videoClip;
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

        if (videoPlayer.isActiveAndEnabled) {
            videoPlayer.time = 0;
            videoPlayer.Prepare();
        } else {
            Debug.LogError("VideoPlayer is not active and enabled");
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (interactable.isHovered) {

            Debug.Log("Is Hovered");

            hasBeenHovered = true;
            hoverTimer += Time.deltaTime;
            double spawnThreshold = Math.Max(this.videoPlayer.clip.length - videoTime - 2, hoverToActivateThreshold);
            Debug.Log("SpawnThreshold is " + spawnThreshold.ToString());

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
    }

    private void PlayVideo() {

        if (!videoPlayer.isPlaying) {
            videoPlayer.Play();
            audioSource.Play();
        }
    }

    private void PlayVideoAndSpawnNext() {
        PlayVideo();

        if (!spawnedNext) {
            spawnedNext = true;
        }
    }
    private void PauseVideo() {
        videoPlayer.Pause();
        audioSource.Pause();
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
        float step = GetDistanceFromCamera() * 0.01f;
        this.transform.position = Vector3.MoveTowards(
            this.transform.position,
            originalPosition,
            step
        );

        this.audioSource.volume = 1.0f;
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
}
