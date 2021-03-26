using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RawImage))]
public class OnHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {

    public Texture videoTexture;
    public Texture imageTexture;
    public VideoClip videoClip;
    public float hoverToSelectThreshold = 2;
    public float hoverToActivateThreshold = 6;
    public bool isFirstScreen = true;
    private RawImage image;
    private VideoPlayer videoPlayer;
    private AudioSource audioSource;
    private bool isHovered = false;
    private bool isHoverEnabled = true;

    private bool hasBeenHovered = false;
    private float hoverTimer = 0;
    private Vector3 originalPosition;


    void Awake() {
        videoPlayer = this.GetComponent<VideoPlayer>();
        videoPlayer.clip = videoClip;
        audioSource = videoPlayer.GetComponentInChildren<AudioSource>();
        videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
        videoPlayer.EnableAudioTrack(0, true);
        videoPlayer.SetTargetAudioSource(0, audioSource);
        videoPlayer.controlledAudioTrackCount = 1;
        audioSource.volume = 1.0f;

        originalPosition = this.gameObject.transform.position;

        image = GetComponent<RawImage> ();
        image.color = Color.gray;
    }

    void OnEnable() {
        if (this.isFirstScreen) {
            image.texture = imageTexture;
            isHoverEnabled = false;
        } else {
            image.texture = videoTexture;
        }
    }
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

    void Update() {

        if (isHovered && isHoverEnabled) {

            hoverTimer += Time.deltaTime;
            double spawnThreshold = Math.Max(
                this.videoPlayer.clip.length - this.videoPlayer.time - 2, 
                hoverToActivateThreshold
            );

            if (hoverTimer > hoverToSelectThreshold) {
                if (isFirstScreen && image.texture.Equals(imageTexture)) {
                    StartCoroutine(animateFirstScreen());
                } else {
                    PlayVideo();
                }

            }
        } else {
            PauseVideo();
        }
    }
    private void PlayVideo() {

        if (!image.texture.Equals(videoTexture)) {
            image.texture = videoTexture;
            image.color = Color.white;
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

    public void OnPointerEnter(PointerEventData eventData) 
    {
        OnHoverEnter ();
        this.isHovered = true;
        this.hasBeenHovered = true;
    }

    public void OnPointerExit(PointerEventData eventData) 
    {
        OnHoverExit ();
        this.isHovered = false;
    }

    void OnHoverEnter() 
    {
        if (isHoverEnabled) {
            image.color = Color.white;
        }
    }

    void OnHoverExit()
    {
        if (isHoverEnabled) {
            image.color = Color.grey;
        }
    }

    private IEnumerator animateFirstScreen() {
        Color curColor = image.color;
        float fadeRate = 0.01f;
        while (curColor.r > 0) {
            curColor.r -= fadeRate;
            curColor.g -= fadeRate;
            curColor.b -= fadeRate;
            image.color = curColor;
            yield return null;
        }

        Debug.Log("Faded to black. Start playing video");
        PlayVideo();
    }

    private IEnumerator firstScreenFadeIn() {
        image.color = Color.black;
        Color curColor = image.color;
        float fadeRate = 0.01f;
        while (curColor.r < 0.5) {
            curColor.r += fadeRate;
            curColor.g += fadeRate;
            curColor.b += fadeRate;
            image.color = curColor;
            yield return null;
        }

        isHoverEnabled = true;
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
