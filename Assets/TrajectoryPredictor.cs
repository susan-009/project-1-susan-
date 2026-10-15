using UnityEngine;

public class TrajectoryPredictor : MonoBehaviour
{
    public LineRenderer line;
    public int points = 30;
    public float timeStep = .1f;

    public void EnsureLine()
    {
        if (line == null)
        {
            GameObject preview = new GameObject("TrajectoryLine");
            preview.transform.SetParent(transform, false);
            line = preview.AddComponent<LineRenderer>();
            line.enabled = false;
        }
        if (line.sharedMaterial == null)
            line.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
        line.useWorldSpace = true;
        line.startWidth = .08f;
        line.endWidth = .03f;
        line.startColor = Color.yellow;
        line.endColor = Color.yellow;
        line.sortingOrder = 100;
    }

    public void Show() { EnsureLine(); line.enabled = true; }
    public void Hide() { if (line != null) line.enabled = false; }

    public void DrawTrajectory(Vector2 velocity)
    {
        EnsureLine();
        Rigidbody2D body = GetComponent<Rigidbody2D>();
        Vector2 gravity = Physics2D.gravity * (body != null ? body.gravityScale : 1f);
        int count = Mathf.Max(2, points);
        line.positionCount = count;
        line.enabled = true;
        for (int i = 0; i < count; i++)
        {
            float t = i * timeStep;
            Vector2 point = (Vector2)transform.position + velocity * t + .5f * gravity * t * t;
            line.SetPosition(i, new Vector3(point.x, point.y, transform.position.z));
        }
    }
}