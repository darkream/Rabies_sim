﻿//add quad+qaudlocation in gloabal var list
// add at create image and color getFileNameTag
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


    private float[] dogstate_proposion;
    public Material Matref;
    public GameObject quadmap;
    Vector2d quadlocation;
    Renderer renA;

    Texture2D quadmaptexture;

//--------------------------------------------------------------------------------------------
    //Rabies test zone


        //SEIRV Destination+amount DATA
      //+++++++++++++++++++++++++++++++++++++++++++++++
    private List<LatLonSize> infectdogdata;
        

    public struct dogamountSEIRV
    {
        
        public float[,,] infectamount; //array is destination
        public float[,] vaccineamount; //array is destination
        public float[,] suspectamount; //array is destination
        public float[,,] exposeamount; //array is destination

        public dogamountSEIRV (int xval,int yval ,int exposedate,int infectdate) : this()
        {
            this.infectamount = new float[xval,yval,infectdate];
            this.vaccineamount = new float[xval,yval];
            this.suspectamount = new float[xval,yval];
            this.exposeamount = new float[xval,yval,exposedate];
        }

    }

    private dogamountSEIRV dogeverygroup;
    
    private List<dogamountSEIRV> dogeachgroup;

    //+++++++++++++++++++++++++++++++++++++++++++++++

        //Group Tracking
    //+++++++++++++++++++++++++++++++++++++++++++++++
   // private List<float[,]> grouptracker  ;  //list of group position and amount

    private int[,] homedestination;
    //+++++++++++++++++++++++++++++++++++++++++++++++


    //Factor moving 
    //+++++++++++++++++++++++++++++++++++++++++++++++
    //each factor //xgrid,ygrid, [0Sum,1Cen,2U,3D,4L,5R] value
    private float[,,] inclinefactor;
    private float[,,] fleefactor; 
    private float[,,] chasefactor;
     private float[,,,] homefactor;//x,y,group,value
     private float[,,,] groupfactor;//x,y,group,value
    private float[,,] attractfactor;

    private float[,] finalplusamount ;
    private float[,,] finalplusamount_E ;
    private float[,,] finalplusamount_R ;
    //+++++++++++++++++++++++++++++++++++++++++++++++

 
    private float biterate=0.2f; //100%
    private float infectedrate=0.1f; //100% for test

    int rabiespreadloop=0;
    int day_count=0;
    int day_max=20;
    int e_to_i_date = 10 ; //10 days


    int i_to_r_date = 3 ; //3 days

    int rabiedetectrange = 5;
    int rabiechaserange = 6;

    int rabies_roam_grid_radius=500;
    bool step_factor=true,step_apply=false,step_create=false;

//--------------------------------------------------------------------------------------------
    Texture2D[] runtexture = new Texture2D[100];

    bool runanimate=false;
    //end test zone

    void Start()
    {
        _mbfunction.initializeLocations();
        dogdata = new List<LatLonSize>();
        attracter = new List<AttractSource>();
        doggroupsize = new List<float>();
        infectdogdata = new List<LatLonSize> ();
        convergeCountdown = loopCriteria;
        singleMoveRate = 1.0f - (hordeMoveRate + exploreMoveRate);
        initialTimeScaleFactor();
        uicontroller.initialValue();
        uicontroller.setTotalProcess(10);
        //clustering.pixelReaderAndFileWritten("Assets/mapcolor.txt");
        //clustering.createStringColorListFromReadFile("Assets/mapcolor.txt");
        //clustering.kMeanClustering();
        //clustering.readAndAssignMapCoordinates("Assets/doc.kml");
        //clustering.readFileToFindMinMax("Assets/mapcolor.txt");
        //add
     
        //0:S ,1:E,2:I,3:R,4:V
        //end add
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

        //infected obj/
        for (int i = 0; i < _mbfunction.infectObjs.Count ; i++) //for each infected dog
        {
            var infectObject = _mbfunction.infectObjs[i];
            var location = _mbfunction.infectedlocations[i];
            infectObject.transform.localPosition = _mbfunction._map.GeoToWorldPosition(location , true);
            infectObject.transform.localScale = new Vector3(_mbfunction._spawnScale , _mbfunction._spawnScale , _mbfunction._spawnScale);
        }



//add for quad
       // for (int i = 0; i < 1 ; i++) //for quad
       // {
       //    quadmap.transform.localPosition = _mbfunction._map.GeoToWorldPosition(quadlocation , true);
            //quadObject.transform.localScale = new Vector3(_mbfunction._spawnScale , _mbfunction._spawnScale , _mbfunction._spawnScale);
       // }


        //Press Q to after z to create onsite heatmap
        if (Input.GetKeyDown("q"))
        {
            
             quadmap= GameObject.CreatePrimitive(PrimitiveType.Quad);
		    // quadmap.transform.position = new Vector3(0, 90, 0);
		     quadmap.transform.localScale = new Vector3(68.43009f, 36.5747f, 1.310921f);
		     quadmap.transform.eulerAngles = new Vector3(90.0f,0.0f,0.0f);
            Material quadMaterial = (Material)Resources.Load( "Mapmat" );
		   renA=quadmap.GetComponent<Renderer>();
		   renA.material = quadMaterial;
           Texture2D myTexture = Resources.Load( "test/selectedMapAndDog0" ) as Texture2D;
            Vector2d latlondelta = _mbfunction.getLatLonFromXY(Screen.width/2, Screen.height/2);
            Vector3 location = Conversions.GeoToWorldPosition(latlondelta.x , latlondelta.y , _mbfunction._map.CenterMercator , _mbfunction._map.WorldRelativeScale).ToVector3xz();
            quadmap.transform.position = new Vector3(location.x ,172.0f , location.z);//fixed map
            quadlocation = new Vector2d(location.x,location.z);
            renA.material.mainTexture = myTexture;

           
        }
//end

//rotate running animate
         if (Input.GetKeyDown("r"))
        {
             //add generate run animate
            string tempstr;
            for(int i = 0;i<100;i++)
            {
                tempstr = "test/run" + i  ;
                runtexture[i]=Resources.Load( tempstr ) as Texture2D;
            }

            runanimate = true;
        }

         if (runanimate)
         {
            float framepersec=10.0f;
            int animateindex = (int)((Time.time * framepersec) % runtexture.Length);
            renA.material.mainTexture = runtexture[animateindex];
         }
//end
       



//add for infected
         //Press E to add dog to the map
        if (Input.GetKeyDown("e"))
        {
            Vector2d latlonDelta = _mbfunction.getLatLonFromMousePosition();
           // doggroupsize.Add(1); //size is static at 625
            _mbfunction.addInfectedLocation(latlonDelta, 1); //add new dog object from clicked position
            infectdogdata.Add(_mbfunction.getNewInfect());
        }
//end


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
               // createImage(0 , 3);
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
                  //  createImage(0 , 5); //create only walking habits
                 //   createImage(0 , 6); //create walking habits and dog group
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
                       // createImage(0 , 5);
                      //  createImage(0 , 6);
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
               // createImage(0, 7);
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
                   // uicontroller.setupActivation(false);
                   // startPreDataRegister = false;
                    Debug.Log("This area has innate: " + (innate_total / (float)timeScaleFactor.Length) + ", outage: " + (outage_total / (float)timeScaleFactor.Length));
                   uicontroller.triggerCompleteProcess();
                }
              
            }
            //test rabies run
             else if (uicontroller.getCompletedProcess() == 10){
                 uicontroller.updateProcessDetail("Rabies is running");
                 if (rabiespreadloop ==0 && step_factor==true)
                {
                 rabiesEnvironmentSet();
                 InclineFactorCalculated();
                }
                 if (rabiespreadloop <20)
                {
                    if(step_factor)
                    {
                    //FleeFactorCalculated();
                    ChaseFactorCalculated();
                    HomeFactorCalculated();
                    
                    step_apply=true;
                    step_factor=false;
                    uicontroller.updateProcessDetail("Factor loop :"+ rabiespreadloop);
                    }
                   if(step_apply)
                   {
                    //Factor_summarize();
                    //Factor_apply();
                    Stateupdater();
                    rabies_bite_and_spread();
                    step_create=true;
                    step_apply=false;
                    uicontroller.updateProcessDetail("Apply Factor calculation:"+ rabiespreadloop);
                   }
                    if(step_create)
                    {
                        createImage(rabiespreadloop,100);
                        dogeverygroup_updater();
                        rabiespreadloop+=1;
                        step_factor=true;
                        step_create=false;
                    uicontroller.updateProcessDetail("Pic creation :"+ rabiespreadloop);
                    }
                }
                else
                {
                Debug.Log("COMPLETE!");
                uicontroller.triggerCompleteProcess();
                 uicontroller.setupActivation(false);
                startPreDataRegister = false;
                }   
             }
        }

        if (Input.GetKeyDown("b")){
            readDogPopulationPoint("Assets/dogpop_point.csv", 3, 2, 1);
        }
    }

//add for rabies testing


//add for check expose sum
private void dogeverygroup_updater()
{
    float sumsus=0.0f;
    float[] sumex = new float[e_to_i_date];
    float[] suminf = new float[i_to_r_date];

                    for(int d=0;d<e_to_i_date;d++)
                        {
                            sumex[d]=0;
                        }
                         for(int dd=0;dd<i_to_r_date;dd++)
                        {
                            suminf[dd]=0;
                        }

       for (int m = 0; m < xgridsize; m++)
            {
                for (int n = 0; n < ygridsize; n++)
                {
                    for (int g = 0; g < dogeachgroup.Count; g++)
                    {
                        sumsus+=dogeachgroup[g].suspectamount[m,n];
                        for(int d=0;d<e_to_i_date;d++)
                        {
                            sumex[d]+=dogeachgroup[g].exposeamount[m,n,d];
                        }
                         for(int dd=0;dd<i_to_r_date;dd++)
                        {
                            suminf[dd]+=dogeachgroup[g].infectamount[m,n,dd];
                        }
                    }
                    dogeverygroup.suspectamount[m,n]=sumsus;
                    sumsus=0.0f;
                    for(int a=0;a<e_to_i_date;a++)
                        {
                            dogeverygroup.exposeamount[m,n,a]=sumex[a];
                            sumex[a]=0.0f;
                        }
                    for(int aa=0;aa<i_to_r_date;aa++)
                        {
                            dogeverygroup.infectamount[m,n,aa]=suminf[aa];
                            suminf[aa]=0.0f;
                        }
                }
            }
}
private float groupgridcounter(int groupnumber)
{
   float gridcount=0;
    
            for (int m = 0; m < xgridsize; m++)
            {
                for (int n = 0; n < ygridsize; n++)
                {
                    if(founddogwithgroup(m,n,groupnumber))gridcount++;
                }
            }
    return gridcount;
}

private int foundgroupnum(int lon,int lat)
{
    int groupnum=-1;
    
            for (int grabies = 0; grabies < dogeachgroup.Count; grabies++)
            {
              if(dogeachgroup[grabies].suspectamount[lon,lat]>0)
              {
                groupnum=grabies;
                break;
              }
            }
    return groupnum;
}

private bool foundinfect(int lon,int lat)
{

    if(lon<0)return false;
    if(lat<0)return false;
    if(lon>=xgridsize)return false;
    if(lat>=ygridsize)return false;
    for(int d=0; d< i_to_r_date;d++)
            {
            
                         if( dogeverygroup.infectamount[lon,lat,d]>0.0f)
                         {
                            return true;
                         }
            }
            return false;
        
}

private bool foundexposed(int lon,int lat)
{

    if(lon<0)return false;
    if(lat<0)return false;
    if(lon>=xgridsize)return false;
    if(lat>=ygridsize)return false;
    for(int d=0; d< e_to_i_date;d++)
            {
            
                         if( dogeverygroup.exposeamount[lon,lat,d]>0.0f)
                         {
                            return true;
                         }
            }
            return false;
        
}

private bool founddog(int lon,int lat)
{

    if(lon<0)return false;
    if(lat<0)return false;
    if(lon>xgridsize)return false;
    if(lat>ygridsize)return false;
   for(int d=0; d< i_to_r_date;d++)
            {
            
                         if( dogeverygroup.infectamount[lon,lat,d]>0.0f)
                         {
                            return true;
                         }
            }

      for(int d1=0; d1< e_to_i_date;d1++)
            {
            
                         if( dogeverygroup.exposeamount[lon,lat,d1]>0.0f)
                         {
                            return true;
                         }
            }
        if(dogeverygroup.suspectamount[lon,lat]>0.0f) return true;
         return false;      
}

private bool founddogwithgroup(int lon,int lat,int groupnum)
{

    if(lon<0)return false;
    if(lat<0)return false;
    if(lon>xgridsize)return false;
    if(lat>ygridsize)return false;
    if(dogeachgroup[groupnum].suspectamount[lon,lat]>0.0f) return true;
         return false;      
}

private float sumatpoint(int x,int y)
{
    float sum=0.0f;
                    sum+=dogeverygroup.suspectamount[x,y];
                    for(int d=0; d< e_to_i_date;d++)
                    {
                    sum+=dogeverygroup.exposeamount[x,y,d];
                    }
                    for(int d2=0; d2< i_to_r_date;d2++)
                    {
                    sum+=dogeverygroup.infectamount[x,y,d2];
                    }    
     return sum;
}

private float sumatpoint_group(int x,int y,int groupnum)
{
    float sum=0.0f;
                    sum+=dogeachgroup[groupnum].suspectamount[x,y];
                    for(int d=0; d< e_to_i_date;d++)
                    {
                    sum+=dogeachgroup[groupnum].exposeamount[x,y,d];
                    }
                    for(int d2=0; d2< i_to_r_date;d2++)
                    {
                    sum+=dogeachgroup[groupnum].infectamount[x,y,d2];
                    }    
     return sum;
}

private float sumeverypoint()
{
    float sum=0.0f;
   
            for (int m = 0; m < xgridsize; m++)
            {
                for (int n = 0; n < ygridsize; n++)
                {
                    sum+=dogeverygroup.suspectamount[m,n];
                    for(int d=0; d< e_to_i_date;d++)
                    {
                    sum+=dogeverygroup.exposeamount[m,n,d];
                    }
                    for(int d2=0; d2< i_to_r_date;d2++)
                    {
                    sum+=dogeverygroup.infectamount[m,n,d2];
                    }
                }
            }
     return sum;
}
 private float exposesum()
 {
     float sum=0.0f;
    for(int d=0; d< e_to_i_date;d++)
        {
            for (int m = 0; m < xgridsize; m++)
            {
                for (int n = 0; n < ygridsize; n++)
                {
                    sum+=dogeverygroup.exposeamount[m,n,d];
                }
            }
        }
       
     return sum;
 }
private float infectsum()
 {
     float sum=0.0f;
    for(int d=0; d< i_to_r_date;d++)
        {
            for (int m = 0; m < xgridsize; m++)
            {
                for (int n = 0; n < ygridsize; n++)
                {
                    sum+=dogeverygroup.infectamount[m,n,d];
                }
            }
        }
       
     return sum;
 }
private float exposesumatpoint(int lon,int lat)
 {
     float sum=0.0f;
    for(int d=0; d< e_to_i_date;d++)
        {
                    sum+=dogeverygroup.exposeamount[lon,lat,d];
        }  
     return sum;
 }
private float infectsumatpoint(int lon,int lat)
 {
     float sum=0.0f;
    for(int d=0; d< i_to_r_date;d++)
        {
                    sum+=dogeverygroup.infectamount[lon,lat,d];
        }  
     return sum;
 }

 private float exposesumatpoint_group(int lon,int lat,int groupnum)
 {
     float sum=0.0f;
    for(int d=0; d< e_to_i_date;d++)
        {
                    sum+=dogeachgroup[groupnum].exposeamount[lon,lat,d];
        }  
     return sum;
 }
private float infectsumatpoint_group(int lon,int lat,int groupnum)
 {
     float sum=0.0f;
    for(int d=0; d< i_to_r_date;d++)
        {
                    sum+=dogeachgroup[groupnum].infectamount[lon,lat,d];
        }  
     return sum;
 }

 private float fleelevelx(int lon,int lat,int leftrightindicator)
 {
     float fleeval=0.0f;
     int temp=0;
     //if infect found at same lon line destination, it choose same left and right ,but not middle
     //for middle, it will calculated separately on fleecenter function 

    //left side calculation
    if(leftrightindicator==1) //right
    {
             for(int x=lon-rabiedetectrange;x<lon;x++)
              {
                   temp = (int)Mathf.Abs((int)Mathf.Abs(lon-x)-rabiedetectrange); 
                     for (int y=lat-temp;y<=lat+temp;y++)
                     {
                             if(foundinfect(x,y))
                             {
                                 fleeval+= (rabiedetectrange-((Mathf.Abs(lon-x)+Mathf.Abs(lat-y))))*infectsumatpoint(x,y);//(detectrange-infectandpointrange) *infectnum*80%//morenear morefleeval
                             }
                     }
              }
        return fleeval;
    }
    //right side calculation
      if(leftrightindicator==0) //left
    {
        for(int x=lon+1;x<=lon+rabiedetectrange;x++)
              {
                   temp = (int)Mathf.Abs((int)Mathf.Abs(lon-x)-rabiedetectrange); 
                     for (int y=lat-temp;y<=lat+temp;y++)
                     {
                             if(foundinfect(x,y))
                             {
                                 fleeval+= (rabiedetectrange-((Mathf.Abs(lon-x)+Mathf.Abs(lat-y))))*infectsumatpoint(x,y);//(detectrange-infectandpointrange) *infectnum*80%//morenear morefleeval
                             }
                     }
              }
        return fleeval;
    }

    return fleeval;

       
 }

 private float fleelevely(int lon,int lat,int updownindicator)
 {
      float fleeval=0.0f;
     int temp=0;
     //for middle, it will calculated separately on fleecenter function 

    //left side calculation
    if(updownindicator==1) //up
    {
             for(int y=lat-rabiedetectrange;y<lat;y++)
              {
                   temp = (int)Mathf.Abs((int)Mathf.Abs(lat-y)-rabiedetectrange); 
                     for (int x=lon-temp;x<=lon+temp;x++)
                     {            
                             if(foundinfect(x,y))
                             {
                                 fleeval+= (rabiedetectrange-((Mathf.Abs(lon-x)+Mathf.Abs(lat-y))))*infectsumatpoint(x,y);//(detectrange-infectandpointrange) *infectnum*80%//morenear morefleeval
                             }                     
                     }
              }
        return fleeval;
    }
    //right side calculation
      if(updownindicator==0) //down
    {
        for(int y=lat+1;y<=lat+rabiedetectrange;y++)
              {
                   temp = (int)Mathf.Abs((int)Mathf.Abs(lat-y)-rabiedetectrange); 
                     for (int x=lon-temp;x<=lon+temp;x++)
                     {
                             if(foundinfect(x,y))
                             {
                                 fleeval+= (rabiedetectrange-((Mathf.Abs(lon-x)+Mathf.Abs(lat-y))))*infectsumatpoint(x,y);//(detectrange-infectandpointrange) *infectnum*80%//morenear morefleeval
                             }                       
                     }
              }
        return fleeval;
    }

    return fleeval;

 }

private float fleecenterx(int lon,int lat)
{
    float fleeval=0.0f;

     for (int y=lat-rabiedetectrange;y<=lat+rabiedetectrange;y++)
                     {
                             if(foundinfect(lon,y))
                             {
                                 fleeval+= (rabiedetectrange-(Mathf.Abs(lat-y)))*infectsumatpoint(lon,y);//(detectrange-infectandpointrange) *infectnum*80%//morenear morefleeval
                             }
                     }

    return fleeval;
}

private float fleecentery(int lon,int lat)
{
    float fleeval=0.0f;

     for (int x=lon-rabiedetectrange;x<=lon+rabiedetectrange;x++)
                     {            
                             if(foundinfect(x,lat))
                             {
                                 fleeval+= (rabiedetectrange-(Mathf.Abs(lon-x)))*infectsumatpoint(x,lat);//(detectrange-infectandpointrange) *infectnum*80%//morenear morefleeval
                             }                     
                     }
    return fleeval;
}

private int Nearesthome_range(int x,int y,int groupnum)
{
    int maxloop=0,range=0,lowestrange=0;
    if(xgridsize>=ygridsize)maxloop=xgridsize;
    else maxloop=ygridsize;
    for(int k=0;k<maxloop;k++)
    {
         for(int i=-k;i<=k;i++)
        {
            for(int j=-k;j<k;j++)
            {
                if(i==k || i==-k || j==-k || j==k)
                {
                        if(homedestination[x+i,y+j]==groupnum);
                          {
                              range=(int)Mathf.Abs(i)+(int)Mathf.Abs(j);
                              if(lowestrange==0) 
                              {
                                  lowestrange=range;
                              }
                              else if (lowestrange>range)
                              {
                                  lowestrange=range;
                              }
                         }
                }
                
            }
        }
        if(lowestrange>0)return lowestrange;
    }

return -1;
}

//Rabies setter
//---------------------------------------------------------------------------------------------------------------------------
private void rabiesEnvironmentSet()
{
       dogeverygroup= new dogamountSEIRV(xgridsize,ygridsize,e_to_i_date,i_to_r_date);
         dogeachgroup = new List<dogamountSEIRV>();
        for(int i=0;i<dogdata.Count;i++)
        {
              dogeachgroup.Add( new dogamountSEIRV(xgridsize,ygridsize,e_to_i_date,i_to_r_date));
        }
         homedestination = new int[xgridsize , ygridsize];
         fleefactor = new float[xgridsize , ygridsize,6];
         chasefactor = new float[xgridsize , ygridsize,6];
         inclinefactor = new float[xgridsize , ygridsize,6];
         homefactor = new float [xgridsize , ygridsize,dogdata.Count,6];
         //let's set environment
//--------------------------------------------------------------------------------------------------------------------
        //set suspect and set every other to zero first
        for (int m = 0; m < xgridsize; m++)
            {
                for (int n = 0; n < ygridsize; n++)
                {
                    dogeverygroup.suspectamount[m,n]=doggroup[m,n];
                     for (int d = 0; d < e_to_i_date; d++)
                     {
                         dogeverygroup.exposeamount[m,n,d]=0.0f;
                     }
                     for (int dd = 0; dd < i_to_r_date; dd++)
                     {
                        dogeverygroup.infectamount[m,n,dd]=0.0f;
                     }
                }
            }


        //set infect dog
  
                for (int i = 0; i < infectdogdata.Count; i++)
                {
                 dogeverygroup.infectamount[ infectdogdata[i].lonid ,  infectdogdata[i].latid,0] =1.0f;// infectdogdata[i].size;
                }

        //Set group
        
        for(int x=0;x<xgridsize;x++)
             {
             for(int y=0;y<ygridsize;y++)
                {
                    if(dogeverygroup.suspectamount[x,y]>0.0f){homedestination[x,y]=findNearestGroupNeighbour(x,y);}
                    else{homedestination[x,y]=-1;}
                    dogeachgroup[(findNearestGroupNeighbour(x,y))].suspectamount[x,y]=dogeverygroup.suspectamount[x,y];
                    if(foundinfect(x,y))
                    {
                         dogeachgroup[(findNearestGroupNeighbour(x,y))].infectamount[x,y,0]+=dogeverygroup.infectamount[x,y,0];
                    }
                }
             }
            // Debug.Log("I do only once");

}
//----------------------------------------------------------------------------------------------------------------------------




//Begin Factor Calculation Zone
//----------------------------------------------------------------------------------------------------------------------------
private void InclineFactorCalculated ()
{
       float left_incdis=0.0f,right_incdis=0.0f,up_incdis=0.0f,down_incdis=0.0f;
           
            for (int m = 0; m < xgridsize; m++)
            {
                for (int n = 0; n < ygridsize; n++)
                {
                        if(m==0) left_incdis = distributeElevationLevel(heightxy[m , n] , heightxy[m , n ])*0.20f;
                        else left_incdis = distributeElevationLevel(heightxy[m , n] , heightxy[m-1 , n ])*0.20f;
                        if(m==xgridsize-1) right_incdis = distributeElevationLevel(heightxy[m , n] , heightxy[m , n ])*0.20f;
                        else  right_incdis = distributeElevationLevel(heightxy[m , n] , heightxy[m+1 , n ])*0.20f;
                        if(n==0) down_incdis =distributeElevationLevel(heightxy[m , n] , heightxy[m , n ])*0.20f;
                        else down_incdis =distributeElevationLevel(heightxy[m , n] , heightxy[m , n - 1])*0.20f;
                        if(n==ygridsize-1) up_incdis =distributeElevationLevel(heightxy[m , n] , heightxy[m , n ]) *0.20f;
                        else  up_incdis =distributeElevationLevel(heightxy[m , n] , heightxy[m , n + 1]) *0.20f;
                        inclinefactor[m,n,1]=(-1.0f)*(up_incdis+down_incdis+left_incdis+right_incdis);
                        inclinefactor[m,n,2]=up_incdis;
                        inclinefactor[m,n,3]=down_incdis;
                        inclinefactor[m,n,4]=left_incdis;
                        inclinefactor[m,n,5]=right_incdis;    
                }
            }
} 
//----------------------------------------------------------------------------------------------------------------------------
private void FleeFactorCalculated()
{
     float left_incdis=0.0f,right_incdis=0.0f,up_incdis=0.0f,down_incdis=0.0f;
     float sumforfinalize=0.0f;
       for (int m = 0; m < xgridsize; m++)
                {
                    for (int n = 0; n < ygridsize; n++)
                    {
                        //flee value of each side
                        up_incdis =fleelevely(m,n,0);
                        down_incdis =fleelevely(m,n,1);
                        left_incdis = fleelevelx(m,n,0);
                        right_incdis = fleelevelx(m,n,1);
                         //plus center axis flee value to both side in same axis
                        float cenx=0.0f,ceny=0.0f,fight1=0.0f,fight2=0.0f;
                        cenx=fleecenterx(m,n);
                        ceny=fleecentery(m,n);
                        up_incdis += (ceny/2.0f);
                        down_incdis += (ceny/2.0f);
                        left_incdis += (cenx/2.0f);
                        right_incdis +=(cenx/2.0f);

                        //then calculate fight back 20%
                        fight1 =  up_incdis*0.2f;
                        fight2 =  down_incdis*0.2f;
                        up_incdis += (fight2-fight1);
                        up_incdis += (fight1-fight2);
                        fight1 =  left_incdis*0.2f;
                        fight2 =  right_incdis*0.2f;
                        left_incdis += (fight2-fight1);
                        right_incdis += (fight1-fight2);

                        //now we got all flee value , finalize
                        sumforfinalize = up_incdis+down_incdis+left_incdis+right_incdis;
                        up_incdis =  up_incdis/sumforfinalize;
                        down_incdis =  down_incdis/sumforfinalize;
                        left_incdis =  left_incdis/sumforfinalize;
                        right_incdis =  right_incdis/sumforfinalize;
                        fleefactor[m,n,1]=-1.0f;// everydog flee from center //will be back if some side can't expand
                        fleefactor[m,n,2]=up_incdis;
                        fleefactor[m,n,3]=down_incdis;
                        fleefactor[m,n,4]=left_incdis;
                        fleefactor[m,n,5]=right_incdis;
                    }
                }
}

//------------------------------------------------------------------------------------------------------------------------
private void ChaseFactorCalculated()
{
         float left_incdis=0.0f,right_incdis=0.0f,up_incdis=0.0f,down_incdis=0.0f;
         float sumforfinalize=0.0f;
         float chasevalue=0.0f;
             for (int m = 0; m < xgridsize; m++)
                {
                    for (int n = 0; n < ygridsize; n++)
                    {
                        if(foundinfect(m,n))
                        {
                            up_incdis=0.0f;
                            down_incdis=0.0f;
                            left_incdis=0.0f;
                            right_incdis=0.0f;
                            for(int x=m-rabiechaserange;x<=m+rabiechaserange;x++)
                            {
                             for(int y=n-(rabiechaserange-(int)Mathf.Abs(x-m));y<=n+((rabiechaserange-(int)Mathf.Abs(x-m)));y++)
                                 {
                                       if(founddog(x,y))
                                       {
                                           chasevalue = (rabiechaserange-(Mathf.Abs(x-m)+Mathf.Abs(y-n)))*dogeverygroup.suspectamount[x,y]+exposesumatpoint(x,y);
                                           if(x<m) left_incdis+= chasevalue;
                                           if(x>m) right_incdis+= chasevalue;
                                           if(y<n) down_incdis+= chasevalue;
                                           if(y>n) up_incdis+= chasevalue;
                                       }
                                 }
                            }
                        sumforfinalize = up_incdis+down_incdis+left_incdis+right_incdis;
                        up_incdis =  up_incdis/sumforfinalize;
                        down_incdis =  down_incdis/sumforfinalize;
                        left_incdis =  left_incdis/sumforfinalize;
                        right_incdis =  right_incdis/sumforfinalize;
                        chasefactor[m,n,1]=-1.0f;// everydog flee from center //will be back if some side can't expand
                        chasefactor[m,n,2]=up_incdis;
                        chasefactor[m,n,3]=down_incdis;
                        chasefactor[m,n,4]=left_incdis;
                        chasefactor[m,n,5]=right_incdis;
                        }
                    }
                }
     
}
//-----------------------------------------------------------------------------------------------------
private void HomeFactorCalculated()
{
     float left_incdis=0.0f,right_incdis=0.0f,up_incdis=0.0f,down_incdis=0.0f;
     float sumforfinalize=0.0f;

     for (int grabies = 0; grabies < dogeachgroup.Count; grabies++)
            {            
             for (int m = 0; m < xgridsize; m++)
                {
                    for (int n = 0; n < ygridsize; n++)
                    {
                            up_incdis=0.0f;
                            down_incdis=0.0f;
                            left_incdis=0.0f;
                            right_incdis=0.0f;
                         //homefactor[m,n,g]
                         if(homedestination[m,n]!=grabies) //outside home
                         {
                             if(founddogwithgroup(m,n,grabies))
                             {
                                //find nearest route to home
                                int nearestrange=0;
                                int tempgroupcalc=0;
                                  nearestrange=Nearesthome_range(m,n,grabies);
                                  for(int i=m-nearestrange;i<=m+nearestrange;i++) //x axis
                                  {
                                    if(i==m-nearestrange) 
                                    {
                                        if(homedestination[i,n]==grabies) left_incdis += nearestrange;
                                    }
                                    else if(i==m+nearestrange)
                                    {
                                       if(homedestination[i,n]==grabies) right_incdis += nearestrange;
                                    }
                                    else //top and down
                                    {
                                        tempgroupcalc = nearestrange-(int)Mathf.Abs(i-m);
                                         if(homedestination[i,n+(tempgroupcalc)]==grabies)  //top
                                         {
                                             up_incdis += tempgroupcalc;
                                             if(i<m)
                                             {
                                                 left_incdis += m-i;
                                             }
                                             else if(i>m)
                                             {
                                                right_incdis += i-m;
                                             }
                                         }
                                         if(homedestination[i,n-(tempgroupcalc)]==grabies) //down
                                         {
                                            down_incdis += tempgroupcalc;
                                             if(i<m)
                                             {
                                                 left_incdis +=  m-i;
                                             }
                                             else if(i>m)
                                             {
                                                right_incdis += i-m;
                                             }
                                         }
                                    }
                                  }
                             }  
                         
                        //complete calculation
                         sumforfinalize = up_incdis+down_incdis+left_incdis+right_incdis;
                        up_incdis =  up_incdis/sumforfinalize;
                        down_incdis =  down_incdis/sumforfinalize;
                        left_incdis =  left_incdis/sumforfinalize;
                        right_incdis =  right_incdis/sumforfinalize;
                        homefactor[m,n,grabies,1]=-1.0f;// everydog flee from center //will be back if some side can't expand
                        homefactor[m,n,grabies,2]=up_incdis;
                        homefactor[m,n,grabies,3]=down_incdis;
                        homefactor[m,n,grabies,4]=left_incdis;
                        homefactor[m,n,grabies,5]=right_incdis;
                    }
                }
             }
        }
}



//---------------------------------------------------------------------------------------------------------------
private void Factor_summarize()
{
    
            //all weigth equally
            //All factor convalution
            float uptemp=0.0f,downtemp=0.0f,lefttemp=0.0f,righttemp=0.0f;
            float left_incdis=0.0f,right_incdis=0.0f,up_incdis=0.0f,down_incdis=0.0f;
            finalplusamount = new float[xgridsize , ygridsize];
            finalplusamount_E = new float[xgridsize , ygridsize,e_to_i_date];
            finalplusamount_R = new float[xgridsize , ygridsize,i_to_r_date];
            float newdistribution_criteria =0.0001f;
            float exposecriteria=newdistribution_criteria*(exposesum()/sumeverypoint());
            float infectedcriteria=newdistribution_criteria*(infectsum()/sumeverypoint());
            //++++++++++++++++++++++++++++++++++++++++++++

             for (int grabies = 0; grabies < dogeachgroup.Count; grabies++)
                {
                 for (int m = 0; m < xgridsize; m++)
                {
                    for (int n = 0; n < ygridsize; n++)
                    {
                         
                            up_incdis=inclinefactor[m,n,2]*((fleefactor[m,n,2]+homefactor[m,n,grabies,2])/2.0f)*0.25f;
                            down_incdis=inclinefactor[m,n,3]*((fleefactor[m,n,3]+homefactor[m,n,grabies,3])/2.0f)*0.25f;
                            left_incdis=inclinefactor[m,n,4]*((fleefactor[m,n,4]+homefactor[m,n,grabies,4])/2.0f)*0.25f;
                            right_incdis=inclinefactor[m,n,5]*((fleefactor[m,n,5]+homefactor[m,n,grabies,5])/2.0f)*0.25f;
                             //Suspect update
                             //++++++++++++++++++++++++++++++++++++++++++++
                            uptemp = up_incdis*dogeachgroup[grabies].suspectamount[m,n];
                            downtemp =down_incdis*dogeachgroup[grabies].suspectamount[m,n];
                            lefttemp  = left_incdis*dogeachgroup[grabies].suspectamount[m,n];
                            righttemp =right_incdis*dogeachgroup[grabies].suspectamount[m,n];
                            if(uptemp<newdistribution_criteria) uptemp=0.0f;
                            if(downtemp<newdistribution_criteria) downtemp=0.0f;
                            if(lefttemp<newdistribution_criteria) lefttemp=0.0f;
                            if(righttemp<newdistribution_criteria) righttemp=0.0f;

                            finalplusamount[m,n]-=(uptemp+ downtemp+lefttemp+righttemp);
                            if(m==0)finalplusamount[m,n]+=lefttemp;
                            else  finalplusamount[m-1,n]+=lefttemp;
                            if(m==xgridsize-1)finalplusamount[m,n]+=righttemp;
                            else  finalplusamount[m+1,n]+=righttemp;
                            if(n==0)finalplusamount[m,n]+=downtemp;
                            else  finalplusamount[m,n-1]+=downtemp;
                            if(n==ygridsize-1)finalplusamount[m,n]+=uptemp;
                            else  finalplusamount[m,n+1]+=uptemp;
                            //++++++++++++++++++++++++++++++++++++++++++++

                             //exposed update
                             //++++++++++++++++++++++++++++++++++++++++++++
                            for(int d=0;d<e_to_i_date;d++)
                            {
                                uptemp = up_incdis*dogeachgroup[grabies].exposeamount[m,n,d];
                                downtemp =down_incdis*dogeachgroup[grabies].exposeamount[m,n,d];
                                lefttemp  = left_incdis*dogeachgroup[grabies].exposeamount[m,n,d];
                                righttemp =right_incdis*dogeachgroup[grabies].exposeamount[m,n,d];
                            if(uptemp<exposecriteria) uptemp=0.0f;
                            if(downtemp<exposecriteria) downtemp=0.0f;
                            if(lefttemp<exposecriteria) lefttemp=0.0f;
                            if(righttemp<exposecriteria) righttemp=0.0f;

                            finalplusamount_E[m,n,d]-=(uptemp+ downtemp+lefttemp+righttemp);
                            if(m==0)finalplusamount_E[m,n,d]+=lefttemp;
                            else  finalplusamount_E[m-1,n,d]+=lefttemp;
                            if(m==xgridsize-1)finalplusamount_E[m,n,d]+=righttemp;
                            else  finalplusamount_E[m+1,n,d]+=righttemp;
                            if(n==0)finalplusamount_E[m,n,d]+=downtemp;
                            else  finalplusamount_E[m,n-1,d]+=downtemp;
                            if(n==ygridsize-1)finalplusamount_E[m,n,d]+=uptemp;
                            else  finalplusamount_E[m,n+1,d]+=uptemp;
                            }
                            //++++++++++++++++++++++++++++++++++++++++++++
                         
                            //infected update
                             //++++++++++++++++++++++++++++++++++++++++++++
                            up_incdis=inclinefactor[m,n,2]*((chasefactor[m,n,2]+homefactor[m,n,grabies,2])/2.0f)*0.25f;
                            down_incdis=inclinefactor[m,n,3]*((chasefactor[m,n,3]+homefactor[m,n,grabies,3])/2.0f)*0.25f;
                            left_incdis=inclinefactor[m,n,4]*((chasefactor[m,n,4]+homefactor[m,n,grabies,4])/2.0f)*0.25f;
                            right_incdis=inclinefactor[m,n,5]*((chasefactor[m,n,5]+homefactor[m,n,grabies,5])/2.0f)*0.25f;
                             for(int d=0;d<i_to_r_date;d++)
                            {
                                uptemp = up_incdis*dogeachgroup[grabies].infectamount[m,n,d];
                                downtemp =down_incdis*dogeachgroup[grabies].infectamount[m,n,d];
                                lefttemp  = left_incdis*dogeachgroup[grabies].infectamount[m,n,d];
                                righttemp =right_incdis*dogeachgroup[grabies].infectamount[m,n,d];
                            if(uptemp<infectedcriteria) uptemp=0.0f;
                            if(downtemp<infectedcriteria) downtemp=0.0f;
                            if(lefttemp<infectedcriteria) lefttemp=0.0f;
                            if(righttemp<infectedcriteria) righttemp=0.0f;

                            finalplusamount_E[m,n,d]-=(uptemp+ downtemp+lefttemp+righttemp);
                            if(m==0)finalplusamount_E[m,n,d]+=lefttemp;
                            else  finalplusamount_E[m-1,n,d]+=lefttemp;
                            if(m==xgridsize-1)finalplusamount_E[m,n,d]+=righttemp;
                            else  finalplusamount_E[m+1,n,d]+=righttemp;
                            if(n==0)finalplusamount_E[m,n,d]+=downtemp;
                            else  finalplusamount_E[m,n-1,d]+=downtemp;
                            if(n==ygridsize-1)finalplusamount_E[m,n,d]+=uptemp;
                            else  finalplusamount_E[m,n+1,d]+=uptemp;
                            }
                            //++++++++++++++++++++++++++++++++++++++++++++
                    }
                }


                for (int m = 0; m < xgridsize; m++)
                {
                    for (int n = 0; n < ygridsize; n++)
                    {
                        //suspect
                        //--------------
                        dogeachgroup[grabies].suspectamount[m,n]+=finalplusamount[m,n];
                        finalplusamount[m,n]=0.0f;
                        //--------------
                        for (int d = 0; d < e_to_i_date; d++)
                        {
                            dogeachgroup[grabies].exposeamount[m,n,d]+=finalplusamount_E[m,n,d];
                            finalplusamount_E[m,n,d]=0.0f;
                        }
                         for (int dd = 0; dd < i_to_r_date; dd++)
                        {
                            dogeachgroup[grabies].infectamount[m,n,dd]+=finalplusamount_R[m,n,dd];
                            finalplusamount_R[m,n,dd]=0.0f;
                        }
                    }
                }

            }
}



//---------------------------------------------------------------------------------------------------------------
private void Factor_apply()
{
//Applying
            //++++++++++++++++++++++++++++++++++++++++++++
             for (int grabies = 0; grabies < dogeachgroup.Count; grabies++)
                {
             for (int m = 0; m < xgridsize; m++)
                {
                    for (int n = 0; n < ygridsize; n++)
                    {
                        //suspect
                        //--------------
                        dogeachgroup[grabies].suspectamount[m,n]+=finalplusamount[m,n];
                        //--------------
                        for (int d = 0; d < e_to_i_date; d++)
                        {
                            dogeachgroup[grabies].exposeamount[m,n,d]+=finalplusamount_E[m,n,d];
                        }
                         for (int dd = 0; dd < i_to_r_date; dd++)
                        {
                            dogeachgroup[grabies].infectamount[m,n,dd]+=finalplusamount_R[m,n,dd];
                        }
                    }
                }
            }

            //++++++++++++++++++++++++++++++++++++++++++++

}

//----------------------------------------------------------------------------------------------------------------

private void Stateupdater()
{
    
     for (int grabies = 0; grabies < dogeachgroup.Count; grabies++)
        {           
            //day of expose change
                //xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
          float[,] e_to_i_amount=new float[xgridsize,ygridsize];

            for(int d=e_to_i_date-1; d>=0;d--)
            {
            for (int m = 0; m < xgridsize; m++)
                 {
                    for (int n = 0; n < ygridsize; n++)
                     {

                         if(d== (e_to_i_date-1))
                         {
                            e_to_i_amount[m,n] = dogeachgroup[grabies].exposeamount[m,n,d];
                            //Debug.Log("yay");
                         }

                        if(d!=0)
                        {
                         dogeachgroup[grabies].exposeamount[m,n,d]=dogeachgroup[grabies].exposeamount[m,n,d-1];
                        }
                        else  if(d==0)
                        {
                        dogeachgroup[grabies].exposeamount[m,n,d]=0.0f;
                        }  
                     }
                 }
            }
            //xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx

            //day of infect change
           //xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
            for(int d=i_to_r_date-1; d>=0;d--)
            {
            for (int m = 0; m < xgridsize; m++)
                 {
                    for (int n = 0; n < ygridsize; n++)
                     {

                         if(d== (i_to_r_date-1))
                         {
                            dogeachgroup[grabies].infectamount[m,n,d]=0.0f; //dead
                         }

                        if(d!=0)
                        {
                         dogeachgroup[grabies].infectamount[m,n,d]=dogeachgroup[grabies].infectamount[m,n,d-1];
                        }
                        else if(d==0)
                        {
                         dogeachgroup[grabies].infectamount[m,n,d]=e_to_i_amount[m,n];
                        }
                     }
                 }
            }
        }
            //xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
            //

             //if run rabies found normal dog,bite
             //need to revise on dog tranfer amount
             //shouldn't fix at group suspect
            //xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx    
             /*  for (int m = 0; m < xgridsize; m++)
                 {
                    for (int n = 0; n < ygridsize; n++)
                     {
                         if (foundinfect(m,n))
                         {
                           float rabietranfer;
                           rabietranfer = dogeverygroup.suspectamount[m,n] * biterate * infectedrate * infectsumatpoint(m,n); 
                           if (rabietranfer > dogeverygroup.suspectamount[m,n]) rabietranfer=dogeverygroup.suspectamount[m,n];
                           //then check on proposion of each group
                            float  currentgroup_proposion=0.0f;
                           for(int grabies2=0; grabies2 < dogeachgroup.Count; grabies2 ++)
                           {
                            if(dogeverygroup.suspectamount[m,n]!=0.0f) currentgroup_proposion = dogeachgroup[grabies2].suspectamount[m,n]/dogeverygroup.suspectamount[m,n];
                            else currentgroup_proposion = 0.0f;
                            dogeachgroup[grabies2].exposeamount[m,n,0] += (rabietranfer*currentgroup_proposion);
                           dogeachgroup[grabies2].suspectamount[m,n]-= (rabietranfer*currentgroup_proposion) ;
                           }
                         }
                     }
                 }*/
           //xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx
}
//----------------------------------------------------------------------------------------------------------------


private void rabies_bite_and_spread()
{

     for (int m = 0; m < xgridsize; m++)
                {
                    for (int n = 0; n < ygridsize; n++)
                    {
                        if (foundinfect(m,n))
                         {
                           float rabietranfer;
                           rabietranfer = dogeverygroup.suspectamount[m,n] * biterate * infectedrate * infectsumatpoint(m,n); 
                           if (rabietranfer > dogeverygroup.suspectamount[m,n]) rabietranfer=dogeverygroup.suspectamount[m,n];
                           //group now not move an inches,except placed rabies
                           //So no way we will see two group in one area 
                            //but it fucking spread to whole group right now
                            //Damn realistic ever
                            //Help me a lot fuck
                            //spread number to fuck whole group
                            if(rabietranfer>0)
                            {
                                int pointgroupnum=foundgroupnum(m,n);
                                rabietranfer=rabietranfer/groupgridcounter(pointgroupnum);
                                 for (int mm = 0; mm < xgridsize; mm++)
                                        {
                                        for (int nn = 0; nn < ygridsize; nn++)
                                            {
                                                if(founddogwithgroup(mm,nn,pointgroupnum))
                                                {
                                                    if(rabietranfer>dogeachgroup[pointgroupnum].suspectamount[mm,nn])
                                                    {
                                                        dogeachgroup[pointgroupnum].exposeamount[mm,nn,0]+=dogeachgroup[pointgroupnum].suspectamount[mm,nn];
                                                        dogeachgroup[pointgroupnum].suspectamount[mm,nn]=0;
                                                    }
                                                    else
                                                    {
                                                        dogeachgroup[pointgroupnum].exposeamount[mm,nn,0]+=rabietranfer;
                                                        dogeachgroup[pointgroupnum].suspectamount[mm,nn]-=rabietranfer;
                                                    }
                                                }
                                            }
                                        }
                            }
                         }
                    }
                }
}


//----------------------------------------------------------------------------------------------------------------









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
                texture.SetPixel( lon ,lat , getColorFromColorType((ygridsize-1) -lat , lon , imagetype));
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

        //show dog S state
        else if (imagetype == 9)
        {
            
                return Color.black;
            
        }

        //show dog S state //full white when at 5+
        else if (imagetype == 10)
        {
            float passlimitdog ;
             if (doggroup[lon , lat]* dogstate_proposion[0] > 0.0f)
            {
                passlimitdog = ( doggroup[lon , lat]* dogstate_proposion[0]) / 5.0f;
                if ( passlimitdog > 1.0f ) passlimitdog=1.0f;
                return new Color(passlimitdog ,passlimitdog  ,passlimitdog);
            }
            else
            {
                return Color.black;
            }
        }

         //show dog E state 
        else if (imagetype == 11)
        {
            if (doggroup[lon , lat]* dogstate_proposion[1] > 0.0f)
            {
                return new Color(doggroup[lon , lat] / highest_home_rate , doggroup[lon , lat] / highest_home_rate ,0.0f);
            }
            else
            {
                return Color.black;
            }
        }
         //show dog E state //full yellow when at 5+
        else if (imagetype == 12)
        {
           float passlimitdog ;
             if (doggroup[lon , lat]* dogstate_proposion[1] > 0.0f)
            {
                passlimitdog = ( doggroup[lon , lat]* dogstate_proposion[1]) / 5.0f;
                if ( passlimitdog > 1.0f ) passlimitdog=1.0f;
                return new Color(passlimitdog ,passlimitdog  ,0.0f);
            }
            else
            {
                return Color.black;
            }
        }


        else if (imagetype == 100)
        {

            
           if (infectsumatpoint(lon,lat) > 0.0f)
            {
                return new Color(255.0f , 0.0f , 0.0f);
            }
            else if(exposesumatpoint(lon,lat) < 0.0f)
            {
                return new Color(255.0f  , 255.0f  , 255.0f);
            }
            else if(exposesumatpoint(lon,lat) > 0.0f)
            {
                return new Color(255.0f  , 255.0f  , 0.0f);
            }
            else if(dogeverygroup.suspectamount[lon , lat] > 0.0f)
            {
                return new Color(0.0f , 255.0f  , 0.0f);
            }
            else
            {
               return new Color(0.0f , 0.0f , ((heightxy[lon , lat] - minh) / (maxh - minh)));
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
            return "/../Assets/Resources/test/selectedDogTerrain" + route + ".png";
        }
        else if (imagetype == 2)
        {
            return "/../Assets/Resources/test/selectedMapAndDog" + route + ".png";
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
         else if (imagetype == 9)
        {
            return "/../Assets/P2render/state_s_dog" + route + ".png";
        }
         else if (imagetype == 10)
        {
            return "/../Assets/P2render/state_s_dogmax5" + route + ".png";
        }
         else if (imagetype == 11)
        {
            return "/../Assets/P2render/state_e_dog" + route + ".png";
        }
         else if (imagetype == 12)
        {
            return "/../Assets/P2render/state_w_dogmax5" + route + ".png";
        }

         else if (imagetype == 100)
        {
            return "/../Assets/Resources/test/run" + route + ".png";
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
        //createImage(current_time, 8);
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