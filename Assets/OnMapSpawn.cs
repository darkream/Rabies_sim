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

    List<float> doglat, doglong;
    List<GameObject> dogObjs;

    void Start()
    {
        int initSize = doglocationStrings.Count;
        doglocations = new List<Vector2d>(); //initialization for the dog object
        doglat = new List<float>(); //initialization for lat, long
        doglong = new List<float>();
        dogObjs = new List<GameObject>();
        for (int i = 0; i < initSize; i++)
        {
            var locationString = doglocationStrings[i];
            doglocations.Add(Conversions.StringToLatLon(locationString));
            doglat.Add((float)doglocations[i].x);
            doglong.Add((float)doglocations[i].y);
            var instance = Instantiate(dogpanel); //use the dog prefab to the map
            instance.transform.localPosition = _map.GeoToWorldPosition(doglocations[i] , true);
            instance.transform.localScale = new Vector3(_spawnScale , _spawnScale , _spawnScale);
            dogObjs.Add(instance);
        }
        //Debug.Log(getDistanceFromLatLonInKm(doglat[0] , doglong[0] , doglat[1] , doglat[1]));
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
    }

    //Harversine Formula
    private float getDistanceFromLatLonInKm(float lat1, float lon1, float lat2, float lon2)
    {
        float R = 6371.0f; // Radius of the earth in km
        float dLat = deg2rad(lat2 - lat1);  // deg2rad below
        float dLon = deg2rad(lon2 - lon1);
        float a = 
          Mathf.Sin(dLat / 2.0f) * Mathf.Sin(dLat / 2.0f) +
          Mathf.Cos(deg2rad(lat1)) * Mathf.Cos(deg2rad(lat2)) *
          Mathf.Sin(dLon / 2.0f) * Mathf.Sin(dLon / 2.0f)
          ;
        float c = 2 * Mathf.Atan2(Mathf.Sqrt(a) , Mathf.Sqrt(1.0f - a));
        float d = R * c; // Distance in km
        return d;
    }

    private float deg2rad(float deg)
    {
        return deg * (Mathf.PI / 180);
    }
}