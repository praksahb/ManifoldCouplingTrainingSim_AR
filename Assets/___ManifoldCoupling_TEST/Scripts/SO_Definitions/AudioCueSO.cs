using UnityEngine;

[CreateAssetMenu(menuName = "Audio/Audio Cue", fileName = "AudioCue_")]
public class AudioCueSO : ScriptableObject
{
    public AudioClip alignmentSuccess;
    public AudioClip alignmentFail;

    public AudioClip snap;
    public AudioClip unsnap;

    public AudioClip handleLock;
    public AudioClip handleUnlock;

    public AudioClip stepCompleted;
    public AudioClip stepFailed;

    public AudioClip buttonClick;
    public AudioClip tutorialComplete;
}
