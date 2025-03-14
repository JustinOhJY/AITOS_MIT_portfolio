// Required namespaces for Unity Engine functionalities and ML-Agents
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine.SceneManagement;

namespace TurnTheGameOn.SimpleTrafficSystem
{
    /// <summary>
    /// AITrafficLightManager uses ML-Agents to control traffic lights in a simulation environment,
    /// optimizing traffic flow based on dynamic conditions and interactions.
    /// </summary>
    public class AITrafficLightManager : Agent
    {
        // Public variables exposed in Unity's inspector for easy modification and setup
        public AITrafficLightCycle[] trafficLightCycles; // Array of traffic light cycles to be managed
        // Private variables for internal state management
        private float timer; // General timer for managing light transitions
        private GameObject[] carObjects; // Array of car GameObjects in the scene
        private bool yellowLight = false; // Flag to check if yellow light is active
        private bool done = false; // Flag to determine if the transition cycle is completed
        private float greenTimer = 0.5f; // Duration for which green light should stay on
        private int switchNum = 0; // Current switch case index
        private int lastSwitchNum = 0; // Last switch case index used to prevent rapid toggling
        public int rewardMult = 1; // Multiplier for the reward calculation
        public bool inProgress = false; // Flag indicating if a light switch is in progress
        private float rewardTimer = 1f; // Timer to control the frequency of reward distribution
        private float decisionTimer = 0f; // Timer before next AI decision is requested
        private float sceneTimer = 30f; // Timer to potentially reset the scene
        private bool inProgressGreen = false; // Flag to indicate green light transition
        private bool inProgressRed = false; // Flag to indicate red light transition
        public GameObject[] waypointRoute; // Array of waypoints, potentially used for navigation or other logic

        /// <summary>
        /// Start is called on the frame when a script is enabled just before any of the Update methods is called the first time.
        /// It initializes traffic lights to red and sets up timers.
        /// </summary>
        private void Start()
        {
            // Initialize traffic lights to red if they are configured
            if (trafficLightCycles.Length > 0)
            {
                // Set all traffic lights in the second cycle to red
                for (int j = 0; j < trafficLightCycles[1].trafficLights.Length; j++)
                {
                    trafficLightCycles[1].trafficLights[j].EnableRedLight();
                }
                // Set all traffic lights in the first cycle to red
                for (int i = 0; i < trafficLightCycles[0].trafficLights.Length; i++)
                {
                    trafficLightCycles[0].trafficLights[i].EnableRedLight();
                }
                // Reset the general timer
                timer = 0.0f;
            }
            else
            {
                // Log a warning and disable this component if no traffic light cycles are set
                Debug.LogWarning("There are no lights, it will be disabled.");
                enabled = false;
            }
        }

        /// <summary>
        /// Called when an episode begins in the context of ML-Agents. Used for any necessary reinitialization.
        /// </summary>
        public override void OnEpisodeBegin()
        {
            // Placeholder for episode initialization logic
        }

        /// <summary>
        /// Provides manual control over the agent's actions, useful for testing and debugging.
        /// </summary>
        /// <param name="actionsOut">The buffer to write the actions that the agent will take.</param>
        public override void Heuristic(in ActionBuffers actionsOut)
        {
            ActionSegment<int> discreteActions = actionsOut.DiscreteActions;
            // Check if the decision timer has elapsed before taking manual inputs
            if (decisionTimer > 0.0f)
            {
                // No action is taken if the timer has not elapsed
            }
            else
            {
                // Map keyboard inputs to agent actions
                if (Input.GetKey(KeyCode.S))
                {
                    discreteActions[0] = 1; // Example action for switching to a specific state
                }
                else if (Input.GetKey(KeyCode.A))
                {
                    discreteActions[0] = 2; // Another example action
                }
                else
                {
                    discreteActions[0] = 0; // Default action, possibly 'do nothing'
                }
            }
        }


        /// <summary>
        /// FixedUpdate is called every fixed framerate frame. This method handles timing operations,
        /// decisions for switching traffic lights, and managing AI rewards based on traffic conditions.
        /// </summary>
        private void FixedUpdate()
        {
            // Update timers that manage how often rewards are given and how long the scene has been active
            rewardTimer += Time.deltaTime;
            sceneTimer -= Time.deltaTime;

            // Apply a small penalty every frame to encourage faster decision-making
            AddReward((rewardMult / carObjects.Length) * -0.05f);

            // Decrement decision timer until it reaches zero, then request a new decision from the AI
            if (decisionTimer > 0.0f)
            {
                decisionTimer -= Time.deltaTime;
            }
            else
            {
                // Request a new decision from the AI model when the timer expires
                RequestDecision();

                // Process traffic light changes for the scenario where the first light cycle is active
                if ((switchNum == 1 && lastSwitchNum != 1) || inProgressGreen)
                {
                    inProgressGreen = true;  // Indicate that green light process is active
                    if (!done)
                    {
                        // Transition to yellow light if not already done
                        if (!yellowLight)
                        {
                            inProgress = true;
                            // Activate yellow lights in the first traffic cycle
                            foreach (var light in trafficLightCycles[0].trafficLights)
                            {
                                light.EnableYellowLight();
                            }
                            yellowLight = true;
                            timer = 2f; // Set timer for yellow light duration
                        }
                        else
                        {
                            // Switch to red light after yellow light timer expires
                            if (timer > 0.0f)
                            {
                                timer -= Time.deltaTime;
                            }
                            else
                            {
                                foreach (var light in trafficLightCycles[0].trafficLights)
                                {
                                    light.EnableRedLight();
                                }
                                yellowLight = false;
                                done = true;
                            }
                        }
                    }
                    else
                    {
                        // After yellow and red, activate green lights if green timer expires
                        if (greenTimer > 0.0f)
                        {
                            greenTimer -= Time.deltaTime;
                        }
                        else
                        {
                            foreach (var light in trafficLightCycles[1].trafficLights)
                            {
                                light.EnableGreenLight();
                            }
                            greenTimer = 1.5f;
                            done = false;
                            switchNum = 0;
                            lastSwitchNum = 1;
                            inProgress = false;
                            decisionTimer = 0f; // Reset decision timer after the entire cycle
                            inProgressGreen = false;
                            inProgressRed = false;
                            AddReward(-2f); // Reward the AI for successfully managing the cycle
                        }
                    }
                }

                // Similar logic for the scenario where the second light cycle is active
                if ((switchNum == 2 && lastSwitchNum != 2) || inProgressRed)
                {
                    lastSwitchNum = 1;
                    inProgressRed = true; // Indicate that red light process is active

                    if (!done)
                    {
                        // Transition to yellow light if not already done
                        if (!yellowLight)
                        {
                            inProgress = true;
                            foreach (var light in trafficLightCycles[1].trafficLights)
                            {
                                light.EnableYellowLight();
                            }
                            yellowLight = true;
                            timer = 2f; // Set timer for yellow light duration
                        }
                        else
                        {
                            // Switch to red light after yellow light timer expires
                            if (timer > 0.0f)
                            {
                                timer -= Time.deltaTime;
                            }
                            else
                            {
                                foreach (var light in trafficLightCycles[1].trafficLights)
                                {
                                    light.EnableRedLight();
                                }
                                yellowLight = false;
                                done = true;
                            }
                        }
                    }
                    else
                    {
                        // After yellow and red, activate green lights if green timer expires
                        if (greenTimer > 0.0f)
                        {
                            greenTimer -= Time.deltaTime;
                        }
                        else
                        {
                            foreach (var light in trafficLightCycles[0].trafficLights)
                            {
                                light.EnableGreenLight();
                            }
                            greenTimer = 1.5f;
                            done = false;
                            switchNum = 0;
                            lastSwitchNum = 2;
                            inProgress = false;

                            decisionTimer = 9f; // Reset decision timer after the entire cycle
                            inProgressGreen = false;
                            inProgressRed = false;
                            AddReward(-2f); // Reward the AI for successfully managing the cycle
                        }
                    }
                }
            }
        }


        /// <summary>
        /// Processes actions received from the decision-making component of the ML model.
        /// </summary>
        /// <param name="actions">Buffers containing actions the model decided to take.</param>
        public override void OnActionReceived(ActionBuffers actions)
        {
            // Avoid processing actions if the decision timer is still counting down
            if (decisionTimer > 0.0f)
            {
                return;  // Early exit if the AI should not yet act
            }

            // Interpret the discrete action from the action buffer
            switch (actions.DiscreteActions[0])
            {
                case 1:
                    switchNum = 1; // Set traffic light switch state to 1
                    break;
                case 2:
                    switchNum = 2; // Set traffic light switch state to 2
                    break;
                case 0:
                    // No action taken
                    break;
            }

            // Log the action for debugging purposes
            Debug.Log($"Action received: {actions.DiscreteActions[0]}");
        }

        /// <summary>
        /// Collects observations from the environment to be used by the ML model.
        /// </summary>
        /// <param name="sensor">The sensor object that collects observations.</param>
        public override void CollectObservations(VectorSensor sensor)
        {
            // Add the position of each car in the scene to the observations for the agent
            foreach (GameObject car in carObjects)
            {
                sensor.AddObservation(car.transform.position);
            }
        }

        /// <summary>
        /// Update is called once per frame, managing dynamic elements like car tracking and reward calculations.
        /// </summary>
        public void Update()
        {
            // Update arrays of GameObjects based on current scene state
            carObjects = GameObject.FindGameObjectsWithTag("Car");
            waypointRoute = GameObject.FindGameObjectsWithTag("waypoints");

            int currentNum = 0; // Tracks the number of active cars concerning rewards
                                // Iterate over car objects to calculate rewards based on their timers
            foreach (GameObject car in carObjects)
            {
                var carTimer = car.GetComponent<timer>();
                currentNum += carTimer.counting;
                // Give a reward if a car's timer has stopped and it hasn't been rewarded yet
                if (carTimer.counting == 0 && !carTimer.endRewards)
                {
                    AddReward(3f / rewardTimer);
                    carTimer.endRewards = true;
                }
            }

            // Update the reward multiplier based on the number of cars actively being timed
            rewardMult = currentNum;

            // If no cars are left to time, reward heavily and restart the scene
            if (rewardMult == 0)
            {
                AddReward(10f);
                Scene currentScene = SceneManager.GetActiveScene();
                SceneManager.LoadScene(currentScene.buildIndex);
                EndEpisode();  // Ends the ML agent's episode
            }

            // Restart the scene if the scene timer runs out
            if (sceneTimer <= 0)
            {
                foreach (GameObject car in carObjects)
                {
                    if (car.GetComponent<timer>().counting != 0)
                    {
                        AddReward(-1f); // Penalize for cars that are still active
                    }
                }
                Scene currentScene = SceneManager.GetActiveScene();
                SceneManager.LoadScene(currentScene.buildIndex);
                EndEpisode();  // Ends the ML agent's episode
            }
        }
    }
}

