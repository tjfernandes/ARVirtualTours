using System.Collections;
using System.Collections.Generic;
using Inworld;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;
using UnityEngine.EventSystems;

public class UIManager : MonoBehaviour
{
    private MainController mainController;
    private StateManager stateManager;
    private QuizController quizController;

    #region UI elements
        public GameObject UICanvas;
        public GameObject bubbleRight;
        public AudioClip buttonClickAudio;

        private Button askButton;
        private GameObject bubbleChatPanel;
        private TMP_InputField chatInputField;
        private Button speakButton;
        private Sprite speakButtonSprite;
        private GameObject quizPanel;
        private Button quizButton;
        private Button stopQuizButton;
        private GameObject finalScore;
        private GameObject confirmation;
        private Button yesButton;
        private Button noButton;


    #endregion

    void Awake()
    {
        // Get the main controller
        mainController = GetComponent<MainController>();

        // Get the state manager
        stateManager = GetComponent<StateManager>();

        // Get the quiz controller
        quizController = GetComponent<QuizController>();

        // UI elements
        speakButton = UICanvas.transform.Find("SpeakButton").GetComponent<Button>();
        quizPanel = UICanvas.transform.Find("QuizPanel").gameObject;

        askButton = UICanvas.transform.Find("AskButton").GetComponent<Button>();
        chatInputField = UICanvas.transform.Find("Chat").GetComponent<TMP_InputField>();
        quizButton = UICanvas.transform.Find("QuizButton").GetComponent<Button>();
        stopQuizButton = UICanvas.transform.Find("StopQuizButton").GetComponent<Button>();

        finalScore = quizPanel.transform.Find("FinalScore").gameObject;
        confirmation = quizPanel.transform.Find("ConfirmationQuit").gameObject;

        yesButton = confirmation.transform.Find("YesButton").GetComponent<Button>();
        noButton = confirmation.transform.Find("NoButton").GetComponent<Button>();

        AddButtonClickAudio(new List<GameObject> { askButton.gameObject, quizButton.gameObject, stopQuizButton.gameObject, yesButton.gameObject, noButton.gameObject });

        askButton.onClick.AddListener(() => chatInputField.Select());
        chatInputField.onSubmit.AddListener(HandleTextSubmitted);


        // Quiz event management
        if (this.GetComponent<QuizController>().Questions.Count > 0)
        {
            quizButton.onClick.AddListener(() => InworldController.CurrentCharacter.SendTrigger("start_game", false));
            stopQuizButton.onClick.AddListener(() => InworldController.CurrentCharacter.SendTrigger("forfeit_game", false));
        }
        else
        {
            quizButton.gameObject.GetComponent<Button>().interactable = false;
        }
        

        speakButton.onClick.AddListener(OnSpeakButtonClicked);

        // Confirmation event management 
        yesButton.onClick.AddListener(OnYesButtonClicked);
        noButton.onClick.AddListener(OnNoButtonClicked);
    }

    void AddButtonClickAudio(List<GameObject> uiElements = null)
    {
        foreach (GameObject uiElement in uiElements)
        {
            AudioSource audioSource = uiElement.AddComponent<AudioSource>();
            audioSource.clip = buttonClickAudio;
            audioSource.playOnAwake = false;
            audioSource.loop = false;

            Button button = uiElement.GetComponent<Button>();
            button.onClick.AddListener(() => audioSource.Play());
        }
    }
    
    void Start()
    {
        Image speakButtonImage = speakButton.GetComponent<Image>();
        speakButtonSprite = speakButtonImage.sprite;
    }

    #region UI Event Listeners

        private void HandleTextSubmitted(string text)
        {
            Debug.Log("Text submitted: " + text);   
            if (!string.IsNullOrEmpty(text))
            {
                if (InworldController.Instance != null)
                {
                    if (text.StartsWith("*"))
                        InworldController.Instance.SendNarrativeAction(text.Remove(0, 1));
                    else
                    {
                        // Get the bubble chat panel from character children
                        GameObject guide = GameObject.FindWithTag("guide");
                        Debug.Log("Guide: " + guide);

                        bubbleChatPanel = guide.transform.Find("Canvas/ChatScreen/Anchor/Scroll Rect/Panel").gameObject;

                        Debug.Log("Bubble chat panel: " + bubbleChatPanel);

                        GameObject bubble = Instantiate(bubbleRight, bubbleChatPanel.transform);
                        
                        bubble.transform.Find("TxtName").GetComponent<TextMeshProUGUI>().text = "You";
                        bubble.transform.Find("TxtData").GetComponent<TextMeshProUGUI>().text = text;

                        // Send the text to the inworld controller
                        InworldController.Instance.SendText(text);
                    }
                }
                else
                {
                    Debug.LogError("InworldController.Instance sis null");
                }
                //chatInputField.text = ""; // Clear the keyboard text
            }    
        }

        private void OnSpeakButtonClicked()
        {
            Debug.Log("Speak button clicked");

            if (!speakButton.gameObject.activeSelf)
            {
                return;
            }

            // Play microphone activated sound
            AudioSource audioSource = speakButton.GetComponent<AudioSource>();
            audioSource.Play();

            // Toggle the audio capture
            Animator speakButtonAnimator = speakButton.GetComponent<Animator>();
            Image speakButtonImage = speakButton.GetComponent<Image>();
            if (!mainController.audioCapture.activeSelf)
            {
                mainController.OnPlayerStartSpeaking();            
                speakButtonAnimator.enabled = true;
            }
            else
            {
                mainController.OnPlayerStopSpeaking();
                speakButtonAnimator.enabled = false;
                speakButtonImage.sprite = speakButtonSprite;
            }
        }

        private void OnYesButtonClicked()
        {
            InworldController.CurrentCharacter.SendTrigger("forfeit_confirm", true);
        }

        private void OnNoButtonClicked()
        {
            GameObject confirmation = quizPanel.transform.Find("ConfirmationQuit").gameObject;

            foreach (Transform child in quizPanel.transform) {
                child.gameObject.SetActive(true);
            }

            confirmation.SetActive(false);

            GameObject finalScore = quizPanel.transform.Find("FinalScore").gameObject;
            finalScore.SetActive(false);

            InworldController.CurrentCharacter.SendTrigger("forfeit_declined", true);
        }

        public void DeactivateChatComponents()
        {
            if (askButton != null) askButton.interactable = false;
            if (speakButton != null)
            {
                speakButton.interactable = false;  
                Animator speakButtonAnimator = speakButton.GetComponent<Animator>();
                Image speakButtonImage = speakButton.GetComponent<Image>();
                speakButtonAnimator.enabled = false;
                speakButtonImage.sprite = speakButtonSprite;
            }
            if (quizButton != null) quizButton.interactable = false;
            if (stopQuizButton != null) stopQuizButton.interactable = false;
        }

        public void ActivateChatComponents()
        {
            if (askButton != null) askButton.interactable = true;
            if (speakButton != null) speakButton.interactable = true;
            if (quizButton != null && stateManager.currentState != stateManager.gameState && this.GetComponent<QuizController>().Questions.Count > 0) quizButton.interactable = true;
            if (stopQuizButton != null && stateManager.currentState == stateManager.gameState) stopQuizButton.interactable = true;
        }

    #endregion

    public void AskEndQuizConfirmation()
    {
        foreach (Transform child in quizPanel.transform) {
            child.gameObject.SetActive(false);
        }

        // Activate the confirmation GameObject
        confirmation.SetActive(true);
    }

    public void ConfirmEndQuiz()
    {
        quizPanel.SetActive(false);
    }

    public void ChangeToQuizMode()
    {
        quizButton.gameObject.SetActive(false);
        stopQuizButton.gameObject.SetActive(true);

        // Make the quiz panel visible
        quizPanel.SetActive(true);

        foreach (Transform child in quizPanel.transform) {
            child.gameObject.SetActive(true);
        }

        GameObject confirmation = quizPanel.transform.Find("ConfirmationQuit").gameObject;

        confirmation.SetActive(false);
        finalScore.SetActive(false);
    }

    public void ExitQuizMode()
    {
        // Make the quiz panel invisible
        quizPanel.SetActive(false);
        quizButton.gameObject.SetActive(true);
        quizButton.interactable = true;
        stopQuizButton.gameObject.SetActive(false);
    }

    public void DisplayQuestionText(Question question)
    {
        GameObject questionPanel = quizPanel.transform.Find("QuestionPanel").gameObject;
        TextMeshProUGUI questionText = questionPanel.transform.Find("QuestionText").GetComponent<TextMeshProUGUI>();
        questionText.text = "Question:\n\n" + question.QuestionText;

        // Diplay options
        for (int i = 0; i < question.Options.Length; i++)
        {
            Button optionButton = quizPanel.transform.Find("Option" + (i+1)).gameObject.GetComponent<Button>();
            TextMeshProUGUI optionText = optionButton.transform.Find("Text").GetComponent<TextMeshProUGUI>();
            optionText.text = question.Options[i];

            // Assign click listener
            optionButton.onClick.RemoveAllListeners(); // Remove existing listeners to avoid stacking
            optionButton.onClick.AddListener(() => OnOptionSelected(optionButton));
        }
    }

    private void OnOptionSelected(Button selectedButton)
    {
        int selectedIndex = -1;
        Button selectedOptionButton = null;
        // Iterate through all buttons and reset their colors
        for (int i = 0; i < quizController.Questions[quizController.CurrentQuestionIndex].Options.Length; i++)
        {
            Button optionButton = quizPanel.transform.Find("Option" + (i+1)).gameObject.GetComponent<Button>();
            if (optionButton == selectedButton)
            {
                selectedIndex = i;
                selectedOptionButton = optionButton;
                // Change the selected button's color to yellow
                optionButton.GetComponent<Image>().color = ColorUtility.TryParseHtmlString("#e5eb50", out Color color) ? color : Color.white;
                optionButton.onClick.RemoveAllListeners(); // Remove the listener to avoid multiple clicks
            }
            else
            {
                // Reset other buttons to their default color (assuming white here)
                optionButton.interactable = false;
            }
        }

        StartCoroutine(quizController.ProcessAnswer(selectedButton, selectedIndex));
    }

    public void DisplayQuizScore(int score)
    {
        // Hide the question panel and the options
        GameObject questionPanel = quizPanel.transform.Find("QuestionPanel").gameObject;
        questionPanel.SetActive(false);
        for (int i = 0; i < 4; i++)
        {
            Button optionButton = quizPanel.transform.Find("Option" + (i+1)).gameObject.GetComponent<Button>();
            optionButton.gameObject.SetActive(false);
        }

        // Display the final score
        GameObject scorePanel = quizPanel.transform.Find("FinalScore").gameObject;
        TextMeshProUGUI scoreText = scorePanel.transform.Find("Text").GetComponent<TextMeshProUGUI>();
        scoreText.text = "Final Score: " + score + "/" + quizController.Questions.Count;

        scorePanel.SetActive(true);

    }


    public void ResetButtonsStyle()
    {
        for (int i = 0; i < quizController.Questions[quizController.CurrentQuestionIndex].Options.Length; i++)
        {
            Button optionButton = quizPanel.transform.Find("Option" + (i+1)).gameObject.GetComponent<Button>();
            optionButton.GetComponent<Image>().color = Color.white;
            optionButton.interactable = true;
        }
    }
}
