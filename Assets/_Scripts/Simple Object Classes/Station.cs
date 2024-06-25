using System.Collections.Generic;
using UnityEngine;

public class Station : MonoBehaviour
{
    const float DIAGONAL_PORT_OFFSET = 0.105f;
    const float STRAIGHT_PORT_OFFSET = 0.15f;

    public GameObject chainExtensionPrefab;

    [SerializeField] private int id;
    [SerializeField] private string type;
    private List<string> memberOfWhatColorChain;

    //Example: 0-indexed station NewStationConnections {"","red green", "red"} means that station has:
    //no connection to itself, red and green connection to station 1 and red connection to station 2
    public List<string> stationConnections;
    public List<int> connectionsAmountToEachStation;

    public string[] connectionsInEachDirection = new string[8]; //N, NE, E, SE, S, SW, W, NW
    public int[] connectionsAmountInEachDirection = new int[8]; //N, NE, E, SE, S, SW, W, NW
    
    public bool[,] LeftCenterRightDirectionPortAvailability = new bool[8,3];

    public string[] lineExtensionsInEachDirection = new string[8];

    private LineGenerator lineGenerator;

    private void Start()
    {
        var stationsParent = GameObject.Find("Stations").GetComponent<StationsParent>();
        id = stationsParent.StationCount++;
        AssignRandomShape(stationsParent);

        lineGenerator = GameObject.Find("GameController").GetComponent<LineGenerator>();

        stationConnections =             new List<string>();
        connectionsAmountToEachStation = new List<int>();
        memberOfWhatColorChain =         new List<string>();


        UpdateAllStationsConnections();

        for(int i = 0; i < 8; i++){
            for(int j = 0; j < 3; j++){
                LeftCenterRightDirectionPortAvailability[i,j] = true;
            }
        }


    }

    private void AssignRandomShape(StationsParent stationsParent)
    {
        int shapesAvailable = stationsParent.StationShapes.Count;
        int randomShapeIndex = Random.Range(0, shapesAvailable);
        type = stationsParent.StationShapes[randomShapeIndex];
        GetComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>($"StationShapes/{type}");
    }

    private void UpdateAllStationsConnections()
    {

        foreach (var station in FindObjectsByType<Station>(FindObjectsSortMode.None))
        {
            station.connectionsAmountToEachStation.Add(0);
            station.stationConnections.Add("");
        }


        for (int i = 0; i < id; i++)
        {
            stationConnections.Add("");
            connectionsAmountToEachStation.Add(0);
        }

    }

    public bool IsMemberOfColorLine(string colorName)
    {
        return memberOfWhatColorChain.Contains(colorName);
    }

    public bool HasConnection(GameObject station, string colorName)
    {
        return stationConnections[station.GetComponent<Station>().GetId()].Contains(colorName);
    }

    public void AddConnection(GameObject station, string colorName)
    {
        stationConnections[station.GetComponent<Station>().GetId()]+= colorName + " ";
        connectionsAmountToEachStation[station.GetComponent<Station>().GetId()]++;
    }

    public void AddDirectionalConnection(string colorName, int directionIndex)
    {
        connectionsInEachDirection[directionIndex] += colorName + " ";
        connectionsAmountInEachDirection[directionIndex]++;
    }

    public void AddToColorChain(string colorName)
    {
        if(!memberOfWhatColorChain.Contains(colorName))
            memberOfWhatColorChain.Add(colorName);
    }

    public int GetId()
    {
        return id;
    }

    #region Port Management
    public int GetFirstFreePortIndex(int directionIndex)
    {

        if(LeftCenterRightDirectionPortAvailability[directionIndex,1]){
            return 1;
        }
        else if(LeftCenterRightDirectionPortAvailability[directionIndex,0]){
            return 0;
        }
        else if(LeftCenterRightDirectionPortAvailability[directionIndex,2]){
            return 2;
        }

        return -1;
    }
    public int GetFreePortWithPreference(int directionIndex, int preference)
    {
        int preference2;
        if(preference == 0){
            preference2 = 1;
        }
        else{
            preference2 = 0;
        }

        if(preference == -1)
            return -1;

        if(LeftCenterRightDirectionPortAvailability[directionIndex,preference]){
            return preference;
        }
        else if(LeftCenterRightDirectionPortAvailability[directionIndex,preference2]){
            return preference2;
        }
        else if(LeftCenterRightDirectionPortAvailability[directionIndex,3-preference-preference2]){
            return 3-preference-preference2;
        }
        return -1;
    }

    public void SetPortAvailability(int directionIndex, int portIndex, bool availability)
    {
        if(portIndex < 0 || portIndex > 2){
            return;
        }
        LeftCenterRightDirectionPortAvailability[directionIndex,portIndex] = availability;
    }
    public void AdjustHookToPort(ref Vector2 hookPosition, int angle, int portIndex) 
    {
        //East and west directions require special handling because they don't have left and right ports. They have upper and lower ports which are dependant on the target height. :(
        //In case of east or west direction (initial or final), portIndex should be correctly calculated before calling this method.
        
        if(portIndex == 1) //Middle port, no need to adjust
            return;


        //Port index = 0 is left (alwasy from players perspective), 1 is middle, 2 is right (alwasy from players perspective)
        switch(angle){
            case 0:
                if(portIndex == 0){
                    hookPosition += new Vector2(-STRAIGHT_PORT_OFFSET,0);
                }
                else
                    hookPosition += new Vector2(STRAIGHT_PORT_OFFSET,0);
                return;
            case 45:
                if(portIndex == 0)
                    hookPosition += new Vector2(-DIAGONAL_PORT_OFFSET,DIAGONAL_PORT_OFFSET);
                else
                    hookPosition += new Vector2(DIAGONAL_PORT_OFFSET,-DIAGONAL_PORT_OFFSET);
                return;
            case 90:
                if(portIndex == 0)
                    hookPosition += new Vector2(0,STRAIGHT_PORT_OFFSET);
                else
                    hookPosition += new Vector2(0,-STRAIGHT_PORT_OFFSET);
                return;
            case 135:
                if(portIndex == 0)
                    hookPosition += new Vector2(-DIAGONAL_PORT_OFFSET,-DIAGONAL_PORT_OFFSET);
                else
                    hookPosition += new Vector2(+DIAGONAL_PORT_OFFSET,+DIAGONAL_PORT_OFFSET);
                return;
            case 180:
                if(portIndex == 0)
                    hookPosition += new Vector2(-STRAIGHT_PORT_OFFSET,0);
                else
                    hookPosition += new Vector2(STRAIGHT_PORT_OFFSET,0);
                return;
            case 225:
                if(portIndex == 0)
                    hookPosition += new Vector2(-DIAGONAL_PORT_OFFSET,+DIAGONAL_PORT_OFFSET);
                else
                    hookPosition += new Vector2(DIAGONAL_PORT_OFFSET,-DIAGONAL_PORT_OFFSET);
                return;
            case 270:
                if(portIndex == 0)
                    hookPosition += new Vector2(0,STRAIGHT_PORT_OFFSET);
                else
                    hookPosition += new Vector2(0,-STRAIGHT_PORT_OFFSET);
                return;
            case 315:
                if(portIndex == 0)
                    hookPosition += new Vector2(-DIAGONAL_PORT_OFFSET,-DIAGONAL_PORT_OFFSET);
                else
                    hookPosition += new Vector2(DIAGONAL_PORT_OFFSET,DIAGONAL_PORT_OFFSET);
                return;
            default:
                Debug.LogError("Invalid angle");
                break;
        }
            
    }
    
    public int GetNumberOfFreePorts(int directionIndex)
        {
            int count = 0;
            for(int i = 0; i < 3; i++){
                if(LeftCenterRightDirectionPortAvailability[directionIndex,i]){
                    count++;
                }
            }
            return count;
        }

    public void DebugLogPortAvailability()
    {
        for(int i = 0; i < 8; i++){
            Debug.Log("Direction: " + i);
            for(int j = 0; j < 3; j++){
                Debug.Log("Port: " + j + " Availability: " + LeftCenterRightDirectionPortAvailability[i,j]);
            }
        }
    }
    #endregion

    #region Line Extension Management

    public void AddLineExtension(string colorName, Color originalColor)
    {
        int extDirection = GetFreeExtensionDirection(colorName);

        GameObject extension = Instantiate(chainExtensionPrefab, transform.position, Quaternion.identity);

        extension.GetComponent<LineExtension>().colorName = colorName;
        extension.GetComponent<LineExtension>().stationId = id;
        
        extension.transform.rotation = Quaternion.Euler(0,0,extDirection*45+225); //Very magic number, ik
        extension.transform.SetParent(transform);

        bool lineGlow = lineGenerator.lineGlow;

        extension.GetComponent<LineExtension>().SetInitialColor(originalColor);
        extension.GetComponent<LineExtension>().ToggleGlow(lineGlow);


        lineExtensionsInEachDirection[extDirection] = colorName;
    }

    public void DestroyLineExtension(string colorName)
    {
        for(int i = 0; i < 8; i++)
        {
            if(lineExtensionsInEachDirection[i] == colorName)
            {
                lineExtensionsInEachDirection[i] = "";
                break;
            }
        }
        foreach(Transform child in transform)
        {
            if(child.GetComponent<LineExtension>()!=null && child.GetComponent<LineExtension>().colorName == colorName)
            {
                Destroy(child.gameObject);
                break;
            }
        }
    }

    private int GetFreeExtensionDirection(string colorName)
    {
        int oppositeDirection = 0;
        for(int i = 0; i<8; i++)
        {
            if(connectionsInEachDirection[i].Contains(colorName)) 
                oppositeDirection = 7 - i;
        }


        int j = 0, freeDirection = oppositeDirection;
        while (lineExtensionsInEachDirection[freeDirection] != "")
        {
            j++;
            freeDirection = oppositeDirection + j/2*(j%2 == 0 ? 1 : -1);
            freeDirection%=8;
        }

        return freeDirection;
    }
    #endregion

}