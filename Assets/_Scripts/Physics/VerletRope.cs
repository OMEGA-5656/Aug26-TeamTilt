using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(LineRenderer))]
public class VerletRope : MonoBehaviour
{
    [Header("Attachments")]
    public Transform startPoint;
    public Transform endPoint;
    
    [Header("Rope Settings")]
    public int segmentCount = 15;
    public float totalLength = 5f;
    public float gravity = -15f;
    public int constraintIterations = 50;
    
    private LineRenderer _lineRenderer;
    private List<RopeNode> _nodes = new List<RopeNode>();
    private float _segmentLength;

    private class RopeNode
    {
        public Vector2 currentPos;
        public Vector2 oldPos;
        
        public RopeNode(Vector2 pos)
        {
            currentPos = pos;
            oldPos = pos;
        }
    }

    private void Awake()
    {
        _lineRenderer = GetComponent<LineRenderer>();
        _lineRenderer.positionCount = segmentCount;
        _segmentLength = totalLength / (Mathf.Max(1, segmentCount - 1));
        
        Vector2 startPos = startPoint != null ? (Vector2)startPoint.position : (Vector2)transform.position;
        
        for (int i = 0; i < segmentCount; i++)
        {
            _nodes.Add(new RopeNode(startPos - new Vector2(0, _segmentLength * i)));
        }
    }

    private void FixedUpdate()
    {
        Simulate(Time.fixedDeltaTime);
        ApplyConstraints();
    }

    private void Update()
    {
        DrawRope();
    }

    private void Simulate(float dt)
    {
        Vector2 gravityVec = new Vector2(0, gravity * dt * dt);
        
        for (int i = 0; i < segmentCount; i++)
        {
            RopeNode node = _nodes[i];
            
            // Verlet integration
            Vector2 velocity = node.currentPos - node.oldPos;
            node.oldPos = node.currentPos;
            node.currentPos += velocity + gravityVec;
        }
    }

    private void ApplyConstraints()
    {
        for (int iteration = 0; iteration < constraintIterations; iteration++)
        {
            // Pin start and end points
            if (startPoint != null)
            {
                _nodes[0].currentPos = startPoint.position;
            }
            if (endPoint != null)
            {
                _nodes[segmentCount - 1].currentPos = endPoint.position;
            }

            for (int i = 0; i < segmentCount - 1; i++)
            {
                RopeNode node1 = _nodes[i];
                RopeNode node2 = _nodes[i + 1];

                Vector2 diff = node1.currentPos - node2.currentPos;
                float currentDistance = diff.magnitude;
                float error = currentDistance - _segmentLength;

                if (currentDistance > 0.0001f)
                {
                    Vector2 correction = diff.normalized * error * 0.5f;

                    // Apply corrections, skipping the very first node if pinned
                    if (i != 0 || startPoint == null)
                    {
                        node1.currentPos -= correction;
                    }
                    
                    // Skip the very last node if pinned
                    if (i + 1 != segmentCount - 1 || endPoint == null)
                    {
                        node2.currentPos += correction;
                    }
                }
            }
        }
    }

    private void DrawRope()
    {
        for (int i = 0; i < segmentCount; i++)
        {
            _lineRenderer.SetPosition(i, _nodes[i].currentPos);
        }
    }
}
