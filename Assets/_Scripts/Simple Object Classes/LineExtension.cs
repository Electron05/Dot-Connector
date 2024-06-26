using UnityEngine;

public class LineExtension : MonoBehaviour
{
    const int EXTENSION_COLOR_INTENSITY = 9;

    public string colorName;
    public int stationId;

    public void SetInitialColor(Color color)
    {
        foreach (Transform linePart in transform)
        {
            if(linePart.gameObject.name == "Collider") continue; // Skip the collider (it shouldn't be visible)
            linePart.GetComponent<SpriteRenderer>().color = color;
        }
    }

    public void ToggleGlow(bool glow){
        foreach (Transform linePart in transform){
            if(linePart.gameObject.name == "Collider") continue; // Skip the collider (it shouldn't be visible)
            if (glow){
                linePart.GetComponent<Renderer>().material = Resources.Load<Material>("Materials/2D Glow");

                //https://forum.unity.com/threads/setting-material-emission-intensity-in-script-to-match-ui-value.661624/
                //Some random dude found out how to set emission intensity in unity
                Color glowColor = linePart.GetComponent<SpriteRenderer>().color;
                glowColor *= Mathf.Pow(2f, EXTENSION_COLOR_INTENSITY - 0.4169F); 
                linePart.GetComponent<Renderer>().material.SetColor("_GlowColor", glowColor);
            } else {
                linePart.GetComponent<Renderer>().material = Resources.Load<Material>("Materials/Def Unlit");
            }
        }
    }


}
