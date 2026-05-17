using UnityEngine;

public class PrewarmParticles : MonoBehaviour
{
    void Start()
    {
        ParticleSystem ps = GetComponent<ParticleSystem>();
        ps.Simulate(10f, true, true); // simulate 10 seconds ahead
        ps.Play();
    }
}