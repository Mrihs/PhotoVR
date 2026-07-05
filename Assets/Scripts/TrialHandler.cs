using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrialHandler : MonoBehaviour
{
    // Stores the two left-side elevator door parts.
    public Transform[] leftDoorParts;

    // Stores the two right-side elevator door parts.
    public Transform[] rightDoorParts;

    // Defines how far each left door part moves when opening.
    public Vector3 leftDoorOpenOffset = new Vector3(-1.2f, 0f, 0f);

    // Defines how far each right door part moves when opening.
    public Vector3 rightDoorOpenOffset = new Vector3(1.2f, 0f, 0f);

    // Defines how long the doors need to open or close.
    public float doorMoveDuration = 2f;

    // Detects when the participant has moved far enough out of the elevator.
    public TrialTrigger exitLiftTrigger;

    // Detects when the participant has entered the elevator again.
    public TrialTrigger enterLiftTrigger;

    // Stores all room lights that should fade in and out.
    public List<Light> roomLights = new List<Light>();

    // Defines the maximum brightness of the room lights.
    public float maxLightIntensity = 1.5f;

    // Defines how long the light fade takes.
    public float lightFadeDuration = 2f;

    // Defines how long the lights stay on.
    public float lightsOnDuration = 5f;

    // Defines the left object spawn position.
    public Transform leftSpawnPoint;

    // Defines the right object spawn position.
    public Transform rightSpawnPoint;

    // Stores all possible object prefabs for the trial.
    public List<GameObject> objectPrefabs = new List<GameObject>();

    // Stores the material whose texture is changed between trials.
    public Material imageMaterial;

    // Stores all possible image textures.
    public List<Texture2D> trialImages = new List<Texture2D>();

    // Defines how long the participant must stay in the elevator before the trial ends.
    public float timeInLiftBeforeTrialEnds = 2f;

    // Define time between Trials
    public float timeBetweenTrials = 1f;


    // Stores the closed local positions of all left door parts.
    private Vector3[] leftDoorClosedPositions;

    // Stores the closed local positions of all right door parts.
    private Vector3[] rightDoorClosedPositions;

    // Stores the currently spawned object on the left side.
    private GameObject currentLeftObject;

    // Stores the currently spawned object on the right side.
    private GameObject currentRightObject;

    // Stores whether a trial is currently running.
    private bool trialRunning;

    private void Awake()
    {
        // Stores the initial closed positions of all door parts.
        StoreClosedDoorPositions();

        // Turns all room lights off immediately.
        SetLightsInstant(0f);

        // Makes sure the doors start closed.
        CloseDoorsInstant();
    }


    private void Start()
    {
        Debug.Log("TrialHandler running on: " + gameObject.name);

        Debug.Log("Exit trigger assigned: " + 
            (exitLiftTrigger != null ? exitLiftTrigger.gameObject.name : "NULL"));

        Debug.Log("Enter trigger assigned: " + 
            (enterLiftTrigger != null ? enterLiftTrigger.gameObject.name : "NULL"));

        StartTrial();
    }


    public void StartTrial()
    {
        // Prevents starting a second trial while one is already running.
        if (trialRunning)
            return;

        // Starts the trial sequence.
        StartCoroutine(TrialRoutine());
    }

    private IEnumerator TrialRoutine()
    {
        // Marks the trial as running.
        trialRunning = true;

        // Randomizes the objects and image while the participant is in the elevator.
        SetupRandomTrialStimuli();

        if (exitLiftTrigger == null)
        {
            Debug.LogError("ExitLiftTrigger is not assigned in the TrialHandler Inspector.");
            yield break;
        }

        if (enterLiftTrigger == null)
        {
            Debug.LogError("EnterLiftTrigger is not assigned in the TrialHandler Inspector.");
            yield break;
        }
        // Resets the exit trigger.
        exitLiftTrigger.ResetTrigger();

        // Resets the enter trigger.
        enterLiftTrigger.ResetTrigger();

        // Opens the elevator doors.
        yield return OpenDoors();

        // Waits until the participant has left the elevator far enough.
        yield return new WaitUntil(() => exitLiftTrigger.WasTriggered);

        // Closes the elevator doors behind the participant.
        yield return CloseDoors();

        // Fades the room lights on.
        yield return FadeLights(0f, maxLightIntensity);

        // Keeps the lights on for the defined duration.
        yield return new WaitForSeconds(lightsOnDuration);

        // Fades the room lights off.
        yield return FadeLights(maxLightIntensity, 0f);

        // Opens the elevator doors again.
        yield return OpenDoors();

        // Waits until the participant enters the elevator again.
        yield return new WaitUntil(() => enterLiftTrigger.WasTriggered);

        // Closes the elevator doors behind the participant.
        yield return CloseDoors();

        // Waits inside the elevator before ending the trial.
        yield return new WaitForSeconds(timeInLiftBeforeTrialEnds);

        // Ends the current trial.
        EndTrial();
    }

    private void EndTrial()
    {
        // Marks the trial as finished.
        trialRunning = false;

        // Prints a message to the Unity Console.
        Debug.Log("Trial ended.");

        // Starts the next trial after a short pause.
        StartCoroutine(StartNextTrialAfterDelay());
    }

    private IEnumerator StartNextTrialAfterDelay()
    {
        // Waits before starting the next trial.
        yield return new WaitForSeconds(timeBetweenTrials);

        // Starts the next trial.
        StartTrial();
    }

    private void StoreClosedDoorPositions()
    {
        // Creates an array for the left closed door positions.
        leftDoorClosedPositions = new Vector3[leftDoorParts.Length];

        // Creates an array for the right closed door positions.
        rightDoorClosedPositions = new Vector3[rightDoorParts.Length];

        // Loops through all left door parts.
        for (int i = 0; i < leftDoorParts.Length; i++)
        {
            // Stores the initial local position of this left door part.
            leftDoorClosedPositions[i] = leftDoorParts[i].localPosition;
        }

        // Loops through all right door parts.
        for (int i = 0; i < rightDoorParts.Length; i++)
        {
            // Stores the initial local position of this right door part.
            rightDoorClosedPositions[i] = rightDoorParts[i].localPosition;
        }
    }

    private void SetupRandomTrialStimuli()
    {
        // Removes objects from the previous trial.
        ClearOldObjects();

        // Checks whether at least one prefab is available.
        if (objectPrefabs.Count > 0)
        {
            // Selects a random prefab from the list.
            GameObject prefab = objectPrefabs[Random.Range(0, objectPrefabs.Count)];

            // Spawns the selected prefab at the left spawn point.
            currentLeftObject = Instantiate(prefab, leftSpawnPoint.position, leftSpawnPoint.rotation);

            // Spawns the selected prefab at the right spawn point with a 180 degree rotation.
            currentRightObject = Instantiate(prefab, rightSpawnPoint.position, rightSpawnPoint.rotation * Quaternion.Euler(0f, 180f, 0f));
        }

        // Checks whether a material and at least one texture are available.
        if (imageMaterial != null && trialImages.Count > 0)
        {
            // Selects a random image from the list.
            Texture2D randomImage = trialImages[Random.Range(0, trialImages.Count)];

            // Applies the image to the material.
            ApplyTextureToImageMaterial(randomImage);
        }
    }

    private void ApplyTextureToImageMaterial(Texture2D texture)
    {
        // Applies the texture to the Autodesk Interactive Base Color Map.
        if (imageMaterial.HasProperty("_BaseColorMap"))
            imageMaterial.SetTexture("_BaseColorMap", texture);

        // Applies the texture to the Autodesk Interactive Color Map.
        if (imageMaterial.HasProperty("_ColorMap"))
            imageMaterial.SetTexture("_ColorMap", texture);

        // Applies the texture to the URP Base Map.
        if (imageMaterial.HasProperty("_BaseMap"))
            imageMaterial.SetTexture("_BaseMap", texture);

        // Applies the texture to the Standard Main Texture.
        if (imageMaterial.HasProperty("_MainTex"))
            imageMaterial.SetTexture("_MainTex", texture);

        // Sets the base color to white so the texture is displayed without tinting.
        if (imageMaterial.HasProperty("_BaseColor"))
            imageMaterial.SetColor("_BaseColor", Color.white);

        // Sets the material color to white so the texture is displayed without tinting.
        if (imageMaterial.HasProperty("_Color"))
            imageMaterial.SetColor("_Color", Color.white);
    }

    private void ClearOldObjects()
    {
        // Destroys the previously spawned left object if it exists.
        if (currentLeftObject != null)
            Destroy(currentLeftObject);

        // Destroys the previously spawned right object if it exists.
        if (currentRightObject != null)
            Destroy(currentRightObject);
    }

    private IEnumerator OpenDoors()
    {
        // Opens all door parts.
        yield return MoveDoors(true);
    }

    private IEnumerator CloseDoors()
    {
        // Closes all door parts.
        yield return MoveDoors(false);
    }

    private IEnumerator MoveDoors(bool open)
    {
        // Stores the start positions of the left door parts.
        Vector3[] leftStartPositions = GetCurrentPositions(leftDoorParts);

        // Stores the start positions of the right door parts.
        Vector3[] rightStartPositions = GetCurrentPositions(rightDoorParts);

        // Creates the target positions of the left door parts.
        Vector3[] leftTargetPositions = GetTargetPositions(leftDoorClosedPositions, leftDoorOpenOffset, open);

        // Creates the target positions of the right door parts.
        Vector3[] rightTargetPositions = GetTargetPositions(rightDoorClosedPositions, rightDoorOpenOffset, open);

        // Starts the interpolation timer.
        float t = 0f;

        // Runs until the door movement duration is reached.
        while (t < doorMoveDuration)
        {
            // Increases the timer by the time since the last frame.
            t += Time.deltaTime;

            // Converts the timer into a normalized value between 0 and 1.
            float p = Mathf.Clamp01(t / doorMoveDuration);

            // Smooths the movement curve.
            p = Mathf.SmoothStep(0f, 1f, p);

            // Moves all left door parts.
            MoveDoorParts(leftDoorParts, leftStartPositions, leftTargetPositions, p);

            // Moves all right door parts.
            MoveDoorParts(rightDoorParts, rightStartPositions, rightTargetPositions, p);

            // Waits until the next frame.
            yield return null;
        }

        // Snaps all left door parts exactly to their target positions.
        MoveDoorParts(leftDoorParts, leftTargetPositions, leftTargetPositions, 1f);

        // Snaps all right door parts exactly to their target positions.
        MoveDoorParts(rightDoorParts, rightTargetPositions, rightTargetPositions, 1f);
    }

    private Vector3[] GetCurrentPositions(Transform[] parts)
    {
        // Creates an array for the current positions.
        Vector3[] positions = new Vector3[parts.Length];

        // Loops through all door parts.
        for (int i = 0; i < parts.Length; i++)
        {
            // Stores the current local position of this door part.
            positions[i] = parts[i].localPosition;
        }

        // Returns the current positions.
        return positions;
    }

    private Vector3[] GetTargetPositions(Vector3[] closedPositions, Vector3 openOffset, bool open)
    {
        // Creates an array for the target positions.
        Vector3[] targetPositions = new Vector3[closedPositions.Length];

        // Loops through all stored closed positions.
        for (int i = 0; i < closedPositions.Length; i++)
        {
            // Chooses either the open or closed target position.
            targetPositions[i] = open ? closedPositions[i] + openOffset : closedPositions[i];
        }

        // Returns all target positions.
        return targetPositions;
    }

    private void MoveDoorParts(Transform[] parts, Vector3[] startPositions, Vector3[] targetPositions, float progress)
    {
        // Loops through all door parts.
        for (int i = 0; i < parts.Length; i++)
        {
            // Moves this door part between its start and target position.
            parts[i].localPosition = Vector3.Lerp(startPositions[i], targetPositions[i], progress);
        }
    }

    private IEnumerator FadeLights(float from, float to)
    {
        // Starts the interpolation timer.
        float t = 0f;

        // Runs until the light fade duration is reached.
        while (t < lightFadeDuration)
        {
            // Increases the timer by the time since the last frame.
            t += Time.deltaTime;

            // Converts the timer into a normalized value between 0 and 1.
            float p = Mathf.Clamp01(t / lightFadeDuration);

            // Calculates the current light intensity.
            float intensity = Mathf.Lerp(from, to, p);

            // Applies the current light intensity.
            SetLightsInstant(intensity);

            // Waits until the next frame.
            yield return null;
        }

        // Snaps the lights exactly to the final intensity.
        SetLightsInstant(to);
    }

    private void SetLightsInstant(float intensity)
    {
        // Loops through all room lights.
        foreach (Light l in roomLights)
        {
            // Skips missing light references.
            if (l == null)
                continue;

            // Sets the light intensity.
            l.intensity = intensity;
        }
    }

    private void CloseDoorsInstant()
    {
        // Moves all left door parts to their closed positions.
        MoveDoorParts(leftDoorParts, leftDoorClosedPositions, leftDoorClosedPositions, 1f);

        // Moves all right door parts to their closed positions.
        MoveDoorParts(rightDoorParts, rightDoorClosedPositions, rightDoorClosedPositions, 1f);
    }
}