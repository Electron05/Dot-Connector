using UnityEngine;

public class Line : MonoBehaviour
{
    const int LINE_COLOR_INTENSITY = 11;
    public int from,to;
    public string color;

    public void SetInialColor(Color color){
        foreach (Transform linePart in transform){
            linePart.GetComponent<SpriteRenderer>().color = color;
        }
    }

    public void ToggleGlow(bool glow){
        foreach (Transform linePart in transform){

            if (glow){
                linePart.GetComponent<Renderer>().material = Resources.Load<Material>("Materials/2D Glow");
                Color glowColor = linePart.GetComponent<SpriteRenderer>().color;
                
                //https://forum.unity.com/threads/setting-material-emission-intensity-in-script-to-match-ui-value.661624/
                //Some random dude found out how to set emission intensity in unity
                
                glowColor*= Mathf.Pow(2f, LINE_COLOR_INTENSITY - 0.4169F); 
                linePart.GetComponent<Renderer>().material.SetColor("_GlowColor", glowColor);

            } else {
                linePart.GetComponent<Renderer>().material = Resources.Load<Material>("Materials/Def Unlit");
            }
        }
    }
}
