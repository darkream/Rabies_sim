using UnityEngine;
using Mapbox.Utils;
using Mapbox.Unity.Map;
using Mapbox.Unity.Utilities;
using Mapbox.Map;
using Mapbox.Unity.MeshGeneration.Data;
using System.IO;
using System.Collections.Generic;

public class OnMapSpawn : MonoBehaviour
{
    [SerializeField]
    MapboxInheritance _mbfunction;

    [SerializeField]
    ColorStreamClustering clustering;

    List<float> doggroupsize; //initial group size of the list of dogs above

    [SerializeField]
    float distribution_criteria = 0.5f; //0.5 dog means at least 1 dog

    [SerializeField]
    Camera _referenceCamera;

    private float startlat = 7.044082f, startlon = 100.4482f; //default_ lat: 7.01125306678015, lon: 100.518001044838, zoom: 16.1
    public int xgridsize, ygridsize; //default_ xsize = 1700 grid, ysize = 1000 grid
    private float minh, maxh;
    private float[,] heightxy;
    private List<LatLonSize> dogdata;
    private float[,] doggroup; //dog group size in 2D-array
    private float[,] tempgroup;
    private int[,] edge;
    private int[,] walk;
    private float[,] mapAfford;
    private int[,] groupassign;
    private float[,] dogfound;

    [SerializeField]
    int loopCriteria = 4;
    
    private int convergeCountdown;
    private int convergeChangeCount = 0;
    private int dogimageid = 0;

    [SerializeField]
    float initial_dog_groupsize;

    [SerializeField]
    float homeRangeMultiplier = 2.0f; //default home range multiplier = x1.8

    [SerializeField]
    bool allowElevation = true;

    [SerializeField]
    float hordeMoveRate = 0.4f;

    [SerializeField]
    float exploreMoveRate = 0.2f;

    [SerializeField]
    float highest_activity_rate; //1.0 is maximum, 0.0 is minimum

    [SerializeField]
    float lowest_activity_rate;

    [SerializeField]
    int time_length; //300 seconds is equals to 5 minutes for each activity

    [SerializeField]
    int time_cycle; //4 cycles for each day

    [SerializeField]
    UIController uicontroller;

    private float singleMoveRate;
    private List<AttractSource> attracter;
    private List<AttractNode> node;
    private float[,] walkingHabits;
    private int highest_walking_rate = 0;
    private float highest_home_rate = 0.0f;
    private float highest_habits_rate = 0.0f;
    private float[] highest_afford;
    private int[] count_area;
    private float[] timeScaleFactor;
    private float[,] wh;
    private float affordScale = 1.25f; //afford scale increasion is at 25% if it is 1.25f
    private int currentprogress = 0;
    private bool startPreDataRegister = false;
    private int atAttract = 0;
    private int atTime = 0;

    void Start()
    {
        _mbfunction.initializeLocations();
        dogdata = new List<LatLonSize>();
        attracter = new List<AttractSource>();
        doggroupsize = new List<float>();
        convergeCountdown = loopCriteria;
        singleMoveRate = 1.0f - (hordeMoveRate + exploreMoveRate);
        initialTimeScaleFactor();
        uicontroller.initialValue();
        uicontroller.setTotalProcess(9);
        //clustering.pixelReaderAndFileWritten("Assets/mapcolor.txt");
        //clustering.createStringColorListFromReadFile("Assets/mapcolor.txt");
        //clustering.kMeanClustering();
        //clustering.readAndAssignMapCoordinates("Assets/doc.kml");
        //clustering.readFileToFindMinMax("Assets/mapcolor.txt");
    }

    private void Update()
    {
        for (int i = 0; i < _mbfunction.dogObjs.Count; i++) //for each spawn object
        {
            var dogObject = _mbfunction.dogObjs[i];
            var location = _mbfunction.doglocations[i]; //spawn the object to the dog locations
            dogObject.transform.localPosition = _mbfunction._map.GeoToWorldPosition(location , true);
            dogObject.transform.localScale = new Vector3(_mbfunction._spawnScale , _mbfunction._spawnScale , _mbfunction._spawnScale);
        }

        for (int i = 0; i < _mbfunction.mappointObjs.Count; i++)
        {
            var mapObject = _mbfunction.mappointObjs[i];
            var location = _mbfunction.mappointlocations[i]; //spawn the object to the dog locations
            mapObject.transform.localPosition = _mbfunction._map.GeoToWorldPosition(location , true);
            mapObject.transform.localScale = new Vector3(_mbfunction._spawnScale , _mbfunction._spawnScale , _mbfunction._spawnScale);
        }

        for (int i = 0; i < _mbfunction.attractObjs.Count ; i++) //for each attraction source
        {
            var attractObject = _mbfunction.attractObjs[i];
            var location = _mbfunction.attractlocations[i];
            attractObject.transform.localPosition = _mbfunction._map.GeoToWorldPosition(location , true);
            attractObject.transform.localScale = new Vector3(_mbfunction._spawnScale , _mbfunction._spawnScale , _mbfunction._spawnScale);
        }

        //Press Z to select the screen
        if (Input.GetKeyDown("z"))
        {
            //int w = Screen.width();
            //int h = Screen.height();
            Vector2d latlondelta = _mbfunction.getLatLonFromXY(0, Screen.height);
            _mbfunction.setStartLatLon(latlondelta);
            latlondelta = _mbfunction.getLatLonFromXY(Screen.width, 0);
            _mbfunction.setEndLatLon(latlondelta);
            startlat = _mbfunction.s_lat;
            startlon = _mbfunction.s_lon;
            xgridsize = _mbfunction.x_gsize;
            ygridsize = _mbfunction.y_gsize;
            heightxy = new float[xgridsize, ygridsize];
            pointToColorMap(startlat , startlon , xgridsize , ygridsize);
            createImage(0, 0); //Create Height Map
            Debug.Log("Map Array is created with size (" + xgridsize + ", " + ygridsize + ")");
        }

        //Press X to add dog to the map
        if (Input.GetKeyDown("x"))
        {
            Vector2d latlonDelta = _mbfunction.getLatLonFromMousePosition();
            doggroupsize.Add(initial_dog_groupsize); //size is static at 625
            _mbfunction.addDogLocation(latlonDelta, initial_dog_groupsize); //add new dog object from clicked position
            dogdata.Add(_mbfunction.getNewDog());
        }

        //Press C to add attract source to the map
        if (Input.GetKeyDown("c"))
        {
            Vector2d latlonDelta = _mbfunction.getLatLonFromMousePosition();
            _mbfunction.spawnAttractSource(latlonDelta.x , latlonDelta.y);
            attracter.Add(_mbfunction.getNewAttracter());
        }

        if (Input.GetKeyDown("v"))
        {
            startPreDataRegister = true;
            uicontroller.setupActivation(true);
        }

        if (startPreDataRegister)
        {
            //to initiate dog group
            if (uicontroller.getCompletedProcess() == 0){
                initializeDogGroup();
                uicontroller.updateProcessDetail("initializing dog group");
                uicontroller.triggerCompleteProcess();
            }
            //and start distribution until it converge
            else if (uicontroller.getCompletedProcess() == 1){
                if (convergeCountdown > 0)
                {
                    dogimageid++;
                    normalDistribution(dogimageid);
                    uicontroller.updateProcessDetail("normal distribution at " + dogimageid + "-th loop");
                    if (dogimageid < initial_dog_groupsize / 4){
                        uicontroller.updateCurrentProgress((dogimageid / (initial_dog_groupsize/4.0f)));
                    }
                    else {
                        uicontroller.updateCurrentProgress(1.0f);
                    }
                }
                else {
                    uicontroller.triggerCompleteProcess();
                }
            }
            //to create image
            else if (uicontroller.getCompletedProcess() == 2){
                uicontroller.updateProcessDetail("creating image for distribution");
                uicontroller.updateCurrentProgress(0.0f);
                maxColor(0);
                createImage(0, 1);
                createImage(0, 2);
                uicontroller.triggerCompleteProcess();
            }
            //to apply kernel density
            else if (uicontroller.getCompletedProcess() == 3){
                uicontroller.updateProcessDetail("apply kernel density");
                kernelDensityEstimation();
                createImage(0 , 3);
                uicontroller.triggerCompleteProcess();
            }
            //to start edge detection and home range calculation
            else if (uicontroller.getCompletedProcess() == 4){
                uicontroller.updateProcessDetail("apply walking extension, selection, and nodes attraction");
                if (attracter.Count <= 0){
                    edgeExpansion();
                    maxColor(1); //type 1 is walk type
                    createImage(0 , 4); //create walk extension image image

                    normalizeWalkingExtension();
                    kernelDensityEstimation(false);
                    walkingWithinHomeRange();
                    maxColor(2); //type 2 is walking habits type
                    createImage(0 , 5); //create only walking habits
                    createImage(0 , 6); //create walking habits and dog group
                }
                uicontroller.triggerCompleteProcess();
            }
            //and Also using kdb of (LoCoH)
            else if (uicontroller.getCompletedProcess() == 5){
                uicontroller.updateProcessDetail("apply extracted nodes to the attraction points");
                if (attracter.Count > 0){
                    walkToAttraction();
                }
                uicontroller.triggerCompleteProcess();
            }
            //let the dog walk to the closest attraction
            else if (uicontroller.getCompletedProcess() == 6){
                uicontroller.updateProcessDetail("extract attraction point");
                if (attracter.Count > 0){
                    if (atAttract < heuristic_init.Count){
                        uicontroller.updateProcessDetail("attraction point: " + atAttract + " / " + heuristic_init.Count + " is being extracted");
                        applyAttraction(heuristic_init[atAttract].lonid, heuristic_init[atAttract].latid);
                        atAttract++;
                        uicontroller.updateCurrentProgress((float)atAttract / heuristic_init.Count);
                    }
                    else {
                        highest_habits_rate = 0.0f;
                        maxColor(2);
                        createImage(0 , 5);
                        createImage(0 , 6);
                        uicontroller.triggerCompleteProcess();
                    }
                }
                else {
                    uicontroller.triggerCompleteProcess();
                }
            }
            //to initialize walking habits for controlled simulation
            else if (uicontroller.getCompletedProcess() == 7){
                uicontroller.updateProcessDetail("initialize simulation map");
                uicontroller.updateCurrentProgress(0.0f);
                highest_habits_rate = 0.0f;
                initializeWalkingSimulationMap();
                assignGroup();
                createImage(0, 7);
                highest_habits_rate = 0.0f;
                maxColor(2);
                decisionTree();
                uicontroller.triggerCompleteProcess();
            }
            //to normalize dog group and walking habits
            else if (uicontroller.getCompletedProcess() == 8){
                uicontroller.updateProcessDetail("normalize dog groups and habits");
                normalizeDogGroup();
                normalizeWalkingHabits();
                uicontroller.triggerCompleteProcess();
            }
            //to create sequence of dog habits in a day
            else if (uicontroller.getCompletedProcess() == 9){
                if (atTime < timeScaleFactor.Length){
                    uicontroller.updateProcessDetail("Dog sequence at "+ atTime + " is being created\nat activity rate " + timeScaleFactor[atTime]);
                    uicontroller.updateCurrentProgress((float)atTime / timeScaleFactor.Length);
                    createDogSequence(atTime);
                    calculateTieFromWH(atTime);
                    atTime++;
                }
                else {
                    uicontroller.setupActivation(false);
                    startPreDataRegister = false;
                    Debug.Log("This area has innate: " + (innate_total / (float)timeScaleFactor.Length) + ", outage: " + (outage_total / (float)timeScaleFactor.Length));
                }
            }
        }

        if (Input.GetKeyDown("b")){
            readDogPopulationPoint("Assets/dogpop_point.csv", 3, 2, 1);
        }
    }

    private float distributeElevationLevel(float height1, float height2)
    {
        if (!allowElevation){
            return 1.0f;
        }
        float degree = findDegreeSlope(_mbfunction.GridSize , abs(height1 - height2));
        if (degree > _mbfunction.walkable_degree || degree < -_mbfunction.walkable_degree)
        {
            degree = _mbfunction.walkable_degree;
        }
        return abs(Mathf.Cos(degree * 90.0f / _mbfunction.walkable_degree));
    }

    private float findDegreeSlope(float x , float y)
    {
        return Mathf.Atan2(y , x) / _mbfunction.one_degree_per_radian;
    }

    private float abs(float h)
    {
        if (h < 0.0f)
            return -h;
        else
            return h;
    }

    //Set height in the heatmap
    private void pointToColorMap(float lat, float lon, int xsize, int ysize)
    {
        float currlat = lat;
        float currlon = lon;
        float firstlat = lat;

        for (int x = 0; x < xsize; x++)
        {
            for (int y = 0; y < ysize; y++)
            {
                currlat -= _mbfunction.addLatByMeters(_mbfunction.GridSize);
                heightxy[x , y] = _mbfunction.getHeightAt(currlat , currlon);
                if (heightxy[x , y] < minh)
                    minh = heightxy[x , y];
                if (heightxy[x,y] > maxh)
                    maxh = heightxy[x , y];
            }
            currlat = firstlat;
            currlon += _mbfunction.addLonByMeters(_mbfunction.GridSize);
        }
    }

    //(reference: https://en.wikipedia.org/wiki/Home_range)
    private void initializeDogGroup()
    {
        doggroup = new float[xgridsize , ygridsize];
        tempgroup = new float[xgridsize , ygridsize];
        edge = new int[xgridsize , ygridsize];
        walk = new int[xgridsize , ygridsize];
        walkingHabits = new float[xgridsize , ygridsize];
        mapAfford = new float[xgridsize, ygridsize];
        groupassign = new int[xgridsize, ygridsize];
        dogfound = new float[xgridsize, ygridsize];

        for (int y = 0; y < ygridsize; y++)
        {
            for (int x = 0; x < xgridsize; x++)
            {
                doggroup[x , y] = 0.0f;
                tempgroup[x , y] = 0.0f;
                edge[x , y] = 0;
                walkingHabits[x , y] = 0.0f;
                groupassign[x, y] = 0;
                dogfound[x , y] = 0.0f;
            }
        }
        for (int i = 0; i < dogdata.Count; i++)
        {
            doggroup[dogdata[i].lonid , dogdata[i].latid] = dogdata[i].size;
            tempgroup[dogdata[i].lonid , dogdata[i].latid] = dogdata[i].size;
        }
    }

    //(reference: https://en.wikipedia.org/wiki/Normal_distribution)
    private void normalDistribution(int round)
    {
        int at_lat, at_lon;
        int initial_size = dogdata.Count;
        int small_lat = dogdata[0].latid, 
            big_lat = small_lat, 
            small_lon = dogdata[0].lonid, 
            big_lon = small_lon;
            
        for (int i = 1 ; i < initial_size ; i++)
        {
            at_lat = dogdata[i].latid;
            at_lon = dogdata[i].lonid;
            if (at_lat < small_lat)
            {
                small_lat = at_lat;
            }
            if (at_lat > big_lat)
            {
                big_lat = at_lat;
            }
            if (at_lon < small_lon)
            {
                small_lon = at_lon;
            }
            if (at_lon > big_lon)
            {
                big_lon = at_lon;
            }
        }

        int right = inSize(big_lon + round, false), 
            left  = inSize(small_lon - round, false), 
            down  = inSize(small_lat - round), 
            up    = inSize(big_lat + round);

        for (int lonid = left; lonid < right; lonid++)
        {
            for (int latid = down; latid < up; latid++)
            {
                centralDistribution(latid , lonid);
            }
        }
        
        extractDistribution();
        trackConvergence();
    }

    //Distributed unit must be inside the array
    private int inSize(int atlatorlon, bool isLat = true)
    {
        if (atlatorlon < 0)
            return 0;
        if (isLat)
        {
            if (atlatorlon >= ygridsize)
                return ygridsize - 1;
        }
        else
        {
            if (atlatorlon >= xgridsize)
                return xgridsize - 1;
        }
        return atlatorlon;
    }

    //Distribute relatives value to the center value
    private void centralDistribution(int latid , int lonid)
    {
        if (!latValid(latid) || !lonValid(lonid))
        {
            return;
        }

        float elev_up, elev_dn, elev_lf, elev_rt;
        float up_val = 0.0f, dn_val = 0.0f, lf_val = 0.0f, rt_val = 0.0f;

        bool upvalid = latValid(latid + 1), dnvalid = latValid(latid - 1);
        bool lfvalid = lonValid(lonid - 1), rtvalid = lonValid(lonid + 1);

        //find elevation of each direction
        //combine the received value from up, down, left, and right
        if (upvalid)
        {
            elev_up = distributeElevationLevel(heightxy[lonid , latid] , heightxy[lonid , latid + 1]);
            up_val = (doggroup[lonid , latid + 1] / 5.0f) * elev_up;
            if (up_val < distribution_criteria)
                up_val = 0.0f;
        }
        if (dnvalid)
        {
            elev_dn = distributeElevationLevel(heightxy[lonid , latid] , heightxy[lonid , latid - 1]);
            dn_val = (doggroup[lonid , latid - 1] / 5.0f) * elev_dn;
            if (dn_val < distribution_criteria)
                dn_val = 0.0f;
        }
        if (rtvalid)
        {
            elev_rt = distributeElevationLevel(heightxy[lonid , latid] , heightxy[lonid + 1 , latid]);
            rt_val = (doggroup[lonid + 1 , latid] / 5.0f) * elev_rt;
            if (rt_val < distribution_criteria)
                rt_val = 0.0f;
        }
        if (lfvalid)
        {
            elev_lf = distributeElevationLevel(heightxy[lonid , latid] , heightxy[lonid - 1 , latid]);
            lf_val = (doggroup[lonid - 1 , latid] / 5.0f) * elev_lf;
            if (lf_val < distribution_criteria)
                lf_val = 0.0f;
        }

        //combine the received value from up, down, left, and right
        float rear_distribute = up_val + dn_val + rt_val + lf_val;

        //if it takes value from its rear and greater than criteria
        if (rear_distribute > distribution_criteria)
        {
            //save the changes from up, down, left, right, and mid
            tempgroup[lonid , latid] += rear_distribute;
            if (upvalid) tempgroup[lonid , latid + 1] -= up_val;
            if (dnvalid) tempgroup[lonid , latid - 1] -= dn_val;
            if (rtvalid) tempgroup[lonid + 1 , latid] -= rt_val;
            if (lfvalid) tempgroup[lonid - 1 , latid] -= lf_val;
        }
    }

    private bool latValid(int lat_id)
    {
        if (lat_id < 0 || lat_id >= ygridsize)
        {
            return false;
        }
        return true;
    }

    private bool lonValid(int lon_id)
    {
        if (lon_id < 0 || lon_id >= xgridsize)
        {
            return false;
        }
        return true;
    }

    private void extractDistribution()
    {
        float tempvalue;
        for (int y = 0; y < ygridsize; y++)
        {
            for (int x = 0; x < xgridsize; x++)
            {
                tempvalue = tempgroup[x , y];
                if (tempvalue > 0.0f)
                {
                    if (doggroup[x , y] == 0.0f)
                    {
                        convergeChangeCount++;
                    }
                    edge[x , y] = 1;
                }
                //update the changes
                doggroup[x , y] = tempvalue;
            }
        }
    }

    private void trackConvergence()
    {
        if (convergeChangeCount == 0)
        {
            convergeCountdown--;
        }
        else
        {
            convergeCountdown = loopCriteria;
        }
        convergeChangeCount = 0;
    }

    //create image by image tag where 0=map, 1=dog, 2=mapanddog, 3=edge, 4=walking
    private void createImage(int route, int imagetype)
    {
        Texture2D texture = new Texture2D(xgridsize , ygridsize , TextureFormat.RGB24 , false);

        for (int lat = 0; lat < ygridsize; lat++)
        {
            for (int lon = 0; lon < xgridsize; lon++)
            {
                texture.SetPixel(lon , lat , getColorFromColorType((ygridsize - 1) - lat , lon , imagetype));
            }
        }

        texture.Apply();

        //encode to png
        byte[] bytes = texture.EncodeToPNG();
        Destroy(texture);

        File.WriteAllBytes(Application.dataPath + getFileNameTag(imagetype, route) , bytes);
    }

    //get color from image tag where 0=map, 1=dog, 2=mapanddog, 3=edge
    private Color getColorFromColorType(int lat, int lon, int imagetype)
    {
        if(imagetype == 0) //Edge Image
        {
            return new Color(((heightxy[lon , lat] - minh) / (maxh - minh)) , 0.0f , 0.0f);
        }
        else if (imagetype == 1) //Dog Image
        {
            if (doggroup[lon , lat] > 0.0f)
            {
                return new Color(0.0f , 255.0f * (doggroup[lon , lat] / highest_home_rate) , 0.0f);
            }
            else
            {
                return Color.black;
            }
        }
        else if (imagetype == 2) //Dog And Map Image
        {
            if (doggroup[lon , lat] > 0.0f)
            {
                return new Color(0.0f , doggroup[lon , lat] / highest_home_rate , 0.0f);
            }
            else
            {
                return new Color(((heightxy[lon , lat] - minh) / (maxh - minh)) , 0.0f , 0.0f);
            }
        }
        else if (imagetype == 3) //Edge Image
        {
            if (edge[lon , lat] > 0)
                return Color.white;
            else
                return Color.black;
        }
        else if (imagetype == 4) //Walking Extension Image
        {
            if (doggroup[lon , lat] > 0.0f)
            {
                return new Color(0.0f , doggroup[lon , lat] / highest_home_rate , 0.0f);
            }
            else if (walk[lon , lat] > 0)
            {
                float colorvalue = walk[lon , lat] / (float)highest_walking_rate;
                return new Color(colorvalue , colorvalue , 0.0f);
            }
            else
            {
                return new Color(((heightxy[lon , lat] - minh) / (maxh - minh)) , 0.0f , 0.0f);
            }
        }
        else if (imagetype == 5) //Walking Habits Only Image
        {
            if (walkingHabits[lon , lat] > 0)
            {
                float colorvalue = walkingHabits[lon , lat] / highest_habits_rate;
                return new Color(0.0f , colorvalue , colorvalue);
            }
            else
            {
                return new Color(((heightxy[lon , lat] - minh) / (maxh - minh)) , 0.0f , 0.0f);
            }
        }
        else if (imagetype == 6) //Walking Habits with Dog Group
        {
            if (doggroup[lon , lat] > 0.0f)
            {
                return new Color(0.0f , doggroup[lon , lat] / highest_home_rate , 0.0f);
            }
            else if (walkingHabits[lon , lat] > 0)
            {
                float colorvalue = walkingHabits[lon , lat] / highest_habits_rate;
                return new Color(0.0f , colorvalue , colorvalue);
            }
            else
            {
                return new Color(((heightxy[lon , lat] - minh) / (maxh - minh)) , 0.0f , 0.0f);
            }
        }
        else if (imagetype == 7) //Walking Affordance
        {
            if (mapAfford[lon, lat] > 0){
                float colorvalue = (float)mapAfford[lon, lat] / highest_afford[groupassign[lon, lat]];
                return new Color(colorvalue, 0.0f, colorvalue);
            }
            else {
                return new Color(((heightxy[lon , lat] - minh) / (maxh - minh)) , 0.0f , 0.0f);
            }
        }
        else if (imagetype == 8)
        {
            float colorvalue = wh[lon, lat] / highest_estimate_simulation_dog_color;
            return new Color(colorvalue, colorvalue, colorvalue);
        }
        return Color.black;
    }

    //get file name from image tag where 0=map, 1=dog, 2=mapanddog, 3=edge
    private string getFileNameTag(int imagetype, int route)
    {
        if (imagetype == 0)
        {
            return "/../Assets/MickRendered/plainSelectedTerrain.png";
        }
        else if (imagetype == 1)
        {
            return "/../Assets/MickRendered/selectedDogTerrain" + route + ".png";
        }
        else if (imagetype == 2)
        {
            return "/../Assets/MickRendered/selectedMapAndDog" + route + ".png";
        }
        else if (imagetype == 3)
        {
            return "/../Assets/MickRendered/selectedEdge" + route + ".png";
        }
        else if (imagetype == 4)
        {
            return "/../Assets/MickRendered/selectedHabits" + route + ".png";
        }
        else if (imagetype == 5)
        {
            return "/../Assets/MickRendered/selectedHabitsDownscape" + route + ".png";
        }
        else if (imagetype == 6)
        {
            return "/../Assets/MickRendered/selectedHabitsAndDog" + route + ".png";
        }
        else if (imagetype == 7)
        {
            return "/../Assets/MickRendered/selectedAfford" + route + ".png";
        }
        else if (imagetype == 8)
        {
            return "/../Assets/MickRendered/SingleDay/" + route + ".png";
        }
        return "/../Assets/MickRendered/createdImage" + route + ".png";
    }

    //(reference: https://en.wikipedia.org/wiki/Multivariate_kernel_density_estimation)
    private void kernelDensityEstimation(bool apply_edge = true)
    {
        //using edge detection on dog group (image processing)
        int[,] tempedge = new int[xgridsize , ygridsize];
        int[,] dir_val;

        //Relay array index to directional value
        if (apply_edge)
        {
            dir_val = edge;
        }
        else
        {
            dir_val = walk;
        }

        // kernel is  { [ -1 -1 -1] , [ -1  8 -1], [ -1 -1 -1] }
        for (int y = 0; y < ygridsize; y++)
        {
            for (int x = 0; x < xgridsize; x++)
            {
                if (y == 0)
                {
                    if (x == 0)
                    {
                        tempedge[x , y] = 5 * dir_val[x , y];
                        tempedge[x , y] -= 2 * dir_val[x + 1 , y];
                        tempedge[x , y] -= 2 * dir_val[x , y + 1];
                        tempedge[x , y] -= dir_val[x + 1 , y + 1];
                    }
                    else if (x == xgridsize - 1)
                    {
                        tempedge[x , y] = 5 * dir_val[x , y];
                        tempedge[x , y] -= 2 * dir_val[x - 1 , y];
                        tempedge[x , y] -= 2 * dir_val[x , y + 1];
                        tempedge[x , y] -= dir_val[x - 1 , y + 1];
                    }
                    else
                    {
                        tempedge[x , y] = 7 * dir_val[x , y];
                        tempedge[x , y] -= 2 * dir_val[x - 1 , y];
                        tempedge[x , y] -= 2 * dir_val[x - 1 , y];
                        tempedge[x , y] -= dir_val[x - 1 , y + 1];
                        tempedge[x , y] -= dir_val[x , y + 1];
                        tempedge[x , y] -= dir_val[x + 1 , y + 1];
                    }
                }
                else if (y == ygridsize - 1)
                {
                    if (x == 0)
                    {
                        tempedge[x , y] = 5 * dir_val[x , y];
                        tempedge[x , y] -= 2 * dir_val[x + 1 , y];
                        tempedge[x , y] -= 2 * dir_val[x , y - 1];
                        tempedge[x , y] -= dir_val[x + 1 , y - 1];
                    }
                    else if (x == xgridsize - 1)
                    {
                        tempedge[x , y] = 5 * dir_val[x , y];
                        tempedge[x , y] -= 2 * dir_val[x - 1 , y];
                        tempedge[x , y] -= 2 * dir_val[x , y - 1];
                        tempedge[x , y] -= dir_val[x - 1 , y - 1];
                    }
                    else
                    {
                        tempedge[x , y] = 7 * dir_val[x , y];
                        tempedge[x , y] -= 2 * dir_val[x - 1 , y];
                        tempedge[x , y] -= 2 * dir_val[x - 1 , y];
                        tempedge[x , y] -= dir_val[x - 1 , y - 1];
                        tempedge[x , y] -= dir_val[x , y - 1];
                        tempedge[x , y] -= dir_val[x + 1 , y - 1];
                    }
                }
                else if (x == 0)
                {
                    tempedge[x , y] = 7 * dir_val[x , y];
                    tempedge[x , y] -= 2 * dir_val[x, y + 1];
                    tempedge[x , y] -= 2 * dir_val[x, y - 1];
                    tempedge[x , y] -= dir_val[x + 1 , y - 1];
                    tempedge[x , y] -= dir_val[x + 1, y];
                    tempedge[x , y] -= dir_val[x + 1 , y + 1];
                }
                else if (x == xgridsize - 1)
                {
                    tempedge[x , y] = 7 * dir_val[x , y];
                    tempedge[x , y] -= 2 * dir_val[x , y + 1];
                    tempedge[x , y] -= 2 * dir_val[x , y - 1];
                    tempedge[x , y] -= dir_val[x - 1 , y - 1];
                    tempedge[x , y] -= dir_val[x - 1 , y];
                    tempedge[x , y] -= dir_val[x - 1 , y + 1];
                }
                else
                {
                    tempedge[x , y] = 8 * dir_val[x , y];
                    tempedge[x , y] -= dir_val[x - 1 , y - 1];
                    tempedge[x , y] -= dir_val[x , y - 1];
                    tempedge[x , y] -= dir_val[x + 1 , y - 1];
                    tempedge[x , y] -= dir_val[x - 1 , y];
                    tempedge[x , y] -= dir_val[x + 1 , y];
                    tempedge[x , y] -= dir_val[x - 1 , y + 1];
                    tempedge[x , y] -= dir_val[x , y + 1];
                    tempedge[x , y] -= dir_val[x + 1 , y + 1];
                }
            }
        }

        //Reapply the detected edge
        for (int y = 0; y < ygridsize; y++)
        {
            for (int x = 0; x < xgridsize; x++)
            {
                if (tempedge[x , y] <= 0.0f)
                {
                    dir_val[x , y] = 0;
                }
                else
                {
                    dir_val[x , y] = 1;
                    if (apply_edge)
                    {
                        findNearestGroupNeighbour(x, y, true);
                    }
                }
            }
        }

        if (apply_edge)
        {
            //Conclude the average radius of each dog group
            for (int i = 0; i < dogdata.Count; i++)
            {
                _mbfunction.dogradius[i] /= _mbfunction.factradius[i]; //Set dog radius from each group
                Debug.Log("Dog Group id: " + i + " has radius " + _mbfunction.dogradius[i] + " pixels");
            }
        }
    }

    private int findNearestGroupNeighbour(int x , int y, bool setRadius = false)
    {
        int selectedgroup = 0;
        float thisx = abs(dogdata[0].lonid - x);
        float thisy = abs(dogdata[0].latid - y);
        float distance = (thisx * thisx) + (thisy * thisy);
        float smallestsize = distance;

        //Find the closest neighbour of the current edge
        for (int i = 1; i < dogdata.Count; i++)
        {
            thisx = abs(dogdata[i].lonid - x);
            thisy = abs(dogdata[i].latid - y);

            //Euclidean distance while not square rooted
            distance = (thisx * thisx) + (thisy * thisy);
            if (distance < smallestsize)
            {
                smallestsize = distance;
                selectedgroup = i;
            }
        }

        if (setRadius)
        {
            _mbfunction.dogradius[selectedgroup] += Mathf.Sqrt(smallestsize);
            _mbfunction.factradius[selectedgroup]++;
        }

        return selectedgroup;
    }

    //(reference: https://en.wikipedia.org/wiki/Multivariate_kernel_density_estimation)
    private void edgeExpansion()
    {
        int radius, that;
        int[,] tempedge = new int[xgridsize , ygridsize];

        //Initialize value for walking pattern on edge
        for (int y = 0; y < ygridsize; y++)
        {
            for (int x = 0; x < xgridsize; x++)
            {
                that = edge[x , y];
                tempedge[x , y] = that;
                walk[x , y] = that;
            }
        }

        //Radius expansion
        for (int y = 0; y < ygridsize; y++)
        {
            for (int x = 0; x < xgridsize; x++)
            {
                if (edge[x , y] > 0) //If the edge exists
                {
                    radius = (int)_mbfunction.dogradius[findNearestGroupNeighbour(x , y)];
                    int topy = inSize(y + radius + 1);
                    int boty = inSize(y - radius - 1);

                    int leftx, rightx, xsize;

                    //Draw from bottom to center
                    for (int i = boty; i < y; i++)
                    {
                        xsize = findXSize(x , y , i, radius);
                        leftx = inSize(x - xsize, false);
                        rightx = inSize(x + xsize, false);
                        for (int j = leftx; j < rightx; j++)
                        {
                            tempedge[j , i]++;
                        }
                    }

                    //Draw from top to center
                    for (int i = topy; i >= y; i--)
                    {
                        xsize = findXSize(x , y , i , radius);
                        leftx = inSize(x - xsize , false);
                        rightx = inSize(x + xsize , false);
                        for (int j = leftx; j < rightx; j++)
                        {
                            tempedge[j , i]++;
                        }
                    }
                }
            }
        }

        //Reset Variables
        for (int y = 0; y < ygridsize; y++)
        {
            for (int x = 0; x < xgridsize; x++)
            {
                walk[x , y] = tempedge[x , y];
            }
        }
    }

    //Find the highest home, walk, or habits value
    private void maxColor(int type)
    {
        for (int y = 0; y < ygridsize; y++)
        {
            for (int x = 0; x < xgridsize; x++)
            {
                if (type == 0) //Home Type
                {
                    if (doggroup[x , y] > highest_home_rate)
                    {
                        highest_home_rate = doggroup[x , y];
                    }
                }
                else if (type == 1) //Walk Type
                {
                    if (walk[x , y] > highest_walking_rate)
                    {
                        highest_walking_rate = walk[x , y];
                    }
                }
                else if (type == 2) //Walk Habits Type
                {
                    if (walkingHabits[x , y] > highest_habits_rate)
                    {
                        highest_habits_rate = walkingHabits[x , y];
                    }
                }
            }
        }
    }

    //Euclidean Distance: find x2 from d, x1, y1, y2
    private int findXSize(int x, int y, int dy, float distance)
    {
        //distance^2 = (x2 - x1)^2 + (y2 - y1)^2
        //d^2 - y^2  = (x2 - x1)^2
        //sqrt(d-y) =  x2 - x1
        float dmy = Mathf.Sqrt((distance * distance) - ((dy - y) * (dy - y)));

        //Therefore, x2 = sqrt(d - y) + x1
        int x2 = (int)(dmy + x);

        //Return the difference of size x2 - x1
        return (x2 - x) + 1;
    }

    //Normalize walking extension map
    private void normalizeWalkingExtension()
    {
        for (int y = 0; y < ygridsize; y++)
        {
            for (int x = 0; x < xgridsize; x++)
            {
                if (walk[x , y] > 0)
                {
                    walk[x , y] = 1;
                }
            }
        }
    }

    //For the whole home range, walking will be calculated from elevation decremental
    private void walkingWithinHomeRange()
    {
        for (int y = 0; y < ygridsize; y++)
        {
            for (int x = 0; x < xgridsize; x++)
            {
                if (edge[x , y] > 0){
                    walkToAllExtensionalRange(x , y);
                }
            }
        }
    }

    //For each home point reaching path will move to the end direction of maximum walking range
    private void walkToAllExtensionalRange(int thisx, int thisy)
    {
        int radius;
        for (int y = 0; y < ygridsize; y++)
        {
            for (int x = 0; x < xgridsize; x++)
            {
                if (walk[x , y] > 0)
                {
                    radius = (int)_mbfunction.dogradius[findNearestGroupNeighbour(thisx , thisy)];
                    walkingBehaviour(thisx , thisy , x , y , radius);
                }
            }
        }       
    }

    //The moving behaviour probabilities of normal dog from t (this) point to d (destination) point
    private void walkingBehaviour(int d_x , int d_y , int t_x , int t_y , int mul)
    {
        int y_dir = 1, x_dir = 1;
        float cursize = doggroupsize[findNearestGroupNeighbour(t_x, t_y)]; //TO BE CHANGED
        float thisheight, y_dir_eva, x_dir_eva, distance;

        walkingHabits[d_x , d_y] += 1.0f;

        t_y = inSize(t_y);
        t_x = inSize(t_x , false);

        if (d_y > t_y)
        {
            y_dir = -1;
        }
        if (d_x > t_x)
        {
            x_dir = -1;
        }

        distance = ((d_y - t_y) * (d_y - t_y)) + ((d_x - t_x) * (d_x - t_x));
        float walking_criteria = (cursize / Mathf.Sqrt(distance)) / mul;

        while (d_y != t_y && d_x != t_x)
        {
            if (cursize <= distribution_criteria)
            {
                break;
            }

            if (d_y == t_y)
            {
                x_dir_eva = distributeElevationLevel(heightxy[d_x , d_y] , heightxy[d_x + x_dir , d_y]);
                cursize *= x_dir_eva;
                cursize -= walking_criteria;
                d_x += x_dir;
            }
            else if (d_x == t_x)
            {
                y_dir_eva = distributeElevationLevel(heightxy[d_x , d_y] , heightxy[d_x , d_y + y_dir]);
                cursize *= y_dir_eva;
                cursize -= walking_criteria;
                d_y += y_dir;
            }
            else
            {
                thisheight = heightxy[d_x , d_y];
                y_dir_eva = distributeElevationLevel(thisheight , heightxy[d_x , d_y + y_dir]);
                x_dir_eva = distributeElevationLevel(thisheight , heightxy[d_x + x_dir , d_y]);

                if (d_y - t_y == 1 || d_y - t_y == -1)
                {
                    cursize *= y_dir_eva;
                    cursize -= walking_criteria;
                    d_y += y_dir;
                }
                else if (d_x - t_x == 1 || d_x - t_x == -1)
                {
                    cursize *= x_dir_eva;
                    cursize -= walking_criteria;
                    d_x += x_dir;
                }
                else
                {
                    walkingHabits[d_x , d_y + y_dir] += x_dir_eva / (y_dir_eva + x_dir_eva + 1.0f);
                    walkingHabits[d_x + x_dir , d_y] += y_dir_eva / (y_dir_eva + x_dir_eva + 1.0f);
                    walkingHabits[d_x + x_dir , d_y + y_dir] += 1.0f / (y_dir_eva + x_dir_eva + 1.0f);

                    if (x_dir_eva > y_dir_eva)
                    {
                        d_x += x_dir;
                        cursize *= x_dir_eva;
                    }
                    else
                    {
                        d_y += y_dir;
                        cursize *= y_dir_eva;
                    }
                    cursize -= walking_criteria;
                }
            }
            walkingHabits[d_x , d_y] += 1.0f;
        }
    }

    private void initializeWalkingSimulationMap(){
        for (int y = 0 ; y < ygridsize;  y++){
            for (int x = 0; x < xgridsize; x++){
                if (doggroup[x , y] > 0.0f || walkingHabits[x , y] > 0.0f){
                    mapAfford[x , y] = 0.0f; //movable slot
                }
                else {
                    mapAfford[x , y] = -1.0f; //unmovable slot
                }
            }
        }

        for (int i = 0 ; i < dogdata.Count ; i++){
            mapAfford[dogdata[i].lonid, dogdata[i].latid] = 1.0f;
        }
        affordanceCounter();
    }

    private void affordanceCounter(){
        bool isUpdating = true;
        float b4;

        while (isUpdating){
            isUpdating = false;
            for (int y = 0 ; y < ygridsize ; y++){
                for (int x = 0 ; x < xgridsize ; x++){
                    b4 = mapAfford[x , y];
                    mapAfford[x , y] = getAfford(x, y);
                    if (b4 != mapAfford[x , y]){
                        isUpdating = true;
                    }
                }
            }
        }
    }

    private float getAfford(int x, int y){
        float[] dir = {0.0f, 0.0f, 0.0f, 0.0f}; //move 4 directions
        float min = 0.0f + xgridsize + ygridsize;
        if (mapAfford[x, y] == 0){ //if it is movable
            if (y >= 1){ //not top most
                dir[0] = mapAfford[x , y - 1] + 1.0f;
                if (y <= ygridsize - 2){ //not bot most
                    dir[1] = mapAfford[x , y + 1] + 1.0f;
                }
            }
            if (x >= 1){ //not left most
                dir[2] = mapAfford[x - 1 , y] + 1.0f;
                if (x <= xgridsize - 2){ //not right most
                    dir[3] = mapAfford[x + 1 , y] + 1.0f;
                }
            }
        }

        for (int i = 0 ; i < 4 ; i++){ //for each direction
            if (dir[i] > 1.0f) { //if it is allowed
                if (dir[i] < min){
                    min = dir[i];
                }
            }
        }

        if (min == xgridsize + ygridsize){ //if there is no candidate
            return mapAfford[x, y];
        }
        else {
            return min;
        }
    }

    private void assignGroup(){
        highest_afford = new float[dogdata.Count];
        count_area = new int[dogdata.Count];
        for (int i = 0 ; i < dogdata.Count ; i++){
            highest_afford[i] = 0;
            count_area[i] = 0;
        }
        for (int y = 0 ; y < ygridsize; y++) {
            for (int x = 0; x < xgridsize ; x++) {
                if (mapAfford[x , y] > 0) {
                    groupassign[x, y] = findNearestGroupNeighbour(x, y);
                    count_area[groupassign[x, y]]++;
                    if (mapAfford[x, y] > highest_afford[groupassign[x, y]]){
                        highest_afford[groupassign[x, y]] = mapAfford[x, y];
                    }
                }
            }
        }
    }

    private void decisionTree(){
        singleMoveRate = 1.0f - (hordeMoveRate + exploreMoveRate);

        highest_habits_rate = 0.0f;
        normalizeAfford(); //create image of afford normalization

        maxColor(2);
        createImage(1, 6);

        //express the walkable
        findBehaviouricRatio();
        normalizeReach(); //create image of afford combination

        createImage(2, 6);
    }

    private void normalizeAfford(){
        float[] totalHabits = new float[dogdata.Count];
        for (int i = 0 ; i < dogdata.Count ; i++){
            totalHabits[i] = 0.0f;
        }
        for (int y = 0 ; y < ygridsize ; y++){
            for (int x = 0 ; x < xgridsize ; x++){
                if (mapAfford[x , y] > 0){
                    walkingHabits[x , y] = walkingHabits[x, y] * (abs(highest_afford[groupassign[x, y]] - mapAfford[x , y]) / highest_afford[groupassign[x, y]]);
                    totalHabits[groupassign[x , y]] += walkingHabits[x, y];
                }
            }
        }
        for (int y = 0; y < ygridsize ; y++){
            for (int x = 0; x < xgridsize ; x++){
                if (mapAfford[x , y] > 0){
                    walkingHabits[x , y] /= totalHabits[groupassign[x, y]];
                }
            }
        }
    }

    private int singlecount = 0, explorecount = 0;
    private void findBehaviouricRatio(){
        for (int y = 0 ; y < ygridsize ; y++){
            for (int x = 0 ; x < xgridsize ; x++){
                if (doggroup[x , y] > 0.0f){
                    singlecount++;
                }
                else if (dogfound[x , y] > 0.0f){
                    explorecount++;
                }
            }
        }
    }

    private void normalizeReach(){
        int cA;
        float hS, hHR;
        for (int y = 0 ; y < ygridsize ; y++){
            for (int x = 0 ; x < xgridsize ; x++){
                if (doggroup[x , y] > 0){
                    dogfound[x , y] = (dogdata[groupassign[x , y]].size * singleMoveRate) / singlecount;
                }
                else if (mapAfford[x , y] > 0){
                    hHR = highest_habits_rate;
                    cA = count_area[groupassign[x, y]];
                    hS = dogdata[groupassign[x,y]].size;
                    dogfound[x , y] = getDogFoundWithWeight(walkingHabits[x , y], hHR, cA, hS, exploreMoveRate);
                }
            }
        }
    }

    private float getDogFoundWithWeight(float walkHabit, float maxHabit, int weight, float horde_size, float behaviour_rate) {
        return (horde_size * (walkHabit * maxHabit) * behaviour_rate) / weight;
    }

    List<AttractSource> heuristic_init;
    private void walkToAttraction(){
        node = new List<AttractNode>();
        heuristic_init = new List<AttractSource>();
        for (int y = 0 ; y < ygridsize ; y++){
            for (int x = 0 ; x < xgridsize ; x++){
                if (edge[x , y] > 0){
                    heuristic_init.Add(new AttractSource(y , x));
                }
            }
        }
        assignGroup();
    }

    private void applyAttraction(int x, int y){
        int source = findNearestAttractionSource(x, y);
        int endx = attracter[source].lonid;
        int endy = attracter[source].latid;
        //Walking from edge to attraction point
        inclineHeuristic(x , y, endx, endy);
        heuristicBackTrack(node.Count - 1);
        node = new List<AttractNode>();
        //Walking back from attraction point to edge
        inclineHeuristic(endx , endy, x, y);
        heuristicBackTrack(node.Count - 1);
        node = new List<AttractNode>();
    }

    private void inclineHeuristic(int startx, int starty, int endx, int endy){
        //HEURISTICS MODEL FROM A TO B, where moved_distance = GridSize * distributeElevationLevel per Grid
        node.Add(new AttractNode(startx, starty, 0, true, -1, -1));
        bool reachEndPoint = false;
        int selected_node;
        while (!reachEndPoint){
            selected_node = findLowestCostNode();
            reachEndPoint = extractShortest(selected_node, endx, endy);
        }
    }

    private bool extractShortest(int n_id, int to_x, int to_y){
        int curx = node[n_id].x;
        int cury = node[n_id].y;
        float cost = node[n_id].cost;
        int parentNode = node[n_id].parent_node;
        node[n_id] = new AttractNode(curx , cury , cost , false , node[n_id].parent_dir, parentNode);
        float distance, hardReach;

        int[] dirx = {0 , 0 , -1 , 1};
        int[] diry = {-1 , 1 , 0 , 0};
        for (int i = 0 ; i < 4 ; i++){
            if ((i == 0 && cury > 0) || (i == 1 && cury < ygridsize - 1) || (i == 2 && curx > 0) || (i == 3 && curx < xgridsize - 1)) 
            {
                if (!isHeuristicReturn(curx + dirx[i], cury + diry[i])){
                    distance = abs(to_x - (curx + dirx[i])) + abs(to_y - (cury + diry[i]));
                    hardReach = distributeElevationLevel(heightxy[curx , cury], heightxy[curx + dirx[i] , cury + diry[i]]);
                    if (hardReach < 0.1f) hardReach = 0.1f;
                    node.Add(new AttractNode(curx + dirx[i] , cury + diry[i], cost + (distance / hardReach), true, i, n_id));
                    if (distance == 0.0f) return true;
                }
            }
        }
        return false;
    }

    private int findLowestCostNode(){
        int min_node = 0;
        float min_cost = -1.0f;
        for (int i = 1 ;  i < node.Count ; i++){
            if ((node[i].cost < min_cost || min_cost == -1.0f) && node[i].mutable){
                min_cost = node[i].cost;
                min_node = i;
            }
        }
        return min_node;
    }

    private int findNearestAttractionSource(int x, int y){
        int selectedSource = 0;
        float thisx = abs(attracter[0].lonid - x);
        float thisy = abs(attracter[0].latid - y);
        float distance = (thisx * thisx) + (thisy * thisy);
        float smallestsize = distance;

        for (int i = 1 ; i < attracter.Count ; i++){
            thisx = abs(attracter[i].lonid - x);
            thisy = abs(attracter[i].latid - y);

            distance = (thisx * thisx) + (thisy * thisy);
            if (distance < smallestsize){
                smallestsize = distance;
                selectedSource = i;
            }
        }

        return selectedSource;
    }

    private bool isHeuristicReturn(int x , int y){
        for (int i = 0 ; i < node.Count ; i++){
            if(node[i].x == x && node[i].y == y){
                return true;
            }
        }
        return false;
    }

    private void heuristicBackTrack(int node_id){
        int dir = node[node_id].parent_dir;;
        int curx = node[node_id].x;
        int cury = node[node_id].y;
        walkingHabits[curx, cury] += 1.0f;
        while (dir != -1 && node_id != -1) {
            dir = node[node_id].parent_dir;
            if (dir == 0){
                cury++;
            }
            else if (dir == 1){
                cury--;
            }
            else if (dir == 2){
                curx++;
            }
            else if (dir == 3){
                curx--;
            }
            walkingHabits[curx, cury] += 1.0f;
            node_id = node[node_id].parent_node;
        }
    }

    private void initialTimeScaleFactor(){
        int timescalesize = ((24 * 60 * 60) / time_length); //The size of time scale based on seconds in a day
        timescalesize += (timescalesize % time_cycle); //The interval time is separated into 4 time cycles
        timeScaleFactor = new float[timescalesize];
        setDogTimeCycle(timescalesize);
    }

    private void setDogTimeCycle(int time_scale_size){
        //The changing of dog activity rate based on temporal
        float activity_difference = -1.0f * (highest_activity_rate - lowest_activity_rate) / (time_scale_size / time_cycle);

        float current_activity_rate = highest_activity_rate; //Dog activity rate starts from 6:00 am + time_cycle
        
        int count = 0;
        int cycle = 0;

        for (int t = 0 ; t < time_scale_size ; t++){ // Active 6 hours + Less Active 6 hours :: cycle
            current_activity_rate += activity_difference;
            timeScaleFactor[count] = current_activity_rate;
            count++;
            cycle++;

            if (cycle > (time_scale_size / time_cycle)){
                cycle = 0;
                activity_difference *= -1.0f;
            }
        }
    }

    private float highest_estimate_simulation_dog_color;
    private void createDogSequence(int current_time){
        wh = new float[xgridsize, ygridsize];
        float affordable;
        float h2s = hordeMoveRate / 2.0f;
        float h2e = h2s;

        highest_estimate_simulation_dog_color = 0.0001f;
        for (int y = 0 ; y < ygridsize ; y++){
            for (int x = 0 ; x < xgridsize ; x++){
                if (doggroup[x , y] > 0.0f){
                    wh[x , y] = doggroup[x , y] * (doggroupsize[groupassign[x , y]]) * (singleMoveRate + h2s);
                }
                else if (walkingHabits[x , y] > 0.0f){
                    affordable = timeScaleFactor[current_time] * highest_afford[groupassign[x, y]];
                    if (mapAfford[x , y] <= affordable){
                        wh[x , y] = walkingHabits[x , y] * (exploreMoveRate + h2e) * timeScaleFactor[current_time] * (doggroupsize[groupassign[x , y]]);
                    }
                    else {
                        wh[x , y] = 0.0f;
                    }
                }
                else {
                    wh[x , y] = 0.0f;
                }

                if (wh[x , y] > highest_estimate_simulation_dog_color)
                {
                    highest_estimate_simulation_dog_color = wh[x , y];
                }
            }
        }
        /*  The changing of behaviour will not occurred for many reasons
        float oh2e, oh2s;
        oh2s = h2s;
        oh2e = h2e;
        h2s = singleMoveRate * (exploreMoveRate + oh2e);
        h2e = exploreMoveRate * (singleMoveRate + oh2s);
        singleMoveRate -= h2s;
        exploreMoveRate -= h2e;
        singleMoveRate += (1.0f / 3.0f) * (h2s + h2e);
        exploreMoveRate += (1.0f / 3.0f) * (h2s + h2e);
        h2s = (1.0f / 6.0f) * (h2s + h2e);
        h2e = h2s;
        oh2s = (h2s + h2e) - (hordeMoveRate * 2.0f);
        if (oh2s > 0){
            singleMoveRate += oh2s / 2.0f;
            exploreMoveRate += oh2s / 2.0f;
            h2s -= oh2s / 2.0f;
            h2e -= oh2s / 2.0f;
        }
        */
        createImage(current_time, 8);
    }

    private void normalizeDogGroup(){
        float[] totaldoggroup = new float[dogdata.Count];
        for (int i = 0 ; i < dogdata.Count ; i++){
            totaldoggroup[i] = 0.0f;
        }
        for (int y = 0 ; y < ygridsize ; y++){
            for (int x = 0 ; x < xgridsize ; x++){
                totaldoggroup[groupassign[x , y]] += doggroup[x, y];
            }
        }
        for (int y = 0 ; y < ygridsize ; y++){
            for (int x = 0 ; x < xgridsize ; x++){
                doggroup[x, y] /= totaldoggroup[groupassign[x , y]];
            }
        }
    }

    private void normalizeWalkingHabits(){
        float[] totalhabits = new float[dogdata.Count];
        for (int i = 0 ; i < dogdata.Count ; i++){
            totalhabits[i] = 0.0f;
        }
        for (int y = 0 ; y < ygridsize ; y++){
            for (int x = 0 ; x < xgridsize ; x++){
                totalhabits[groupassign[x , y]] += walkingHabits[x, y];
            }
        }
        for (int y = 0 ; y < ygridsize ; y++){
            for (int x = 0 ; x < xgridsize ; x++){
                walkingHabits[x, y] /= totalhabits[groupassign[x , y]];
            }
        }
    }

    private void readDogPopulationPoint(string path, int lat_index, int lon_index, int size_index){
        StreamReader reader = new StreamReader(path);
        string line = reader.ReadLine();
        string[] fractions;
        float lat, lon, size;
        Vector2d botleft = _mbfunction.getLatLonFromXY(0, 0);
        Vector2d topright = _mbfunction.getLatLonFromXY(Screen.width, Screen.height);
        Debug.Log(botleft);
        Debug.Log(topright);

        while(!reader.EndOfStream){
            line = reader.ReadLine();
            fractions = line.Split(',');
            lat = float.Parse(fractions[lat_index]);
            lon = float.Parse(fractions[lon_index]);

            if (lon > botleft.x && lon < topright.x && lat > botleft.y && lat < topright.y){
                Vector2d latlonDelta = new Vector2d(lon, lat);
                size = float.Parse(fractions[size_index]);
                doggroupsize.Add(size);
                _mbfunction.addDogLocation(latlonDelta, size);
                dogdata.Add(_mbfunction.getNewDog());
            }
        }
        Debug.Log(doggroupsize.Count);
    }

    float innate_total = 0.0f, outage_total = 0.0f;
    private void calculateTieFromWH(int at_time){
        float inside_total = 0.0f;
        float outside_total = 0.0f;
        int outside_count = 0, inside_count = 0;
        for (int y = 0 ; y < ygridsize ; y++){
            for (int x = 0 ; x < xgridsize ; x++){
                if (doggroup[x , y] > 0.0f){
                    inside_total += wh[x , y];
                    inside_count++;
                }
                else if (walkingHabits[x , y] > 0.0f){
                    outside_total += wh[x , y];
                    outside_count++;
                }
            }
        }
        inside_total /= inside_count;
        outside_total /= outside_count; //find the average

        outside_total *= timeScaleFactor[at_time];
        inside_total *= highest_activity_rate - timeScaleFactor[at_time];

        Debug.Log("At" + at_time + " has o: " + outside_total + ", i: " + inside_total);
        
        outage_total += outside_total;
        innate_total += inside_total;
    }
}