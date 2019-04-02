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
    }

    public void createStringColorListFromReadFile(string path){
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

    private List<Color> k_average;
    private List<Color> colorlists;
    private List<float> k_variance;

    public void kMeanClustering(){
        int runnable_k = 3;
        int average_converge = 4;
        int cur_average_converge = 0;
        bool average_isChanged = false;

        randomSelectInitColor(runnable_k); //Start with Random Numbers
        findClosestKListColor(runnable_k);
        findNewAverageRGB();

        //Run with the new average until converge
        while (cur_average_converge < average_converge){
            findClosestKListColor(runnable_k);
            average_isChanged = findNewAverageRGB();
            if (average_isChanged){
                cur_average_converge = 0;
            }
            else {
                cur_average_converge++;
            }
            Debug.Log("This is converge id : " + cur_average_converge);
            findNewVariance(); //Find the variance of the sample
        }
    }

    private void randomSelectInitColor(int k){
        createColorLists();

        //Initiate Variables
        k_average = new List<Color>();
        int rd_index = 0;
        Color thatcolor = Color.red;
        bool retry_random = true;

        for (int i = 0 ; i < k ; i++){
            //RANDOM THE DIFFERENT INITIAL UNIT
            while(retry_random){
                retry_random = false;
                rd_index = Random.Range(0, colorlists.Count - 1);
                thatcolor = colorlists[rd_index];
                for (int j = 0 ; j < k_average.Count ; j++){
                    if (thatcolor.Equals(k_average[j])){
                        retry_random = true;
                    }
                }
            }
            retry_random = true;
            k_average.Add(thatcolor);
            Debug.Log(rd_index);
        }
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

        //Find Nearest Neighbour
        for (int i = 0 ; i < colorlists.Count ; i++){
            closest_distance = findDistanceBetweenColors(colorlists[i], k_average[0]);
            candidate_distance = closest_distance;
            closest_k = 0;
            for (int at_list = 1 ; at_list < k_average.Count ; at_list++){
                candidate_distance = findDistanceBetweenColors(colorlists[i], k_average[at_list]);
                if (candidate_distance < closest_distance){
                    closest_distance = candidate_distance;
                    closest_k = at_list;
                }
            }
            colorgroup[i] = closest_k;
        }
    }

    //3-dimensional euclidean distance without Square Root
    private float findDistanceBetweenColors(Color a, Color b){
        return ((b.r - a.r) * (b.r - a.r)) + ((b.g - a.g) * (b.g - a.g)) + ((b.b - a.b) * (b.b - a.b));
    }

    float color_average_difference_criteria = 0.1f; //Difference must be at least in (criteria, default: 0.1f = 10%)
    private bool findNewAverageRGB(){
        float[] r = new float[k_average.Count];
        float[] g = new float[k_average.Count];
        float[] b = new float[k_average.Count];
        int[] gcount = new int[k_average.Count];
        bool isChanged = false;

        for (int i = 0 ; i < k_average.Count ; i++){
            r[i] = 0.0f;
            g[i] = 0.0f;
            b[i] = 0.0f;
            gcount[i] = 0;
        }

        //Sum all colors that assign to the group
        for (int i = 0 ; i < colorlists.Count ; i++){
            r[colorgroup[i]] += colorlists[i].r;
            g[colorgroup[i]] += colorlists[i].g;
            b[colorgroup[i]] += colorlists[i].b;
            gcount[colorgroup[i]]++;
        }

        //Divide all to find average and reassign to k_average
        Color col_container;
        for (int i = 0 ; i < k_average.Count ; i++){
            r[i] /= gcount[i];
            g[i] /= gcount[i];
            b[i] /= gcount[i];
            
            col_container = new Color(r[i], g[i], b[i]);
            if (findColorDifference(col_container , k_average[i]) > color_average_difference_criteria) {
                isChanged = true;
            }
            k_average[i] = col_container;
        }
        return isChanged;
    }

    private float findColorDifference(Color a, Color b){
        return ((Mathf.Abs(a.r - b.r) / a.r) + (Mathf.Abs(a.g - b.g) / a.g) + (Mathf.Abs(a.b - b.b) / a.b)) / 3.0f;
    }

    private void findNewVariance(){
        k_variance = new List<float>();
        int[] variance_count = new int[k_average.Count];

        for (int i = 0 ; i < k_average.Count ; i++){
            variance_count[i] = 0;
            k_variance.Add(0.0f);
        }

        for (int i = 0 ; i < colorlists.Count ; i++){
            k_variance[colorgroup[i]] += findDistanceBetweenColors(k_average[colorgroup[i]] , colorlists[i]);
            variance_count[colorgroup[i]]++;
        }

        for (int i = 0 ; i < k_average.Count ; i++){
            k_variance[i] /= variance_count[i];
            Debug.Log(k_average[i] + " with variance: " + k_variance[i]);
        }
    }

    private float[] color_max;
    private float[] color_min;
    private bool[] color_vec; //true = positive, false = negative number

    public void findColorVector(Color a, Color b){
        color_vec = new bool[3];
        if (a.r < b.r){
            color_vec[0] = true;
        }
        else {
            color_vec[0] = false;
        }
        if (a.g < b.g){
            color_vec[1] = true;
        }
        else {
            color_vec[1] = false;
        }
        if (a.b < b.b){
            color_vec[2] = true;
        }
        else {
            color_vec[2] = false;
        }
    }

    public void readFileToFindMinMax(string path){
        StreamReader reader = new StreamReader(path);
        string line;
        string[] x;
        Color thiscolor;

        color_max = new float[] {0.0f, 0.0f, 0.0f};
        color_min = new float[] {1.0f, 1.0f, 1.0f};

        while (!reader.EndOfStream){
            line = reader.ReadLine();
            x = line.Split(' ');
            for (int i = 0 ; i < x.Length - 1 ; i++){
                if (!x[i].Equals("1,1,1") && !x[i].Equals("0,0,0")){ //The color must not be pure white or black
                    thiscolor = stringToColor(x[i]);         
                    if (thiscolor.r > color_max[0]){
                        color_max[0] = thiscolor.r;
                    }
                    if (thiscolor.r < color_min[0]){
                        color_min[0] = thiscolor.r;
                    }
                    if (thiscolor.g > color_max[1]){
                        color_max[1] = thiscolor.g;
                    }
                    if (thiscolor.g < color_min[1]){
                        color_min[1] = thiscolor.g;
                    }
                    if (thiscolor.b > color_min[2]){
                        color_max[2] = thiscolor.b;
                    }
                    if (thiscolor.b < color_min[2]){
                        color_min[2] = thiscolor.b;
                    }
                }
            }
        }

        Debug.Log("Max: " + color_max[0] + ", " + color_max[1] + ", " + color_max[2]);
        Debug.Log("Min: " + color_min[0] + ", " + color_min[1] + ", " + color_min[2]);
        reader.Close();
    }

    private float map_x1, map_x2, map_y1, map_y2;
    public string[] coordTag = new string[] {"north" , "south" , "east" , "west"};
    public void readAndAssignMapCoordinates(string path){
        StreamReader reader = new StreamReader(path);
        string line;

        while(!reader.EndOfStream){
            line = reader.ReadLine();
            extractMapCoordFromPossibleString(line);
        }
    }

    public void extractMapCoordFromPossibleString(string thatline){
        string startTag, endTag, thatvalue;
        int startIndex, endIndex;
        for (int i = 0 ; i < coordTag.Length ; i++){
            startTag = "<" + coordTag[i] + ">";
            endTag = "</" + coordTag[i] + ">";
            if (thatline.IndexOf(startTag) != -1){
                startIndex = thatline.IndexOf(startTag) + startTag.Length;
                endIndex = thatline.IndexOf(endTag, startIndex);
                thatvalue = thatline.Substring(startIndex, endIndex - startIndex);
                assignMapCoordValue(i , thatvalue);
                Debug.Log(coordTag[i] + ": " + thatvalue);
            }
        }
    }

    private void assignMapCoordValue(int dir, string value){
        float val = float.Parse(value);
        if (dir == 0) { //north
            map_y2 = val;
        } 
        else if (dir == 1) { //south
            map_y1 = val;
        }
        else if (dir == 2) { //east
            map_x2 = val;
        }
        else if (dir == 3) { //west
            map_x1 = val;
        }
    }

    public float getNumberFromIntensity(Color thatcolor, char channel){
        if (channel == 'b'){
            if (color_vec[2] == true) //if the vector of blue is positive
                return (thatcolor.b - color_min[2]) / (color_max[2] - color_min[2]);
            else
                return (thatcolor.b - color_max[2]) / (color_min[2] - color_max[2]);
        }
        else if (channel == 'g'){
            if (color_vec[1] == true) //if the vector of green is positive
                return (thatcolor.g - color_min[1]) / (color_max[1] - color_min[1]);
            else
                return (thatcolor.g - color_max[1]) / (color_min[1] - color_max[1]);
        }
        else {
            if (color_vec[0] == true) //if the vector of red is positive
                return (thatcolor.r - color_min[0]) / (color_max[0] - color_min[0]);
            else
                return (thatcolor.r - color_max[0]) / (color_min[0] - color_max[0]);
        }
    }
}
