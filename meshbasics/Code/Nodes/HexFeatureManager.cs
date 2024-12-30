﻿using Godot;
using System;
using static Godot.HttpRequest;

namespace JHM.MeshBasics;

public sealed partial class HexFeatureManager : Node3D {
    [Export] public PackedScene FeaturePrefab { get; set; }
    private Node3D _container;

    public void Clear() {
        if (_container != null) {
            _container.QueueFree();
        }
        _container = new Node3D();
        this.AddChild(_container);
        _container.Position = Position;
        _container.Name = "FeatureContainer";
    }

    public void Apply() { }

    public void AddFeature(Vector3 position) {
        HexHash hash = HexMetrics.SampleHashGrid(position);
        if (hash.A >= 0.5f) return;

        var instance = _container.InstantiateChild<Node3D>(FeaturePrefab);
        instance.Position = HexMetrics.Perturb(position);
        instance.Rotation = new(0.0f, 2 * Mathf.Pi * hash.B, 0.0f);
    }
}