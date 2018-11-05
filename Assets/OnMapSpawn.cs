using UnityEngine;
using Mapbox.Utils;
using Mapbox.Unity.Map;
using Mapbox.Unity.Utilities;
using System.Collections.Generic;
using Mapbox.Unity.MeshGeneration.Factories;
using Mapbox.Geocoding;

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
    float radius_earth = 6378.1f; // Radius of the earth in km

    List<float> doglat, doglon;
    List<GameObject> dogObjs;

    [SerializeField]
    public Camera _referenceCamera;

    void Start()
    {
        int initSize = doglocationStrings.Count;
        doglocations = new List<Vector2d>(); //initialization for the dog object
        doglat = new List<float>(); //initialization for lat, long
        doglon = new List<float>();
        dogObjs = new List<GameObject>();
        for (int i = 0; i < initSize; i++)
        {
            addDogLocation(doglocationStrings[i]);
        }
        Debug.Log(getDistanceFromLatLonInKm(doglat[0] , doglon[0] , doglat[1] , doglon[1]) + " km");
        float newlon = doglon[0] + addLonByMeters(5000.0f); // move upward by 5 km
        float newlat = doglat[0] + addLatByMeters(5000.0f); // move right by 5 km
        Debug.Log("move up from " + doglon[0] + " to " + newlon);
        Debug.Log("move right from " + doglat[0] + " to " + newlat);
        addDogLocation(newlat , newlon);
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
            var mousePosScreen = Input.mousePosition;
            //assign distance of camera to ground plane to z, otherwise ScreenToWorldPoint() will always return the position of the camera
            //(reference: http://answers.unity3d.com/answers/599100/view.html)
            mousePosScreen.z = _referenceCamera.transform.localPosition.y;
            var pos = _referenceCamera.ScreenToWorldPoint(mousePosScreen);

            var latlongDelta = _map.WorldToGeoPosition(pos);
            addDogLocation(latlongDelta); //add new dog object from clicked position
        }
    }

    //Harversine Formula, 
    //(reference: https://stackoverflow.com/questions/639695/how-to-convert-latitude-or-longitude-to-meters)
    private float getDistanceFromLatLonInKm(float lat1, float lon1, float lat2, float lon2)
    {
        float dLat = deg2rad(lat2 - lat1);  // deg2rad below
        float dLon = deg2rad(lon2 - lon1);
        float a = 
          Mathf.Sin(dLat / 2.0f) * Mathf.Sin(dLat / 2.0f) +
          Mathf.Cos(deg2rad(lat1)) * Mathf.Cos(deg2rad(lat2)) *
          Mathf.Sin(dLon / 2.0f) * Mathf.Sin(dLon / 2.0f)
          ;
        float c = 2.0f * Mathf.Atan2(Mathf.Sqrt(a) , Mathf.Sqrt(1.0f - a));
        float d = radius_earth * c; // Distance = Radius.km x coefficient
        return d;
    }

    private float deg2rad(float deg)
    {
        return deg * (Mathf.PI / 180.0f);
    }

    //Directions of this function indicate by +/- of meter value
    private float addLatByMeters(float meter) //return the increased or decreased Lat by meter
    {
        return meter / 111111.0f ; // because sin(90 degree) is 1
    }

    //Directions of this function indicate by +/- of meter value
    private float addLonByMeters(float meter) //return the increased or decreased Lon by meter
    {
        return meter / 111111.0f; //because cos(0 degree) is 1
    }

    //Original [lat, long] add meter conversion
    private float addLatByMeters(float meter, float theta) 
    {
        return (meter / 111111.0f) * Mathf.Sin(theta);
    }

    private float addLonByMeters(float meter, float theta)
    {
        return (meter / 111111.0f) * Mathf.Cos(theta);
    }

    //Add new dog location by float lat lon
    private void addDogLocation(float lat, float lon)
    {
        doglocations.Add(Conversions.StringToLatLon(lat + ", " + lon));
        createDogObject();
    }

    //Add new doglocation by location string of vector lat lon
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

    //create the object to the map location
    private void createDogObject()
    {
        int lastIndex = doglocations.Count - 1;
        doglat.Add((float)doglocations[lastIndex].x);
        doglon.Add((float)doglocations[lastIndex].y);
        GameObject obj = Instantiate(dogpanel); //use the dog prefab to the map
        obj.transform.localPosition = _map.GeoToWorldPosition(doglocations[lastIndex] , true);
        obj.transform.localScale = new Vector3(_spawnScale , _spawnScale , _spawnScale);
        dogObjs.Add(obj);
    }
}