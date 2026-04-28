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
        if (points.Count < 2)
            return points.Count > 0 ? points[0] : Vector3.zero;

        // 特化优化：点数少时直接用固定公式
        if (points.Count == 2)
            return Vector3.Lerp(points[0], points[1], t);

        if (points.Count == 3)
            return QuadraticBezier(points[0], points[1], points[2], t);

        if (points.Count == 4)
            return CubicBezier(points[0], points[1], points[2], points[3], t);

        // 点数多时使用 De Casteljau（通用方案）
        return DeCasteljauBezier(points, t);
    }

    private static Vector3 QuadraticBezier(Vector3 p0, Vector3 p1, Vector3 p2, float t)
    {
        float u = 1f - t;
        return u * u * p0 + 2 * u * t * p1 + t * t * p2;
    }

    private static Vector3 CubicBezier(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float u = 1f - t;
        float uu = u * u, tt = t * t;
        return uu * u * p0 + 3 * uu * t * p1 + 3 * u * tt * p2 + tt * t * p3;
    }

    // 通用的 De Casteljau，适用于任意点数
    private static Vector3 DeCasteljauBezier(List<Vector3> points, float t)
    {
        // 可以在这里加对象池优化，避免频繁创建临时 List
        List<Vector3> temp = new List<Vector3>(points);

        while (temp.Count > 1)
        {
            for (int i = 0; i < temp.Count - 1; i++)
            {
                temp[i] = Vector3.Lerp(temp[i], temp[i + 1], t);
            }
            temp.RemoveAt(temp.Count - 1);  // 移除最后一个，复用 list
        }

        return temp[0];
    }
}
