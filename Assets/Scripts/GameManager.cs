// ==========================================================================
//  Author: B.N. Berrevoets (bert)
//  Created: 21/11/2020
// ==========================================================================

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public delegate void AsteroidHandler();

    // Used to track the state of the game
    public enum GameState
    {
        Intro,
        Playing,
        GameOver
    }

    public static int playerScore = 0;

    public static GameState GameStatus;

    [Header("Game Structure Events")]
    public UnityEvent onStartActivated;

    public UnityEvent onGameOver;
    public UnityEvent onGameReset;

    [Header("The Time Slider Components")]
    public Image sliderImg;

    public float gameDuration;

    [Header("Asteroids Bin")]
    public GameObject spawnedAsteroids;

    private float sliderCurrentFillAmount = 1;

    private void Start()
    {
        GameStatus = GameState.Intro;
    }

    private void Update()
    {
        if (GameStatus == GameState.Playing)
        {
            sliderCurrentFillAmount -= (Time.deltaTime / gameDuration);
            sliderImg.fillAmount    =  sliderCurrentFillAmount;

            if (sliderCurrentFillAmount <= 0)
            {
                GameOver();
            }
        }
    }

    private void GameOver()
    {
        GameStatus = GameState.GameOver;

        // clear all asteroids
        spawnedAsteroids.transform.Clear();

        onGameOver?.Invoke();
    }

    public static event AsteroidHandler AsteroidDestroyed;

    public static void AsteroidHit(int scoreBonus)
    {
        if (GameStatus == GameState.Playing)
        {
            playerScore += scoreBonus;
            AsteroidDestroyed?.Invoke();
        }
        else
        {
            Debug.Log("Not in Play Mode");
        }
    }

    public void StartGame()
    {
        sliderCurrentFillAmount = 1.0f;
        GameStatus              = GameState.Playing;
        onStartActivated?.Invoke();
    }

    public void ResetGame()
    {
        playerScore             = 0;
        sliderCurrentFillAmount = 1.0f;
        GameStatus              = GameState.Intro;
        onGameReset?.Invoke();
    }
}
