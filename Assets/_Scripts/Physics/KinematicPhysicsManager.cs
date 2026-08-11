using UnityEngine;
using System.Collections.Generic;

public class KinematicPhysicsManager : MonoBehaviour
{
    public static KinematicPhysicsManager Instance { get; private set; }

    private readonly List<KinematicActor> _actors = new List<KinematicActor>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void RegisterActor(KinematicActor actor)
    {
        if (!_actors.Contains(actor)) _actors.Add(actor);
    }

    public void UnregisterActor(KinematicActor actor)
    {
        _actors.Remove(actor);
    }

    private void FixedUpdate()
    {
        // 1. Calculate desired velocities based on inputs/gravity (Pre-Step)
        foreach (var actor in _actors)
        {
            actor.CalculateVelocity(Time.fixedDeltaTime);
        }

        // 2. Perform Movement and Collision Resolution (Step)
        // Deterministic execution order allows us to cleanly handle stacking and collisions.
        foreach (var actor in _actors)
        {
            actor.MoveActor(Time.fixedDeltaTime);
        }
    }
}
