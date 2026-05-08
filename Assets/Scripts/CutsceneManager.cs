using UnityEngine;
using System.Collections;

public class CutsceneManager : MonoBehaviour
{
    public static CutsceneManager instance;

    [Header("Camera Transition")]
    public Transform mainCamera;
    public Transform playerTransform;
    public float transitionDuration = 2.0f; // Ïû³ýÄ§·¨Êý×Ö

    void Awake()
    {
        if (instance == null) instance = this;
    }

    public void PlayIntro()
    {
        StartCoroutine(AnimateCamera());
    }

    IEnumerator AnimateCamera()
    {
        if (PlayerMovement.instance != null) PlayerMovement.instance.enabled = false;

        float elapsed = 0;
        Vector3 startPos = mainCamera.position;
        Vector3 endPos = playerTransform.position + new Vector3(0, 3, -5);

        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            mainCamera.position = Vector3.Lerp(startPos, endPos, elapsed / transitionDuration);
            mainCamera.LookAt(playerTransform);
            yield return null;
        }

        if (PlayerMovement.instance != null) PlayerMovement.instance.enabled = true;
    }
}