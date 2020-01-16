using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
public class ReportPageController : MonoBehaviour
{
    CoreUIController coreui;
    public GameObject mapobject;
    public GameObject reportpage;
    public GameObject heatmappage;

    int systemday;
    int frameperday;

    List<string> dropdownlist = new List<string>();

    bool Animate_onrun=false;

    float framepersec=5.0f;

//==============================================
    //report page object zone
    public Button Openfolder_button;
    public Button ShowHeatmap_button;
    public Button Showreport_button;
    public Button Animation_button;
    public Dropdown dropdownday_reportpage;
    public Dropdown dropdownday_heatmappage;
    //restart button has it own script
    public Text report_textbox;
    public Text Animation_button_text;
    public Toggle Show_S;
    public Toggle Show_E;
    public Toggle Show_I;

    public GameObject S_image_obj;
    public GameObject E_image_obj;
    public GameObject I_image_obj;
    public RawImage S_image;
    public RawImage E_image;
    public RawImage I_image;

    Texture2D[] animate_texture_S;
    Texture2D[] animate_texture_E;
    Texture2D[] animate_texture_I;

    //end object
//==============================================
    //Other Script
     OnMapSpawn mapdata;

    public GameObject doglayer;

    public GameObject infectlayer;

//==============================================
    
    void Start()
    {
      mapdata=mapobject.GetComponent<OnMapSpawn>();
      Openfolder_button.GetComponent<Button>().onClick.AddListener(openreportfolder);
      ShowHeatmap_button.GetComponent<Button>().onClick.AddListener(showheatmap);
      Showreport_button.GetComponent<Button>().onClick.AddListener(showreportpage);
      Animation_button.GetComponent<Button>().onClick.AddListener(Runanimate_map);
      dropdownday_heatmappage.onValueChanged.AddListener(delegate {Rereadanimate_Image(dropdownday_heatmappage);});
      systemday=mapdata.dayloop;
      frameperday=mapdata.loopperday;
      for(int j=1;j<systemday;j++)
      {
          dropdownlist.Add("day"+j);
      }
      dropdownday_heatmappage.AddOptions(dropdownlist);
      dropdownday_reportpage.AddOptions(dropdownlist);
      animate_texture_S=new Texture2D[frameperday];
      animate_texture_E=new Texture2D[frameperday];
      animate_texture_I=new Texture2D[frameperday];
      for(int i = 0;i<frameperday;i++)
        {
            animate_texture_S[i]=Resources.Load ("S_state/state_s_dog"+ dropdownday_heatmappage.value+"_runloop_"+i) as Texture2D;
            animate_texture_E[i]=Resources.Load ("E_state/state_e_dog"+ dropdownday_heatmappage.value+"_runloop_"+i) as Texture2D;
            animate_texture_I[i]=Resources.Load ("I_state/state_i_dog"+ dropdownday_heatmappage.value+"_runloop_"+i) as Texture2D;
        }
    }
    // Update is called once per frame
    void Update()
    {
       if(reportpage.activeSelf)
        {
            readtextfile(dropdownday_reportpage.value);
        }
        else if (heatmappage.activeSelf)
        {
            Toggle_Controller();
            if(Animate_onrun)
            {
                    Animation_button_text.text="Stop Animation";
                    int animateindex = (int)((Time.time * framepersec) % animate_texture_S.Length);
                    S_image.texture=animate_texture_S[animateindex];
                    E_image.texture=animate_texture_E[animateindex];
                    I_image.texture=animate_texture_I[animateindex];
            }
            if(!Animate_onrun)
            {
                Animation_button_text.text="Run Animation";
                 readimage_S(dropdownday_heatmappage.value);
                 readimage_E(dropdownday_heatmappage.value);
                 readimage_I(dropdownday_heatmappage.value);
            }
        }
    }

    private void openreportfolder(){
         string path; 
         path=Application.dataPath + "/Resources/test/"; 
         path = path.Replace(@"/", @"\");   // explorer doesn't like front slashes
         Debug.Log(path);
         try
         {
              System.Diagnostics.Process.Start("explorer.exe", "/root,"+path);
         }
         catch(System.ComponentModel.Win32Exception e)
         {
             // just silently skip error
             // we currently have no platform define for the current OS we are in, so we resort to this
             e.HelpLink = ""; // do anything with this variable to silence warning about not using it
         }
    }

    private void showheatmap()
    {
        reportpage.SetActive(false);
        heatmappage.SetActive(true);
        doglayer.SetActive(false);
        infectlayer.SetActive(false);
    }
    private void showreportpage()
    {
        reportpage.SetActive(true);
        heatmappage.SetActive(false);
    }


    private void readtextfile(int indexvalue)
    {
        string path = (Application.dataPath + "/../Assets/Resources/test/Report_day"+(indexvalue+1)+".txt");

        //Read the text from directly from the test.txt file
        StreamReader reader = new StreamReader(path); 
        report_textbox.text=reader.ReadToEnd();
        reader.Close();
    }

    private void readimage_S(int indexvalue)
    {
        Texture2D pictexture;
        pictexture = Resources.Load("S_state/state_s_dog"+indexvalue+"_runloop_0") as Texture2D;
        S_image.texture = pictexture;

    }

    private void readimage_E(int indexvalue)
    {
        Texture2D pictexture;
        pictexture = Resources.Load("E_state/state_e_dog"+indexvalue+"_runloop_0") as Texture2D;
        E_image.texture = pictexture;

    }

    private void readimage_I(int indexvalue)
    {
        Texture2D pictexture;
        pictexture = Resources.Load("I_state/state_i_dog"+indexvalue+"_runloop_0") as Texture2D;
        I_image.texture = pictexture;

    }

    private void Toggle_Controller()
    {
            if(Show_S.isOn)
            {
                S_image_obj.SetActive(true);
               // readimage_S(dropdownday_heatmappage.value);
            }
            else if (Show_S.isOn==false)
            {
                S_image_obj.SetActive(false);
            }
            if(Show_E.isOn)
            {
                E_image_obj.SetActive(true);
                //readimage_E(dropdownday_heatmappage.value);
            }
            else if (Show_E.isOn==false)
            {
                E_image_obj.SetActive(false);
            }
             if(Show_I.isOn)
            {
                I_image_obj.SetActive(true);
               // readimage_I(dropdownday_heatmappage.value);
            }
            else if (Show_I.isOn==false)
            {
                I_image_obj.SetActive(false);
            }
    }

    private void Runanimate_map()
    {
        Animate_onrun=!Animate_onrun;
    }

    private void Rereadanimate_Image(Dropdown change)
    {
        Animate_onrun=false; //stop run animate suddenly
        for(int i = 0;i<frameperday;i++)
        {
            animate_texture_S[i]=Resources.Load ("S_state/state_s_dog"+change.value+"_runloop_"+i) as Texture2D;
            animate_texture_E[i]=Resources.Load ("E_state/state_e_dog"+change.value+"_runloop_"+i) as Texture2D;
            animate_texture_I[i]=Resources.Load ("I_state/state_i_dog"+change.value+"_runloop_"+i) as Texture2D;
        }
    }

}


/*
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
*/