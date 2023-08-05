using UnityEngine;

public class CollisionDetector : MonoBehaviour
{
    public bool IsColliding { get; private set; }

    private void OnCollisionEnter(Collision collision)
    {
        IsColliding = true;
    }

    private void OnCollisionExit(Collision collision)
    {
        IsColliding = false;
    }
}
