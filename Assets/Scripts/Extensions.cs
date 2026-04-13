using UnityEngine;

public static class Extensions
{
    private static readonly LayerMask layerMask = Physics2D.AllLayers;

    public static bool Raycast(this Rigidbody2D rigidbody, Vector2 direction)
    {
        if (rigidbody.bodyType == RigidbodyType2D.Kinematic) return false;

        Vector2 edge = rigidbody.ClosestPoint(rigidbody.position + direction);
        float radius = (edge - rigidbody.position).magnitude / 2f;
        float distance = radius + 0.125f;

        Vector2 point = rigidbody.position + (direction.normalized * distance);
        Collider2D collider = Physics2D.OverlapCircle(point, radius, layerMask);
        if (collider == null || collider.isTrigger || collider.attachedRigidbody == rigidbody)
            return false;

        return !IsDecorationCollider(collider);
    }

    private static bool IsDecorationCollider(Collider2D collider)
    {
        if (collider == null) return false;

        Transform current = collider.transform;
        while (current != null)
        {
            string lower = current.name.ToLowerInvariant();
            if (lower.Contains("bush") || lower.Contains("cloud") || lower.Contains("hill") || lower.Contains("decor"))
                return true;

            current = current.parent;
        }

        return false;
    }

    public static bool DotTest(this Transform transform, Transform other, Vector2 testDirection)
    {
        Vector2 direction = other.position - transform.position;
        return Vector2.Dot(direction.normalized, testDirection) > 0.25f;
    }
}
