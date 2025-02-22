using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;


public class UIController : MonoBehaviour
{
    public GameObject blackSquare;
    public UnitSelectionWindow unitSelectionWindow;
    public GameObject unitSelectionTutorial;
    public UnitInfoWindow unitInfoWindow;
    public TileInfoWindow tileInfoWindow;
    public EndTurnButton endTurnButton;
    public GameObject endTurnWarning;
    public TurnCountDown turnCountDown;
    public Playable playerTurnPopup;
    public Playable enemyTurnPopup;
    public bool isTutorial;
    public new AudioComponent audio;
    [SerializeField]
    private SoundEffect clickSound;
    public Unit unitWhoseWindowIsOpen;

    /// <summary>
    /// Displays the given tile data in the info window
    /// </summary>
    /// <param name="tileData">the tile data to display. Will do nothing if null</param>
    public void ShowTileInWindow(HexTileData tileData)
    {
        HideUnitInfoWindow();
        tileInfoWindow.ShowTile(tileData);
    }

    /// <summary>
    /// Hides the tile info window if visible
    /// </summary>
    public void HideTileWindow()
    {
        if (tileInfoWindow)
        {
            tileInfoWindow.Hide();
        }
    }

    /// <summary>
    /// Initializes the turn count down to the given value
    /// </summary>
    /// <param name="turns">the number of turns to set the turn counter to</param>
    public void InitializeTurnCount(int turns)
    {
        turnCountDown.Initialize(turns, LevelManager.currentLevel > LevelManager.instance.totalLevels - LevelManager.instance.BossLevels);
    }

    /// <summary>
    /// Indicates whether the battle is over due to the turn count down
    /// </summary>
    /// <returns>returns true if the turn count down is 0 or less, false otherwise</returns>
    public bool isOutOfTurns()
    {
        return turnCountDown && turnCountDown.currentTurn <= 0;
    }

    /// <summary>
    /// Decreases the turn count down by 1
    /// </summary>
    public void DecrementTurnCount()
    {
        turnCountDown.Decrement();
    }

    public void DecrementBoss()
    {
        turnCountDown.DecrementBoss();
    }

    public IEnumerator ShowPlayerTurnAnim()
    {
        if (playerTurnPopup == null)
        {
            yield break;
        }
        yield return StartCoroutine(playerTurnPopup.Play());
    }

    public IEnumerator ShowEnemyTurnAnim()
    {
        if (enemyTurnPopup == null)
        {
            yield break;
        }
        yield return StartCoroutine(enemyTurnPopup.Play());
    }

    /// <summary>
    /// Disables the end turn button so it cannot be interacted with
    /// </summary>
    /// <returns>a coroutine representing the disabling animation</returns>
    public IEnumerator DisableEndTurnButton()
    {
        StartCoroutine(SetEndTurnButtonHighlight(false));
        Debug.Log("DISABLING");
        endTurnButton.SetInteractable(false);
        yield break;
    }

    /// <summary>
    /// Enables the end turn button so it can be interacted with
    /// </summary>
    /// <returns>a coroutine representing the enabling animation</returns>
    public IEnumerator EnableEndTurnButton()
    {
        endTurnButton.SetInteractable(true);
        yield break;
    }

    /// <summary>
    /// Sets whether or not the end turn button is highlighted
    /// </summary>
    /// <param name="highlighted">determines whether or not the button is highlighted</param>
    /// <returns>a coroutine representing the highlight/de-highlight animation</returns>
    public IEnumerator SetEndTurnButtonHighlight(bool highlighted)
    {
        return endTurnButton.SetHighlighted(highlighted);
    }

    /// <summary>
    /// Displays the warning for ending your turn early
    /// </summary>
    /// <returns>a coroutine representing the animation for the warning to appear</returns>
    public IEnumerator ShowEarlyEndTurnWarning()
    {
        endTurnWarning.SetActive(true);
        yield break;
    }

    public IEnumerator SwitchScene(int index, int fadeSpeed = 2)
    {
        Color goColor = blackSquare.GetComponent<Image>().color;
        float newA = 0;

        while (blackSquare.GetComponent<Image>().color.a < 1)
        {
            newA = goColor.a + (fadeSpeed * Time.deltaTime);
            goColor = new Color (goColor.r, goColor.g, goColor.b, newA);
            blackSquare.GetComponent<Image>().color = goColor;
            yield return null;
        }
        
        SceneManager.LoadScene(index);
    }

    public IEnumerator FadeOut(int fadeSpeed = 2)
    {
        Color goColor = blackSquare.GetComponent<Image>().color;
        float newA = 0;
        while (blackSquare.GetComponent<Image>().color.a < 1)
        {
            newA = goColor.a + (fadeSpeed * Time.deltaTime);
            goColor = new Color (goColor.r, goColor.g, goColor.b, newA);
            blackSquare.GetComponent<Image>().color = goColor;
            yield return null;
        }        
    }

    public IEnumerator FadeIn(int fadeSpeed = 2)
    {
        Color goColor = blackSquare.GetComponent<Image>().color;
        float newA = 0;
        while (blackSquare.GetComponent<Image>().color.a > 0)
        {
            newA = goColor.a - (fadeSpeed * Time.deltaTime);
            goColor = new Color (goColor.r, goColor.g, goColor.b, newA);
            blackSquare.GetComponent<Image>().color = goColor;
            yield return null;
        }          
    }

    public IEnumerator SwitchScene(string sceneName, int fadeSpeed = 2)
    {
        Color goColor = blackSquare.GetComponent<Image>().color;
        float newA = 0;

        while (blackSquare.GetComponent<Image>().color.a < 1)
        {
            newA = goColor.a + (fadeSpeed * Time.deltaTime);
            goColor = new Color (goColor.r, goColor.g, goColor.b, newA);
            blackSquare.GetComponent<Image>().color = goColor;
            yield return null;
        }
        SceneManager.LoadScene(sceneName);
    }

    public void ShowUnitInfoWindow(Unit unit)
    {
        HideTileWindow();
        unitInfoWindow.ShowStats(unit);
        unitWhoseWindowIsOpen = unit;
    }

    /// <summary>
    /// Hides the unit info window.
    /// Also hides the tile info window.
    /// </summary>
    public void HideUnitInfoWindow()
    {
        unitInfoWindow.HideStats();
        if (tileInfoWindow)
        {
            tileInfoWindow.Hide();
        }
        unitWhoseWindowIsOpen = null;
    }

    public void StartButton()
    {
        StartCoroutine(SwitchScene(SceneManager.GetActiveScene().buildIndex + 1, 2));
    }

    public void PlayAgainButton()
    {
        StartCoroutine(SwitchScene("StartMenu"));
    }

    public IEnumerator ShowSelectionWindow(bool random = true, bool tutorial = false)
    {
        Debug.Log("Showing window");
        return unitSelectionWindow.Show(random, tutorial);
    }

    public IEnumerator ShowSelectionTutorial()
    {

        yield break;
    }

    public IEnumerator HideSelectionWindow()
    {
        return unitSelectionWindow.Hide();
    }

    public void PlayClickSound()
    {
        audio.PlaySound(clickSound);
    }

}
