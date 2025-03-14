// Necessary imports for Unity functionalities
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The timer class manages a timing and scoring mechanism in a Unity game environment.
/// It interacts with the logicscript class to update scores based on game events and elapsed time.
/// </summary>
public class timer : MonoBehaviour
{
    // Reference to logicscript for score management
    public logicscript logic;

    // Tracks the total time elapsed since the timer was activated
    public float timeElasped = 0f;

    // Counter to track specific game events, impacts game state
    private int count = 0;

    // Flag to control the active state of the timer
    private bool timerActive = true;

    // Flag to indicate if the timer's conditions have been met to consider it done
    public bool done = false;

    // Public integer that can be used to monitor counting state externally
    public int counting = 1;

    // Flag to control whether end game rewards are to be issued
    public bool endRewards = false;

    /// <summary>
    /// Initialization function that sets up the timer by finding and getting the logicscript component.
    /// </summary>
    void Start()
    {
        // Find the GameObject tagged as 'logic' and get its logicscript component
        logic = GameObject.FindGameObjectWithTag("logic").GetComponent<logicscript>();
    }

    /// <summary>
    /// Update is called once per frame, checks the timer state and updates scores or pauses based on game logic.
    /// </summary>
    void Update()
    {
        // If the timer is active, update the score through logicscript
        if (timerActive)
        {
            logic.addScore();
        }

        // Check if the count has reached the limit to pause the timer and mark as done
        if (count == 2)
        {
            PauseTimer();
            done = true;
            counting = 0;
        }
    }

    /// <summary>
    /// Pauses the timer by setting timerActive flag to false.
    /// </summary>
    public void PauseTimer()
    {
        timerActive = false;
    }

    /// <summary>
    /// Resumes the timer by setting timerActive flag to true.
    /// </summary>
    public void ResumeTimer()
    {
        timerActive = true;
    }

    /// <summary>
    /// Detects collisions with other objects, specifically looking for objects tagged as "Score".
    /// Increments the count upon such collisions.
    /// </summary>
    /// <param name="collideObj">The Collider of the object that triggers the event.</param>
    private void OnTriggerEnter(Collider collideObj)
    {
        // If the collided object is tagged "Score", increment the count
        if (collideObj.gameObject.CompareTag("Score"))
        {
            count += 1;
        }
    }
}
