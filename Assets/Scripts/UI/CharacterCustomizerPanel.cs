using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using NSMB.Utils;
using Photon.Pun;

using Hashtable = ExitGames.Client.Photon.Hashtable;

public class CharacterCustomizerPanel : MonoBehaviour
{
    [Header("Character Changer")]
    [SerializeField] private GameObject playerModelPreview;
    public Material characterMaterial;
    [SerializeField] private ScrollRect characterList;
    [SerializeField] private GameObject characterItemPrefab;

    [Header("Character Colors")]
    [SerializeField] public GameObject characterpanel;
    [SerializeField] public Image previewShirt;
    [SerializeField] public Image previewOveralls;
    [SerializeField] public GameObject ColorsPanel;

    [SerializeField] private GameObject colorItemPrefab, colorListGameobject;
    [SerializeField] private Sprite clearSprite;

    [Header("Current")]
    [Header("Model")]
    public PlayerData playerCurrent;
    [SerializeField] private GameObject currentPlayerModel;
    [SerializeField] private int currentLoadedCharacter;
    [Header("Colors")]
    public PlayerColorSet playerColorCurrent;
    [SerializeField] private int currentCharacterColor;

    void Start()
    {
        this.gameObject.SetActive(false);
        currentLoadedCharacter = -1;
        UpdateData();
        //add character options and
        //color options to the menu
        AddCharacterOptions();
        AddColorOptions();

        CheckCharacterPreview();
        SetCharacterColors();


    }

    private void UpdateData()
    {
        PlayerColorSet[] colors = GlobalController.Instance.skins;
        PlayerData[] characters = GlobalController.Instance.characters;

        PlayerColorSet color = colors[Settings.Instance.skin];
        PlayerData character = characters[Settings.Instance.character];

        currentCharacterColor = Settings.Instance.skin;
        playerColorCurrent = color;
        playerCurrent = character;
    }



    private List<ColorButton> colorButtons = new();
    private List<Button> buttons;
    private List<Navigation> navigations;

    public void CheckCharacterPreview()
    {
        int currentChar = PlayerPrefs.GetInt("Character", 0);
        if (currentLoadedCharacter == -1)
        {
            Debug.Log("Character not loaded! loading now");
            ChangeCharacterPreview(currentChar);
            currentLoadedCharacter = currentChar;

        }

        if (currentLoadedCharacter != PlayerPrefs.GetInt("Character", 0))
        {
            Debug.Log("Changing Model!");
            ChangeCharacterPreview(currentChar);
            currentLoadedCharacter = currentChar;
        }
    }

    private void ChangeCharacterPreview(int loadCharIndex)
    {
        if(currentPlayerModel != null)
        {
            Debug.Log("Destroying old model!");
            Destroy(currentPlayerModel);

        }
        PlayerData[] characters = GlobalController.Instance.characters;

        //get player model prefab
        var charModel = Resources.Load("Prefabs/" + characters[loadCharIndex].prefab);
        //and load it
        GameObject model = (GameObject)Instantiate(charModel, playerModelPreview.transform);
        currentPlayerModel = model;

        //steal transforms before we destroy it
        var AnimControl = model.GetComponent<PlayerAnimationController>();
        GameObject largeModel = AnimControl.largeModel;
        GameObject smolModel = AnimControl.smallModel;
        GameObject shellModel = AnimControl.blueShell;
        GameObject propellerHat = AnimControl.propellerHelmet;
        GameObject propeller = AnimControl.propeller;

        var components = model.GetComponents(typeof(Component));
        //Destroy everything but Transform and Animator
        Destroy(model.GetComponent<PlayerController>());
        foreach (var comp in components)
        {
            if (!(comp is Transform))
            {
                if (!(comp is Animator))
                {
                    Destroy(comp);
                }
            }
        }

        largeModel.SetActive(true);
        smolModel.SetActive(false);
        shellModel.SetActive(false);
        propellerHat.SetActive(false);
        propeller.SetActive(false);
        model.GetComponent<Animator>().SetBool("onGround", true);
        model.GetComponent<Animator>().Play("idle", -1, 0f);


        UpdateData();
    }

    public void SetCharacterColors()
    {
        PlayerColors colors = GlobalController.Instance.skins[currentCharacterColor].GetPlayerColors(Utils.GetCharacterData());
        characterMaterial = playerCurrent.BigShirtMaterial;

        characterMaterial.SetColor("OverallsColor", colors.overallsColor.linear);
        characterMaterial.SetColor("ShirtColor", colors.hatColor.linear);
    }

    public void AddCharacterOptions()
    {
        PlayerData[] characters = GlobalController.Instance.characters;

        for (int i = 0; i < characters.Length; i++)
        {
            PlayerData currentChar = characters[i];

            GameObject newbutton = Instantiate(characterItemPrefab, characterList.content.transform);

            newbutton.SetActive(true);

            CharacterButton cb = newbutton.GetComponent<CharacterButton>();

            cb.characterIconImage.sprite = currentChar.readySprite;
            cb.characterNumber = i;
            cb.characterData = currentChar;
        }
    }


    public void AddColorOptions()
    {
        buttons = new();
        navigations = new();

        PlayerColorSet[] colors = GlobalController.Instance.skins;

        for (int i = 0; i < colors.Length; i++)
        {
            PlayerColorSet color = colors[i];

            GameObject newButton = Instantiate(colorItemPrefab, colorListGameobject.transform);
            ColorButton cb = newButton.GetComponent<ColorButton>();
            colorButtons.Add(cb);
            cb.palette = color;

            Button b = newButton.GetComponent<Button>();
            newButton.name = color?.name ?? "Reset";
            if (color == null)
                b.image.sprite = clearSprite;

            newButton.SetActive(true);
            buttons.Add(b);

            Navigation navigation = new() { mode = Navigation.Mode.Explicit };

            if (i > 0 && i % 4 != 0)
            {
                Navigation n = navigations[i - 1];
                n.selectOnRight = b;
                navigations[i - 1] = n;
                navigation.selectOnLeft = buttons[i - 1];
            }
            if (i >= 4)
            {
                Navigation n = navigations[i - 4];
                n.selectOnDown = b;
                navigations[i - 4] = n;
                navigation.selectOnUp = buttons[i - 4];
            }

            navigations.Add(navigation);
        }

        for (int i = 0; i < buttons.Count; i++)
        {
            buttons[i].navigation = navigations[i];
        }

        ChangeCharacterColor(Utils.GetCharacterData());
    }
    public void ChangeCharacterColor(PlayerData data)
    {
        foreach (ColorButton b in colorButtons)
            b.Instantiate(data);
    }

    private int selected;
    public void SelectColor(Button button)
    {
        selected = buttons.IndexOf(button);
        MainMenuManager.Instance.SetPlayerColor(buttons.IndexOf(button));
        UpdateData();
        SetCharacterColors();
    }


    public void SelectCharacter(CharacterButton selectedInfo)
    {
        selected = selectedInfo.characterNumber;

        Hashtable prop = new()
        {
            { Enums.NetPlayerProperties.Character, selected }
        };
        PhotonNetwork.LocalPlayer.SetCustomProperties(prop);
        Settings.Instance.character = selectedInfo.characterNumber;
        Settings.Instance.SaveSettingsToPreferences();

        PlayerData data = GlobalController.Instance.characters[selected];
        MainMenuManager.Instance.sfx.PlayOneShot(Enums.Sounds.Player_Voice_Selected.GetClip(data));
        UpdateData();
        CheckCharacterPreview();
    }

    public void ShowColors()
    {
        characterpanel.SetActive(false);
        ColorsPanel.SetActive(true);
        colorListGameobject.SetActive(true);
    }
    public void ShowCharacters()
    {
        characterpanel.SetActive(true);
        ColorsPanel.SetActive(false);
        colorListGameobject.SetActive(false);
    }
}
