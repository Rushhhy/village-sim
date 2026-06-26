using System.Collections;
using UnityEngine;

public class ClickSquashEffect : MonoBehaviour
{
    private Coroutine effectCoroutine;

    public void Play()
    {
        if (effectCoroutine != null)
            StopCoroutine(effectCoroutine);

        effectCoroutine = StartCoroutine(EffectCoroutine());
    }

    private IEnumerator EffectCoroutine()
    {
        Vector3 originalScale = transform.localScale;
        Vector3 enlargedScale = originalScale * 1.1f;
        Vector3 squashedScale = originalScale * 0.96f;

        yield return ScaleTo(originalScale, enlargedScale, 0.1f);
        yield return ScaleTo(enlargedScale, squashedScale, 0.12f);
        yield return ScaleTo(squashedScale, originalScale, 0.1f);

        transform.localScale = originalScale;
        effectCoroutine = null;
    }

    private IEnumerator ScaleTo(Vector3 from, Vector3 to, float duration)
    {
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(from, to, t / duration);
            yield return null;
        }
    }
}