using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Inworld;
using System.IO.Compression;
using System.Collections;
using Inworld.Interactions;

[Serializable]
public class Question
{
    public string QuestionText;
    public string[] Options;
    public int CorrectAnswerIndex;
}

public class QuizController : MonoBehaviour
{
    private StateManager stateManager;
    private UIManager uiManager;

    public List<Question> Questions;
    [NonSerialized] public int CurrentQuestionIndex = 0;

    private int score = 0;


    void Awake()
    {
        stateManager = GetComponent<StateManager>();
        uiManager = GetComponent<UIManager>();
    }

    public void ResetQuiz()
    {
        CurrentQuestionIndex = 0; // Reset the question index
        score = 0; // Reset the score

        Debug.Log("Questions: " + Questions.Count);

        ShuffleQuestions(Questions); // Optional: Shuffle the questions again

        DisplayCurrentQuestion(); // Display the first question
    }

    private void ShuffleQuestions<T>(List<T> list)
    {
        System.Random rng = new();
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            (list[n], list[k]) = (list[k], list[n]);
        }
    }
    private void DisplayCurrentQuestion()
    {
        
        if (CurrentQuestionIndex < Questions.Count)
        {
            uiManager.ResetButtonsStyle();
            Question question = Questions[CurrentQuestionIndex];

            // Display question text
            uiManager.DisplayQuestionText(question);
        }
        else
        {
            StartCoroutine(EndGame());
        }
    }


    public IEnumerator ProcessAnswer(Button selectedButton, int selectedIndex)
    {
        // Wait for 2 seconds before proceeding
        yield return new WaitForSeconds(1.5f);

        Dictionary<string, string> parameters = new();
        string currentQuestion = Questions[CurrentQuestionIndex].QuestionText;
        parameters.Add("question", currentQuestion);
        string selectedAnswer = Questions[CurrentQuestionIndex].Options[selectedIndex];
        parameters.Add("answer", selectedAnswer);

        if (selectedIndex == Questions[CurrentQuestionIndex].CorrectAnswerIndex)
        {
            score++;

            // Change the selected button's color to green
            selectedButton.GetComponent<Image>().color = ColorUtility.TryParseHtmlString("#2fbd44", out Color color) ? color : Color.white;

            InworldController.CurrentCharacter.SendTrigger("correct_answer", false, parameters);

            // Wait until the character is no longer speaking
            yield return new WaitUntil(() => !InworldController.CurrentCharacter.IsSpeaking);
            // Wait for 1 second before proceeding
            yield return new WaitForSeconds(2f);
            // Wait until the character is no longer speaking
            yield return new WaitUntil(() => !InworldController.CurrentCharacter.IsSpeaking);
        }
        else
        {
            // Change the selected button's color to red
            selectedButton.GetComponent<Image>().color = ColorUtility.TryParseHtmlString("#e33b3b", out Color color) ? color : Color.white;
            // Character should say "Incorrect answer!"
            InworldController.CurrentCharacter.SendTrigger("incorrect_answer", false, parameters);

            // Wait until the character is no longer speaking
            yield return new WaitUntil(() => !InworldController.CurrentCharacter.IsSpeaking);
            // Wait for 1 seconds before proceeding
            yield return new WaitForSeconds(2f);
            // Wait until the character is no longer speaking
            yield return new WaitUntil(() => !InworldController.CurrentCharacter.IsSpeaking);

        }

        // Now that isSpeaking is false, display the next question
        CurrentQuestionIndex++;
        DisplayCurrentQuestion();

    }

    private IEnumerator EndGame()
    {
        Debug.Log("End of the quiz");
        // Character should inform the quiz is over
        InworldController.CurrentCharacter.SendTrigger("end_quiz", false);
        
        // Display quiz score
        uiManager.DisplayQuizScore(score);

        yield return new WaitForSeconds(2.5f);
        yield return new WaitUntil(() => !InworldController.CurrentCharacter.IsSpeaking);

        stateManager.SwitchState(stateManager.talkingState);
    }



}
