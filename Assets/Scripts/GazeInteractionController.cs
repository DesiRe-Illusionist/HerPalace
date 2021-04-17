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
    public XRRayInteractor interactor;

    private XRBaseInteractable[] allInteractables;

    private Queue<VideoClip> videoClips = new Queue<VideoClip>();
    private Texture imageTexture;
    private Texture videoTexture;
    private Material material;
    private VideoPlayer videoPlayer;
    private AudioSource audioSource;
    private float hoverTimer = 0;
    private float totalHoverTime = 0;
    private bool isHovered = false;
    private bool hasSpawnedNextEpisode = false;
    private bool allSpawned = false;
    private bool hasBeenHovered = false;
    private bool isInFocusedMode = false;
    private bool isHoverEnabled = true;

    private bool isReloading = false;
    private Vector3 center = new Vector3(0,1.5f,0);
    private Vector3 rotationAxis = Vector3.up;
    private float radius = (float) new System.Random().NextDouble() * 4 + 6.0f; // random number between 6 - 10
    private float rotationSpeed = (float) new System.Random().NextDouble() * 2 + 3.0f; // random number between 3-5
    private float radiusSpeed = 0.05f;
    private Vector3 originalPosition;
    private float originalDistanceFromCamera;
    private float originalRotationSpeed;
    
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

        transform.position = (transform.position - center).normalized * radius + center;
        transform.LookAt(center, Vector3.up);

        originalPosition = transform.position;
        originalDistanceFromCamera = radius;
        originalRotationSpeed = rotationSpeed;

        allInteractables = this.GetComponentsInParent<XRBaseInteractable>();
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
        transform.LookAt(center, Vector3.up);
        this.isHovered = IsHovered();

        if (allSpawned) {
            if (!this.isHovered || hoverTimer <= hoverToSelectThreshold) {
                transform.RotateAround(center, rotationAxis, rotationSpeed * Time.deltaTime);
                Vector3 desiredPosition = (transform.position - center).normalized * radius + center;
                transform.position = Vector3.MoveTowards(transform.position, desiredPosition, Time.deltaTime * radiusSpeed);
            }
        }

        if (isHoverEnabled) {
            if (this.isHovered) {
                OnHoverEnter();
                hoverTimer += Time.deltaTime;

                if (allSpawned) {
                    PlayVideo();
                    FocusOnCurrentVideo();

                    if (hoverTimer > hoverToSelectThreshold && hoverTimer < hoverToActivateThreshold) {
                        ReturnToOriginalPosition();
                    } else if (hoverTimer >= hoverToActivateThreshold) {
                        MoveTowardCamera();
                    }
                } else { 
                    if (hoverTimer > hoverToSelectThreshold) {
                        if (isFirstScreen && material.mainTexture.Equals(imageTexture)) {
                            StartCoroutine(AnimateFirstScreen());
                        } else {
                            PlayVideo();
                        }
                    }
                }
            } else {
                OnHoverExit();
                
                if (allSpawned) {
                    PlayVideo();

                    if (isInFocusedMode) {
                        ReleaseFocus();
                    }
                    if (hoverTimer >= hoverToActivateThreshold) {
                        ReturnToOriginalPosition();
                        audioSource.volume = 1.0f;
                    }
                } else {
                    if (hasBeenHovered) {
                        PauseVideo();
                    }
                }

                totalHoverTime += hoverTimer;
                Vector3 newRotationAxis = Vector3.up;
                newRotationAxis.x += UnityEngine.Random.Range(-0.1f, 0.1f);
                newRotationAxis.z += UnityEngine.Random.Range(-0.1f, 0.1f);
                rotationAxis = newRotationAxis.normalized;

                if (totalHoverTime > 60) {
                    rotationSpeed = originalRotationSpeed * (totalHoverTime / 60f);
                }
                hoverTimer = 0;
            }
        }
    }

    private void PlayVideo() {

        if (isFirstScreen && !material.mainTexture.Equals(videoTexture)) {
            material.mainTexture = videoTexture;
            material.color = Color.white;
        }
        
        if (!videoPlayer.isPlaying && !isReloading) {
            videoPlayer.Play();
            audioSource.Play();
        }
    }

    private void PauseVideo() {
        if (videoPlayer.isPlaying) {
            videoPlayer.Pause();
            audioSource.Pause();
        }
    }

    private void FocusOnCurrentVideo() {
        isInFocusedMode = true;
        StopCoroutine(AudioVolumeFadeOut());
        StartCoroutine(AudioVolumeFadeIn());
        this.SendMessageUpwards(
            "StartFocusedMode", 
            this.gameObject.GetInstanceID(), 
            SendMessageOptions.RequireReceiver
        );
    }

    private void ReleaseFocus() {
        isInFocusedMode = false;
        StopCoroutine(AudioVolumeFadeOut());
        StartCoroutine(AudioVolumeFadeIn());
        this.SendMessageUpwards(
            "ExitFocusedMode", 
            this.gameObject.GetInstanceID(), 
            SendMessageOptions.RequireReceiver
        );
    }

    public void MoveTowardCamera() {
        float step = Time.deltaTime * 0.2f;

        this.transform.position = Vector3.MoveTowards(
            this.transform.position,
            center,
            step
        );

        float fadeRate =  0.0001f;
        Color curColor = this.material.color;
        if (curColor.r > 0) {
            curColor.r -= fadeRate;
            curColor.g -= fadeRate;
            curColor.b -= fadeRate;
            material.color = curColor;
        }
    }

    public void MoveAwayFromCamera() {
        // float step = Vector3.Distance(new Vector3(0,0,0), this.transform.position) * 0.001f;
        // this.transform.position = Vector3.MoveTowards(
        //     this.transform.position,
        //     (this.transform.position - center) * 3,
        //     step
        // );

        radius = 200;
        radiusSpeed = 0.4f;
    }
    public void ReturnToOriginalPosition() {

        radius = originalDistanceFromCamera;
        radiusSpeed = 10f;

        if (isHovered) {
            Vector3 desiredPosition = (transform.position - center).normalized * radius + center;
            transform.position = Vector3.MoveTowards(transform.position, desiredPosition, Time.deltaTime * radiusSpeed);
        }
    }
    public void SetFirstScreen(bool value) {
        this.isFirstScreen = value;
    }

    public void SetAllSpawned(bool value) {
        this.allSpawned = value;
        this.hoverToSelectThreshold = 1.0f;
    }

    private bool IsHovered() {
        List<XRBaseInteractable> potentialInteractables = new List<XRBaseInteractable>(allInteractables);
        interactor.GetValidTargets(potentialInteractables);
        if (potentialInteractables.Count > 0 && potentialInteractables[0] == (XRBaseInteractable) interactable) {
            return true;
        }
        return false;
    }

    public bool GetIsSelected() {
        return this.isHovered && hoverTimer > hoverToSelectThreshold;
    }

    public float GetDistanceFromOriginalPos() {
        return Vector3.Distance(this.originalPosition, this.transform.position);
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
        
        float disappearDistance = 3f;
        float curDistance = Vector3.Distance(new Vector3(0,0,0), this.transform.position);
        float targetColorVal = 0f;
        if (curDistance > disappearDistance && curDistance <= originalDistanceFromCamera) {
            targetColorVal = (float) Math.Pow((curDistance - disappearDistance) / (originalDistanceFromCamera - disappearDistance), 2);
        } else if (curDistance > originalDistanceFromCamera) {
            targetColorVal = 1.0f;
        }
        Color targetColor = new Color(targetColorVal, targetColorVal, targetColorVal, 1);
        if (!targetColor.Equals(material.color)) {
            StopCoroutine("FadeOnExit");
            StartCoroutine(FadeOnHover(targetColor));
            // material.color = Color.white;
        }
    }

    private void OnHoverExit() {
        Color dimedColor = new Color(0.25f, 0.25f, 0.25f, 1);
        if (!dimedColor.Equals(material.color)) {
            material.color = dimedColor;
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

    private IEnumerator FadeOnHover(Color targetColor) {
        Color curColor = material.color;
        float fadeRate = 0.01f;
        while (Math.Abs(curColor.r - targetColor.r) > 0.02) {
            curColor.r += (targetColor.r - curColor.r) * fadeRate;
            curColor.g += (targetColor.g - curColor.g) * fadeRate;
            curColor.b += (targetColor.b - curColor.b) * fadeRate;
            material.color = curColor;
            yield return null;
        }
    }

    public IEnumerator AudioVolumeFadeOut() {

        float fadeRate = 0.005f;
        while (this.audioSource.volume > 0.4f) {
            this.audioSource.volume -= fadeRate;
            yield return null;
        }
    }

    public IEnumerator AudioVolumeFadeIn() {

        float fadeRate = 0.005f;
        while (this.audioSource.volume < 1.0f) {
            this.audioSource.volume += fadeRate;
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

    private IEnumerator WaitAndReload(float waitTime, VideoPlayer vp) {
        float timer = 0;
        while (timer < waitTime) {
            timer += Time.deltaTime;
            yield return null;
        }

        LoadVideoClip();
        vp.time = 0;
        vp.SetTargetAudioSource(0, audioSource);
        isReloading = false;
    }

    private IEnumerator WaitAndSpawn(float waitTime) {
        float timer = 0;
        while (timer < waitTime) {
            timer += Time.deltaTime;
            yield return null;
        }

        SendMessageUpwards("SpawnNext", SendMessageOptions.RequireReceiver);
        hasSpawnedNextEpisode = true;
    }

    private void ReloadVideoAndPlay(VideoPlayer vp) {
        VideoClip curClip = vp.clip;
        this.videoClips.Enqueue(curClip);
        Debug.Log("ReloadVideoAndPlay: Screen has " + this.videoClips.Count + " videos.");

        isReloading = true;
        vp.Stop();
        audioSource.Stop();

        float spawnWaitTime = 2f;
        float nextVideoWaitTime = allSpawned ? 0f : 6f;

        if (!hasSpawnedNextEpisode) {
            StartCoroutine(WaitAndSpawn(spawnWaitTime));
        }

        StartCoroutine(WaitAndReload(nextVideoWaitTime, vp));
    }
}