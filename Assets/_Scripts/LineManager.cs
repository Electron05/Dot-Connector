using UnityEngine;

public class LineManager : MonoBehaviour
{
    bool glow = true;

    public void ToggleGlowForAllLines(){
        glow = !glow;

        foreach (Transform chain in transform){
            foreach (Transform line in chain){
                line.GetComponent<Line>().ToggleGlow(glow);
            }
        }

        GameObject[] lineExtensions = GameObject.FindGameObjectsWithTag("Extension");
        foreach (GameObject lineExtension in lineExtensions){
            lineExtension.GetComponentInParent<LineExtension>().ToggleGlow(glow);
        }

    }
}
