
using UnityEngine;
using System.Collections;

public class CaveDarknessController : MonoBehaviour
{
    public SpriteRenderer darknessOverlay;

    public float darkAlpha = 0.7f;
    public float fadeDuration = 2f;

    void Start()
    {
        if (darknessOverlay != null)
        {
            darknessOverlay.sortingOrder = 10;
            SetAlpha(darkAlpha);
        }
    }

    void SetAlpha(float alpha)
    {
        Color color = darknessOverlay.color;
        color.a = alpha;
        darknessOverlay.color = color;
    }

    public void LightUpCave()
    {
        StopAllCoroutines();
        StartCoroutine(FadeOut());
    }

    IEnumerator FadeOut()
    {
        float startAlpha = darknessOverlay.color.a;
        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;

            float alpha = Mathf.Lerp(
                startAlpha,
                0f,
                timer / fadeDuration
            );

            SetAlpha(alpha);

            yield return null;
        }

        SetAlpha(0f);
        darknessOverlay.gameObject.SetActive(false);
    }
}



