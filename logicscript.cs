// Necessary imports for Unity functionalities
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The logicscript class handles score management in a Unity game.
/// It updates a score based on the time elapsed.
/// </summary>
public class logicscript : MonoBehaviour
{
    // Public variable to keep track of the timed score
    public float timedScore = 0;

    // UI Text element to display the score
    public Text scoreText;

    /// <summary>
    /// Increases the score based on the time elapsed since the last frame.
    /// This method is intended to be called once per frame.
    /// </summary>
    [ContextMenu("Increase Score")]
    public void addScore()
    {
        // Increase timedScore by the time in seconds it took to complete the last frame
        timedScore += Time.deltaTime;
    }
}
