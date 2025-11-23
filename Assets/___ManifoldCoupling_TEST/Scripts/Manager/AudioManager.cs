using UnityEngine;

[DisallowMultipleComponent]
public class AudioManager : Singleton<AudioManager>
{
    [SerializeField] private AudioCueSO _audioCue;
    [SerializeField] private AudioSource _uiSource;       // UI clicks & notifications
    [SerializeField] private AudioSource _worldSource;    // 3D world SFX

    protected override void Awake()
    {
        base.Awake();

        if (_uiSource == null)
        {
            _uiSource = gameObject.AddComponent<AudioSource>();
            _uiSource.spatialBlend = 0f;   // 2D
        }

        if (_worldSource == null)
        {
            _worldSource = gameObject.AddComponent<AudioSource>();
            _worldSource.spatialBlend = 1f; // 3D
        }
    }

    // -----------------------------
    // Public API
    // -----------------------------

    public void PlayUI(AudioClip clip)
    {
        if (clip != null)
            _uiSource.PlayOneShot(clip);
    }

    public void PlayWorld(AudioClip clip, Vector3 pos)
    {
        if (clip == null) return;
        AudioSource.PlayClipAtPoint(clip, pos);
    }

    public void PlaySnap() => PlayWorld(_audioCue.snap, Camera.main.transform.position);
    public void PlayUnsnap() => PlayWorld(_audioCue.unsnap, Camera.main.transform.position);
    public void PlayHandleLock() => PlayWorld(_audioCue.handleLock, Camera.main.transform.position);
    public void PlayHandleUnlock() => PlayWorld(_audioCue.handleUnlock, Camera.main.transform.position);

    public void PlayAlignmentSuccess() => PlayWorld(_audioCue.alignmentSuccess, Camera.main.transform.position);
    public void PlayAlignmentFail() => PlayWorld(_audioCue.alignmentFail, Camera.main.transform.position);

    public void PlayStepCompleted() => PlayUI(_audioCue.stepCompleted);
    public void PlayStepFailed() => PlayUI(_audioCue.stepFailed);

    public void PlayButtonClick() => PlayUI(_audioCue.buttonClick);
    public void PlayTutorialComplete() => PlayUI(_audioCue.tutorialComplete);
}
