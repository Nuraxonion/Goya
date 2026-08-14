using UnityEngine;

// Helpers for building AnimationCurve defaults in code.
//
// Curves are populated here rather than authored into scene YAML because an
// AnimationCurve field that deserialises empty evaluates to 0 - which silently
// means "no enemies spawn" or "every level costs nothing". Once a curve is edited
// in the inspector it serialises normally and the IsEmpty guards skip it.
public static class CurveUtil
{
    public static bool IsEmpty(AnimationCurve curve)
    {
        return curve == null || curve.length == 0;
    }

    // Piecewise-linear between the given (time, value) pairs. Tangents are set
    // explicitly so the curve evaluates exactly as authored - Unity's default
    // smoothing would overshoot between widely spaced keys.
    public static AnimationCurve LinearCurve(params float[] pairs)
    {
        int n = pairs.Length / 2;
        Keyframe[] keys = new Keyframe[n];

        for (int i = 0; i < n; i++)
            keys[i] = new Keyframe(pairs[i * 2], pairs[i * 2 + 1]);

        for (int i = 0; i < n; i++)
        {
            float inTangent = 0f;
            float outTangent = 0f;

            if (i > 0)
                inTangent = (keys[i].value - keys[i - 1].value) / (keys[i].time - keys[i - 1].time);

            if (i < n - 1)
                outTangent = (keys[i + 1].value - keys[i].value) / (keys[i + 1].time - keys[i].time);

            if (i == 0) inTangent = outTangent;
            if (i == n - 1) outTangent = inTangent;

            keys[i].inTangent = inTangent;
            keys[i].outTangent = outTangent;
        }

        return new AnimationCurve(keys);
    }

    // Holds each value until the next key. Infinite tangents are Unity's constant
    // tangent mode.
    public static AnimationCurve StepCurve(params float[] pairs)
    {
        int n = pairs.Length / 2;
        Keyframe[] keys = new Keyframe[n];

        for (int i = 0; i < n; i++)
        {
            keys[i] = new Keyframe(pairs[i * 2], pairs[i * 2 + 1])
            {
                inTangent = float.PositiveInfinity,
                outTangent = float.PositiveInfinity
            };
        }

        return new AnimationCurve(keys);
    }
}
