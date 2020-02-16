using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class CoreUIController : MonoBehaviour
{
    //INITIAL VALUE
    public int MODE_MAP_SELECTION = 1;
    public int MODE_NORMAL_DOG_SELECTION = 2;
    public int MODE_INFECT_DOG_SELECTION = 3;
    private int screenMode = 1;
    public bool mapIsLocked = false;
    public bool Normaldog_finish = false;
    public bool startPreRegisterData = false;

    //MAP SELECTION UI
    public GameObject mapSelection;
    public Button mapSelectionButton;

    //ADD NORMAL DOG UI
    public GameObject addNormalDogScreen;
    public GameObject populationQuantBox;
    public InputField dogQuantity;
    public Button cancelDogButton;
    public Button addDogButton;
    public Button useDefaultDataButton;
    public Button acceptDogPopButton;
    public Text errorDogInput;
    public Text allowDogInputStatus;
    public Image inputState;
    public bool dogIsCancelledNotification = false;
    public bool dogIsAddedNotification = false;
    public bool allowAddDogObject = false;
    public bool useDefaultDogNotification = false;
    public bool usedDefaultData = false;
    public Button deleteButton;
    public GameObject deleteList;
    public Button deleteAllButton;
    public Text deleteButtonText;
    public bool deleteAllDogsNotification = false;
    public GameObject dogListContent;
    public GameObject dogDeleteInContent;
    private float dogDeletePosition = 145.0f;
    private int dogContentID = 1;
    public int deleteOneDogNotification = -1;
    public GameObject dumpingSite;

    public bool onDoginputNotification = false;

    //ADD Infect DOG UI
    public GameObject addInfectDogScreen;
    public GameObject populationQuantBox_I;
    public InputField dogQuantity_I;
    public Button cancelDogButton_I;
    public Button addDogButton_I;
    public Button acceptDogPopButton_I;
    public Text errorDogInput_I;
    public Text allowDogInputStatus_I;
    public Image inputState_I;
    public bool dogIsCancelledNotification_I = false;
    public bool dogIsAddedNotification_I = false;
    public bool allowAddDogObject_I = false;
    public bool onDoginputNotification_I = false;
     
    //PARAMETER SETTING UI (FIRST PAGE)
    public GameObject paramSetMapCalScreen;
    public bool initMapCalculationParameter = false;
    public Text[] mapCalText;
    public InputField[] mapCal;
    public Button mapCalNext;
    public Text errorMapCalNotNumber;
    public bool resetMapCalculationParameter = false;
    public Button showWorldAdvancedSetting;
    public Text worldAdvancedSettingText;

    //PARAMETER SETTING UI (SECOND PAGE)
    public GameObject paramSetDogBehaviorScreen;
    public bool initDogBehaviorParameter = false;
    public InputField[] dogBehavior;
    public Toggle openElevation;
    public Button dogBehaviorNext;
    public Text errorDogBehaviorNotNumber;
    public bool resetDogBehaviorParameter = false;
    public GameObject paramSetImageGenParameter;
    public bool initImageGenParameter = false;
    public InputField[] imageGen;
    public Text singleBehavior;
    public Button extendmapnext;
    public Button imageGenNext;
    public Text errorImageGenNotNumber;
    public bool resetImageGenParameter = false;
    public bool runProgramNotification = false;
    public bool extendmapNotification=false;

    //LANGUAGE SELECTOR UI
    public GameObject languageSelectorScreen;
    public GameObject thaiFlag;
    public GameObject engFlag;
    public StringUIController stringScript;

    //Extend map
    public GameObject ExtendmapSelection;

    public bool extendmapset=false;
    //Report page
    //Due to core UI behaviour ,report page need to open on onmapspawn
    //public GameObject thaiFlag;
    public Button Openfolder_button;

    //dog layer hiding
    public GameObject Normaldoglayer;
    public GameObject Infectdoglayer;

    public bool zoomlock=false;

    //INSTRUCTION TEXT UI
    public Text[] instructiontext;
    string[] title;
    string[] instruction;
    public int changeableAmount = 42;

    //ZOOM LEVEL UI
    public Text mapZoom;
    public Text extendZoom;

    public int getScreenMode(){
        return screenMode;
    }

    public void setScreenMode(int mode){
        screenMode = mode;
    }

    //CALL THIS FUNCTION TO INITIATE PROCESS
    public void setupActivation(){
        engFlag.GetComponent<Button>().onClick.AddListener(moveToChangeParameters);
        thaiFlag.GetComponent<Button>().onClick.AddListener(moveToChangeParametersButThai);
        mapSelectionButton.GetComponent<Button>().onClick.AddListener(moveToAddNormalDog);
        cancelDogButton.GetComponent<Button>().onClick.AddListener(cancelDogPopulation);
        useDefaultDataButton.GetComponent<Button>().onClick.AddListener(useDefaultDogData);
        addDogButton.GetComponent<Button>().onClick.AddListener(addDogPopulation);
        acceptDogPopButton.GetComponent<Button>().onClick.AddListener(moveToAddInfectDog);

        addDogButton_I.GetComponent<Button>().onClick.AddListener(addDogPopulation_I);
        cancelDogButton_I.GetComponent<Button>().onClick.AddListener(cancelDogPopulation_I);
        acceptDogPopButton_I.GetComponent<Button>().onClick.AddListener(moveToChangeParameters2);
        
        mapCalNext.GetComponent<Button>().onClick.AddListener(moveToMapSelection);
        showWorldAdvancedSetting.GetComponent<Button>().onClick.AddListener(showOrHideWorldAdvancedSetting);
        dogBehaviorNext.GetComponent<Button>().onClick.AddListener(moveToChangeParameters3);
        imageGenNext.GetComponent<Button>().onClick.AddListener(moveToRunProgram);
        
        extendmapnext.GetComponent<Button>().onClick.AddListener(moveToExtendMapSelection);
        deleteButton.GetComponent<Button>().onClick.AddListener(showOrHideDeleteList);
        deleteAllButton.GetComponent<Button>().onClick.AddListener(notifyDeleteAll);
        imageGen[0].onValueChanged.AddListener(delegate {hordeMustNotExceedOne();});
        imageGen[1].onValueChanged.AddListener(delegate {exploreMustNotExceedOne();});
        if (PlayerPrefs.GetString("isThai") == "True") {
            stringScript.changeToThaiLanguage(Application.streamingAssetsPath+"/thaitranslated.txt");
            stringScript.isThai=true;
        }
        readInstructionTextList();

        moveToChangeParameters();
    }

    //TO HIDE ALL UI BEFORE PLACING ANOTHER ONE


    private void hideAllScreens(){
        languageSelectorScreen.SetActive(false);
        mapSelection.SetActive(false);
        populationQuantBox.SetActive(false);
        addNormalDogScreen.SetActive(false);
        addInfectDogScreen.SetActive(false);
        paramSetMapCalScreen.SetActive(false);
        paramSetDogBehaviorScreen.SetActive(false);
        paramSetImageGenParameter.SetActive(false);
        ExtendmapSelection.SetActive(false);
        for (int i = 0 ; i < instructiontext.Length ; i++){
            instructiontext[i].text = "";
        }
    }

    public void hideDogObject(){
        Normaldoglayer.SetActive(false);
        Infectdoglayer.SetActive(false);
    }
    public void ShowDogObject(){
        Normaldoglayer.SetActive(true);
        Infectdoglayer.SetActive(true);
    }
    //LIST OF "MOVE TO" FUNCTION
    private void moveToLanguageSelector(){
        hideAllScreens();
        languageSelectorScreen.SetActive(true);
    }

    private void moveToChangeParametersButThai(){
        hideAllScreens();
        stringScript.changeToThaiLanguage(Application.streamingAssetsPath+"/thaitranslated.txt");
        paramSetMapCalScreen.SetActive(true);
        initMapCalculationParameter = true;
        stringScript.isThai=true;
    }

    private void moveToChangeParameters(){
        hideAllScreens();
        paramSetMapCalScreen.SetActive(true);
        initMapCalculationParameter = true;
    }

    private void moveToMapSelection(){
        if (checkMapCalParamIntegrity()){
            hideAllScreens();
            resetMapCalculationParameter = true;
            mapSelection.SetActive(true);
        } else {
            errorMapCalNotNumber.text = stringScript.getErrorMapCalNotNumberText();
        }
    }

     private void moveToExtendMapSelection(){
            hideAllScreens();
           // resetMapCalculationParameter = true;
           extendmapNotification=true;
            ExtendmapSelection.SetActive(true);
            zoomlock=false;
    }

    private void moveToAddNormalDog(){
        hideAllScreens();
        allowAddDogObject = true;
        mapIsLocked = true;
        zoomlock=true;
        addNormalDogScreen.SetActive(true);
    }

    private void moveToAddInfectDog(){
        hideAllScreens();
       // switchAllowInput(false);
        switchAllowInput_I(true);
        allowAddDogObject_I = true;
        infecttextsetter();
        mapIsLocked = true;
        Normaldog_finish=true;
        addInfectDogScreen.SetActive(true);
        setScreenMode(MODE_INFECT_DOG_SELECTION);
    }

    private void moveToChangeParameters2(){
        allowAddDogObject = false;
        switchAllowInput(false);
        switchAllowInput_I(false);
        hideAllScreens();
        paramSetDogBehaviorScreen.SetActive(true);
        initDogBehaviorParameter = true;
    }

    private void moveToChangeParameters3(){
        if (checkDogBehaviorParamIntegrity()){
            hideAllScreens();
            resetDogBehaviorParameter = true;
            paramSetImageGenParameter.SetActive(true);
            initImageGenParameter = true;
        } else {
            errorDogBehaviorNotNumber.text = stringScript.getErrorMapCalNotNumberText();
        }
    }

    private void moveToRunProgram(){
        if (checkImageGenParamIntegrity()){
            hideAllScreens();
            extendmapset=true;
            extendmapNotification=false;
            runProgramNotification = true;
            zoomlock=true;
        } else {
            errorImageGenNotNumber.text = stringScript.getErrorMapCalNotNumberText();
        }
    }

    //OTHER UTILITY FUNCTIONS
    private void showOrHideWorldAdvancedSetting(){
        if (worldAdvancedSettingText.text == stringScript.getShowAdvancedSettingText()){
            resetWorldAdvancedSetting(stringScript.getHideAdvancedSettingText(), true);
        } else {
            resetWorldAdvancedSetting(stringScript.getShowAdvancedSettingText(), false);
        }
    }

    private void resetWorldAdvancedSetting(string changedText, bool isEnabled) {
        worldAdvancedSettingText.text = changedText;
            for (int i = 0 ; i < mapCal.Length - 1 ; i++){
                mapCal[i].gameObject.SetActive(isEnabled);
                mapCalText[i].gameObject.SetActive(isEnabled);
            }
    }

    public void initializeDogPopulationInput(){
        switchAllowInput(false);
        useDefaultDataButton.enabled = false;
        acceptDogPopButton.enabled = false;
        dogQuantity.GetComponent<InputField>().text = "";
        if (PlayerPrefs.GetString("isThai") == "True"){
            showDogErrorInput("ตำแหน่งสุนัขแสดงถึง จุดกึ่งกลางของที่อยู่สุนัข โปรแกรมจะสร้างขนาดของที่อยู่ในภายหลัง");
            errorDogInput.GetComponent<Text>().font = stringScript.thaiFont;
            errorDogInput.GetComponent<Text>().fontSize = (int)(errorDogInput.GetComponent<Text>().fontSize * 0.8f);
        } else {
            showDogErrorInput("Dog position represents \"center of population\", coverage area will be operated after this.");
        }
        populationQuantBox.SetActive(true);
         onDoginputNotification=true;
    }

    public void initializeDogPopulationInput_I(){
        switchAllowInput_I(false);
        acceptDogPopButton_I.enabled = false;
        dogQuantity_I.GetComponent<InputField>().text = "";
        if (PlayerPrefs.GetString("isThai") == "True"){
            showDogErrorInput_I("ตำแหน่งสุนัขแสดงถึง จุดกึ่งกลางของที่อยู่สุนัข โปรแกรมจะสร้างขนาดของการแพร่กระจายในภายหลัง");
            errorDogInput_I.GetComponent<Text>().font = stringScript.thaiFont;
            errorDogInput_I.GetComponent<Text>().fontSize = (int)(errorDogInput_I.GetComponent<Text>().fontSize * 0.8f);
        } else {
            showDogErrorInput_I("Dog position represents \"center of population\", coverage area will be operated after this.");
        }
        populationQuantBox_I.SetActive(true);
        onDoginputNotification_I=true;
    }

    private void cancelDogPopulation(){
        populationQuantBox.SetActive(false);
        dogIsCancelledNotification = true;
        useDefaultDataButton.enabled = true;
        acceptDogPopButton.enabled = true;
    }

    private void addDogPopulation(){
        populationQuantBox.SetActive(false);
        dogIsAddedNotification = true;
        useDefaultDataButton.enabled = true;
        acceptDogPopButton.enabled = true;
    }

    public float getDogPopulation(){
        float value = 0;
        if (float.TryParse(dogQuantity.text, out value)) {
            return value;
        }
        return -1;
    }

    public void setDeletableDogToContent(){
        var newDog = Instantiate(dogDeleteInContent, new Vector3(0.0f, 0.0f, 1.0f), Quaternion.identity);
        newDog.transform.SetParent(dogListContent.transform, false);
        newDog.GetComponent<RectTransform>().anchoredPosition = new Vector2(60.0f, dogDeletePosition);
        newDog.transform.GetChild(0).GetComponent<Text>().text = stringScript.getDeleteDogText() + dogContentID;
        newDog.GetComponent<Button>().onClick.AddListener( () => { destroyDogSelfContent(newDog); } );
        dogDeletePosition -= 20.0f;
        dogContentID++;
    }

    public void destroyAllDogContents(){
        int count = dogListContent.transform.childCount;
        Debug.Log("Dog number to destroy: " + count);
        for (int i = 0 ; i < count ; i++){
            dogListContent.transform.GetChild(0).gameObject.SetActive(false);
            dogListContent.transform.GetChild(0).SetParent(dumpingSite.transform, false);
        }
        dogDeletePosition = 145.0f;
    }

    private void destroyDogSelfContent(GameObject thisButton){
        int index = thisButton.transform.GetSiblingIndex();
        Debug.Log("This is deleted index : " + index);
        dogDeletePosition += 20.0f;
        deleteOneDogNotification = index;
        thisButton.transform.SetParent(dumpingSite.transform, false);
        thisButton.SetActive(false);
    }

    public void stepTheRestChildUp(){
        float yposition = 145.0f;
        for (int i = 0 ; i < dogListContent.transform.childCount ; i++){
            dogListContent.transform.GetChild(i).GetComponent<RectTransform>().anchoredPosition = new Vector2(60.0f, yposition);
            yposition -= 20.0f;
        }
    }

    public void showDogErrorInput(string text){
        errorDogInput.GetComponent<Text>().text = text;
    }

    public void showDogErrorInput_I(string text){
        errorDogInput_I.GetComponent<Text>().text = text;
    }

    private void useDefaultDogData(){
        if (!usedDefaultData){
            usedDefaultData = true;
            useDefaultDogNotification = true;
        }
    }

    private void cancelDogPopulation_I(){
        populationQuantBox_I.SetActive(false);
        dogIsCancelledNotification_I = true;
        acceptDogPopButton_I.enabled = true;
    }

    private void addDogPopulation_I(){
        populationQuantBox_I.SetActive(false);
        dogIsAddedNotification_I = true;
        acceptDogPopButton_I.enabled = true;
    }

    public int getDogPopulation_I(){
        float value = 0;
        if (float.TryParse(dogQuantity_I.text, out value)) {
            return (int)value;
        }
        return -1;
    }

    public void switchAllowInput(bool active){
        allowAddDogObject = active;
        if (active) {
            allowDogInputStatus.text = stringScript.getUnlockDogInputText();
            allowDogInputStatus.fontSize = 18;
            inputState.enabled = true;
        }
        else {
            allowDogInputStatus.text = stringScript.getLockedDogInputText();
            allowDogInputStatus.fontSize = 18;
            inputState.enabled = false;
        }
    }

    public void switchAllowInput_I(bool active){
        allowAddDogObject_I = active;
        if (active) {
            allowDogInputStatus_I.text = stringScript.getUnlockDogInputText();
            allowDogInputStatus_I.fontSize = 18;
            inputState_I.enabled = true;
        }
        else {
            allowDogInputStatus_I.text = stringScript.getLockedDogInputText();
            allowDogInputStatus_I.fontSize = 18;
            inputState_I.enabled = false;
        }
    }
    public void infecttextsetter()
    {
        if(stringScript.isThai==true)
        {
            allowDogInputStatus_I.text = "วางกลุ่มประชากรสุนัขติดเชื้อ";
         allowDogInputStatus_I.fontSize = 17;
        }
        else
        {
             allowDogInputStatus_I.text = "Infect Population Attachment";
              allowDogInputStatus_I.fontSize = 22;
        }
    }

    //UTILITY FUNCTIONS FOR PARAMETER SETTINGS
    public bool checkMapCalParamIntegrity(){
        float a;
        foreach(InputField input in mapCal){
            if (!float.TryParse(input.text, out a)) return false;
        }
        return true;
    }

    public bool checkDogBehaviorParamIntegrity(){
        float a;
        foreach(InputField input in dogBehavior){
            if (!float.TryParse(input.text, out a)) return false;
        }
        return true;
    }

    public bool checkImageGenParamIntegrity(){
        float a;
        foreach(InputField input in imageGen){
            if (!float.TryParse(input.text, out a)) return false;
        }
        return true;
    }

    private void hordeMustNotExceedOne(){
        float horde, explore;
        if (float.TryParse(imageGen[0].text, out horde) && float.TryParse(imageGen[1].text, out explore)){
            if (horde + explore > 0.9999f) {
                horde = (1.0f - explore);
                imageGen[0].text = "" + horde;
            }
            singleBehavior.text = "" + (1.0f - (horde + explore));
        }
    }

    private void exploreMustNotExceedOne(){
        float horde, explore;
        if (float.TryParse(imageGen[0].text, out horde) && float.TryParse(imageGen[1].text, out explore)){
            if (horde + explore > 0.9999f) {
                explore = (1.0f - horde);
                imageGen[1].text = "" + explore;
            }
            singleBehavior.text = "" + (1.0f - (horde + explore));
        }
    }

    private void showOrHideDeleteList(){
        if (deleteButtonText.text == stringScript.getDeleteText()){
            deleteList.SetActive(true);
            deleteButtonText.text = stringScript.getCloseText();
        } else {
            deleteList.SetActive(false);
            deleteButtonText.text = stringScript.getDeleteText();
        }
    }

    private void notifyDeleteAll(){
        deleteAllDogsNotification = true;
    }

     private void openreportfolder(){
         string path; 
         path=Application.streamingAssetsPath; 
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

    private void readInstructionTextList(){
        StreamReader reader = new StreamReader(Application.streamingAssetsPath +"/instructiontext.txt");
        string line = reader.ReadLine();
        int count = 0;
        title = new string[changeableAmount];
        instruction = new string[changeableAmount];

        while(!reader.EndOfStream){
            if (count >= changeableAmount){
                break;
            }
            string[] thisline = reader.ReadLine().Split('_');
            title[count] = thisline[0];
            instruction[count] = thisline[1];
            count++;
        }
    }

    public void notifyInstructionTextChange(int index){
        int trueindex = 0;
        if (PlayerPrefs.GetString("isThai") == "True"){
            trueindex = index - (title.Length/2);
        } else {
            trueindex = index;
        }
        for (int i = 0 ; i < instructiontext.Length ; i++){
            instructiontext[i].text = title[trueindex] + "\n" + instruction[index];
            instructiontext[i].font = stringScript.thaiFont;
            instructiontext[i].fontSize = (int)(instructiontext[i].fontSize * 0.8f);
        }
    }

    public int getInstructionIDFromString(string thistitle){
        for (int i = 0 ; i < changeableAmount ; i++){
            if (thistitle == title[i]){
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

    public void updateZoomLevel(float zoomLevel) {
        mapZoom.text = zoomLevel.ToString();
        extendZoom.text = zoomLevel.ToString();
    }
}
