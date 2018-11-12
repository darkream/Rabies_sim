using UnityEngine;
using Mapbox.Utils;
using Mapbox.Unity.Map;
using Mapbox.Unity.Utilities;
using System.Collections.Generic;
using Mapbox.Map;
using Mapbox.Unity.MeshGeneration.Data;

public class OnMapSpawn : MonoBehaviour
{
    [SerializeField]
    AbstractMap _map;

    [SerializeField]
    [Geocode]
    List<string> doglocationStrings; //list of dogs == 7.03169034704473, 100.478511282507 default
    List<Vector2d> doglocations;

    [SerializeField]
    float _spawnScale = 10f;

    [SerializeField]
    GameObject gridpanel; //Prefabs for grid layer

    [SerializeField]
    GameObject dogpanel; //Prefabs for dog layer

    [SerializeField]
    float radius_earth = 6378.1f; // Radius of the earth in km (Google Answer)

    [SerializeField] //(reference: http://www.longitudestore.com/how-big-is-one-gps-degree.html)
    float equator_latitude_per_degree = 110570.0f; //110 km per degree
    float pole_latitude_per_degree = 111690.0f; //111.69 km per degree
    float widest_longitude_per_degree = 111321.0f; //111.321 km longitude per degree at equator (while 0 at pole)
    float one_degree_per_radian = 0.0174532925f; //PI divided by 180


    //Data List of Arrays
    List<float> doglat, doglon, doggroundheight;
    List<GameObject> dogObjs;

    [SerializeField]
    public Camera _referenceCamera;

    [SerializeField]
    public GameObject DogLayer;

    void Start()
    {
        int initSize = doglocationStrings.Count;
        doglocations = new List<Vector2d>(); //initialization for the dog object
        doglat = new List<float>(); //initialization for lat, long
        doglon = new List<float>();
        doggroundheight = new List<float>();
        dogObjs = new List<GameObject>();
        for (int i = 0; i < initSize; i++)
        {
            addDogLocation(doglocationStrings[i]);
        }
    }

    private void Update()
    {
        zdistribution();

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
        return meter / 111111.0f; // because sin(90 degree) is 1
    }

    //Directions of this function indicate by +/- of meter value
    private float addLonByMeters(float meter) //return the increased or decreased Lon by meter
    {
        return meter / 111111.0f; //because cos(0 degree) is 1
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

    //using Mapbox Conversion return length of meter in lat, lon distance
    private float addLatByMetersMapbox(float meter)
    {
        return (float)Conversions.MetersToLatLon(new Vector2d(meter , 0.0)).x;
    }

    private float addLonByMetersMapbox(float meter)
    {
        return (float)Conversions.MetersToLatLon(new Vector2d(0.0 , meter)).y;
    }

    private Vector2d addLatLonByMetersMapbox(Vector2d latlonvector)
    {
        return Conversions.MetersToLatLon(latlonvector);
    }

    //Add new dog location by float lat lon
    private void addDogLocation(float lat , float lon)
    {
        doglocations.Add(Conversions.StringToLatLon(lat + ", " + lon));
        createDogObject();
    }

    //Add new doglocation by location string of vector lat lon, Formula: "lat, long"
    private void addDogLocation(string locationstring)
    {
        doglocations.Add(Conversions.StringToLatLon(locationstring));
        createDogObject();
    }

    //Add new doglocation by location vector lat lon
    private void addDogLocation(Vector2d latlonvector)
    {
        doglocations.Add(latlonvector);
        createDogObject();
    }

    //create the object to the map location (with default height)
    private void createDogObject()
    {
        int lastIndex = doglocations.Count - 1;
        doglat.Add((float)doglocations[lastIndex].x);
        doglon.Add((float)doglocations[lastIndex].y);
        spawnDogPrefabWithHeight(doglat[lastIndex] , doglon[lastIndex]);
    }

    //create the object to the map location (with calculated map height)
    //(reference: https://github.com/mapbox/mapbox-unity-sdk/issues/222)
    void spawnDogPrefabWithHeight(double lat , double lon)
    {
        //get tile ID
        var tileIDUnwrapped = TileCover.CoordinateToTileId(new Mapbox.Utils.Vector2d(lat , lon) , (int)_map.Zoom);

        //get tile
        UnityTile tile = _map.MapVisualizer.GetUnityTileFromUnwrappedTileId(tileIDUnwrapped);

        //lat lon to meters because the tiles rect is also in meters
        Vector2d v2d = Conversions.LatLonToMeters(new Mapbox.Utils.Vector2d(lat , lon));
        //get the origin of the tile in meters
        Vector2d v2dcenter = tile.Rect.Center - new Mapbox.Utils.Vector2d(tile.Rect.Size.x / 2.0 , tile.Rect.Size.y / 2.0);
        //offset between the tile origin and the lat lon point
        Vector2d diff = v2d - v2dcenter;

        //maping the diffetences to (0-1)
        float Dx = (float)(diff.x / tile.Rect.Size.x);
        float Dy = (float)(diff.y / tile.Rect.Size.y);

        //height in unity units
        var h = tile.QueryHeightData(Dx , Dy);

        //height in meter
        float height_in_meter = h / tile.TileScale; //*This is important, check out the function in UnityTile.cs
        doggroundheight.Add(height_in_meter); //stored ground height to the array of that specific dog
        Debug.Log("World Height: " + height_in_meter + " meter(s)");

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

    private void zdistribution()
    {
        //dog distribute zone
        //distribute tester
        //update time
        if (Input.GetKeyDown("z"))
        {
            // distribute dog top side on every existed dog 
            int tempcount = dogObjs.Count; //prevent unlimited loophole
            for (int j = 0; j < tempcount; j++)
            {
                // distribute dog top side on every existed dog 
                float templon = doglon[j];
                float templat = doglat[j] + addLatByMeters(1000.0f);
                //check that new lat lon is same as old one
                Distribute_add(templat , templon , tempcount);
                // distribute dog down side on every existed dog 
                templon = doglon[j];
                templat = doglat[j] + addLatByMeters(-1000.0f);
                //check that new lat lon is same as old one
                Distribute_add(templat , templon , tempcount);
                // distribute dog rigth side on every existed dog 
                templon = doglon[j] + addLonByMeters(1000.0f);
                templat = doglat[j];
                //check that new lat lon is same as old one
                Distribute_add(templat , templon , tempcount);
                // distribute dog left side on every existed dog 
                templon = doglon[j] + addLonByMeters(-1000.0f);
                templat = doglat[j];
                //check that new lat lon is same as old one
                Distribute_add(templat , templon , tempcount);
            }
        }
    }

    //dog distribute zone
    private void Distribute_add(float templat , float templon , int tempcount)
    {
        bool check_exist = false;
        for (int k = 0; k < tempcount; k++)
        {
            if (templon == doglon[k] && templat == doglat[k]) //if distribute pos got object
            {
                // add dog number that not exist in script yet
                Debug.Log("Dog distribute to other dog grid,add up");
                check_exist = true;
            }
        }
        if (!check_exist) //if is new pos
        {
            addDogLocation(templat , templon);
        }
    }
}