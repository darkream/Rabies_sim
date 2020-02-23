using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class StringUIController : MonoBehaviour
{
    public Text[] textToChangeLanguage;
    public Font thaiFont;
    public bool isThai;

    public void changeToThaiLanguage(string path){
        StreamReader reader = new StreamReader(path);
        string line = reader.ReadLine();
        int count = 0;

        while(!reader.EndOfStream){
            if (count > textToChangeLanguage.Length){
                break;
            }
            textToChangeLanguage[count].font = thaiFont;
            textToChangeLanguage[count].text = ThaiFontAdjuster.Adjust(reader.ReadLine());
            textToChangeLanguage[count].fontSize = (int)(textToChangeLanguage[count].fontSize * 0.8f);
            count++;
        }
    }

    public string translationSelector(string thai, string eng){
        if (isThai) return ThaiFontAdjuster.Adjust(thai);
        else return eng;
    }
    public string getErrorMapCalNotNumberText(){
        return translationSelector("*ค่าบางตัวอาจไม่ใช่ตัวเลข*", "*some values are not number*");
    }

    public string getShowAdvancedSettingText(){
        return translationSelector("แสดงการตั้งค่าเพิ่มเติม", "Show World Advanced Setting");
    }

    public string getHideAdvancedSettingText(){
        return translationSelector("ปิดการตั้งค่าเพิ่มเติม", "Hide World Advanced Setting");
    }

    public string getUnlockDogInputText(){
        return translationSelector("ปลดล๊อค: วางพิกัดของสุนัขได้", "Unlocked: you can add dog location");
    }

    public string getLockedDogInputText(){
        return translationSelector("ล๊อค: ห้ามวางพิกัดของสุนัข", "Locked: dog is not allowed to be added");
    }

    public string getDeleteText(){
        return translationSelector("ลบ","DELETE");
    }

    public string getCloseText(){
        return translationSelector("ปิด","CLOSE");
    }

    public string getDeleteDogText(){
        return translationSelector("ตำแหน่งหมาที่", "Delete Dog ");
    }

    public string getErrorInputField(){
        return translationSelector("พบข้อผิดพลาดในช่องข้อมูล","Error Input Field");
    }
}
