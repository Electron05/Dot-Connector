using UnityEngine;

public class GameController : MonoBehaviour
{
    void Awake()
    {
        SetFPS();
    }
    private void SetFPS() {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 120;
    }
}
