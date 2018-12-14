namespace Mapbox.Examples
{
	using UnityEngine;
	using Mapbox.Utils;
	using Mapbox.Unity.Map;
	using Mapbox.Unity.Utilities;
	using System.Collections.Generic;
    using Mapbox.Unity.MeshGeneration.Factories;
    using Mapbox.Geocoding;

	public class SpawnOnMap : MonoBehaviour
	{
        private int onSpawnCount = 0;
        private bool WBI4Triggered = false;

        [SerializeField]
		AbstractMap _map;

		[SerializeField]
		[Geocode]
		string[] _locationStrings;
		Vector2d[] _locations;

		[SerializeField]
		float _spawnScale = 10f;

		[SerializeField]
		GameObject _markerPrefab;

        [SerializeField]
        GameObject gridpanel; //Prefabs for grid layer

        [SerializeField]
        GameObject dogpanel; //Prefabs for dog layer

        [SerializeField]
        List<string> doglocationlist; //list of dogs == 11/1595/9831, 11/1596/9841 default

        List<GameObject> _spawnedObjects;
        List<GameObject> dogObjects;

        private string[] neighbourname = new string[4];
        private float[] neighbourx = new float[4];
        private float[] neighboury = new float[4];

        void Start()
		{
			_locations = new Vector2d[_locationStrings.Length];
			_spawnedObjects = new List<GameObject>();
            dogObjects = new List<GameObject>(); //initialization for the dog object
			for (int i = 0; i < _locationStrings.Length; i++)
			{
				var locationString = _locationStrings[i];
				_locations[i] = Conversions.StringToLatLon(locationString);
				var instance = Instantiate(_markerPrefab);
				instance.transform.localPosition = _map.GeoToWorldPosition(_locations[i], true);
				instance.transform.localScale = new Vector3(_spawnScale, _spawnScale, _spawnScale);
				_spawnedObjects.Add(instance);
			}
            createGrid(0); //create the grid when the map background is created from 0 to the end
        }

		private void Update()
		{
            if (!WBI4Triggered) //since the world based index to zoom panel and lat long need 4 instances
            {
                if (onSpawnCount < transform.childCount) //when 4 instances are created
                {
                    WBI4Triggered = true;
                    createGrid(onSpawnCount); //the grid must also duplicate itself further from the Start
                }
            }
			int count = _spawnedObjects.Count;
			for (int i = 0; i < count; i++)
			{
				var spawnedObject = _spawnedObjects[i];
				var location = _locations[i];
				spawnedObject.transform.localPosition = _map.GeoToWorldPosition(location, true);
				spawnedObject.transform.localScale = new Vector3(_spawnScale, _spawnScale, _spawnScale);
			}
            //GEOLOCATION CONTROL THE CENTER MAP SHOWING
            //Debug.Log(_map.CenterLatitudeLongitude);

            //Finding neighbour world based index from each grid
            for (int j = 0; j < transform.childCount; j++)
            {
                //If the grid already has the dog
                if (transform.GetChild(j).childCount > 1)
                {
                    string parentname = "dog" + transform.GetChild(j).name + "(Clone)";
                    string childname = transform.GetChild(j).GetChild(1).name;
                    if (!parentname.Equals(childname))
                    {
                        Debug.Log("triggered parent mismatch child >> " + parentname + " " + childname);
                        //destroy the child
                        Destroy(transform.GetChild(j).GetChild(1).gameObject);
                    }
                }
                else
                {
                    //Neighbour Grid from up down left right
                    Vector3 thiswbi = transform.GetChild(j).position;
                    neighbourname[0] = findNearestGridAt(thiswbi.x - 100.0f , thiswbi.z);
                    neighbourname[1] = findNearestGridAt(thiswbi.x + 100.0f , thiswbi.z);
                    neighbourname[2] = findNearestGridAt(thiswbi.x , thiswbi.z - 100.0f);
                    neighbourname[3] = findNearestGridAt(thiswbi.x , thiswbi.z + 100.0f);

                    for (int i = 0; i < 4; i++)
                    {
                        if (neighbourname[i].Equals("")) //if there is no neighbour, then match itself
                        {
                            SplitAndAddItself(transform.GetChild(i).name , i);
                        }
                        else
                        {
                            SplitAndAddWBI(neighbourname[i] , transform.GetChild(i).name , i); //else match the neighbour grid
                        }
                    }

                    foreach (string dog in doglocationlist)
                    {
                        if (isWithinNeighbour(dog))
                        {
                            createDogAt(j);
                            break;
                        }
                    }
                }
            }
        }

        private void createGrid(int fromGridNumber)
        {
            onSpawnCount = transform.childCount; //how many map tiles exist now
            for (int i = fromGridNumber; i < onSpawnCount; i++) //from input starting grid number to count
            {
                //Initialization for grid panel
                gridpanel.transform.localPosition = transform.GetChild(i).position;
                gridpanel.transform.localPosition += transform.up * 15.0f; //layer height is not yet specified
                gridpanel.transform.localScale = new Vector3(_spawnScale * 10.0f, _spawnScale , _spawnScale * 10.0f);
                Instantiate(gridpanel , gridpanel.transform.position , Quaternion.identity, transform.GetChild(i));
            }
        }

        private void createDogAt(int childindex)
        {
            //Initialize the gameObject on the target
            dogpanel.transform.localPosition = transform.GetChild(childindex).position;
            dogpanel.transform.localPosition += transform.up * 30.0f; //layer height is not yet specified
            dogpanel.transform.localScale = new Vector3(_spawnScale , _spawnScale , _spawnScale);
            dogpanel.name = "dog" + transform.GetChild(childindex).name;
            Instantiate(dogpanel , dogpanel.transform.position , Quaternion.identity , transform.GetChild(childindex));
            dogObjects.Add(dogpanel);
        }

        private string findNearestGridAt(float x, float z)
        {
            Vector3 targetposition = new Vector3(transform.position.x + x , transform.position.y , transform.position.z + z);
            for (int i = 0; i < transform.childCount; i++)
            {
                if (Vector3.Distance(targetposition, transform.GetChild(i).position) < 0.5f)
                {
                    return transform.GetChild(i).name;
                }
            }
            return "";
        }

        private void SplitAndAddWBI(string text, string anothertext, int at)
        {
            string[] WBI = text.Split('/');
            string[] anotherWBI = anothertext.Split('/');
            if (at < 2)
                neighbourx[at] = (float.Parse("0."+WBI[1]) + float.Parse("0."+anotherWBI[1])) / 4.0f;
            else
                neighboury[at] = (float.Parse("0."+WBI[2]) + float.Parse("0."+anotherWBI[2])) / 4.0f;
        }

        private void SplitAndAddItself(string text, int at)
        {
            string[] WBI = text.Split('/');
            if (at < 2)
                neighbourx[at] = float.Parse("0." + WBI[1]);
            else
                neighboury[at] = float.Parse("0." + WBI[2]);
        }

        private bool isWithinNeighbour(string text)
        {
            string[] WBI = text.Split('/');
            float x = float.Parse("0." + WBI[1]);
            float y = float.Parse("0." + WBI[2]);
            //Debug.Log("is " + x + " within " + neighbourx[0] + " - " + neighbourx[1]);
            //Debug.Log("is " + y + " within " + neighboury[2] + " - " + neighboury[3]);
            bool xtruth = false;
            bool ytruth = false;
            if (x >= neighbourx[0])
            {
                if (x <= neighbourx[1])
                {
                    xtruth = true;
                    //Debug.Log(x + " in " + neighbourx[0] + " and " + neighbourx[1] + " is truth");
                }
            }
            if (x <= neighbourx[0])
            {
                if (x >= neighbourx[1])
                {
                    xtruth = true;
                }
            }
            if (y <= neighboury[2])
            {
                if (y >= neighboury[3])
                {
                    ytruth = true;
                }
            }
            if (y >= neighboury[2])
            {
                if (y <= neighboury[3])
                {
                    ytruth = true;
                }
            }
            if (xtruth && ytruth)
            {
                Debug.Log("finally");
                return true;
            }
            else
            {
                return false;
            }
        }
	}
}