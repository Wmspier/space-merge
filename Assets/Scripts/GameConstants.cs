using UnityEngine;

namespace Hex
{
    public static class GameConstants
    {
        public static class HexMetrics
        {
            public const float EdgeLength = 2.309f / 2f;
            
            // Generation
            public const float OuterRadius = 1f;
            public const float InnerRadius = OuterRadius * 0.866025404f;
            // Point Up
            public static readonly Vector3[] Corners = {
                new (0f, 0f, OuterRadius),
                new (InnerRadius, 0f, 0.5f * OuterRadius),
                new (InnerRadius, 0f, -0.5f * OuterRadius),
                new (0f, 0f, -OuterRadius),
                new (-InnerRadius, 0f, -0.5f * OuterRadius),
                new (-InnerRadius, 0f, 0.5f * OuterRadius),
                new (0f, 0f, OuterRadius)
            };
            // Flat Up
            public static readonly Vector3[] CornersFlat = {
                new (-.5f * OuterRadius, 0f, OuterRadius),
                new (.5f * OuterRadius, 0f, OuterRadius),
                new (OuterRadius, 0f, 0f),
                new (.5f * OuterRadius, 0f, -OuterRadius),
                new (-.5f * OuterRadius, 0f, -OuterRadius),
                new (-OuterRadius, 0f, 0f),
                new (-.5f * OuterRadius, 0f, OuterRadius)
            };
        }
    }
}