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
    private Texture imageTexture;
    private Texture videoTexture;
    private Material material;
    private VideoPlayer videoPlayer;
    private AudioSource audioSource;
    private float hoverTimer = 0;
    private bool hasSpawnedNextEpisode = false;
    private bool allSpawned = false;
    private bool hasBeenHovered = false;
    private bool episodesAssigned = false;
    private bool isHoverEnabled = true;
    private Vector3 originalPosition;
    
    void Awake() {
        videoPlayer = this.GetComponent<VideoPlayer>();
        audioSource = videoPlayer.GetComponentInChildren<AudioSource>();
        videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
        videoPlayer.EnableAudioTrack(0, true);
        videoPlayer.SetTargetAudioSource(0, audioSource);
        videoPlayer.controlledAudioTrackCount = 1;
        audioSource.volume = 1.0f;
        videoPlayer.loopPointReached += ReloadVideoAndPlay;

        videoTexture = videoPlayer.targetTexture;

        material = this.GetComponent<MeshRenderer>().material;
        material.color = Color.gray;

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

        if (this.isFirstScreen) {
            material.mainTexture = imageTexture;
            isHoverEnabled = false;
        } else {
            material.mainTexture = videoTexture;
        }

        if (videoPlayer.isActiveAndEnabled) {
            StartCoroutine(PrepareVideoPlayer());
        } else {
            Debug.LogError("VideoPlayer is not active and enabled");
        }

        if (isFirstScreen) {
            StartCoroutine(FirstScreenFadeIn());
        } else {
            StartCoroutine(PlayVideoAtStart());
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (isHoverEnabled) {
            if (interactable.isHovered) {
                OnHoverEnter();
                hoverTimer += Time.deltaTime;

                if (allSpawned) {
                    PlayVideo();
                    if (hoverTimer > hoverToSelectThreshold) {
                        FocusOnCurrentVideo();

                        if (hoverTimer < hoverToActivateThreshold) {
                            ReturnToOriginalPosition();
                        } else if (hoverTimer >= hoverToActivateThreshold) {
                            MoveTowardCamera();
                        }
                    }  
                } else if (!hasSpawnedNextEpisode) {
                    double spawnThreshold = Math.Max(
                        this.videoPlayer.clip.length - this.videoPlayer.time - 2, 
                        hoverToActivateThreshold
                    );

                    if (hoverTimer > hoverToSelectThreshold && hoverTimer <= spawnThreshold) {
                        if (isFirstScreen && material.mainTexture.Equals(imageTexture)) {
                            StartCoroutine(AnimateFirstScreen());
                        } else {
                            PlayVideo();
                        }
                    } else if (hoverTimer > spawnThreshold) {
                        PlayVideoAndSpawnNext();
                    }
                } else {
                    if (hoverTimer > hoverToSelectThreshold) {
                        PlayVideo();
                    }
                }
            } else {
                OnHoverExit();
                
                if (allSpawned) {
                    PlayVideo();
                    if (hoverTimer >= hoverToActivateThreshold) {
                        ReturnToOriginalPosition();
                        audioSource.volume = 1.0f;
                    }
                } else {
                    if (hasBeenHovered) {
                        PauseVideo();
                    }
                }
                hoverTimer = 0;
            }
        }
    }

    public void Dim() {

        this.audioSource.volume = 0.2f;
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

    private void PlayVideoAndSpawnNext() {
        PlayVideo();

        if (!hasSpawnedNextEpisode) {
            SendMessageUpwards("SpawnNext", SendMessageOptions.RequireReceiver);
            hasSpawnedNextEpisode = true;
        }
    }
    private void PauseVideo() {
        if (videoPlayer.isPlaying) {
            videoPlayer.Pause();
            audioSource.Pause();
        }
    }

    private void FocusOnCurrentVideo() {

        audioSource.volume = 1.0f;
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

        float fadeStep = Time.deltaTime * 0.1f;
        Color curColor = this.material.color;
        curColor.r -= fadeStep;
        curColor.g -= fadeStep;
        curColor.b -= fadeStep;
        this.material.color = curColor;
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

    public void ReturnToOriginalPosition() {
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
        this.allSpawned = value;
    }

    public void SetEpisodesAssigned(bool value) {
        this.episodesAssigned = value;
    }

    public bool IsSelected() {
        return interactable.isHovered && hoverTimer > hoverToSelectThreshold;
    }

    public float GetDistanceFromCamera() {
        return Vector3.Distance(new Vector3(0f, 0f, 0f), this.transform.position);
    }

    public bool IsImageSet() {
        return this.imageTexture != null;
    }

    public void SetImage(Texture texture) {
        this.imageTexture = texture;
    }

    public void AddVideoClip(VideoClip videoClip) {
        this.videoClips.Enqueue(videoClip);
    }

    private void LoadVideoClip() {
        VideoClip clip = videoClips.Dequeue();
        videoPlayer.clip = clip;
    }


    private void OnHoverEnter() {
        hasBeenHovered = true;
        if (material.color.r < 1.0f) {
            // StartCoroutine(ScreenHighlight());
            material.color = Color.white;
        }
    }

    private void OnHoverExit() {
        if (!Color.gray.Equals(material.color)) {
            material.color = Color.gray;
        }
    }

    private IEnumerator AnimateFirstScreen() {
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

    private IEnumerator FirstScreenFadeIn() {
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

    private IEnumerator ScreenHighlight() {
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

    private IEnumerator PrepareVideoPlayer() 
    {
        LoadVideoClip();
        Debug.Log("PrepareVideoPlayer: Screen has " + this.videoClips.Count + " videos.");

        while (!videoPlayer.isPrepared) {
            videoPlayer.Prepare();
            yield return null;
        }
    }

    private IEnumerator PlayVideoAtStart() {
        while (!videoPlayer.isPrepared || videoPlayer.clip == null) {
            videoPlayer.Prepare();
            Debug.Log("Preparing Video");
            yield return null;
        }

        Debug.Log("Video player prepared");
        PlayVideo();
    }

    private IEnumerator ReloadVideoAndPlay() {
        VideoClip curClip = videoPlayer.clip;
        this.videoClips.Enqueue(curClip);
        Debug.Log("ReloadVideoAndPlay: Screen has " + this.videoClips.Count + " videos.");
        LoadVideoClip();

        while (!videoPlayer.isPrepared) {
            videoPlayer.Prepare();
            yield return null;
        }
        videoPlayer.time = 0;
        videoPlayer.SetTargetAudioSource(0, audioSource);
        videoPlayer.Play();
        audioSource.Play();
    }

    private void ReloadVideoAndPlay(UnityEngine.Video.VideoPlayer vp) {
        VideoClip curClip = vp.clip;
        this.videoClips.Enqueue(curClip);
        Debug.Log("ReloadVideoAndPlay: Screen has " + this.videoClips.Count + " videos.");
        LoadVideoClip();


        vp.time = 0;
        vp.SetTargetAudioSource(0, audioSource);
        vp.Play();
        vp.Play();
    }
}