using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.IO;

public class ColorStreamClustering : MonoBehaviour
{
    [SerializeField]
    Texture2D thaimap;

    List<string> stringcolorlist;

    public void pixelReaderAndFileWritten(string path){
        Debug.Log(thaimap);
        StreamWriter writer = new StreamWriter(path, false);

        string thisline = "";

        for (int i = 0; i < thaimap.width; i++)
        {
            for (int j = 0; j < thaimap.height; j++)
            { 
                Color pixel = thaimap.GetPixel(i, j);
                thisline += pixel.r + "," + pixel.g + "," + pixel.b + " ";
            }
            writer.WriteLine(thisline);
            thisline = "";
        }
        writer.Close();
        stringcolorlist = new List<string>();

        StreamReader reader = new StreamReader(path);
        string line = "", thiscolor;
        string[] x;
        while (!reader.EndOfStream){
            line = reader.ReadLine();
            x = line.Split(' ');
            for (int i = 0 ; i < x.Length - 1 ; i++){
                thiscolor = x[i];
                if(!colorRepetitionCheck(thiscolor)){
                    stringcolorlist.Add(thiscolor);
                }
            }
        }
        Debug.Log("There are " + stringcolorlist.Count + " distinct colors");
        reader.Close();
    }

    private bool colorRepetitionCheck(string target_color){
        for (int i = 0 ; i < stringcolorlist.Count ; i++){
            if (target_color.Equals(stringcolorlist[i])){
                return true;
            }
        }
        return false;
    }

    private List<Color> k_list;
    private List<Color> colorlists;

    private float mean = 0.0f, variance = 0.0f;
    public void kMeanClustering(){
        int runnable_k = 3, selected_k = 3;
        randomSelectInitColor(runnable_k);
    }

    private void randomSelectInitColor(int k){
        createColorLists();
        k_list = new List<Color>();
        int rd_index = 0;
        Color thatcolor = Color.red;
        bool retry_random = true;
        for (int i = 0 ; i < k ; i++){
            while(retry_random){
                retry_random = false;
                rd_index = Random.Range(0, colorlists.Count - 1);
                thatcolor = colorlists[rd_index];
                for (int j = 0 ; j < k_list.Count ; j++){
                    if (thatcolor.Equals(k_list[j])){
                        retry_random = true;
                    }
                }
            }
            retry_random = true;
            k_list.Add(thatcolor);
            Debug.Log(rd_index);
        }
        findClosestKListColor(k);
    }

    List<int> colorgroup;
    private void createColorLists(){
        colorlists = new List<Color>();
        colorgroup = new List<int>();
        for (int i = 0 ; i < stringcolorlist.Count ; i++){
            colorlists.Add(stringToColor(stringcolorlist[i]));
            colorgroup.Add(0);
        }
    }

    private Color stringToColor(string rgb_text){
        string[] rgb = rgb_text.Split(',');
        return new Color(float.Parse(rgb[0]) , float.Parse(rgb[1]) , float.Parse(rgb[2]));
    }

    private void findClosestKListColor(int k){
        float closest_distance = 0.0f;
        float candidate_distance;
        int closest_k;
        for (int i = 0 ; i < colorlists.Count ; i++){
            closest_distance = findDistanceBetweenColors(colorlists[i], k_list[0]);
            candidate_distance = closest_distance;
            closest_k = 0;
            for (int at_list = 1 ; at_list < k_list.Count ; at_list++){
                candidate_distance = findDistanceBetweenColors(colorlists[i], k_list[at_list]);
                if (candidate_distance < closest_distance){
                    closest_distance = candidate_distance;
                    closest_k = at_list;
                }
            }
            colorgroup[i] = closest_k;
        }
    }

    //3-dimensional euclidean distance
    private float findDistanceBetweenColors(Color a, Color b){
        return ((b.r - a.r) * (b.r - a.r)) + ((b.g - a.g) * (b.g - a.g)) + ((b.b - a.b) * (b.b - a.b));
    }
}
