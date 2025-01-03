﻿using Godot;
using System;

namespace JHM.MeshBasics;

public sealed partial class HexMapEditor : Control {
    [Export] private ShaderMaterial _terrainMaterial;
    [Export] private PackedScene _unitPrefab;
    [Export] public Color[] Colors { get; set; }
    [Export] public HexGrid HexGrid { get; set; }

    private int _activeElevation = 1;
    private int _activeTerrainTypeIndex = 0;
    private bool _applyElevation = false;
    private bool _applyWaterLevel = false;
    private bool _applyUrbanLevel;
    private bool _applyFarmLevel;
    private bool _applyPlantLevel;
    private bool _applySpecialIndex;
    private bool _applyTerrainType = false;
    private int _brushSize;
    private OptionalToggle _riverMode;
    private OptionalToggle _roadMode;
    private OptionalToggle _wallMode;
    private bool _isDrag;
    private HexDirection _dragDirection;
    private HexCell _previousCell;
    private int _activeWaterLevel;
    private int _activeUrbanLevel = 1;
    private int _activeFarmLevel = 1;
    private int _activePlantLevel = 1;
    private int _activeSpecialIndex = 1;

    private const int _speed = 24;

    public override void _EnterTree() {
        base._EnterTree();
        ShowGrid(true);
        HexUnit.UnitPrefab = _unitPrefab;
        HexGrid.MapReset += OnMapReset;
        HexCellShaderData.SetShaderParameter("HEX_MAP_EDIT_MODE", true);
        SetEditMode(true);
    }

    public override void _ExitTree() {
        HexGrid.MapReset -= OnMapReset;
    }

    public override void _UnhandledInput(InputEvent @event) {
        if (Input.IsMouseButtonPressed(MouseButton.Left)) {
            HandleInput();
            return;
        }
        if (Input.IsKeyPressed(Key.U)) { 
            if (Input.IsKeyPressed(Key.Shift)) { 
                DestroyUnit();
            } else { 
                CreateUnit();
            }
            return;
        }
        _previousCell = null;
    }

    public void SetEditMode(bool value) {
        Visible = value;
        ProcessMode = value ? ProcessModeEnum.Inherit : ProcessModeEnum.Disabled;
        HexGrid.SetUIVisible(!value);
    }

    public void SetElevation(float elevationStep) {
        // var result = HexMetrics.Maxelevation * elevationStep / 100;
        _activeElevation = (int)elevationStep;
    }

    public void SetTerrainTypeIndex(float index) {
        _activeTerrainTypeIndex = (int)index;
    }

    public void ToggleElevationEnabled(bool value) {
        _applyElevation = value;
    }

    public void SetBrushSize(float size) {
        _brushSize = (int)size;
    }

    public void SetApplyTerrainType(bool value) => _applyTerrainType = value;

    public void SetRiverMode(int mode) {
        _riverMode = (OptionalToggle)mode;
    }

    public void SetRoadMode(int mode) {
        _roadMode = (OptionalToggle)mode;
    }

    public void SetWalledMode(int mode) {
        _wallMode = (OptionalToggle)mode;
    }

    public void SetApplyWaterLevel(bool toggle) {
        _applyWaterLevel = toggle;
    }

    public void SetWaterLevel(float level) {
        _activeWaterLevel = (int)level;
    }

    public void SetApplyUrbanLevel(bool toggle) {
        _applyUrbanLevel = toggle;
    }

    public void SetUrbanLevel(float level) {
        _activeUrbanLevel = (int)level;
    }

    public void SetApplyFarmLevel(bool toggle) {
        _applyFarmLevel = toggle;
    }

    public void SetFarmLevel(float level) {
        _activeFarmLevel = (int)level;
    }

    public void SetApplyPlantLevel(bool toggle) {
        _applyPlantLevel = toggle;
    }

    public void SetPlantLevel(float level) {
        _activePlantLevel = (int)level;
    }

    public void SetApplySpecialIndex(bool toggle) {
        _applySpecialIndex = toggle;
    }

    public void SetSpecialIndex(float index) {
        _activeSpecialIndex = (int)index;
    }

    public void ShowGrid(bool visible) {
        _terrainMaterial.SetShaderParameter("GRID_ON", visible);
        GD.Print(_terrainMaterial.Get("GRID_ON").AsBool());
    }

    private void ValidateDrag(HexCell currentCell) {
        for (
            _dragDirection = HexDirection.NE;
            _dragDirection <= HexDirection.NW;
            _dragDirection++
        ) {
            if (_previousCell.GetNeighbor(_dragDirection) == currentCell) {
                _isDrag = true;
                return;
            }
        }
        _isDrag = false;
    }

    private void HandleInput() {
        if (HexGrid.IsRefreshing) return;
        var cell = GetCellUnderCursor();
        if (cell is null) {
            _previousCell = null;
            return;
        }
        if (_previousCell != null && _previousCell != cell) {
            ValidateDrag(cell);
        }
        else {
            _isDrag = false;
        }
        EditCells(cell);
        _previousCell = cell;
    }

    private HexCell GetCellUnderCursor() {
        var mousePosition = Mouse3D.MouseWorldPosition;
        return HexGrid.GetCell(mousePosition);
    }

    private void CreateUnit() { 
        var cell = GetCellUnderCursor();
        if (cell is null || cell.Unit != null) return;
        var rotation = (float)(new Random().NextDouble() * 360.0f);
        var unit = HexGrid.InstantiateOrphan<HexUnit>(HexUnit.UnitPrefab);
        HexGrid.AddUnit(unit, cell, rotation);
    }

    private void DestroyUnit() {
        HexCell cell = GetCellUnderCursor();
        if (cell is null || cell.Unit is null) return;
        HexGrid.RemoveUnit(cell.Unit);
    }

    private void EditCells(HexCell center) {
        if (center is null) return;
        int centerX = center.Coordinates.X;
        int centerZ = center.Coordinates.Z;
        for (int r = 0, z = centerZ - _brushSize; z <= centerZ; z++, r++) {
            for (int x = centerX - r; x <= centerX + _brushSize; x++) {
                EditCell(HexGrid.GetCell(new HexCoordinates(x, z)));
            }
        }

        for (int r = 0, z = centerZ + _brushSize; z > centerZ; z--, r++) {
            for (int x = centerX - _brushSize; x <= centerX + r; x++) {
                EditCell(HexGrid.GetCell(new HexCoordinates(x, z)));
            }
        }

    }

    private void EditCell(HexCell cell) {
        if (cell is null) return;

        if (_applyTerrainType) {
            cell.TerrainTypeIndex = _activeTerrainTypeIndex;
        }

        if (_applyElevation) {
            cell.Elevation = _activeElevation;
        }
        if (_applyWaterLevel) {
            cell.WaterLevel = _activeWaterLevel;
        }

        if (_riverMode == OptionalToggle.Off) {
            cell.RemoveRiver();
        }
        if (_roadMode == OptionalToggle.Off) {
            cell.RemoveRoads();
        }
        if (_wallMode != OptionalToggle.Disable) {
            cell.Walled = _wallMode == OptionalToggle.On;
        }

        if (_isDrag) {
            HexCell otherCell = cell.GetNeighbor(_dragDirection.Opposite());
            if (otherCell is not null) {
                if (_riverMode == OptionalToggle.On) {
                    otherCell.SetOutgoingRiver(_dragDirection);
                }
                if (_roadMode == OptionalToggle.On) {
                    otherCell.AddRoad(_dragDirection);
                }
            }
        }
        if (_applySpecialIndex) {
            cell.SpecialIndex = _activeSpecialIndex;
        }
        if (_applyUrbanLevel) {
            cell.UrbanLevel = _activeUrbanLevel;
        }
        if (_applyFarmLevel) {
            cell.FarmLevel = _activeFarmLevel;
        }
        if (_applyPlantLevel) {
            cell.PlantLevel = _activePlantLevel;
        }
    }

    private void OnMapReset() {
        _previousCell = null;
    }


    #region Definitions
    private enum OptionalToggle {
        Disable = 0, On = 1, Off = 2
    }
    #endregion
}
