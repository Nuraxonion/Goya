using UnityEngine;

public class FireballGreyTrail : MonoBehaviour
{
    [Header("Trail Material")]
    public Material trailMaterial;

    [Header("Trail Visual")]
    public float width = 0.18f;
    public Color color = new Color(1f, 1f, 1f, 0.6f);

    [Header("Trail Quality")]
    public float minPointDistance = 0.025f;

    [Header("Texture")]
    public float textureTiling = 2f;

    [Header("Sorting")]
    public int sortingOrder = 1;

    private LineRenderer line;
    private Vector3 lastPoint;
    private int pointCount;

    private void Start()
    {
        GameObject trailObject = new GameObject("Fireball_Grey_Trail");

        line = trailObject.AddComponent<LineRenderer>();

        line.useWorldSpace = true;
        line.positionCount = 0;

        line.startWidth = width;
        line.endWidth = width;

        line.startColor = color;
        line.endColor = color;

        line.numCapVertices = 8;
        line.numCornerVertices = 8;

        line.textureMode = LineTextureMode.Tile;
        line.alignment = LineAlignment.View;

        line.sortingLayerName = "Default";
        line.sortingOrder = sortingOrder;

        if (trailMaterial != null)
        {
            line.material = trailMaterial;
        }
        else
        {
            Shader shader = Shader.Find("Sprites/Default");
            line.material = new Material(shader);
        }

        line.material.color = color;
        line.material.mainTextureScale = new Vector2(textureTiling, 1f);

        AddPoint(transform.position);
        lastPoint = transform.position;
    }

    private void Update()
    {
        float distance = Vector3.Distance(transform.position, lastPoint);

        if (distance >= minPointDistance)
        {
            AddPoint(transform.position);
            lastPoint = transform.position;
        }
    }

    private void AddPoint(Vector3 position)
    {
        pointCount++;
        line.positionCount = pointCount;
        line.SetPosition(pointCount - 1, position);
    }
}