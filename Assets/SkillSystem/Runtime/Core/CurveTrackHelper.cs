using SkillSystem;
using System.Collections.Generic;
using UnityEngine;

public class CurveTrackHelper
{
    public static Vector3 EvaluateCurve(List<Vector3> points, float t, CurveClipAsset.CurveType curve_type)
    {
        if (points.Count < 2) return points[0];

        switch (curve_type)
        {
            case CurveClipAsset.CurveType.Linear:
                return LinearInterpolate(points, t);
            case CurveClipAsset.CurveType.CatmullRom:
                return CatmullRomInterpolate(points, t);
            case CurveClipAsset.CurveType.Bezier:
                return BezierInterpolate(points, t);
            default:
                return LinearInterpolate(points, t);
        }
    }

    private static Vector3 LinearInterpolate(List<Vector3> points, float t)
    {
        float total_dist = points.Count - 1;
        float exact_index = t * total_dist;
        int idx_0 = Mathf.Clamp(Mathf.FloorToInt(exact_index), 0, points.Count - 1);
        int idx_1 = Mathf.Clamp(idx_0 + 1, 0, points.Count - 1);
        float frac = exact_index - idx_0;
        return Vector3.Lerp(points[idx_0], points[idx_1], frac);
    }

    private static Vector3 CatmullRomInterpolate(List<Vector3> points, float t)
    {
        float total_dist = points.Count - 1;
        float exact_index = t * total_dist;
        int idx_0 = Mathf.Clamp(Mathf.FloorToInt(exact_index) - 1, 0, points.Count - 1);
        int idx_1 = Mathf.Clamp(idx_0 + 1, 0, points.Count - 1);
        int idx_2 = Mathf.Clamp(idx_0 + 2, 0, points.Count - 1);
        int idx_3 = Mathf.Clamp(idx_0 + 3, 0, points.Count - 1);
        float frac = exact_index - Mathf.Floor(exact_index);
        return CatmullRom(points[idx_0], points[idx_1], points[idx_2], points[idx_3], frac);
    }

    private static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;
        return 0.5f * (
            (2f * p1) +
            (-p0 + p2) * t +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
            (-p0 + 3f * p1 - 3f * p2 + p3) * t3
        );
    }

    private static Vector3 BezierInterpolate(List<Vector3> points, float t)
    {
        // 简化：将每两个点作为一段线性贝塞尔（实际项目可拓展为高阶控制点）
        return LinearInterpolate(points, t);
    }
}
