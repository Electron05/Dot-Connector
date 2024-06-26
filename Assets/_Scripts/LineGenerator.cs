
using UnityEngine;
using UnityEngine.Rendering.Universal.Internal;



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


    private Vector2 currentHookPosition;

    [SerializeField] private int dragInitialAngleGlobal = -1; //Currently "hooked" angle at the initial station


    private bool isDragging = false;

    private bool isChangingEndOfChain; // Are we changing the end of the chain (or the start of the chain)?





    #region Utility Methods

    private int GetClosest45DegAngle(float angle){
        int roundedAngle = (int)Mathf.Round(angle / 45f) * 45;
        if (roundedAngle==360) roundedAngle = 0;
        return roundedAngle;
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


        currentHookPosition = currentStation.transform.position;


        CreateTwoPartLine();
        isDragging = true;
    }

    private void StartDragFromStation(GameObject station){
            currentStation = station;
            loopFormingStation = currentStation;

            isChangingEndOfChain = true;

            currentHookPosition = station.transform.position;
            CreateTwoPartLine();
            
            isDragging = true;
    }

    private void OnTouchMoved(Vector2 touchPosition)
    {
        Collider2D hitCollider = Physics2D.OverlapPoint(touchPosition);
        if (hitCollider == null || !hitCollider.CompareTag("Station"))
        {
            RefactoredUpdateLine(currentHookPosition,touchPosition);
            return;
        }

        GameObject hitObject = hitCollider.gameObject;
        Station hitStationComponent = hitObject.GetComponent<Station>();
        string color = colorNames[currentColorIndex];

        if (!IsStationValidToAppend(hitObject, hitStationComponent, color))
        {
            RefactoredUpdateLine(currentHookPosition,touchPosition);
            return;
        }

        if(TryToEndOldLine(hitObject)==-1)
            return;
        if (hitObject == loopFormingStation) //If formed a loop
        {
            isDragging = false;
            current2PartLine = null;
            return;
        }
        StartNewLineAfterHook(hitObject, hitStationComponent, color);

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
    

    
    private void SetInitialDragDirection(Vector2 direction){
        float angle = Vector2ToDegree(direction);

        bool angleIsNotSet = dragInitialAngleGlobal == -1;
        bool angleIsCloseTo45 = Mathf.Abs(angle % 45f) < DEGREE_45_HOOK_OFFSET || Mathf.Abs(angle % 45f - 45f) < DEGREE_45_HOOK_OFFSET;
        bool angleIsTooFarFromInitial = Mathf.Abs(dragInitialAngleGlobal - angle) > 45f + DEGREE_45_HOOK_OFFSET && Mathf.Abs(dragInitialAngleGlobal - angle) < 310f; // The second condition is to prevent line jumps when angle is close to 0 or 360

        if (angleIsNotSet || angleIsCloseTo45 || angleIsTooFarFromInitial) {
            dragInitialAngleGlobal = GetClosest45DegAngle(angle);
        }
    }
    
    private Vector2 CalculateEarlyBreakPointPosition(Vector2 startLinePosition, Vector2 currentDragPosition){
        Vector2 difference = currentDragPosition - startLinePosition;
        Vector2 breakPoint = new Vector2(0,0);

        if(Mathf.Abs(difference.y)>Mathf.Abs(difference.x))
        { 
            breakPoint.x = currentDragPosition.x;
            if (currentDragPosition.y < startLinePosition.y) {
                breakPoint.y = startLinePosition.y + difference.x *(difference.x>0?-1:1);
            }
            else {
                breakPoint.y = startLinePosition.y + difference.x * (difference.x >0?1:-1);
            }
        }
        else 
        { 
            breakPoint.y = currentDragPosition.y;
            if (currentDragPosition.x < startLinePosition.x) {
                breakPoint.x = startLinePosition.x + difference.y * (difference.y > 0 ? -1 : 1);
            }
            else {
                breakPoint.x = startLinePosition.x + difference.y * (difference.y > 0 ? 1 : -1);
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


    private void RefactoredUpdateLine(Vector2 startLinePosition, Vector2 endLinePosition){

        SetInitialDragDirection(endLinePosition-startLinePosition); // dragInitialAngleGlobal is set here

        int initialStationPortIndex = currentStation.GetComponent<Station>().GetFirstFreePortIndex(dragInitialAngleGlobal/45);
        JustAdjustInitialHook(ref startLinePosition, initialStationPortIndex);

        CalculateBreakPointPosition(startLinePosition, endLinePosition, out Vector2 breakPointPosition);

        SetLineInPosition(startLinePosition, endLinePosition, breakPointPosition);
    }

    private int TryToEndOldLine(GameObject hitObject){

        //Calculate basic connection (initial angle, final angle, break point)
        int finalAngle;
        Vector2 breakPointPosition;
        Vector2 startLinePosition = currentStation.transform.position, endLinePosition = hitObject.transform.position;

        SetInitialDragDirection(endLinePosition-startLinePosition); // dragInitialAngleGlobal is set here
        CalculateFinalAngle(startLinePosition, endLinePosition, out finalAngle);

        //Check for port availability 
        //If port is not available, return
        if(currentStation.GetComponent<Station>().GetNumberOfFreePorts(dragInitialAngleGlobal/45) == 0 || hitObject.GetComponent<Station>().GetNumberOfFreePorts(finalAngle/45) == 0){
            return -1;
        }
        
        //Now connection is certain
        current2PartLine.GetComponent<Line>().to = hitObject.GetComponent<Station>().GetId();
        AddConnectionBetweenStations(currentStation, hitObject, finalAngle);


        
        //If ports are available, adjust initial hook, adjust final hook, calculate new break point
        int initialStationPortIndex = currentStation.GetComponent<Station>().GetFirstFreePortIndex(dragInitialAngleGlobal/45);
        int finalStationPortIndex = hitObject.GetComponent<Station>().GetFreePortWithPreference(finalAngle/45, initialStationPortIndex);
        

        if ( ShouldInvertFinalPort(hitObject, finalAngle) ) {
            finalStationPortIndex = 2-finalStationPortIndex;
        }
        if(initialStationPortIndex!=finalStationPortIndex && !ShouldInvertFinalPort(hitObject, finalAngle)){
            initialStationPortIndex = currentStation.GetComponent<Station>().GetFreePortWithPreference(dragInitialAngleGlobal/45, finalStationPortIndex);
        }

        currentStation.GetComponent<Station>().SetPortAvailability(dragInitialAngleGlobal/45, initialStationPortIndex, false);
        hitObject.GetComponent<Station>().SetPortAvailability(finalAngle/45, finalStationPortIndex, false);

        JustAdjustInitialHook(ref startLinePosition,initialStationPortIndex);
        JustAdjustFinalHook(hitObject, ref endLinePosition, finalAngle, finalStationPortIndex);


        //Set physical positions of the line parts
        CalculateBreakPointPosition(startLinePosition, endLinePosition, out breakPointPosition);
        SetLineInPosition(startLinePosition, endLinePosition, breakPointPosition);
        return 0;
    }

    private void CalculateFinalAngle(Vector2 startLinePosition, Vector2 endLinePosition, out int finalAngle){
        
        SetInitialDragDirection(endLinePosition-startLinePosition); // dragInitialAngleGlobal is set here

        CalculateBreakPointPosition(startLinePosition, endLinePosition, out Vector2 breakPointPosition);

        if(Vector2ToDegree(endLinePosition-startLinePosition) % 45 == 0){
            finalAngle = (int)Vector2ToDegree(startLinePosition-endLinePosition);
            if (finalAngle == 360) finalAngle = 0;
        }
        else{
            finalAngle = GetClosest45DegAngle(Vector2ToDegree(breakPointPosition-endLinePosition));
        }

    }

    private void CalculateBreakPointPosition(Vector2 startLinePosition, Vector2 endLinePosition, out Vector2 breakPointPosition){
        Vector2 earlyBreakPoint = CalculateEarlyBreakPointPosition(startLinePosition, endLinePosition);
        Vector2 lateBreakPoint = CalculateEarlyBreakPointPosition(endLinePosition, startLinePosition);

        if(dragInitialAngleGlobal%90==0){
            breakPointPosition = lateBreakPoint;
        }
        else{
            breakPointPosition = earlyBreakPoint;
        }
    }

    private void SetLineInPosition(Vector2 startLinePosition, Vector2 endLinePosition, Vector2 breakPointPosition){
        GameObject firstHalf = current2PartLine.transform.Find("FirstHalf").gameObject;
        GameObject secondHalf = current2PartLine.transform.Find("SecondHalf").gameObject;
        GameObject breakCircle = current2PartLine.transform.Find("BreakCircle").gameObject;

        breakCircle.transform.position = breakPointPosition;
        breakCircle.transform.localScale = new Vector3(0.1f, 0.1f, 1f);

        Vector2 direction1 = breakPointPosition-startLinePosition;
        float distance1 = direction1.magnitude;
        Vector2 midPoint1 = (startLinePosition + breakPointPosition) / 2;

        Vector2 direction2 = endLinePosition - breakPointPosition;
        float distance2 = direction2.magnitude;
        Vector2 midPoint2 = (breakPointPosition + endLinePosition) / 2;

        SetLinePart(direction1, distance1, midPoint1, firstHalf.transform);
        SetLinePart(direction2, distance2, midPoint2, secondHalf.transform);

        SetZValue(firstHalf.transform, currentLineZLayer);
        SetZValue(secondHalf.transform, currentLineZLayer);
        SetZValue(breakCircle.transform, currentLineZLayer-Z_LAYER_OFFSET*0.5f);
    }

    private void StartNewLineAfterHook(GameObject newStation, Station hitStationComponent, string color)
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
        currentStation = newStation;
        currentHookPosition = newStation.transform.position;
        CreateTwoPartLine();
    }


    #region Nieghbouring Lines Management
    
    private void JustAdjustInitialHook(ref Vector2 startLinePosition, int initialStationPortIndex){
        currentStation.GetComponent<Station>().AdjustHookToPort(ref startLinePosition, dragInitialAngleGlobal, initialStationPortIndex);
    }
    private void JustAdjustFinalHook(GameObject finalStation, ref Vector2 endLinePosition, int finalAngle, int finalStationPortIndex){
        finalStation.GetComponent<Station>().AdjustHookToPort(ref endLinePosition, finalAngle, finalStationPortIndex);
        
    }
    

    private bool ShouldInvertFinalPort(GameObject finalStation, int finalAngle) { 
        return  ( ( ( (dragInitialAngleGlobal-90)%180==0 && (finalAngle+45)%180==0 ) || ( ((dragInitialAngleGlobal+45)%180==0) && (finalAngle-90)%180==0 ) ) && finalStation.GetComponent<Station>().GetNumberOfFreePorts(finalAngle/45) == 2 );
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

    private void AddConnectionBetweenStations(GameObject station1, GameObject station2, int AngleFromFinalStation)
    {
        string colorName = colorNames[currentColorIndex];


        station1.GetComponent<Station>().AddToColorChain(colorName);
        station2.GetComponent<Station>().AddToColorChain(colorName);

        AddDirectionalConnectionToInitialStation(station1, colorName);
        AddDirectionalConnectionToFinalStation(station2, colorName,AngleFromFinalStation);


        station1.GetComponent<Station>().AddConnection(station2, colorName);
        station2.GetComponent<Station>().AddConnection(station1, colorName);

        //station1.GetComponent<Station>().DebugLogPortAvailability();
        //station2.GetComponent<Station>().DebugLogPortAvailability();
    }

    private void AddDirectionalConnectionToInitialStation(GameObject station, string colorName)
    {
        //dragInitialAngle is the angle at the start of the drag (for more, see UpdateTwoPartLine method)
        station.GetComponent<Station>().AddDirectionalConnection(colorName, dragInitialAngleGlobal/45);
    }

    private void AddDirectionalConnectionToFinalStation(GameObject finalStation, string colorName, int finalAngle)
    {
        finalStation.GetComponent<Station>().AddDirectionalConnection(colorName, finalAngle/45);
    }

    #endregion
    
}
