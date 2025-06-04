using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class RuneParticleSetup : MonoBehaviour
{
    void Start()
    {
        Debug.Log("[RuneParticleSetup] Starting particle system setup");
        ParticleSystem particleSystem = GetComponent<ParticleSystem>();
        
        if (particleSystem == null)
        {
            Debug.LogError("[RuneParticleSetup] No ParticleSystem component found!");
            return;
        }

        Debug.Log("[RuneParticleSetup] Found ParticleSystem component, configuring...");

        // Main settings
        var main = particleSystem.main;
        main.startLifetime = 1f;
        main.startSpeed = 0.5f;
        main.startSize = 0.1f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 100;
        Debug.Log("[RuneParticleSetup] Main settings configured");

        // Emission settings
        var emission = particleSystem.emission;
        emission.rateOverTime = 50;
        Debug.Log("[RuneParticleSetup] Emission settings configured");

        // Shape settings
        var shape = particleSystem.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.02f;
        Debug.Log("[RuneParticleSetup] Shape settings configured");

        // Color over lifetime
        var colorOverLifetime = particleSystem.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { 
                new GradientColorKey(Color.cyan, 0.0f),
                new GradientColorKey(Color.blue, 1.0f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(1.0f, 0.0f),
                new GradientAlphaKey(0.0f, 1.0f)
            }
        );
        colorOverLifetime.color = gradient;
        Debug.Log("[RuneParticleSetup] Color over lifetime configured");

        // Size over lifetime
        var sizeOverLifetime = particleSystem.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(0.0f, 1.0f);
        curve.AddKey(1.0f, 0.0f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1.0f, curve);
        Debug.Log("[RuneParticleSetup] Size over lifetime configured");

        // Stop the particle system initially
        particleSystem.Stop();
        Debug.Log("[RuneParticleSetup] Particle system setup complete and stopped");
    }
} 