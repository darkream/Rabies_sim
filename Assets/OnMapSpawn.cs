using UnityEngine;
using Mapbox.Utils;
using Mapbox.Unity.Map;
using Mapbox.Unity.Utilities;
using Mapbox.Map;
using Mapbox.Unity.MeshGeneration.Data;
using System.IO;
using System.Collections.Generic;

[System.Serializable]
public struct LatLonSize
{
    public int latid;
    public int lonid;
    public float size;

    public LatLonSize(int lt, int ln, float sz)
    {
        latid = lt;
        lonid = ln;
        size = sz;
    }
}

[System.Serializable]
public struct EvacPoint
{
    public int groupid;
    public int latid;
    public int lonid;

    public EvacPoint(int gid, int lt, int ln)
    {
        groupid = gid;
        latid = lt;
        lonid = ln;
    }
}

public class OnMapSpawn : MonoBehaviour
{
    [SerializeField]
    AbstractMap _map;

    [SerializeField]
    [Geocode]
    List<string> doglocationStrings; //list of dogs == 7.03169034704473, 100.478511282507 default
    List<Vector2d> doglocations;

    [SerializeField]
    List<int> doggroupsize; //initial group size of the list of dogs above

    [SerializeField]
    float _spawnScale = 10f;

    [SerializeField]
    GameObject gridpanel; //Prefabs for grid layer

    [SerializeField]
    GameObject dogpanel; //Prefabs for dog layer

    [SerializeField]
    GameObject mappoint;

    [SerializeField]
    float radius_earth = 6378.1f; // Radius of the earth in km (Google Answer)

    [SerializeField]
    float walkable_degree = 60.0f; //Range is between 0.0f to 90.0f degrees

    [SerializeField]
    float distribution_criteria = 0.5f; //0.5 dog means at least 1 dog

    //(reference: http://www.longitudestore.com/how-big-is-one-gps-degree.html)
    private float rough_sphere_per_degree = 111111.0f;
    private float equator_latitude_per_degree = 110570.0f; //110 km per degree
    private float pole_latitude_per_degree = 111690.0f; //111.69 km per degree
    private float widest_longitude_per_degree = 111321.0f; //111.321 km longitude per degree at equator (while 0 at pole)
    private float one_degree_per_radian = 0.0174532925f; //PI divided by 180


    //Data List of dog objects
    List<GameObject> dogObjs;

    //Data List of map point objects
    List<GameObject> mappointObjs;
    List<Vector2d> mappointlocations;

    [SerializeField]
    Camera _referenceCamera;

    [SerializeField]
    GameObject DogLayer; //Dog Layer, their child elements are in here

    [SerializeField]
    float GridSize; //default: "5", unit: meters
// private float startlat = 7.044082f, startlon = 100.4482f; //default_ lat: 7.044082, lon = 100.4482
    public float startlat = 7.044082f, startlon = 100.4482f; //default_ lat: 7.044082, lon = 100.4482

   // private int xgridsize, ygridsize; //default_ xsize = 1700 grid, ysize = 1000 grid
    public int xgridsize, ygridsize; //default_ xsize = 1700 grid, ysize = 1000 grid
    private float minh, maxh;
    private float[,] heightxy;

    private List<int> factradius;
    private List<float> dogradius;
    private List<LatLonSize> dogdata;
    private float[,] doggroup; //dog group size in 2D-array
    private float[,] tempgroup;
    private int[,] edge;

    [SerializeField]
    int loopCriteria = 4;
    
    private int convergeCountdown;
    private int convergeChangeCount = 0;
    private int dogimageid = 0;

    [SerializeField]
    int initial_dog_groupsize;

    [SerializeField]
    float homeRangeMultiplier = 1.8f; //default home range multiplier = x1.8

    [SerializeField]
    float hordeMoveRate = 0.4f;

    [SerializeField]
    float exploreMoveRate = 0.4f;

    private bool allowedDogMovement = false;
    private List<EvacPoint> movepoint;
    private int[,] walkingHabits;

    void Start()
    {
        int initSize = doglocationStrings.Count;
        doglocations = new List<Vector2d>(); //initialization for the dog object
        mappointlocations = new List<Vector2d>();
        dogObjs = new List<GameObject>();
        mappointObjs = new List<GameObject>();
        dogdata = new List<LatLonSize>();
        dogradius = new List<float>();
        factradius = new List<int>();
        movepoint = new List<EvacPoint>();
        convergeCountdown = loopCriteria;
        for (int i = 0; i < initSize; i++)
        {
            addDogLocation(doglocationStrings[i]);
            dogradius.Add(0.0f);
            factradius.Add(0);
        }
    }

    private void Update()
    {
        for (int i = 0; i < dogObjs.Count; i++) //for each spawn object
        {
            var dogObject = dogObjs[i];
            var location = doglocations[i]; //spawn the object to the dog locations
            dogObject.transform.localPosition = _map.GeoToWorldPosition(location , true);
            dogObject.transform.localScale = new Vector3(_spawnScale , _spawnScale , _spawnScale);
        }

        for (int i = 0; i < mappointObjs.Count; i++)
        {
            var mapObject = mappointObjs[i];
            var location = mappointlocations[i]; //spawn the object to the dog locations
            mapObject.transform.localPosition = _map.GeoToWorldPosition(location , true);
            mapObject.transform.localScale = new Vector3(_spawnScale , _spawnScale , _spawnScale);
        }

        //Press Z to select the initiated map
        if (Input.GetKeyDown("z"))
        {
            Vector2d latlongDelta = getLatLonFromMousePosition();
            setStartLatLon(latlongDelta);
        }

        //Press X to select the xsize and ysize of the map
        if (Input.GetKeyDown("x"))
        {
            Vector2d latlongDelta = getLatLonFromMousePosition();
            setEndLatLon(latlongDelta);

            pointToColorMap(startlat , startlon , xgridsize , ygridsize);
            createImage(0, 0); //Create Height Map
            Debug.Log("Map Array is created with size (" + xgridsize + ", " + ygridsize + ")");
        }

        //Press C to add dog in the map
        if (Input.GetKeyDown("c"))
        {
            Vector2d latlongDelta = getLatLonFromMousePosition();
            doggroupsize.Add(initial_dog_groupsize); //size is static at 25
            addDogLocation(latlongDelta); //add new dog object from clicked position
        }

        //Press V to initiate dog group
        if (Input.GetKeyDown("v"))
        {
            Debug.Log("initiate dog group");
            initializeDogGroup();
        }

        //Press B to start distribution until it converge
        if (Input.GetKeyDown("b"))
        {
            while (convergeCountdown > 0)
            {
                dogimageid++;
                normalDistribution(dogimageid);
            }
            Debug.Log("distributed until it is converged at " + dogimageid + "-th loop");
            createImage(0, 1); //Create Dog Map
            createImage(0, 2); //Create Dog and Height Map
        }

        //Press N to start edge detection and home range calculation
        if (Input.GetKeyDown("n"))
        {
            setDogRadius();
            createImage(0 , 3); //Create Edge Map
            Debug.Log("edge map is created");
        }

        if (Input.GetKeyDown("m"))
        {
            //allowedDogMovement = true;
            kernelDensityEstimation();
            createImage(0 , 4); //create walking habits image
            Debug.Log("walking habits map is created");
        }
    }

    //assign distance of camera to ground plane to z, 
    //otherwise ScreenToWorldPoint() will always return the position of the camera
    //(reference: http://answers.unity3d.com/answers/599100/view.html)
    private Vector2d getLatLonFromMousePosition()
    {
        //Reminder this is (x, y, 0), so top-left is Screen.height and bot-right is Screen.width
        Vector3 mousePosScreen = Input.mousePosition;   
        mousePosScreen.z = _referenceCamera.transform.localPosition.y;
        Vector3 pos = _referenceCamera.ScreenToWorldPoint(mousePosScreen);
        return _map.WorldToGeoPosition(pos);
    }

    //Harversine Formula, 
    //(reference: https://stackoverflow.com/questions/639695/how-to-convert-latitude-or-longitude-to-meters)
    private float getDistanceFromLatLonInKm(float lat1 , float lon1 , float lat2 , float lon2)
    {
        float dLat = (lat2 - lat1) * one_degree_per_radian;  // deg2rad below
        float dLon = (lon2 - lon1) * one_degree_per_radian;
        float a =
          Mathf.Sin(dLat / 2.0f) * Mathf.Sin(dLat / 2.0f) +
          Mathf.Cos((lat1 * one_degree_per_radian)) * Mathf.Cos((lat2 * one_degree_per_radian)) *
          Mathf.Sin(dLon / 2.0f) * Mathf.Sin(dLon / 2.0f)
          ;
        float c = 2.0f * Mathf.Atan2(Mathf.Sqrt(a) , Mathf.Sqrt(1.0f - a));
        float d = radius_earth * c; // Distance = Radius.km x coefficient
        return d;
    }

    //Directions of this function indicate by +/- of meter value
    private float addLatByMeters(float meter) //return the increased or decreased Lat by meter
    {
        return meter / rough_sphere_per_degree; // because sin(90 degree) is 1
    }

    //Directions of this function indicate by +/- of meter value
    private float addLonByMeters(float meter) //return the increased or decreased Lon by meter
    {
        return meter / rough_sphere_per_degree; //because cos(0 degree) is 1
    }

    //Original [lat, long] add meter conversion
    private float addLatByMeters(float meter , float theta)
    {
        return (meter / rough_sphere_per_degree) * Mathf.Sin(theta);
    }

    private float addLonByMeters(float meter , float theta)
    {
        return (meter / rough_sphere_per_degree) * Mathf.Cos(theta);
    }

    //Earth is oblate sphere, so if we're getting taking it seriously we have to do this
    private float addLatByMetersOS(float meter , float currentlat)
    {
        //the difference length between equator and the pole
        float difference = pole_latitude_per_degree - equator_latitude_per_degree;
        float latitude_length = ((currentlat / 90.0f) * difference) + equator_latitude_per_degree;
        return meter / latitude_length;
    } 

    //(reference: https://gis.stackexchange.com/questions/142326/calculating-longitude-length-in-miles)
    private float addLonByMetersOS(float meter , float currentlat)
    {
        //The length of longitude depends on the current latitude
        float latitude_degree_radian = currentlat * one_degree_per_radian;
        float longitude_length = widest_longitude_per_degree * latitude_degree_radian;
        return meter / longitude_length;
    }

    //using Mapbox Conversion return length of meter in lat, lon distance
    private float addLatByMetersMapbox(float meter)
    {
        float vy = (meter / one_degree_per_radian) * radius_earth;
        float assoc = (2 * Mathf.Atan(Mathf.Exp(vy * one_degree_per_radian)) - (one_degree_per_radian * 180.0f) / 2);
        return assoc / one_degree_per_radian;
    }

    private float addLonByMetersMapbox(float meter)
    {
        return (meter / one_degree_per_radian) * radius_earth;
    }

    private Vector2d addLatLonByMetersMapbox(Vector2d latlonvector)
    {
        return Conversions.MetersToLatLon(latlonvector);
    }

    //Add new dog location by float lat lon
    private void addDogLocation(float lat , float lon)
    {
        doglocations.Add(new Vector2d(lat,lon));
        createDogObject();
    }

    //Add new doglocation by location string of vector lat lon, Formula: "lat, long"
    private void addDogLocation(string locationstring)
    {
        doglocations.Add(spawnLatLonWithinGrid(Conversions.StringToLatLon(locationstring)));
        createDogObject();
    }

    //Add new doglocation by location vector lat lon
    private void addDogLocation(Vector2d latlonvector)
    {
        doglocations.Add(spawnLatLonWithinGrid(latlonvector));
        createDogObject();
    }

    //create the object to the map location (with default height)
    private void createDogObject()
    {
        //In Latitude, the map is drawn in vector of (+Lon, -Lat) direction
        int lastIndex = doglocations.Count - 1;
        float lat = (float)doglocations[lastIndex].x;
        float lon = (float)doglocations[lastIndex].y;
        int at_lat = ygridsize - getLatGridIndex(abs(startlat - lat));
        int at_lon = getLonGridIndex(abs(startlon - lon));
        dogdata.Add(new LatLonSize(at_lat , at_lon , doggroupsize[lastIndex]));
        spawnDogPrefabWithHeight(lat , lon);
    }

    /// create the object to the map location (with calculated map height)
    /// (reference: https://github.com/mapbox/mapbox-unity-sdk/issues/222)
    private void spawnDogPrefabWithHeight(double lat , double lon)
    {
        UnityTile tile = getTileAt(lat , lon);
        float h = getHeightAt((float)lat,(float)lon);

        Vector3 location = Conversions.GeoToWorldPosition(lat , lon , _map.CenterMercator , _map.WorldRelativeScale).ToVector3xz();
        location = new Vector3(location.x , h * tile.TileScale, location.z);

        var obj = Instantiate(dogpanel);
        obj.transform.position = location;
        obj.transform.localScale = new Vector3(_spawnScale , _spawnScale , _spawnScale);
        obj.transform.parent = DogLayer.transform; //let the dog becomes the child of DogLayer game object
        
        dogObjs.Add(obj);
    }

    //Add map point reference to the world
    private void spawnMapPointer(double lat, double lon)
    {
        mappointlocations.Add(new Vector2d(lat , lon));
        Vector3 location = Conversions.GeoToWorldPosition(lat , lon , _map.CenterMercator , _map.WorldRelativeScale).ToVector3xz();
        location = new Vector3(location.x , 0.0f , location.z);
        var obj = Instantiate(mappoint);
        obj.transform.position = location;
        obj.transform.localScale = new Vector3(_spawnScale , _spawnScale , _spawnScale);
        
        mappointObjs.Add(obj);
    }

    //Get the Tile Material from the Mapbox
    private UnityTile getTileAt(double lat, double lon)
    {
        //get tile ID
        var tileIDUnwrapped = TileCover.CoordinateToTileId(new Vector2d(lat , lon) , (int)_map.Zoom);

        //get tile
        return _map.MapVisualizer.GetUnityTileFromUnwrappedTileId(tileIDUnwrapped);
    }

    private Vector2d spawnLatLonWithinGrid(float lat , float lon)
    {
        lat -= lat % (GridSize / rough_sphere_per_degree);
        lon -= lon % (GridSize / rough_sphere_per_degree);
        return new Vector2d(lat,lon);
    }

    private Vector2d spawnLatLonWithinGrid(Vector2d latlonvector)
    {
        latlonvector.x -= latlonvector.x % (GridSize / rough_sphere_per_degree);
        latlonvector.y -= latlonvector.y % (GridSize / rough_sphere_per_degree);
        return latlonvector;
    }

    private Vector2d spawnLatLonWithinGridMapbox(float lat, float lon)
    {
        Vector2d meter_conversion = Conversions.LatLonToMeters(lat,lon);
        meter_conversion.x += meter_conversion.x % GridSize;
        meter_conversion.y += meter_conversion.y % GridSize;
        return Conversions.MetersToLatLon(meter_conversion);
    }

    private Vector2d spawnLatLonWithinGridMapbox(Vector2d latlonvector)
    {
        Vector2d meter_conversion = Conversions.LatLonToMeters(latlonvector);
        meter_conversion.x += meter_conversion.x % GridSize;
        meter_conversion.y += meter_conversion.y % GridSize;
        return Conversions.MetersToLatLon(meter_conversion);
    }

    //The level of distribution
    //since the distribution always equal to GridSize, height difference create the theta elevation
    private float distributeElevationLevel(float height1, float height2)
    {
        float degree = findDegreeSlope(GridSize , abs(height1 - height2));
        if (degree > walkable_degree || degree < -walkable_degree)
        {
            degree = walkable_degree;
        }
        return abs(Mathf.Cos(degree * 90.0f / walkable_degree));
    }

    private float findDegreeSlope(float x , float y)
    {
        return Mathf.Atan2(y , x) / one_degree_per_radian;
    }

    private float abs(float h)
    {
        if (h < 0.0f)
            return -h;
        else
            return h;
    }

    private float getHeightAt(float lat , float lon)
    {
        UnityTile tile = getTileAt(lat,lon);

        //lat lon to meters because the tiles rect is also in meters
        Vector2d v2d = Conversions.LatLonToMeters(new Vector2d(lat , lon));
        //get the origin of the tile in meters
        Vector2d v2dcenter = tile.Rect.Center - new Vector2d(tile.Rect.Size.x / 2.0 , tile.Rect.Size.y / 2.0);
        //offset between the tile origin and the lat lon point
        Vector2d diff = v2d - v2dcenter;

        //maping the diffetences to (0-1)
        float Dx = (float)(diff.x / tile.Rect.Size.x);
        float Dy = (float)(diff.y / tile.Rect.Size.y);

        //height in unity units
        float h = tile.QueryHeightData(Dx , Dy);

        return h / tile.TileScale; //return height_in_meter
    }

    private void setStartLatLon(Vector2d latlondelta)
    {
        destroyAllMapPoints();
        destroyAllDogs();
        startlat = (float)latlondelta.x;
        startlon = (float)latlondelta.y;
        spawnMapPointer(startlat , startlon);
    }

    private void setEndLatLon(Vector2d latlondelta)
    {
        ygridsize = getLatGridIndex(abs(startlat - (float)latlondelta.x));
        xgridsize = getLonGridIndex(abs(startlon - (float)latlondelta.y));
        heightxy = new float[xgridsize , ygridsize];
        spawnMapPointer(latlondelta.x , latlondelta.y);
    }

    private void destroyAllDogs()
    {
        int count = dogObjs.Count;
        for (int i = 0; i < count; i++)
        {
            Destroy(dogObjs[0]);
            dogObjs.RemoveAt(0);
            doglocations.RemoveAt(0);
            doggroupsize.RemoveAt(0);
            dogdata.RemoveAt(0);
        }
    }

    private void destroyAllMapPoints()
    {
        int count = mappointObjs.Count;
        for (int i = 0; i < count; i++)
        {
            Destroy(mappointObjs[0]);
            mappointObjs.RemoveAt(0);
            mappointlocations.RemoveAt(0);
        }
    }

    //set height in the heatmap
    private void pointToColorMap(float lat, float lon, int xsize, int ysize)
    {
        float currlat = lat;
        float currlon = lon;
        float firstlat = lat;

        for (int x = 0; x < xsize; x++)
        {
            for (int y = 0; y < ysize; y++)
            {
                currlat -= addLatByMeters(GridSize);
                heightxy[x , y] = getHeightAt(currlat , currlon);
                if (heightxy[x , y] < minh)
                    minh = heightxy[x , y];
                if (heightxy[x,y] > maxh)
                    maxh = heightxy[x , y];
            }
            currlat = firstlat;
            currlon += addLonByMeters(GridSize);
        }
    }

    private int getLatGridIndex(float moved_lat)
    {
        return (int)(moved_lat / addLatByMeters(GridSize));
    }

    private int getLonGridIndex(float moved_lon)
    {
        return (int)(moved_lon / addLonByMeters(GridSize));
    }

    //(reference: https://en.wikipedia.org/wiki/Home_range)
    private void initializeDogGroup()
    {
        doggroup = new float[xgridsize , ygridsize];
        tempgroup = new float[xgridsize , ygridsize];
        edge = new int[xgridsize , ygridsize];
        walkingHabits = new int[xgridsize , ygridsize];

        for (int y = 0; y < ygridsize; y++)
        {
            for (int x = 0; x < xgridsize; x++)
            {
                doggroup[x , y] = 0.0f;
                tempgroup[x , y] = 0.0f;
                edge[x , y] = 0;
                walkingHabits[x , y] = 0;
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
        for (int i = 0; i < initial_size; i++)
        {
            at_lat = dogdata[i].latid;
            at_lon = dogdata[i].lonid;
            int right = inSize(at_lon + round, false), 
                left  = inSize(at_lon - round, false), 
                down  = inSize(at_lat - round), 
                up    = inSize(at_lat + round);

            for (int lonid = left; lonid < right; lonid++)
            {
                for (int latid = down; latid < up; latid++)
                {
                    centralDistribution(latid , lonid);
                }
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
                texture.SetPixel(lon , lat , getColorFromColorType(lat , lon , imagetype));
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
                return new Color(0.0f , 255.0f * (doggroup[lon , lat] / initial_dog_groupsize) , 0.0f);
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
                return new Color(0.0f , 255.0f * (doggroup[lon , lat] / initial_dog_groupsize) , 0.0f);
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
        else if (imagetype == 4) //Walking Habit Image
        {
            if (doggroup[lon , lat] > 0.0f)
            {
                return new Color(0.0f , 255.0f * (doggroup[lon , lat] / initial_dog_groupsize) , 0.0f);
            }
            else if (walkingHabits[lon, lat] != 0)
            {
                return new Color(0.0f , 0.0f , 255.0f);
            }
            else
            {
                return new Color(((heightxy[lon , lat] - minh) / (maxh - minh)) , 0.0f , 0.0f);
            }
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
        return "/../Assets/MickRendered/createdImage" + route + ".png";
    }

    //Set dog radius from each group
    private void setDogRadius()
    {
        //using edge detection on dog group (image processing)
        // kernel is  { [ -1 -1 -1] , [ -1  8 -1], [ -1 -1 -1] }
        int[,] tempedge = new int[xgridsize , ygridsize];
        for (int y = 0; y < ygridsize; y++)
        {
            for (int x = 0; x < xgridsize; x++)
            {
                if (y == 0)
                {
                    if (x == 0)
                    {
                        tempedge[x , y] = 5 * edge[x , y];
                        tempedge[x , y] -= 2 * edge[x + 1 , y];
                        tempedge[x , y] -= 2 * edge[x , y + 1];
                        tempedge[x , y] -= edge[x + 1 , y + 1];
                    }
                    else if (x == xgridsize - 1)
                    {
                        tempedge[x , y] = 5 * edge[x , y];
                        tempedge[x , y] -= 2 * edge[x - 1 , y];
                        tempedge[x , y] -= 2 * edge[x , y + 1];
                        tempedge[x , y] -= edge[x - 1 , y + 1];
                    }
                    else
                    {
                        tempedge[x , y] = 7 * edge[x , y];
                        tempedge[x , y] -= 2 * edge[x - 1 , y];
                        tempedge[x , y] -= 2 * edge[x - 1 , y];
                        tempedge[x , y] -= edge[x - 1 , y + 1];
                        tempedge[x , y] -= edge[x , y + 1];
                        tempedge[x , y] -= edge[x + 1 , y + 1];
                    }
                }
                else if (y == ygridsize - 1)
                {
                    if (x == 0)
                    {
                        tempedge[x , y] = 5 * edge[x , y];
                        tempedge[x , y] -= 2 * edge[x + 1 , y];
                        tempedge[x , y] -= 2 * edge[x , y - 1];
                        tempedge[x , y] -= edge[x + 1 , y - 1];
                    }
                    else if (x == xgridsize - 1)
                    {
                        tempedge[x , y] = 5 * edge[x , y];
                        tempedge[x , y] -= 2 * edge[x - 1 , y];
                        tempedge[x , y] -= 2 * edge[x , y - 1];
                        tempedge[x , y] -= edge[x - 1 , y - 1];
                    }
                    else
                    {
                        tempedge[x , y] = 7 * edge[x , y];
                        tempedge[x , y] -= 2 * edge[x - 1 , y];
                        tempedge[x , y] -= 2 * edge[x - 1 , y];
                        tempedge[x , y] -= edge[x - 1 , y - 1];
                        tempedge[x , y] -= edge[x , y - 1];
                        tempedge[x , y] -= edge[x + 1 , y - 1];
                    }
                }
                else if (x == 0)
                {
                    tempedge[x , y] = 7 * edge[x , y];
                    tempedge[x , y] -= 2 * edge[x, y + 1];
                    tempedge[x , y] -= 2 * edge[x, y - 1];
                    tempedge[x , y] -= edge[x + 1 , y - 1];
                    tempedge[x , y] -= edge[x + 1, y];
                    tempedge[x , y] -= edge[x + 1 , y + 1];
                }
                else if (x == xgridsize - 1)
                {
                    tempedge[x , y] = 7 * edge[x , y];
                    tempedge[x , y] -= 2 * edge[x , y + 1];
                    tempedge[x , y] -= 2 * edge[x , y - 1];
                    tempedge[x , y] -= edge[x - 1 , y - 1];
                    tempedge[x , y] -= edge[x - 1 , y];
                    tempedge[x , y] -= edge[x - 1 , y + 1];
                }
                else
                {
                    tempedge[x , y] = 8 * edge[x , y];
                    tempedge[x , y] -= edge[x - 1 , y - 1];
                    tempedge[x , y] -= edge[x , y - 1];
                    tempedge[x , y] -= edge[x + 1 , y - 1];
                    tempedge[x , y] -= edge[x - 1 , y];
                    tempedge[x , y] -= edge[x + 1 , y];
                    tempedge[x , y] -= edge[x - 1 , y + 1];
                    tempedge[x , y] -= edge[x , y + 1];
                    tempedge[x , y] -= edge[x + 1 , y + 1];
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
                    edge[x , y] = 0;
                }
                else
                {
                    edge[x , y] = 1;
                    addAverageRadiusOfGroup(x, y); //Combine (not yet concluded) the radius
                }
            }
        }

        //Conclude the average radius of each dog group
        for (int i = 0; i < dogdata.Count; i++)
        {
            dogradius[i] /= factradius[i];
            Debug.Log("Dog Group id: " + i + " has radius " + dogradius[i] + " pixels");
        }
    }

    private void addAverageRadiusOfGroup(int x, int y)
    {
        int selectedgroup = 0;
        float thisx = abs(dogdata[0].lonid - x), thisy = abs(dogdata[0].latid -y);
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
        //Euclidean distance
        dogradius[selectedgroup] += Mathf.Sqrt(smallestsize);
        factradius[selectedgroup]++;
    }

    //(reference: https://en.wikipedia.org/wiki/Multivariate_kernel_density_estimation)
    private void kernelDensityEstimation()
    {
        int thislat, thislon;
        int mul, curx = 0;
        for (int i = 0; i < dogdata.Count; i++)
        {
            thislat = dogdata[i].latid;
            thislon = dogdata[i].lonid;
            mul = (int)(dogradius[i] * homeRangeMultiplier);

            //Add top, bottom, left, and right
            walkingBehaviour(i , thislat + mul , thislon);
            walkingBehaviour(i , thislat - mul , thislon);
            walkingBehaviour(i , thislat , thislon + mul);
            walkingBehaviour(i , thislat , thislon - mul);

            /*
            //Draw from bottom to center
            for (int y = (thislat - mul) + 1; y < thislat; y++)
            {
                walkingBehaviour(i , y , thislon - curx);
                walkingBehaviour(i , y , thislon + curx);
                curx++;
            }
            curx = 0;
            
            //Draw from top to center
            for (int y = (thislat + mul) - 1; y > thislat; y++)
            {
                walkingBehaviour(i , y , thislon - curx);
                walkingBehaviour(i , y , thislon + curx);
                curx++;
            }
            */
        }
    }

    private void walkingBehaviour(int d_id, int t_lat, int t_lon)
    {
        int d_lat, d_lon;
        int y_dir = 1, x_dir = 1;
        float cursize = initial_dog_groupsize;
        float thisheight, y_dir_eva, x_dir_eva, distance;

        d_lat = dogdata[d_id].latid; //dog lat
        d_lon = dogdata[d_id].lonid; //dog lon

        walkingHabits[d_lon , d_lat]++;

        if (t_lat > ygridsize)
        {
            t_lat = ygridsize - 1;
        }
        if (t_lon > xgridsize)
        {
            t_lon = xgridsize - 1;
        }
        if (t_lat < 0)
        {
            t_lat = 0;
        }
        if (t_lon < 0)
        {
            t_lon = 0;
        }

        if (d_lat > t_lat) //because lat move in vector -lat
        {
            y_dir = -1;
        }
        if (d_lon > t_lon)
        {
            x_dir = -1;
        }

        distance = ((d_lat - t_lat) * (d_lat - t_lat)) + ((d_lon - t_lon)* (d_lon - t_lon));

        while (d_lat != t_lat && d_lon != t_lon)
        {
            if (cursize <= distribution_criteria)
            {
                break;
            }

            if (d_lat - t_lat == 0)
            {
                x_dir_eva = distributeElevationLevel(heightxy[d_lon , d_lat] , heightxy[d_lon + x_dir , d_lat]);
                cursize *= x_dir_eva;
                cursize -= initial_dog_groupsize / distance;
                d_lon += x_dir;
            }
            else if (d_lon - t_lon == 0)
            {
                y_dir_eva = distributeElevationLevel(heightxy[d_lon , d_lat] , heightxy[d_lon , d_lat + y_dir]);
                cursize *= y_dir_eva;
                cursize -= initial_dog_groupsize / distance;
                d_lat += y_dir;
            }
            else
            {
                thisheight = heightxy[d_lon , d_lat];
                y_dir_eva = distributeElevationLevel(thisheight , heightxy[d_lon , d_lat + y_dir]);
                x_dir_eva = distributeElevationLevel(thisheight , heightxy[d_lon + x_dir , d_lat]);

                if (d_lat - t_lat == 1 || d_lat - t_lat == -1)
                {
                    cursize *= y_dir_eva;
                    cursize -= initial_dog_groupsize / distance;
                    d_lat += y_dir;
                }
                else if (d_lon - t_lon == 1 || d_lon - t_lon == -1)
                {
                    cursize *= x_dir_eva;
                    cursize -= initial_dog_groupsize / distance;
                    d_lon += x_dir;
                }
                else if (y_dir_eva > x_dir_eva)
                {
                    cursize *= y_dir_eva;
                    cursize -= initial_dog_groupsize / distance;
                    d_lat += y_dir;
                }
                else //if (x_dir_eva >= y_dir_eva)
                {
                    cursize *= x_dir_eva;
                    cursize -= initial_dog_groupsize / distance;
                    d_lon += x_dir;
                }
            }
            walkingHabits[d_lon , d_lat]++;
        }
        
    }
}