using System.Collections;
using UnityEngine;

public class TreeChopFeedback : MonoBehaviour
{
    private float squashAmount = 0.12f;
    private float duration = 0.04f;

    private Vector3 originalScale;
    private Coroutine feedbackCoroutine;

    private void Awake()
    {
        originalScale = transform.localScale;
    }

    public void PlayChopFeedback()
    {
        if (feedbackCoroutine != null)
        {
            StopCoroutine(feedbackCoroutine);
        }

        feedbackCoroutine = StartCoroutine(ChopFeedbackCoroutine());
    }

    private IEnumerator ChopFeedbackCoroutine()
    {
        Vector3 squashedScale = new Vector3(
            originalScale.x + squashAmount,
            originalScale.y - squashAmount,
            originalScale.z
        );

        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(originalScale, squashedScale, t / duration);
            yield return null;
        }

        t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(squashedScale, originalScale, t / duration);
            yield return null;
        }

        transform.localScale = originalScale;
        feedbackCoroutine = null;
    }
}