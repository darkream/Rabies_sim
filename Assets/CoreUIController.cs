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
    private int screenMode = 1;
    public bool mapIsLocked = false;
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
    public Button imageGenNext;
    public Text errorImageGenNotNumber;
    public bool resetImageGenParameter = false;
    public bool runProgramNotification = false;

    public int getScreenMode(){
        return screenMode;
    }

    public void setScreenMode(int mode){
        screenMode = mode;
    }

    //CALL THIS FUNCTION TO INITIATE PROCESS
    public void setupActivation(){
        mapSelectionButton.GetComponent<Button>().onClick.AddListener(moveToAddNormalDog);
        cancelDogButton.GetComponent<Button>().onClick.AddListener(cancelDogPopulation);
        useDefaultDataButton.GetComponent<Button>().onClick.AddListener(useDefaultDogData);
        addDogButton.GetComponent<Button>().onClick.AddListener(addDogPopulation);
        acceptDogPopButton.GetComponent<Button>().onClick.AddListener(moveToChangeParameters2);
        mapCalNext.GetComponent<Button>().onClick.AddListener(moveToMapSelection);
        showWorldAdvancedSetting.GetComponent<Button>().onClick.AddListener(showOrHideWorldAdvancedSetting);
        dogBehaviorNext.GetComponent<Button>().onClick.AddListener(moveToChangeParameters3);
        imageGenNext.GetComponent<Button>().onClick.AddListener(moveToRunProgram);
        deleteButton.GetComponent<Button>().onClick.AddListener(showOrHideDeleteList);
        deleteAllButton.GetComponent<Button>().onClick.AddListener(notifyDeleteAll);
        imageGen[0].onValueChanged.AddListener(delegate {hordeMustNotExceedOne();});
        imageGen[1].onValueChanged.AddListener(delegate {exploreMustNotExceedOne();});
        moveToChangeParameters();
    }

    //TO HIDE ALL UI BEFORE PLACING ANOTHER ONE
    private void hideAllScreens(){
        mapSelection.SetActive(false);
        populationQuantBox.SetActive(false);
        addNormalDogScreen.SetActive(false);
        paramSetMapCalScreen.SetActive(false);
        paramSetDogBehaviorScreen.SetActive(false);
        paramSetImageGenParameter.SetActive(false);
    }

    //LIST OF "MOVE TO" FUNCTION
    public void moveToChangeParameters(){
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
            errorMapCalNotNumber.text = "*some values are not number*";
        }
    }

    private void moveToAddNormalDog(){
        hideAllScreens();
        allowAddDogObject = true;
        mapIsLocked = true;
        addNormalDogScreen.SetActive(true);
    }

    private void moveToChangeParameters2(){
        allowAddDogObject = false;
        switchAllowInput(false);
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
            errorDogBehaviorNotNumber.text = "*some values are not number*";
        }
    }

    private void moveToRunProgram(){
        if (checkImageGenParamIntegrity()){
            hideAllScreens();
            runProgramNotification = true;
        } else {
            errorImageGenNotNumber.text = "*some values are not number*";
        }
    }

    //OTHER UTILITY FUNCTIONS
    private void showOrHideWorldAdvancedSetting(){
        if (worldAdvancedSettingText.text == "Show World Advanced Setting"){
            resetWorldAdvancedSetting("Hide World Advanced Setting", true);
        } else {
            resetWorldAdvancedSetting("Show World Advanced Setting", false);
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
        showDogErrorInput("");
        populationQuantBox.SetActive(true);
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
        newDog.transform.GetChild(0).GetComponent<Text>().text = "delete dog " + dogContentID;
        newDog.GetComponent<Button>().onClick.AddListener( () => { destroyDogSelfContent(newDog); } );
        dogDeletePosition -= 20.0f;
        dogContentID++;
    }

    public void destroyAllDogContents(){
        int count = dogListContent.transform.childCount;
        for (int i = 0 ; i < count ; i++){
            dogListContent.transform.GetChild(0).SetParent(dumpingSite.transform, false);
            dogListContent.transform.GetChild(0).gameObject.SetActive(false);
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

    private void useDefaultDogData(){
        if (!usedDefaultData){
            usedDefaultData = true;
            useDefaultDogNotification = true;
        }
    }

    public void switchAllowInput(bool active){
        allowAddDogObject = active;
        if (active) {
            allowDogInputStatus.text = "Unlocked: you can add dog location";
            allowDogInputStatus.fontSize = 18;
            inputState.enabled = true;
        }
        else {
            allowDogInputStatus.text = "Locked: dog is not allowed to be added";
            allowDogInputStatus.fontSize = 18;
            inputState.enabled = false;
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
        if (deleteButtonText.text == "DELETE"){
            deleteList.SetActive(true);
            deleteButtonText.text = "CLOSE";
        } else {
            deleteList.SetActive(false);
            deleteButtonText.text = "DELETE";
        }
    }

    private void notifyDeleteAll(){
        deleteAllDogsNotification = true;
    }

    private void notifyDeleteAt(int id){

    }
}
