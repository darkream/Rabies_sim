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
         path=Application.streamingAssetsPath ; 
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
        Rereadanimate_Image(dropdownday_heatmappage);
    }
    private void showreportpage()
    {
        reportpage.SetActive(true);
        heatmappage.SetActive(false);
    }


    private void readtextfile(int indexvalue)
    {
        string path = (Application.streamingAssetsPath + "/Textreport/Report_day"+(indexvalue+1)+".txt");

        //Read the text from directly from the test.txt file
        StreamReader reader = new StreamReader(path); 
        report_textbox.text=reader.ReadToEnd();
        reader.Close();
    }

    private void readimage_S(int indexvalue)
    {
        Texture2D pictexture = new Texture2D(2,2);
        string path = (Application.streamingAssetsPath+"/S_state/state_s_dog"+indexvalue+"_runloop_0"+".png");
        byte[] pngBytes = System.IO.File.ReadAllBytes(path);

        pictexture.LoadImage(pngBytes);
        S_image.texture = pictexture;

    }

    private void readimage_E(int indexvalue)
    {
        Texture2D pictexture = new Texture2D(2,2);
        string path = (Application.streamingAssetsPath+"/E_state/state_e_dog"+indexvalue+"_runloop_0"+".png");
        byte[] pngBytes = System.IO.File.ReadAllBytes(path);

        pictexture.LoadImage(pngBytes);
        E_image.texture = pictexture;


    }

    private void readimage_I(int indexvalue)
    {
        Texture2D pictexture = new Texture2D(2,2);
        string path = (Application.streamingAssetsPath+"/I_state/state_i_dog"+indexvalue+"_runloop_0"+".png");
        byte[] pngBytes = System.IO.File.ReadAllBytes(path);

        pictexture.LoadImage(pngBytes);
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

       // Texture2D pictexture_s = new Texture2D(2,2);
       // Texture2D pictexture_e = new Texture2D(2,2);
      //  Texture2D pictexture_i = new Texture2D(2,2);
        string path_s ;
        string path_e ;
        string path_i ;
        byte[] pngBytes_S ;
        byte[] pngBytes_E ;
        byte[] pngBytes_I ;
       
        
        for(int i = 0;i<frameperday;i++)
        {
            animate_texture_S[i]=new Texture2D(2,2);
            animate_texture_E[i]=new Texture2D(2,2);
            animate_texture_I[i]=new Texture2D(2,2);
            path_s = (Application.streamingAssetsPath+"/S_state/state_s_dog"+change.value+"_runloop_"+i+".png");
            path_e = (Application.streamingAssetsPath+"/E_state/state_e_dog"+change.value+"_runloop_"+i+".png");
            path_i = (Application.streamingAssetsPath+"/I_state/state_i_dog"+change.value+"_runloop_"+i+".png");
            pngBytes_S=System.IO.File.ReadAllBytes(path_s);
            animate_texture_S[i].LoadImage(pngBytes_S);
            pngBytes_E=System.IO.File.ReadAllBytes(path_e);
            animate_texture_E[i].LoadImage(pngBytes_E);
            pngBytes_I=System.IO.File.ReadAllBytes(path_i);
            animate_texture_I[i].LoadImage(pngBytes_I);
        }
    }

}


