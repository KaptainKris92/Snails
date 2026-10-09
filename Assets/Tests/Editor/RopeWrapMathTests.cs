using NUnit.Framework;
using UnityEngine;

public class RopeWrapMathTests
{
    // Anchor on a ceiling at (3, 10). A pillar hangs from the ceiling over x in [0, 1], y in [4, 10].
    // The shell swings left under the pillar, so the rope must bend around the pillar's bottom-left corner.
    private static readonly Vector2 Anchor = new Vector2(3f, 10f);
    private static readonly Vector2 Corner = new Vector2(0f, 4f);

    [Test]
    public void WrapAroundCorner_ThenSwingBack_Unwraps()
    {
        var pivots = new RopePivots();
        pivots.Reset(Anchor);

        // Wrap: the shell is left of and below the corner, the hit is on the pillar's bottom face.
        Vector2 bodyAtWrap = new Vector2(-3f, 2f);
        Vector2 pivot = RopeWrapMath.PivotFromHit(Corner, Vector2.down, 0.02f);
        float wrapped = pivots.Wrap(pivot, bodyAtWrap);

        Assert.AreEqual(2, pivots.Count);
        Assert.AreEqual(pivot, pivots.Last);
        Assert.AreEqual(Vector2.Distance(Anchor, pivot), wrapped, 1e-5f);
        Assert.AreEqual(wrapped, pivots.WrappedLength, 1e-5f);

        // Still on the same side of the line anchor -> pivot: stays wrapped.
        Assert.AreEqual(0f, pivots.TryUnwrap(new Vector2(-2f, 1f)));
        Assert.AreEqual(2, pivots.Count);

        // Swings back under the pillar, crossing the extended anchor -> pivot line: unwraps.
        float freed = pivots.TryUnwrap(new Vector2(0.5f, 1f));
        Assert.AreEqual(wrapped, freed, 1e-5f);
        Assert.AreEqual(1, pivots.Count);
        Assert.AreEqual(Anchor, pivots.Last);
        Assert.AreEqual(0f, pivots.WrappedLength, 1e-5f);
    }

    [Test]
    public void SingleAnchor_NeverUnwraps()
    {
        var pivots = new RopePivots();
        pivots.Reset(Anchor);

        Assert.AreEqual(0f, pivots.TryUnwrap(new Vector2(10f, 10f)));
        Assert.AreEqual(1, pivots.Count);
    }
}
