using Godot;
using System;

namespace JHM.MeshBasics;

public static class HexMetrics {
    private static HexHash[] _hashGrid;
    private static float[][] _featureThresholds = {
        new float[] {0.0f, 0.0f, 0.4f},
        new float[] {0.0f, 0.4f, 0.6f},
        new float[] {0.4f, 0.6f, 0.8f}
    };

    public static int wrapSize;

    public static bool Wrapping {
        get {
            return wrapSize > 0;
        }
    }

    public const float OuterRadius = 10.0f;
    public const float SolidFactor = 0.80f;
    public const float ElevationStep = 3.0f;
    public const float Maxelevation = 5.0f;
    public const int TerracesPerSlope = 2;
    public const float CellPerturbStrength = 4.0f;
    public const float NoiseScale = 0.003f;
    public const float ElevationPerturbStrength = 1.5f;
    public const int ChunkSizeX = 5;
    public const int ChunkSizeZ = 5;
    public const float StreamBedElevationOffset = -1.75f;
    public const float WaterElevationOffset = -0.5f;
    public const int HashGridSize = 256;
    public const float HashGridScale = 0.25f;
    public const float WaterFactor = 0.6f;
    public const float WallHeight = 4.0f;
    public const float WallYOffset = -1.0f;
    public const float WallThickness = 0.75f;
    public const float WallTowerThreshold = 0.5f;
    public const float BridgeDesignLength = 7.0f;
    
    public const float InnerRadius = OuterRadius * 0.866025404f;
    public const float InnerDiameter = InnerRadius * 2f;
    public const float BlendFactor = 1.0f - SolidFactor;
    public const float WaterBlendFactor = 1.0f - WaterFactor;
    public const int TerraceSteps = TerracesPerSlope * 2 + 1;
    public const float HorizontalTerraceStepSize = 1.0f / TerraceSteps;
    public const float VerticalTerraceStepSize = 1f / (TerracesPerSlope + 1);
    public const float OuterToInner = 0.866025404f;
    public const float InnerToOuter = 1.0f / OuterToInner;
    public const float WallElevationOffset = VerticalTerraceStepSize;

    public static Image NoiseSource { get; set; }

    public static Vector3[] Corners = {
        new (0.0f, 0.0f, OuterRadius),
        new (InnerRadius, 0.0f, 0.5f * OuterRadius),
        new (InnerRadius, 0.0f, -0.5f * OuterRadius),
        new (0.0f, 0.0f, -OuterRadius),
        new (-InnerRadius, 0.0f, -0.5f * OuterRadius),
        new (-InnerRadius, 0.0f, 0.5f * OuterRadius),
        new (0.0f, 0.0f, OuterRadius)
    };

    public static Vector3 GetFirstCorner(HexDirection direction) {
        return Corners[(int)direction];
    }

    public static Vector3 GetSecondCorner(HexDirection direction) {
        return Corners[(int)direction + 1];
    }

    public static Vector3 GetFirstSolidCorner(HexDirection direction) {
        return Corners[(int)direction] * SolidFactor;
    }

    public static Vector3 GetSecondSolidCorner(HexDirection direction) {
        return Corners[(int)direction + 1] * SolidFactor;
    }

    public static Vector3 GetBridge(HexDirection direction) {
        return (Corners[(int)direction] + Corners[(int)direction + 1]) * BlendFactor;
    }

    public static Vector3 GetWaterBridge(HexDirection direction) {
        return (Corners[(int)direction] + Corners[(int)direction + 1]) *
            WaterBlendFactor;
    }

    public static Vector3 GetWallThicknessOffset(Vector3 near, Vector3 far) {
        Vector3 offset;
        offset.X = far.X - near.X;
        offset.Y = 0f;
        offset.Z = far.Z - near.Z;
        return offset.Normalized() * (WallThickness * 0.5f); ;
    }

    public static Vector3 WallLerp(Vector3 near, Vector3 far) {
        near.X += (far.X - near.X) * 0.5f;
        near.Z += (far.Z - near.Z) * 0.5f;
        float v =
            near.Y < far.Y ? WallElevationOffset : (1.0f - WallElevationOffset);
        near.Y += (far.Y - near.Y) * v + WallYOffset;
        return near;
    }

    public static Vector3 TerraceLerp(Vector3 a, Vector3 b, int step) {
        float h = step * HexMetrics.HorizontalTerraceStepSize;
        a.X += (b.X - a.X) * h;
        a.Z += (b.Z - a.Z) * h;
        float v = ((step + 1) / 2) * HexMetrics.VerticalTerraceStepSize;
        a.Y += (b.Y - a.Y) * v;
        return a;
    }

    public static Color TerraceLerp(Color a, Color b, int step) {
        float h = step * HexMetrics.HorizontalTerraceStepSize;
        return a.Lerp(b, h);
    }

    public static HexEdgeType GetEdgeType(int elevation1, int elevation2) {
        int delta = elevation2 - elevation1;

        return delta switch {
            0 => HexEdgeType.Flat,
            1 => HexEdgeType.Slope,
            -1 => HexEdgeType.Slope,
            _ => HexEdgeType.Cliff

        };
    }

    public static Vector4 SampleNoise(Vector3 position) {
        var w = NoiseSource.GetWidth();
        var h = NoiseSource.GetHeight();
        position *= w;
        position *= NoiseScale;
        position.X = position.X % w;
        position.Z = position.Z % h;

        if (position.X < 0) position.X += w;
        if (position.Z < 0) position.Z += h;
        if (position.X == w) position.X -= w;
        if (position.Z == h) position.Z -= h;


        var pixel = NoiseSource.GetPixel((int)position.X, (int)position.Z);
        return new Vector4(pixel.R, pixel.G, pixel.B, pixel.A);
    }

    public static HexHash SampleHashGrid(Vector3 position) {
        int x = (int)(position.X * HashGridScale) % HashGridSize;
        if (x < 0) {
            x += HashGridSize;
        }
        int z = (int)(position.Z * HashGridScale) % HashGridSize;
        if (z < 0) {
            z += HashGridSize;
        }
        return _hashGrid[x + z * HashGridSize];
    }

    public static Vector3 GetSolidEdgeMiddle(HexDirection direction) {
        return
            (Corners[(int)direction] + Corners[(int)direction + 1]) *
            (0.5f * SolidFactor);
    }

    public static Vector3 Perturb(Vector3 position) {
        Vector4 sample = SampleNoise(position);
        position.X += (sample.X * 2f - 1f) * CellPerturbStrength;
        position.Z += (sample.Z * 2f - 1f) * CellPerturbStrength;
        return position;
    }

    public static Vector3 GetFirstWaterCorner(HexDirection direction) {
        return Corners[(int)direction] * WaterFactor;
    }

    public static Vector3 GetSecondWaterCorner(HexDirection direction) {
        return Corners[(int)direction + 1] * WaterFactor;
    }

    public static void InitializeHashGrid(int seed) {
        Random rng = new(seed);
        _hashGrid = new HexHash[HashGridSize * HashGridSize];
        for (int i = 0; i < _hashGrid.Length; i++) {
            _hashGrid[i] = HexHash.Create(rng);
        }
    }

    public static float[] GetFeatureThresholds(int level) {
        return _featureThresholds[level];
    }
}
