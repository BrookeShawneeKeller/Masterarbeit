using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class ShowButtonAtTime : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    public GameObject buttonToShow;
    public double triggerTimeInSeconds = 10.0;
    private bool hasTriggered = false;

    void Update()
    {
        if (!hasTriggered && videoPlayer.isPlaying && videoPlayer.time >= triggerTimeInSeconds)
        {
            buttonToShow.SetActive(true);
            hasTriggered = true;
        }
    }
}