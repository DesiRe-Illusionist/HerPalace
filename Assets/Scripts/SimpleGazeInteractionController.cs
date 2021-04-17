using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.Video;

public class SimpleGazeInteractionController : MonoBehaviour
{
    public XRSimpleInteractable interactable;
    public Texture imageTexture;
    public Texture videoTexture;
    public VideoClip videoClip;
    public float hoverToSelectThreshold = 2;
    public float hoverToActivateThreshold = 6;
    public bool isFirstScreen = false;

    public XRRayInteractor interactor;

    public List<XRSimpleInteractable> allInteractables;

    private VideoPlayer videoPlayer;
    private AudioSource audioSource;
    private Material material;
    private Vector3 originalPosition;
    private bool isHoverEnabled = true;
    private bool hasBeenHovered = false;
    private float hoverTimer = 0;

    // public Vector3 center = new Vector3(0,0,0);
    // public Vector3 axis = Vector3.up;
    // public float radius = 4.0f;
    // public float radiusSpeed = 0.05f;
    // public float rotationSpeed = 8.0f; 

    void Awake() {
        videoPlayer = this.GetComponent<VideoPlayer>();
        videoPlayer.clip = videoClip;
        audioSource = videoPlayer.GetComponentInChildren<AudioSource>();
        videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
        videoPlayer.EnableAudioTrack(0, true);
        videoPlayer.SetTargetAudioSource(0, audioSource);
        videoPlayer.controlledAudioTrackCount = 1;
        audioSource.volume = 1.0f;

        material = this.GetComponent<MeshRenderer>().material;
        material.color = Color.gray;

        originalPosition = this.gameObject.transform.position;

        // transform.position = (transform.position - center).normalized * radius + center;
    }

    void OnEnable() {
        if (this.isFirstScreen) {
            material.mainTexture = imageTexture;
            isHoverEnabled = false;
        } else {
            material.mainTexture = videoTexture;
        }
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

        if (isFirstScreen) {
            StartCoroutine(firstScreenFadeIn());
        } else {
            StartCoroutine(playVideoAtStart());
        }
    }

    // Update is called once per frame
    void Update()
    {
        // transform.RotateAround(center, axis, rotationSpeed * Time.deltaTime);
        // Vector3 desiredPosition = (transform.position - center).normalized * radius + center;
        // transform.position = Vector3.MoveTowards(transform.position, desiredPosition, Time.deltaTime * radiusSpeed);

        if (isHoverEnabled) {
            if (isHovered()) {
                hasBeenHovered = true;
                if (material.color.r < 1.0f) {
                    StartCoroutine(screenHighlight());
                }

                hoverTimer += Time.deltaTime;
                double spawnThreshold = Math.Max(
                    this.videoPlayer.clip.length - this.videoPlayer.time - 2, 
                    hoverToActivateThreshold
                );

                if (hoverTimer > hoverToSelectThreshold) {

                    if (isFirstScreen && material.mainTexture.Equals(imageTexture)) {
                        StartCoroutine(animateFirstScreen());
                    } else {
                        PlayVideo();
                    }
                }
            } else {
                if (!Color.gray.Equals(material.color)) {
                    material.color = Color.gray;
                }

                PauseVideo();
            }
        }
    }

    private void PlayVideo() {

        if (isFirstScreen && !material.mainTexture.Equals(videoTexture)) {
            material.mainTexture = videoTexture;
            material.color = Color.white;
        }

        if (!videoPlayer.isPlaying) {
            videoPlayer.Play();
            audioSource.Play();
        }
    }

    private void PauseVideo() {
        if (this.hasBeenHovered && videoPlayer.isPlaying) {
            videoPlayer.Pause();
            audioSource.Pause();
        }
    }

    private bool isHovered() {
        List<XRBaseInteractable> potentialInteractables = new List<XRBaseInteractable>(allInteractables);
        interactor.GetValidTargets(potentialInteractables);
        if (potentialInteractables.Count > 0 && potentialInteractables[0] == (XRBaseInteractable) interactable) {
            return true;
        }

        return false;
    }

    private IEnumerator animateFirstScreen() {
        Color curColor = material.color;
        float fadeRate = 0.01f;
        while (curColor.r > 0) {
            curColor.r -= fadeRate;
            curColor.g -= fadeRate;
            curColor.b -= fadeRate;
            material.color = curColor;
            yield return null;
        }

        Debug.Log("Faded to black. Start playing video");
        PlayVideo();
    }

    private IEnumerator firstScreenFadeIn() {
        material.color = Color.black;
        Color curColor = material.color;
        float fadeRate = 0.001f;
        while (curColor.r < 0.5) {
            curColor.r += fadeRate;
            curColor.g += fadeRate;
            curColor.b += fadeRate;
            material.color = curColor;
            yield return null;
        }

        isHoverEnabled = true;
    }

    private IEnumerator screenHighlight() {
        Color curColor = material.color;
        float fadeRate = 0.01f;
        while (curColor.r < 1.0f) {
            curColor.r += fadeRate;
            curColor.g += fadeRate;
            curColor.b += fadeRate;
            material.color = curColor;
            yield return null;
        }
    }

    private IEnumerator playVideoAtStart() {
        while (!videoPlayer.isPrepared || videoPlayer.clip == null) {
            videoPlayer.Prepare();
            Debug.Log("Preparing Video");
            yield return null;
        }

        Debug.Log("Video player prepared");
        PlayVideo();
    }



}
