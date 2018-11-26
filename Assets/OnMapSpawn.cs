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

    [SerializeField]
    Camera _referenceCamera;

    [SerializeField]
    GameObject DogLayer; //Dog Layer, their child elements are in here

    [SerializeField]
    float GridSize; //default: "5", unit: meters

    private float startlat = 7.044082f, startlon = 100.4482f; //default_ lat: 7.044082, lon = 100.4482
    private int xgridsize = 1700, ygridsize = 1000; //default_ xsize = 1700 grid, ysize = 1000 grid
    private float minh, maxh;
    private float[,] heightxy;

    private List<LatLonSize> dogdata;
    private float[,] doggroup; //dog group size in 2D-array
    private float[,] tempgroup;

    [SerializeField]
    int loopCriteria = 10;

    void Start()
    {
        int initSize = doglocationStrings.Count;
        doglocations = new List<Vector2d>(); //initialization for the dog object
        dogObjs = new List<GameObject>();
        dogdata = new List<LatLonSize>();
        for (int i = 0; i < initSize; i++)
        {
            addDogLocation(doglocationStrings[i]);
        }

        heightxy = new float[xgridsize , ygridsize];
        pointToColorMap(startlat , startlon , xgridsize , ygridsize);
        createMapImage(xgridsize , ygridsize);

        initializeDogGroup();
        for (int i = 0; i < loopCriteria; i++)
        {
            normalDistribution();
        }

        createDogImage(xgridsize , ygridsize);
    }

    private void Update()
    {
        for (int i = 0; i < dogObjs.Count; i++) //for each spawn object
        {
            var spawnedObject = dogObjs[i];
            var location = doglocations[i]; //spawn the object to the dog locations
            spawnedObject.transform.localPosition = _map.GeoToWorldPosition(location , true);
            spawnedObject.transform.localScale = new Vector3(_spawnScale , _spawnScale , _spawnScale);
        }

        //on click spawn dog
        if (Input.GetMouseButtonUp(1))
        {
            Vector3 mousePosScreen = Input.mousePosition;
            //assign distance of camera to ground plane to z, otherwise ScreenToWorldPoint() will always return the position of the camera
            //(reference: http://answers.unity3d.com/answers/599100/view.html)
            mousePosScreen.z = _referenceCamera.transform.localPosition.y;
            Vector3 pos = _referenceCamera.ScreenToWorldPoint(mousePosScreen);

            Vector2d latlongDelta = _map.WorldToGeoPosition(pos);
            addDogLocation(latlongDelta); //add new dog object from clicked position
        }
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
        return (meter / 111111.0f) * Mathf.Sin(theta);
    }

    private float addLonByMeters(float meter , float theta)
    {
        return (meter / 111111.0f) * Mathf.Cos(theta);
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
        int lastIndex = doglocations.Count - 1;
        float lat = (float)doglocations[lastIndex].x;
        float lon = (float)doglocations[lastIndex].y;
        int at_lat = getLatGridIndex(abs(startlat - lat));
        int at_lon = getLonGridIndex(abs(startlon - lon));
        dogdata.Add(new LatLonSize(at_lat , at_lon , doggroupsize[lastIndex]));
        spawnDogPrefabWithHeight(lat , lon);
    }

    /// create the object to the map location (with calculated map height)
    /// (reference: https://github.com/mapbox/mapbox-unity-sdk/issues/222)
    void spawnDogPrefabWithHeight(double lat , double lon)
    {
        //get tile ID
        var tileIDUnwrapped = TileCover.CoordinateToTileId(new Vector2d(lat , lon) , (int)_map.Zoom);

        //get tile
        UnityTile tile = _map.MapVisualizer.GetUnityTileFromUnwrappedTileId(tileIDUnwrapped);

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

        //lat lon to unity units
        Vector3 location = Conversions.GeoToWorldPosition(lat , lon , _map.CenterMercator , _map.WorldRelativeScale).ToVector3xz();
        //replace y in position
        location = new Vector3(location.x , h , location.z);

        var obj = Instantiate(dogpanel);
        obj.transform.position = location;
        obj.transform.localScale = new Vector3(_spawnScale , _spawnScale , _spawnScale);
        obj.transform.parent = DogLayer.transform; //let the dog becomes the child of DogLayer game object
        
        dogObjs.Add(obj);
    }

    private Vector2d spawnLatLonWithinGrid(float lat , float lon)
    {
        lat -= lat % (GridSize / 111111.0f);
        lon -= lon % (GridSize / 111111.0f);
        return new Vector2d(lat,lon);
    }

    private Vector2d spawnLatLonWithinGrid(Vector2d latlonvector)
    {
        latlonvector.x -= latlonvector.x % (GridSize / 111111.0f);
        latlonvector.y -= latlonvector.y % (GridSize / 111111.0f);
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
        //get tile ID
        var tileIDUnwrapped = TileCover.CoordinateToTileId(new Vector2d(lat , lon) , (int)_map.Zoom);

        //get tile
        UnityTile tile = _map.MapVisualizer.GetUnityTileFromUnwrappedTileId(tileIDUnwrapped);

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

    //set height in the heatmap
    private void pointToColorMap(float lat, float lon, int xsize, int ysize)
    {
        int width = xsize, height = ysize;
        float currlat = lat;
        float currlon = lon;
        float firstlat = lat;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
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

    //create image from map height
    private void createMapImage(int input_width, int input_height)
    {
        int width = input_width, height = input_height;
        Texture2D texture = new Texture2D(width , height , TextureFormat.RGB24, false);
        Color color = new Color(255.0f , 0.0f , 0.0f);
        float colorvalue = 0.0f;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                colorvalue = ((heightxy[x,y] - minh) / (maxh - minh));
                color = new Color(colorvalue , 0.0f , 0.0f);
                texture.SetPixel(x , y , color);
            }
        }
        texture.Apply();

        //encode to png
        byte[] bytes = texture.EncodeToPNG();
        Destroy(texture);

        File.WriteAllBytes(Application.dataPath + "/../Assets/MickRendered/plainTerrain.png" , bytes);
    }

    private int getLatGridIndex(float moved_lat)
    {
        return (int)(moved_lat / addLatByMeters(GridSize));
    }

    private int getLonGridIndex(float moved_lon)
    {
        return (int)(moved_lon / addLonByMeters(GridSize));
    }

    private void initializeDogGroup()
    {
        doggroup = new float[xgridsize , ygridsize];
        tempgroup = new float[xgridsize , ygridsize];

        for (int y = 0; y < ygridsize; y++)
        {
            for (int x = 0; x < xgridsize; x++)
            {
                doggroup[x , y] = 0.0f;
                tempgroup[x , y] = 0.0f;
            }
        }
        for (int i = 0; i < dogdata.Count; i++)
        {
            doggroup[dogdata[i].lonid , dogdata[i].latid] = dogdata[i].size;
            tempgroup[dogdata[i].lonid , dogdata[i].latid] = dogdata[i].size;
        }
    }

    private void normalDistribution()
    {
        int at_lat, at_lon;
        int initial_size = dogdata.Count;
        for (int i = 0; i < initial_size; i++)
        {
            at_lat = dogdata[i].latid;
            at_lon = dogdata[i].lonid;

            //normal distribution from up, down, left, right, and mid
            centralDistribution(at_lat + 1 , at_lon);    //up
            centralDistribution(at_lat - 1 , at_lon);    //down
            centralDistribution(at_lat , at_lon + 1);    //left
            centralDistribution(at_lat , at_lon - 1);    //right
            centralDistribution(at_lat, at_lon);         //mid
        }
        extractDistribution();
    }

    private void centralDistribution(int latid, int lonid)
    {
        //find elevation of each direction
        float elev_up = distributeElevationLevel(heightxy[lonid , latid] , heightxy[lonid , latid + 1]);
        float elev_dn = distributeElevationLevel(heightxy[lonid , latid] , heightxy[lonid , latid - 1]);
        float elev_lf = distributeElevationLevel(heightxy[lonid , latid] , heightxy[lonid + 1 , latid]);
        float elev_rt = distributeElevationLevel(heightxy[lonid , latid] , heightxy[lonid - 1 , latid]);

        //combine the received value from up, down, left, and right
        float up = (doggroup[lonid , latid + 1] / 5.0f) * elev_up; //up
        float dn = (doggroup[lonid , latid - 1] / 5.0f) * elev_dn; //down
        float rt = (doggroup[lonid + 1 , latid] / 5.0f) * elev_rt; //left
        float lf = (doggroup[lonid - 1 , latid] / 5.0f) * elev_lf; //right

        float rear_distribute = up + dn + rt + lf;

        //if it takes value from its rear and greater than criteria
        if (rear_distribute > distribution_criteria)
        {
            //save the changes from up, down, left, right, and mid
            tempgroup[lonid , latid] += rear_distribute;
            tempgroup[lonid , latid + 1] -= up;
            tempgroup[lonid , latid - 1] -= dn;
            tempgroup[lonid + 1 , latid] -= rt;
            tempgroup[lonid - 1 , latid] -= lf;
        }
    }

    private void extractDistribution()
    {
        float tempvalue;
        for (int y = 0; y < ygridsize; y++)
        {
            for (int x = 0; x < xgridsize; x++)
            {
                tempvalue = tempgroup[x , y];

                //update the changes
                doggroup[x , y] = tempvalue;
                if (tempvalue > 0.0f)
                {
                    changeSizeOfDogData(y , x , tempvalue);
                }
            }
        }
    }

    private void changeSizeOfDogData(int latid, int lonid, float size)
    {
        bool found = false;
        for (int i = 0; i < dogdata.Count; i++)
        {
            if (dogdata[i].latid == latid)
            {
                if (dogdata[i].lonid == lonid)
                {
                    dogdata[i] = new LatLonSize(dogdata[i].latid, dogdata[i].lonid, size);
                    found = true;
                }
            }
        }
        if (!found)
        {
            dogdata.Add(new LatLonSize(latid , lonid , size));
        }
    }

    private void createDogImage(int sizex, int sizey)
    {
        int width = sizex, height = sizey;
        Texture2D texture = new Texture2D(width , height , TextureFormat.RGB24 , false);
        Color color = new Color(0.0f , 0.0f , 0.0f);
        float colorvalue = 0.0f;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (doggroup[x,y] > 0.0f)
                {
                    colorvalue = 255.0f;
                }
                else
                {
                    colorvalue = 0.0f;
                }
                color = new Color(0.0f , colorvalue , 0.0f);
                texture.SetPixel(x , y , color);
            }
        }

        texture.Apply();

        //encode to png
        byte[] bytes = texture.EncodeToPNG();
        Destroy(texture);

        File.WriteAllBytes(Application.dataPath + "/../Assets/MickRendered/plainDogTerrainWithIncline.png" , bytes);
    }
}