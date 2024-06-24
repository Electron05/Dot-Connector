using UnityEngine;
using TMPro;
public class FpsCounter : MonoBehaviour
{
    private TextMeshProUGUI fpsText;
    private float updateRate = 1f;
    private float fps = 0.0f;

    private int frames = 0;
    private float timeleft;

    void Start()
    {
        fpsText = GetComponent<TextMeshProUGUI>();
        timeleft = updateRate;
    }

    void Update()
    {
        timeleft -= Time.deltaTime;
        ++frames;
        if (timeleft <= 0.0)
        {
            fps = frames / updateRate;
            frames = 0;
            timeleft = updateRate;
            fpsText.text = "FPS: " + fps.ToString("f2");
        }
    }
}
