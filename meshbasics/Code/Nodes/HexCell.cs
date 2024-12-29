using Godot;
using System.Collections.Generic;

namespace JHM.MeshBasics;

public sealed partial class HexCell : Node3D {
    private HexCell[] _neighbors = new HexCell[6];
    private int _elevation = int.MinValue;
    private Node3D _uiRect;
    private Color _color;
    private bool _hasIncomingRiver;
    private bool _hasOutgoingRiver;
    private HexDirection _incomingRiver;
    private HexDirection _outgoingRiver;
    private bool[] _roads = new bool[6];

    public Color Color {
        get {
            return _color;
        }
        set {
            if (_color == value) return;
            _color = value;
            Refresh();
        }
    }
    public HexCoordinates Coordinates { get; set; }
    public HexGridChunk Chunk { get; set; }
    public Node3D UiRect {
        get => _uiRect;
        set {
            _uiRect = value;
            UpdateUiAltitude();
        }
    }
    public int Elevation {
        get => _elevation;
        set {
            if (_elevation == value) return;
            _elevation = value;
            var newPosition = Position;
            newPosition.Y = value * HexMetrics.ElevationStep;
            newPosition.Y +=
                (HexMetrics.SampleNoise(newPosition).Y * 2f - 1f) *
                HexMetrics.ElevationPerturbStrength;
            Position = newPosition;
            UpdateUiAltitude();

            if (
                _hasOutgoingRiver &&
                _elevation < GetNeighbor(_outgoingRiver)._elevation
            ) {
                RemoveOutgoingRiver();
            }
            if (
                _hasIncomingRiver &&
                _elevation > GetNeighbor(_incomingRiver)._elevation
            ) {
                RemoveIncomingRiver();
            }

            for (int i = 0; i < _roads.Length; i++) {
                if (_roads[i] && GetElevationDifference((HexDirection)i) > 1) {
                    SetRoad(i, false);
                }
            }

            Refresh();
        }
    }

    public bool HasIncomingRiver {
        get {
            return _hasIncomingRiver;
        }
    }

    public bool HasOutgoingRiver {
        get {
            return _hasOutgoingRiver;
        }
    }

    public HexDirection IncomingRiver {
        get {
            return _incomingRiver;
        }
    }

    public HexDirection OutgoingRiver {
        get {
            return _outgoingRiver;
        }
    }

    public bool HasRiver {
        get {
            return _hasIncomingRiver || _hasOutgoingRiver;
        }
    }

    public bool HasRiverBeginOrEnd {
        get {
            return _hasIncomingRiver != _hasOutgoingRiver;
        }
    }

    public float StreamBedY {
        get {
            return
                (Elevation + HexMetrics.StreamBedElevationOffset) *
                HexMetrics.ElevationStep;
        }
    }

    public float RiverSurfaceY {
        get {
            return
                (Elevation + HexMetrics.RiverSurfaceElevationOffset) *
                HexMetrics.ElevationStep;
        }
    }

    public bool HasRoads {
        get {
            for (int i = 0; i < _roads.Length; i++) {
                if (_roads[i]) {
                    return true;
                }
            }
            return false;
        }
    }

    public bool HasRiverThroughEdge(HexDirection direction) {
        return
            _hasIncomingRiver && _incomingRiver == direction ||
            _hasOutgoingRiver && _outgoingRiver == direction;
    }

    public bool HasRoadThroughEdge(HexDirection direction) {
        return _roads[(int)direction];
    }

    private void UpdateUiAltitude() {
        if (UiRect is null) return;
        var uiPosition = UiRect.Position;
        uiPosition.Y = Position.Y + 0.05f;
        UiRect.Position = uiPosition;
    }

    public HexCell GetNeighbor(HexDirection direction) {
        return _neighbors[(int)direction];
    }

    public int GetElevationDifference(HexDirection direction) {
        int difference = Elevation - GetNeighbor(direction).Elevation;
        return difference >= 0 ? difference : -difference;
    }

    public void SetNeighbor(HexDirection direction, HexCell cell) {
        _neighbors[(int)direction] = cell;
        cell._neighbors[(int)direction.Opposite()] = this;
    }

    public HexEdgeType GetEdgeType(HexDirection direction) {
        return HexMetrics.GetEdgeType(
            Elevation, _neighbors[(int)direction].Elevation
        );
    }

    public HexEdgeType GetEdgeType(HexCell otherCell) {
        return HexMetrics.GetEdgeType(
            Elevation, otherCell.Elevation
        );
    }

    public void SetShowLabel(bool visible) {
        if (UiRect is null) return;
        UiRect.Visible = visible;
    }

    private void Refresh() {
        if (Chunk is null) return;
        Chunk.Refresh();

        for (int i = 0; i < _neighbors.Length; i++) {
            HexCell neighbor = _neighbors[i];
            if (neighbor is null || neighbor.Chunk == Chunk) continue;
            neighbor.Chunk.Refresh();
        }
    }

    private void RefreshSelfOnly() {
        Chunk.Refresh();
    }

    public void SetOutgoingRiver(HexDirection direction) {
        if (_hasOutgoingRiver && _outgoingRiver == direction) {
            return;
        }

        HexCell neighbor = GetNeighbor(direction);
        if (neighbor == null || _elevation < neighbor._elevation) {
            return;
        }

        RemoveOutgoingRiver();
        if (_hasIncomingRiver && _incomingRiver == direction) {
            RemoveIncomingRiver();
        }

        _hasOutgoingRiver = true;
        _outgoingRiver = direction;
        
        neighbor.RemoveIncomingRiver();
        neighbor._hasIncomingRiver = true;
        neighbor._incomingRiver = direction.Opposite();

        SetRoad((int)direction, false);
    }


    public void RemoveOutgoingRiver() {
        if (!_hasOutgoingRiver) {
            return;
        }
        _hasOutgoingRiver = false;
        RefreshSelfOnly();

        HexCell neighbor = GetNeighbor(_outgoingRiver);
        neighbor._hasIncomingRiver = false;
        neighbor.RefreshSelfOnly();
    }

    public void RemoveIncomingRiver() {
        if (!_hasIncomingRiver) {
            return;
        }
        _hasIncomingRiver = false;
        RefreshSelfOnly();

        HexCell neighbor = GetNeighbor(_incomingRiver);
        neighbor._hasOutgoingRiver = false;
        neighbor.RefreshSelfOnly();
    }

    public void RemoveRiver() {
        RemoveOutgoingRiver();
        RemoveIncomingRiver();
    }

    public void AddRoad(HexDirection direction) {
        if (!_roads[(int)direction] 
            && !HasRiverThroughEdge(direction) 
            && GetElevationDifference(direction) <= 1
        ) {
            SetRoad((int)direction, true);
        }
    }

    public void RemoveRoads() {
        for (int i = 0; i < _neighbors.Length; i++) {
            if (_roads[i]) {
                SetRoad(i, false);
            }
        }
    }

    private void SetRoad(int index, bool state) {
        _roads[index] = state;
        _neighbors[index]._roads[(int)((HexDirection)index).Opposite()] = state;
        _neighbors[index].RefreshSelfOnly();
        RefreshSelfOnly();
    }
}