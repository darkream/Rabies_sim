using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using UnityEngine.EventSystems;

public class MouseOverToInstruction : MonoBehaviour
{
    public Text[] instructiontext;
    string[] title;
    string[] engInstruction;
    string[] thaiInstruction;
    public int changeableAmount = 18;

    void Start(){
        StreamReader reader = new StreamReader("Assets/instructiontext.txt");
        string line = reader.ReadLine();
        int count = 0;
        title = new string[changeableAmount];
        engInstruction = new string[changeableAmount];
        thaiInstruction = new string[changeableAmount];

        while(!reader.EndOfStream){
            if (count >= changeableAmount){
                break;
            }
            string[] thisline = reader.ReadLine().Split('/');
            title[count] = thisline[0];
            engInstruction[count] = thisline[1];
            thaiInstruction[count] = thisline[2];
            count++;
        }
    }

    void OnMouseOver(){
        Debug.Log("the mouse is over");
        if (PlayerPrefs.GetString("isThai") == "True"){
            changeInstructionText(thaiInstruction[getInstructionIDFromString()]);
        } else {
            changeInstructionText(engInstruction[getInstructionIDFromString()]);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
     {
         Debug.Log("Mouse enter");
     }

    private int getInstructionIDFromString(){
        for (int i = 0 ; i < changeableAmount ; i++){
            if (gameObject.GetComponent<Text>().text == title[i]){
                return i;
            }
        }
        return -1;
    }

    private void changeInstructionText(string newtext){
        for (int i = 0 ; i < 3 ; i++){
            instructiontext[i].text = newtext;
        }
    }
}
