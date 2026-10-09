using System.Collections.Generic;
using UnityEngine;

/// Pure geometry for rope wrapping. No physics calls, so it is covered by an EditMode test.
public static class RopeWrapMath
{
    public static float Cross(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;

    /// Which side of the directed line prev -> last the point is on: +1 left, -1 right, 0 on the line.
    public static int Side(Vector2 prev, Vector2 last, Vector2 point)
    {
        float c = Cross(last - prev, point - last);
        return c > 0f ? 1 : (c < 0f ? -1 : 0);
    }

    /// Pivot position for a wrap hit: the hit point pushed off the surface along its normal so the
    /// next shell-to-pivot raycast does not hit the same surface again.
    public static Vector2 PivotFromHit(Vector2 hitPoint, Vector2 hitNormal, float offset)
    {
        return hitPoint + hitNormal * offset;
    }

    /// True once the body has crossed the line through the last two pivots relative to the side it
    /// was on when the last pivot was added. That is the moment the rope would go straight again.
    public static bool ShouldUnwrap(Vector2 prev, Vector2 last, Vector2 body, int sideAtWrap)
    {
        int side = Side(prev, last, body);
        return side != 0 && side != sideAtWrap;
    }
}

/// The ordered list of points the rope bends around. Index 0 is the original anchor, the last
/// entry is what the joint is connected to. Keeps the total length of the wrapped segments so the
/// controller can cap the whole rope at ropeMaxLength.
public sealed class RopePivots
{
    private readonly List<Vector2> points = new List<Vector2>();
    private readonly List<int> sides = new List<int>(); // side of the body when each pivot was added; index 0 unused

    /// Total length of the segments between pivots (excluding the live shell -> last pivot segment).
    public float WrappedLength { get; private set; }

    public int Count => points.Count;
    public Vector2 Last => points[points.Count - 1];
    public Vector2 this[int index] => points[index];

    public void Reset(Vector2 anchor)
    {
        Clear();
        points.Add(anchor);
        sides.Add(0);
    }

    public void Clear()
    {
        points.Clear();
        sides.Clear();
        WrappedLength = 0f;
    }

    /// Adds a pivot between the body and the current last pivot. Returns the length of the new
    /// wrapped segment (previous last pivot -> new pivot).
    public float Wrap(Vector2 pivot, Vector2 body)
    {
        Vector2 prev = Last;
        float segment = Vector2.Distance(prev, pivot);
        sides.Add(RopeWrapMath.Side(prev, pivot, body));
        points.Add(pivot);
        WrappedLength += segment;
        return segment;
    }

    /// Removes the last pivot if the body has swung back across the line through the last two
    /// pivots. Returns the length of the freed segment, or 0 if nothing was unwrapped.
    public float TryUnwrap(Vector2 body)
    {
        if (points.Count < 2)
            return 0f;

        Vector2 last = points[points.Count - 1];
        Vector2 prev = points[points.Count - 2];
        if (!RopeWrapMath.ShouldUnwrap(prev, last, body, sides[sides.Count - 1]))
            return 0f;

        float segment = Vector2.Distance(prev, last);
        points.RemoveAt(points.Count - 1);
        sides.RemoveAt(sides.Count - 1);
        WrappedLength -= segment;
        return segment;
    }
}
