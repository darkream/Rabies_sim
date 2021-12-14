/*
Temporaly cration list

Latlonsize variation (add each state dog's size)

    ^ Ref from above
    |
    |
Createdogobject fucntion (add each state dog size) (at 275 line)


 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Mapbox.Utils;
using Mapbox.Unity.Map;
using Mapbox.Unity.Utilities;
using Mapbox.Map;
using Mapbox.Unity.MeshGeneration.Data;

[System.Serializable]
public struct AttractNode
{
    public int x;
    public int y;
    public float cost;
    public bool mutable; //0 = down, 1 = up, 2 = left, 3 = right
    public int parent_dir;
    public int parent_node;
    public AttractNode(int atx, int aty, float heuristic_cost, bool isMutable, int dir, int parent){
        x = atx;
        y = aty;
        cost = heuristic_cost;
        mutable = isMutable;
        parent_dir = dir;
        parent_node = parent;
    }
}

[System.Serializable]
public struct LatLonSize
{
    public int latid;
    public int lonid;
    public float size;

   /*  public float Dog_S_size;
    public float Dog_E_size;
    public float Dog_I_size;
    public float Dog_R_size;
    public float Dog_V_size;*/

    public LatLonSize(int lt, int ln, float sz)
    {
        latid = lt;
        lonid = ln;
        size = sz;
        /* 
        Dog_S_size = szs;
        Dog_E_size = sze;
        Dog_I_size = szi;
        Dog_R_size = szr;
        Dog_V_size = szv;
        */
    }
}





[System.Serializable]
public struct AttractSource
{
    public int latid;
    public int lonid;

    public AttractSource(int lt, int ln)
    {
        latid = lt;
        lonid = ln;
    }
}

public class MapboxInheritance : MonoBehaviour
{
    [SerializeField]
    Camera _referenceCamera;

     [SerializeField]
    public float radius_earth = 6378.1f; // Radius of the earth in km (Google Answer)

    [SerializeField]
    public float walkable_degree = 60.0f; //Range is between 0.0f to 90.0f degrees

    public AbstractMap _map;

    //(reference: http://www.longitudestore.com/how-big-is-one-gps-degree.html)
    public float rough_sphere_per_degree = 111111.0f;
    public float equator_latitude_per_degree = 110570.0f; //110 km per degree
    public float pole_latitude_per_degree = 111690.0f; //111.69 km per degree
    public float widest_longitude_per_degree = 111321.0f; //111.321 km longitude per degree at equator (while 0 at pole)
    public float one_degree_per_radian = 0.0174532925f; //PI divided by 180

    public float s_lat = 7.044082f, s_lon = 100.4482f; //default_ lat: 7.044082, lon = 100.4482 // s_lat = 7.044082f, s_lon = 100.4482f;
    public int x_gsize, y_gsize; //default_ xsize = 1700 grid, ysize = 1000 grid
    LatLonSize tempdoglocation;

    LatLonSize tempinfect;
    AttractSource tempattracter;

    [SerializeField]
    GameObject dogpanel; //Prefabs for dog layer

    [SerializeField]
    GameObject infectedpanel; //Prefabs for infected layer

    [SerializeField]
    GameObject attractpanel; //Prefabs for attraction layer

    [SerializeField]
    GameObject mappanel; //Prefabs for map point layer

    [SerializeField]
    public float _spawnScale = 1f;

    [SerializeField]
    GameObject DogLayer; //Dog Layer, their child elements are in here

    [SerializeField]
    GameObject InfectLayer;

    [SerializeField]
    public float GridSize = 5.0f; //default: "5", unit: meters

    //Data List of dog objects
    public List<GameObject> dogObjs;
    public List<Vector2d> doglocations;  //List<string> doglocationStrings; //list of dogs == 7.03169034704473, 100.478511282507 default
     //Data list of infected object
    public List<GameObject> infectObjs;
    public List<Vector2d> infectedlocations;

    //Data List of map point objects
    public List<GameObject> mappointObjs;
    public List<Vector2d> mappointlocations;

    //Data List of attraction point objects
    public List<GameObject> attractObjs;
    public List<Vector2d> attractlocations;
    public List<int> factradius;
    public List<float> dogradius;
    public float[,] heightxy;
    public void initializeLocations(){
        doglocations = new List<Vector2d>(); //initialization for the dog object
        mappointlocations = new List<Vector2d>();
        attractlocations = new List<Vector2d>();
        dogObjs = new List<GameObject>();
        infectObjs = new List<GameObject>();
        mappointObjs = new List<GameObject>();
        attractObjs = new List<GameObject>();
        dogradius = new List<float>();
        factradius = new List<int>();

      
    }

    //assign distance of camera to ground plane to z, 
    //otherwise ScreenToWorldPoint() will always return the position of the camera
    //(reference: http://answers.unity3d.com/answers/599100/view.html)
    public Vector2d getLatLonFromMousePosition()
    {
        Vector3 mousePosScreen = Input.mousePosition;   
        mousePosScreen.z = _referenceCamera.transform.localPosition.y;
        Vector3 pos = _referenceCamera.ScreenToWorldPoint(mousePosScreen);
        return _map.WorldToGeoPosition(pos);
    }

    public Vector2d getLatLonFromXY(int w, int h){
        //Reminder this is (x, y, 0), so top-left is Screen.height and bot-right is Screen.width
        Vector3 posScreen = new Vector3(w , h, 0);
        posScreen.z = _referenceCamera.transform.localPosition.y;
        Vector3 pos = _referenceCamera.ScreenToWorldPoint(posScreen);
        return _map.WorldToGeoPosition(pos);
    }

    //Harversine Formula, 
    //(reference: https://stackoverflow.com/questions/639695/how-to-convert-latitude-or-longitude-to-meters)
    public float getDistanceFromLatLonInKm(float lat1 , float lon1 , float lat2 , float lon2)
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
    public float addLatByMeters(float meter) //return the increased or decreased Lat by meter
    {
        return meter / rough_sphere_per_degree; // because sin(90 degree) is 1
    }

    //Directions of this function indicate by +/- of meter value
    public float addLonByMeters(float meter) //return the increased or decreased Lon by meter
    {
        return meter / rough_sphere_per_degree; //because cos(0 degree) is 1
    }

    //Original [lat, long] add meter conversion
    public float addLatByMeters(float meter , float theta)
    {
        return (meter / rough_sphere_per_degree) * Mathf.Sin(theta);
    }

    public float addLonByMeters(float meter , float theta)
    {
        return (meter / rough_sphere_per_degree) * Mathf.Cos(theta);
    }

    //Earth is oblate sphere, so if we're getting taking it seriously we have to do this
    public float addLatByMetersOS(float meter , float currentlat)
    {
        //the difference length between equator and the pole
        float difference = pole_latitude_per_degree - equator_latitude_per_degree;
        float latitude_length = ((currentlat / 90.0f) * difference) + equator_latitude_per_degree;
        return meter / latitude_length;
    } 

    //(reference: https://gis.stackexchange.com/questions/142326/calculating-longitude-length-in-miles)
    public float addLonByMetersOS(float meter , float currentlat)
    {
        //The length of longitude depends on the current latitude
        float latitude_degree_radian = currentlat * one_degree_per_radian;
        float longitude_length = widest_longitude_per_degree * latitude_degree_radian;
        return meter / longitude_length;
    }

    //using Mapbox Conversion return length of meter in lat, lon distance
    public float addLatByMetersMapbox(float meter)
    {
        float vy = (meter / one_degree_per_radian) * radius_earth;
        float assoc = (2 * Mathf.Atan(Mathf.Exp(vy * one_degree_per_radian)) - (one_degree_per_radian * 180.0f) / 2);
        return assoc / one_degree_per_radian;
    }

    public float addLonByMetersMapbox(float meter)
    {
        return (meter / one_degree_per_radian) * radius_earth;
    }

    public Vector2d addLatLonByMetersMapbox(Vector2d latlonvector)
    {
        return Conversions.MetersToLatLon(latlonvector);
    }

        //Add new dog location by float lat lon
    public void addDogLocation(float lat , float lon, float groupsize)
    {
        doglocations.Add(new Vector2d(lat,lon));
        createDogObject(groupsize);
    }

    //Add new doglocation by location string of vector lat lon, Formula: "lat, long"
    public void addDogLocation(string locationstring, float groupsize)
    {
        doglocations.Add(spawnLatLonWithinGrid(Conversions.StringToLatLon(locationstring)));
        createDogObject(groupsize);
    }

    //Add new doglocation by location vector lat lon
    public void addDogLocation(Vector2d latlonvector, float groupsize)
    {
        doglocations.Add(spawnLatLonWithinGrid(latlonvector));
        createDogObject(groupsize);
    }

    //create the object to the map location (with default height)
    public void createDogObject(float groupsize)
    {
        //In Latitude, the map is drawn in vector of (+Lon, -Lat) direction
        int lastIndex = doglocations.Count - 1;
        float lat = (float)doglocations[lastIndex].x;
        float lon = (float)doglocations[lastIndex].y;
        int at_lat = getLatGridIndex(abs(s_lat - lat));
        int at_lon = getLonGridIndex(abs(s_lon - lon));
        //temporaly 
        
        //for lat lon recheck
        if(at_lat>=0&&at_lat<x_gsize&&at_lon>=0&&at_lon<y_gsize)
       { tempdoglocation = new LatLonSize(at_lat , at_lon , groupsize);}

       else
       {
           if(at_lat<0)at_lat=0;
           if(at_lat>=x_gsize)at_lat=x_gsize-1;
           if(at_lon<0)at_lon=0;
           if(at_lon>=y_gsize)at_lon=y_gsize-1;
            tempdoglocation = new LatLonSize(at_lat , at_lon , 0.0f);
       }


        //
        spawnDogPrefabWithHeight(lat , lon);
    }

    public Vector2d temp_latlondelta;
    public void createDogObjectForShow(float lat = 181.0f, float lon = 181.0f){
        if (lat != 181.0f && lon != 181.0f){
            temp_latlondelta = new Vector2d(lat, lon);
        } else {
            temp_latlondelta = getLatLonFromMousePosition();
        }
        doglocations.Add(spawnLatLonWithinGrid(temp_latlondelta));
        int lastIndex = doglocations.Count - 1;
        float newlat = (float)doglocations[lastIndex].x;
        float newlon = (float)doglocations[lastIndex].y;
        spawnDogPrefabWithHeight(newlat , newlon);
    }

    public LatLonSize getNewDog(){
        return tempdoglocation;
    }

    public LatLonSize getNewInfect(){
        return tempinfect;
    }
    /// create the object to the map location (with calculated map height)
    /// (reference: https://github.com/mapbox/mapbox-unity-sdk/issues/222)
    public void spawnDogPrefabWithHeight(double lat , double lon)
    {
        UnityTile tile = getTileAt(lat , lon);
        float h = getHeightAt((float)lat,(float)lon);

        Vector3 location = Conversions.GeoToWorldPosition(lat , lon , _map.CenterMercator , _map.WorldRelativeScale).ToVector3xz();
        location = new Vector3(location.x , h * tile.TileScale, location.z);
        
        var obj = Instantiate(dogpanel);

        //Try to make every dog spawn at higher level
        //obj.transform.position = location;
        obj.transform.position = new Vector3(location.x, location.y + 10.0f, location.z);

        obj.transform.localScale = new Vector3(_spawnScale , _spawnScale , _spawnScale);
        obj.transform.parent = DogLayer.transform; //let the dog becomes the child of DogLayer game object

        
        dogObjs.Add(obj);
        dogradius.Add(0.0f);
        factradius.Add(0);
        obj.active=false; //for speed graphic
    }

    public void clearDogObjectMemory(){
        int removeIndex = dogObjs.Count - 1;
        doglocations.RemoveAt(removeIndex);
        dogObjs.RemoveAt(removeIndex);
        dogradius.RemoveAt(removeIndex);
        factradius.RemoveAt(removeIndex);
        Destroy(DogLayer.GetComponent<Transform>().GetChild(removeIndex).gameObject);
    }

    public void clearDogObjectMemoryAt(int index){
        doglocations.RemoveAt(index);
        dogObjs.RemoveAt(index);
        Debug.Log(dogObjs.Count);
        dogradius.RemoveAt(index);
        factradius.RemoveAt(index);
        Destroy(DogLayer.GetComponent<Transform>().GetChild(index).gameObject);
    }

    public void clearInfectObjectMemory(){
        int removeIndex = infectObjs.Count - 1;
        infectedlocations.RemoveAt(removeIndex);
        infectObjs.RemoveAt(removeIndex);
        Destroy(InfectLayer.GetComponent<Transform>().GetChild(removeIndex).gameObject);
    }

    //Add map point reference to the world

     public void addInfectedLocation(float lat , float lon, int groupsize)
    {
        infectedlocations.Add(new Vector2d(lat,lon));
        createinfectDogObject(groupsize);
    }
    public void addInfectedLocation(Vector2d latlonvector, int groupsize)
    {
        infectedlocations.Add(spawnLatLonWithinGrid(latlonvector));
        createinfectDogObject(groupsize);
    }
    public void createinfectDogObject(int groupsize)
    {
        //In Latitude, the map is drawn in vector of (+Lon, -Lat) direction
        int lastIndex = infectedlocations.Count - 1;
        float lat = (float)infectedlocations[lastIndex].x;
        float lon = (float)infectedlocations[lastIndex].y;
        int at_lat = getLatGridIndex(abs(s_lat - lat));
        int at_lon = getLonGridIndex(abs(s_lon - lon));
        //temporaly 
        tempinfect= new LatLonSize(at_lat , at_lon , groupsize);
        Debug.Log("latx"+at_lat+"lony"+at_lon);

        //
        spawninfectedPrefab(lat , lon);
    }
 
    public void createInfectDogObjectForShow(float lat = 181.0f, float lon = 181.0f){
        if (lat != 181.0f && lon != 181.0f){
            temp_latlondelta = new Vector2d(lat, lon);
        } else {
            temp_latlondelta = getLatLonFromMousePosition();
        }
        infectedlocations.Add(spawnLatLonWithinGrid(temp_latlondelta));
        int lastIndex = infectedlocations.Count - 1;
        float newlat = (float)infectedlocations[lastIndex].x;
        float newlon = (float)infectedlocations[lastIndex].y;

        spawninfectedPrefab(newlat , newlon);
    }
    public void spawninfectedPrefab(double lat , double lon)
    {
         UnityTile tile = getTileAt(lat , lon);
        float h = getHeightAt((float)lat,(float)lon);

        Vector3 location = Conversions.GeoToWorldPosition(lat , lon , _map.CenterMercator , _map.WorldRelativeScale).ToVector3xz();
        location = new Vector3(location.x , (h * tile.TileScale), location.z);

        var obj = Instantiate(infectedpanel);
        obj.transform.position = location;
        obj.transform.localScale = new Vector3(_spawnScale , _spawnScale , _spawnScale);
        obj.transform.parent = InfectLayer.transform; //let the infect becomes the child of InfectLayer game object
        
        infectObjs.Add(obj);
       
     
    }

    public void spawnMapPointer(double lat, double lon)
    {
        mappointlocations.Add(new Vector2d(lat , lon));
        Vector3 location = Conversions.GeoToWorldPosition(lat , lon , _map.CenterMercator , _map.WorldRelativeScale).ToVector3xz();
        location = new Vector3(location.x , 0.0f , location.z);
        var obj = Instantiate(mappanel);
        obj.transform.position = location;
        obj.transform.localScale = new Vector3(_spawnScale , _spawnScale , _spawnScale);
        
        mappointObjs.Add(obj);
    }

    public void spawnAttractSource(double lat, double lon)
    {
        attractlocations.Add(new Vector2d(lat, lon));
        Vector3 location = Conversions.GeoToWorldPosition(lat, lon, _map.CenterMercator, _map.WorldRelativeScale).ToVector3xz();
        location = new Vector3(location.x, 0.0f, location.z);
        var obj = Instantiate(attractpanel);
        obj.transform.position = location;
        obj.transform.localScale = new Vector3(_spawnScale, _spawnScale, _spawnScale);

        attractObjs.Add(obj);

        int lastIndex = attractlocations.Count - 1;
        int at_lat = y_gsize - getLatGridIndex(abs(s_lat - (float)attractlocations[lastIndex].x));
        int at_lon = getLonGridIndex(abs(s_lon - (float)attractlocations[lastIndex].y));
        tempattracter = new AttractSource(at_lat, at_lon);
    }

    public AttractSource getNewAttracter(){
        return tempattracter;
    }

    //Get the Tile Material from the Mapbox
    public UnityTile getTileAt(double lat, double lon)
    {
        //get tile ID
        var tileIDUnwrapped = TileCover.CoordinateToTileId(new Vector2d(lat , lon) , (int)_map.Zoom);

        //get tile
        return _map.MapVisualizer.GetUnityTileFromUnwrappedTileId(tileIDUnwrapped);
    }

    public Vector2d spawnLatLonWithinGrid(float lat , float lon)
    {
        lat -= lat % (GridSize / rough_sphere_per_degree);
        lon -= lon % (GridSize / rough_sphere_per_degree);
        return new Vector2d(lat,lon);
    }

    public Vector2d spawnLatLonWithinGrid(Vector2d latlonvector)
    {
        latlonvector.x -= latlonvector.x % (GridSize / rough_sphere_per_degree);
        latlonvector.y -= latlonvector.y % (GridSize / rough_sphere_per_degree);
        return latlonvector;
    }

    public Vector2d spawnLatLonWithinGridMapbox(float lat, float lon)
    {
        Vector2d meter_conversion = Conversions.LatLonToMeters(lat,lon);
        meter_conversion.x += meter_conversion.x % GridSize;
        meter_conversion.y += meter_conversion.y % GridSize;
        return Conversions.MetersToLatLon(meter_conversion);
    }

    public Vector2d spawnLatLonWithinGridMapbox(Vector2d latlonvector)
    {
        Vector2d meter_conversion = Conversions.LatLonToMeters(latlonvector);
        meter_conversion.x += meter_conversion.x % GridSize;
        meter_conversion.y += meter_conversion.y % GridSize;
        return Conversions.MetersToLatLon(meter_conversion);
    }

    public float getHeightAt(float lat , float lon)
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

    private float abs(float h)
    {
        if (h < 0.0f)
            return -h;
        else
            return h;
    }

    //Initialize the top-left array index
    public void setStartLatLon(Vector2d latlondelta)
    {
        destroyAllMapPoints();
        destroyAllDogs();
        s_lat = (float)latlondelta.x;
        s_lon = (float)latlondelta.y;
        //spawnMapPointer(s_lat , s_lon);
    }

    //Initialize the bottom-right array index
    public void setEndLatLon(Vector2d latlondelta)
    {
        y_gsize = getLatGridIndex(abs(s_lat - (float)latlondelta.x));
        x_gsize = getLonGridIndex(abs(s_lon - (float)latlondelta.y));
       // spawnMapPointer(latlondelta.x , latlondelta.y);
    }

    //Reset dog values
    public void destroyAllDogs()
    {
        int count = dogObjs.Count;
        for (int i = 0; i < count; i++)
        {
            Destroy(dogObjs[0]);
            dogObjs.RemoveAt(0);
            doglocations.RemoveAt(0);
            dogradius.RemoveAt(0);
            factradius.RemoveAt(0);
        }
    }

    //Reset Map Point
    public void destroyAllMapPoints()
    {
        int count = mappointObjs.Count;
        for (int i = 0; i < count; i++)
        {
            Destroy(mappointObjs[0]);
            mappointObjs.RemoveAt(0);
            mappointlocations.RemoveAt(0);
            Destroy(attractObjs[0]);
            attractObjs.RemoveAt(0);
            attractlocations.RemoveAt(0);
        }
    }

    public int getLatGridIndex(float moved_lat)
    {
        return (int)(moved_lat / addLatByMeters(GridSize));
    }

     public int getLonGridIndex(float moved_lon)
    {
        return (int)(moved_lon / addLonByMeters(GridSize));
    }
}
