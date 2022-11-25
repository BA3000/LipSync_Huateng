using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class RawImgVideo : MonoBehaviour
{
    private VideoPlayer videoPlayer;

    private RawImage rawImage;
    private VideoClip videoClip;
    
    // Start is called before the first frame update
    private void Start()
    {
        videoPlayer = gameObject.GetComponent<VideoPlayer>();
        rawImage = gameObject.GetComponent<RawImage>();
        videoClip = (VideoClip) Resources.Load("Videoes/weike", typeof(VideoClip));
    }

    // Update is called once per frame
    private void Update()
    {
        rawImage.texture = videoPlayer.texture;
    }
}
