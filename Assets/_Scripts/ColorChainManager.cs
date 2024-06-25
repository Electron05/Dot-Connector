using UnityEngine;
using System.Collections.Generic;
public class ColorChainManager : MonoBehaviour
{

    public GameObject chainExtensionPrefab;
    private List<colorChain> colorChains = new List<colorChain>();

    public void AddEmptyColorChain(string color)
    {
        colorChain newColorChain = new colorChain{ color = color, length = 0, startId = -1, endId = -1 }; 
        colorChains.Add(newColorChain);
    }

    public void InitializeColorChain(string color, int startId, int endId)
    {
        for(int i = 0; i < colorChains.Count; i++)
        {
            if(colorChains[i].color == color)
            {
                var temp = colorChains[i];
                temp.length = 2;
                temp.startId = startId;
                temp.endId = endId;
                colorChains[i] = temp;
                break;
            }
        }
    }
    
    public void AppendToColorChain(string colorName, int newBoundaryId, bool isChangingEndOfChain)
    {
        for(int i = 0; i < colorChains.Count; i++)
        {
            if(colorChains[i].color == colorName)
            {
                var temp = colorChains[i];
                temp.length++;

                if(isChangingEndOfChain) 
                    temp.endId = newBoundaryId;
                else{
                    temp.startId = newBoundaryId;
                }
                colorChains[i] = temp;
                break;
            }
        }
    }

    public void GenerateChainExtensions(string colorName, Color originalColor){ //repetition
        int colorChainIndex;
        for(colorChainIndex = 0; colorChainIndex < colorChains.Count; colorChainIndex++){
            if(colorChains[colorChainIndex].color == colorName){
                break;
            }
        }
        int startId = colorChains[colorChainIndex].startId;
        int endId = colorChains[colorChainIndex].endId;

        if(startId == endId){ // Possible loop edition in future
            return;
        }

        foreach (var station in FindObjectsByType<Station>(FindObjectsSortMode.None)){
            if(station.GetId() == startId || station.GetId() == endId){
                station.AddLineExtension(colorName, originalColor);
            } 
        }
    }

    public void HideChainExtensions(string colorName){ //Obvious repetition
        int colorChainIndex;
        for(colorChainIndex = 0; colorChainIndex < colorChains.Count; colorChainIndex++){
            if(colorChains[colorChainIndex].color == colorName){
                break;
            }
        }
        int startId = colorChains[colorChainIndex].startId;
        int endId = colorChains[colorChainIndex].endId;

        if(startId == endId){
            return;
        }
        foreach (var station in FindObjectsByType<Station>(FindObjectsSortMode.None)){
            if(station.GetId() == startId || station.GetId() == endId){
                station.DestroyLineExtension(colorName);
            } 
        }
    }

    public int GetColorChainLength(string color)
    {
        for(int i = 0; i < colorChains.Count; i++)
        {
            if(colorChains[i].color == color)
            {
                return colorChains[i].length;
            }
        }
        return -1;
    }

    public int GetColorChainStartId(string colorName)
    {
        for(int i = 0; i < colorChains.Count; i++)
        {
            if(colorChains[i].color == colorName)
            {
                return colorChains[i].startId;
            }
        }
        return -1;
    }
    
    public int GetColorChainEndId(string colorName)
    {
        for(int i = 0; i < colorChains.Count; i++)
        {
            if(colorChains[i].color == colorName)
            {
                return colorChains[i].endId;
            }
        }
        return -1;
    }
    
    public void SetColorChainStartId(string colorName, int newStartId)
    { 
        for(int i = 0; i < colorChains.Count; i++)
        {
            if(colorChains[i].color == colorName)
            {
                var temp = colorChains[i];
                temp.startId = newStartId;
                colorChains[i] = temp;
                break;
            }
        }
    }

    struct colorChain{
        public string color;
        public int length;
        public int startId;
        public int endId;
    }
}
