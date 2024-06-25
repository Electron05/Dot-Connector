
using UnityEngine;



public class LineGenerator : MonoBehaviour
{
    const int OFFSCREENSPAWN_COORDINATE = 1000;
    const float Z_LAYER_OFFSET = 0.1f;

    const float DEGREE_45_HOOK_OFFSET = 1f;


    public GameObject linePrefab;
    public GameObject twoPartLinePrefab;

    private GameObject linesContainer;
    private ColorChainManager colorChainManager;


    public bool lineGlow = true;
    private string[] colorNames = new string[] { "red", "green", "blue", "yellow", "cyan", "magenta" };
    private Color[] colors = new Color[] { Color.red, Color.green, Color.blue, Color.yellow, Color.cyan, Color.magenta };
    private bool[] usedColors = new bool[6];
    private int currentColorIndex = -1;


    private GameObject loopFormingStation; //Initial station of the chain (its start or end)
    private GameObject currentStation; //The station that the line is currently being dragged from


    private GameObject current2PartLine;
    private float currentLineZLayer = 0;


    private Vector2 currentHookPosition; // current position of the hook (the point where the line is being dragged from)
    private Vector2 startDragPosition; // basically the same as above, but it is updated and adjusted (for example, when lines are neighboring each other)

    [SerializeField] private int dragInitialAngle = -1; //Currently "hooked" angle at the initial station


    int currentDragPortIndex = -1; //The port index of the current (initial) station that the line is being dragged from 
    //(port is a point on the station where the line is connected to, different ports are used to prevent lines from overlapping each other)


    private bool isDragging = false;

    private bool isChangingEndOfChain; // Are we changing the end of the chain (or the start of the chain)?





    #region Utility Methods

    private int GetClosest45DegAngle(float angle,bool printRound){
        int roundedAngle = (int)Mathf.Round(angle / 45f) * 45;
        if (roundedAngle==360) roundedAngle = 0;
        return roundedAngle;
    }
    
    private int calculatePostBreakAngle(GameObject finalStation){ //Probably should be moved to other region
        Vector2 finalStationToBreakDirection;
        Vector2 difference = currentStation.transform.position - finalStation.transform.position;
        
        if (Vector2ToDegree(difference) % 45 == 0) {
            finalStationToBreakDirection = difference;  //Note we are calculating the direction from station2 to station1
        }
        else {
            Vector2 breakCirclePosition = current2PartLine.transform.Find("BreakCircle").position;
            

            Touch touch = Input.GetTouch(0);
            Vector2 touchPosition = Camera.main.ScreenToWorldPoint(touch.position);


            finalStationToBreakDirection = breakCirclePosition - touchPosition; //Note we are calculating the direction from station2 to breakCircle
        }
        int finalAngle = GetClosest45DegAngle(Vector2ToDegree(finalStationToBreakDirection),true);

        return finalAngle;
    }

    private void SetZValue(Transform t, float val){
        t.localPosition = new Vector3(t.localPosition.x, t.localPosition.y, val);
    }

    private float Vector2ToDegree(Vector2 v)
    {
        //example: (1,0) -> 0, (0,1) -> 90, (-1,0) -> 180, (0,-1) -> 270
        float angle = Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg;
        angle-=90f;
        if (angle < 0f) angle += 360f;
        return 360f-angle;
    }



    #endregion

    #region Unity Methods
    void Start()
    {
        InitializeColorChains();
    }

    void Update()
    {
        HandleTouchInput();
    }

    #endregion

    private void InitializeColorChains()
    {
        linesContainer = GameObject.Find("LinesContainer");
        colorChainManager = linesContainer.GetComponent<ColorChainManager>();
        for (int i = 0; i < colors.Length; i++)
        {
            GameObject chainParent = new GameObject(colorNames[i]);
            chainParent.transform.SetParent(linesContainer.transform);

            colorChainManager.AddEmptyColorChain(colorNames[i]);
        }
    }

    private void HandleTouchInput()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            Vector2 touchPosition = Camera.main.ScreenToWorldPoint(touch.position);

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    OnTouchBegan(touchPosition);
                    break;

                case TouchPhase.Moved:
                    if (isDragging)
                    {
                        OnTouchMoved(touchPosition);
                    }
                    break;

                case TouchPhase.Ended:
                    if (isDragging)
                    {
                        OnTouchEnded(touchPosition);
                    }
                    break;
            }
        }
    }



    private void OnTouchBegan(Vector2 touchPosition)
    {
        Collider2D hitCollider = Physics2D.OverlapPoint(touchPosition);
        if(hitCollider == null) return;


        if(hitCollider.CompareTag("Extension")){
            StartDragFromExtension(hitCollider.gameObject);
        }
        else if (hitCollider.CompareTag("Station") && TryFindFreeColor(out currentColorIndex))
        {
            StartDragFromStation(hitCollider.gameObject);
        }
    }

    private void StartDragFromExtension(GameObject extension){
        string colorName = extension.GetComponentInParent<LineExtension>().colorName;
        int stationId = extension.GetComponentInParent<LineExtension>().stationId;
        bool isStart = colorChainManager.GetColorChainStartId(colorName) == stationId;

        currentColorIndex = System.Array.IndexOf(colorNames, colorName);
        colorChainManager.HideChainExtensions(colorName);

        int loopFormingStationId = isStart ? colorChainManager.GetColorChainEndId(colorName) : colorChainManager.GetColorChainStartId(colorName); 
        
        if(isStart){
            isChangingEndOfChain = false;
        }
        else{
            isChangingEndOfChain = true;
        }

        foreach (var station in FindObjectsByType<Station>(FindObjectsSortMode.None)){
            if(station.GetId() == stationId){
                currentStation = station.gameObject;
            }
            if(station.GetId() == loopFormingStationId){
                loopFormingStation = station.gameObject;
            }
        }


        startDragPosition = currentStation.transform.position;
        currentHookPosition = startDragPosition;


        CreateTwoPartLine();
        isDragging = true;
    }

    private void StartDragFromStation(GameObject station){
            currentStation = station;
            loopFormingStation = currentStation;

            isChangingEndOfChain = true;

            startDragPosition = station.transform.position;
            currentHookPosition = startDragPosition;
            CreateTwoPartLine();
            
            isDragging = true;
    }

    private void OnTouchMoved(Vector2 touchPosition)
    {
        Collider2D hitCollider = Physics2D.OverlapPoint(touchPosition);
        if (hitCollider == null || !hitCollider.CompareTag("Station"))
        {
            UpdateTwoPartLine(touchPosition,false);
            return;
        }

        GameObject hitObject = hitCollider.gameObject;
        Station hitStationComponent = hitObject.GetComponent<Station>();
        string color = colorNames[currentColorIndex];

        if (!IsStationValidToAppend(hitObject, hitStationComponent, color))
        {
            UpdateTwoPartLine(touchPosition,false);
            return;
        }

        EndOldLineToStation(hitObject);
        if (hitObject == loopFormingStation) //If formed a loop
        {
            isDragging = false;
            current2PartLine = null;
            return;
        }
        StartNewLineAfterHook(hitCollider, hitStationComponent, color);

    }


    private void OnTouchEnded(Vector2 touchPosition)
    {
        Destroy(current2PartLine);
        isDragging = false;
        current2PartLine = null;

        colorChainManager.GenerateChainExtensions(colorNames[currentColorIndex], colors[currentColorIndex]);

    }


    #region Touch Begin Methods

    private bool TryFindFreeColor(out int colorIndex)
    {
        for (int i = 0; i < usedColors.Length; i++)
        {
            if (!usedColors[i])
            {
                colorIndex = i;
                return true;
            }
        }

        colorIndex = -1;
        return false;
    }

    #endregion

    #region Touch Moved Methods
    
    private void UpdateTwoPartLine(Vector2 endDragPosition, bool isFinalUpdate)
    {

        if(!isFinalUpdate)
        {
            startDragPosition = currentHookPosition;
            Vector2 difference = endDragPosition - startDragPosition;
            SetInitialDragDirection(difference); 

            currentDragPortIndex = currentStation.GetComponent<Station>().GetFirstFreePortIndex(dragInitialAngle/45);
            AdjustInitialHook(currentDragPortIndex);
        }


        GameObject firstHalf = current2PartLine.transform.Find("FirstHalf").gameObject;
        GameObject secondHalf = current2PartLine.transform.Find("SecondHalf").gameObject;
        GameObject breakCircle = current2PartLine.transform.Find("BreakCircle").gameObject;


        Vector2 earlyBreakPoint = CalculateEarlyBreakPointPosition(startDragPosition, endDragPosition);
        Vector2 lateBreakPoint = CalculateEarlyBreakPointPosition(endDragPosition, startDragPosition);
        Vector2 breakPoint;

        if(dragInitialAngle%90==0){
            breakPoint = lateBreakPoint;
        }
        else{
            breakPoint = earlyBreakPoint;
        }

        Vector2 direction1 = breakPoint-startDragPosition;
        float distance1 = direction1.magnitude;
        Vector2 midPoint1 = (startDragPosition + breakPoint) / 2;

        Vector2 direction2 = endDragPosition - breakPoint;
        float distance2 = direction2.magnitude;
        Vector2 midPoint2 = (breakPoint + endDragPosition) / 2;

        SetLinePart(direction1, distance1, midPoint1, firstHalf.transform);
        SetLinePart(direction2, distance2, midPoint2, secondHalf.transform);

        breakCircle.transform.position = breakPoint;
        breakCircle.transform.localScale = new Vector3(0.1f, 0.1f, 1f);

        SetZValue(firstHalf.transform, currentLineZLayer);
        SetZValue(secondHalf.transform, currentLineZLayer);
        SetZValue(breakCircle.transform, currentLineZLayer-Z_LAYER_OFFSET*0.5f);
        
    }
    
    private void SetInitialDragDirection(Vector2 direction){
        float angle = Vector2ToDegree(direction);

        bool angleIsNotSet = dragInitialAngle == -1;
        bool angleIsCloseTo45 = Mathf.Abs(angle % 45f) < DEGREE_45_HOOK_OFFSET || Mathf.Abs(angle % 45f - 45f) < DEGREE_45_HOOK_OFFSET;
        bool angleIsTooFarFromInitial = Mathf.Abs(dragInitialAngle - angle) > 45f + DEGREE_45_HOOK_OFFSET && Mathf.Abs(dragInitialAngle - angle) < 310f; // The second condition is to prevent line jumps when angle is close to 0 or 360

        if (angleIsNotSet || angleIsCloseTo45 || angleIsTooFarFromInitial) {
            dragInitialAngle = GetClosest45DegAngle(angle,false);
        }
    }
    
    private Vector2 CalculateEarlyBreakPointPosition(Vector2 startDragPosition, Vector2 currentDragPosition){
        //startDragPosition here is local variable
        Vector2 difference = currentDragPosition - startDragPosition;
        Vector2 breakPoint = new Vector2(0,0);

        if(Mathf.Abs(difference.y)>Mathf.Abs(difference.x))
        { 
            breakPoint.x = currentDragPosition.x;
            if (currentDragPosition.y < startDragPosition.y) {
                breakPoint.y = startDragPosition.y + difference.x *(difference.x>0?-1:1);
            }
            else {
                breakPoint.y = startDragPosition.y + difference.x * (difference.x >0?1:-1);
            }
        }
        else 
        { 
            breakPoint.y = currentDragPosition.y;
            if (currentDragPosition.x < startDragPosition.x) {
                breakPoint.x = startDragPosition.x + difference.y * (difference.y > 0 ? -1 : 1);
            }
            else {
                breakPoint.x = startDragPosition.x + difference.y * (difference.y > 0 ? 1 : -1);
            }
        }
        return breakPoint;
    }
    
    private bool IsStationValidToAppend(GameObject hitObject, Station hitStationComponent, string color)
    {
        return (Vector2)hitObject.transform.position != currentHookPosition &&
            (!hitStationComponent.IsMemberOfColorLine(color) || //If station is not part of the current color chain
            (hitObject == loopFormingStation && colorChainManager.GetColorChainLength(color) != 2)); //Except if it is the start station (loop formation)
    }

    private void EndOldLineToStation(GameObject hitObject) 
    {
        //Ensuring that the best initial drag possible is chosen before finalizing hook (prevents the NW initial direction when connecting directly to N)
        SetInitialDragDirection( (Vector2)hitObject.transform.position - currentHookPosition); 
        startDragPosition = currentHookPosition; 

        current2PartLine.GetComponent<Line>().to = hitObject.GetComponent<Station>().GetId();

        Vector2 positionToGetHooked = hitObject.transform.position;
        int finalStationPortIndex, finalAngle;

        AdjustFinalHook(hitObject, ref positionToGetHooked, out finalStationPortIndex, out finalAngle);
        if ( !ShouldInvertFinalPort(hitObject, finalAngle) ) //If the angles of the connection are E-NW or W-SE, the port is inverted and we do not alter inital port
        {
            //otherwise, we try to set the initial port to the one that is closest to the final port
            currentDragPortIndex = currentStation.GetComponent<Station>().GetFreePortWithPreference(dragInitialAngle/45, finalStationPortIndex);
        }
        AdjustInitialHook(currentDragPortIndex);

        UpdateTwoPartLine(positionToGetHooked,true);

        currentStation.GetComponent<Station>().SetPortAvailability(dragInitialAngle/45, currentDragPortIndex, false);
        hitObject.GetComponent<Station>().SetPortAvailability(finalAngle/45, finalStationPortIndex, false);

        AddConnectionBetweenStations(currentStation, hitObject);

        currentHookPosition = hitObject.transform.position;
        current2PartLine = null;
    }
    
    private void StartNewLineAfterHook(Collider2D hitCollider, Station hitStationComponent, string color)
    {
        if (usedColors[currentColorIndex] == false) //If starting new chain
        {
            colorChainManager.InitializeColorChain(color, currentStation.GetComponent<Station>().GetId(), hitStationComponent.GetId());
        }
        else
        {
            colorChainManager.AppendToColorChain(color, hitStationComponent.GetId(), isChangingEndOfChain);
        }

        usedColors[currentColorIndex] = true;
        //Create New Line
        currentStation = hitCollider.gameObject;
        startDragPosition = hitCollider.transform.position;
        currentHookPosition = startDragPosition;
        CreateTwoPartLine();
    }

    #region Nieghbouring Lines Management
    
    private void AdjustInitialHook(int portIndex){

        currentStation.GetComponent<Station>().AdjustHookToPort(ref startDragPosition, dragInitialAngle, portIndex);
    }
    
    private void AdjustFinalHook(GameObject finalStation, ref Vector2 endDragPosition, out int finalStationPortIndex, out int finalAngle){
        
        finalAngle = calculatePostBreakAngle(finalStation);
        
        finalStationPortIndex = finalStation.GetComponent<Station>().GetFreePortWithPreference(finalAngle/45, currentDragPortIndex);
        
        //Debug.Log("Final station port index: " + finalStationPortIndex );

        if ( ShouldInvertFinalPort(finalStation, finalAngle) ) {
            Debug.Log("Inverting final port from" + finalStationPortIndex + " to " + (2-finalStationPortIndex)) ;
            finalStationPortIndex = 2-finalStationPortIndex;
        }

        finalStation.GetComponent<Station>().AdjustHookToPort(ref endDragPosition, finalAngle, finalStationPortIndex);
    }

    private bool ShouldInvertFinalPort(GameObject finalStation, int finalAngle) { 
        return  ( ( ( (dragInitialAngle-90)%180==0 && (finalAngle+45)%180==0 ) || ( ((dragInitialAngle+45)%180==0) && (finalAngle-90)%180==0 ) ) && finalStation.GetComponent<Station>().GetNumberOfFreePorts(finalAngle/45) == 2 );
    }
    
    #endregion

    #endregion
    

    #region Line GameObject Management
    private void CreateTwoPartLine()
    {
        Color originalColor = colors[currentColorIndex];
        
        current2PartLine = Instantiate(twoPartLinePrefab, transform.position, Quaternion.identity);
        current2PartLine.transform.SetParent(linesContainer.transform.Find(colorNames[currentColorIndex]));

        current2PartLine.GetComponent<Line>().SetInialColor(originalColor);
        current2PartLine.GetComponent<Line>().ToggleGlow(lineGlow);

        current2PartLine.GetComponent<Line>().color = colorNames[currentColorIndex];
        current2PartLine.GetComponent<Line>().from = currentStation.GetComponent<Station>().GetId();

        currentLineZLayer-=Z_LAYER_OFFSET;
        current2PartLine.transform.position = new Vector3(OFFSCREENSPAWN_COORDINATE, OFFSCREENSPAWN_COORDINATE, 0f); //Spawn it offscreen
    }
    
    public void ToggleLineGlow(){
        lineGlow = !lineGlow;
    }

    private void SetLinePart(Vector2 direction, float distance, Vector2 targetPosition, Transform linePart)
    {
        linePart.position = new Vector3(targetPosition.x, targetPosition.y, currentLineZLayer);
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        linePart.rotation = Quaternion.Euler(0, 0, angle);
        linePart.localScale = new Vector3(distance, 0.1f, 1f);
    }
    
    #endregion

    #region Stations logic functions

    private void AddConnectionBetweenStations(GameObject station1, GameObject station2)
    {
        string colorName = colorNames[currentColorIndex];


        station1.GetComponent<Station>().AddToColorChain(colorName);
        station2.GetComponent<Station>().AddToColorChain(colorName);

        AddDirectionalConnectionToInitialStation(station1, colorName);
        AddDirectionalConnectionToFinalStation(station2, colorName);


        station1.GetComponent<Station>().AddConnection(station2, colorName);
        station2.GetComponent<Station>().AddConnection(station1, colorName);

        //station1.GetComponent<Station>().DebugLogPortAvailability();
        //station2.GetComponent<Station>().DebugLogPortAvailability();
    }

    private void AddDirectionalConnectionToInitialStation(GameObject station, string colorName)
    {
        //dragInitialAngle is the angle at the start of the drag (for more, see UpdateTwoPartLine method)
        station.GetComponent<Station>().AddDirectionalConnection(colorName, dragInitialAngle/45);
    }

    private void AddDirectionalConnectionToFinalStation(GameObject finalStation, string colorName)
    {
        int angleIndex = calculatePostBreakAngle(finalStation)/45;
        finalStation.GetComponent<Station>().AddDirectionalConnection(colorName, angleIndex);
    }

    #endregion
    
}
