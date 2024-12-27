using Godot;
using System.Collections.Generic;

namespace JHM.MeshBasics;

public sealed partial class HexMesh : MeshInstance3D {
    [Export] public CollisionShape3D CollisionShape { get; set; }

    private ArrayMesh _mesh;
    private List<Vector3> _vertices = new();
    // private List<int> _triangles = new();
    private List<Color> _colors = new();

    public override void _Ready() {
        _mesh = Mesh as ArrayMesh;
        if (_mesh is null) {
            GD.PrintErr("HexMesh requires an ArrayMesh.");
        }
    }

    public void Triangulate(HexCell[] cells) {
        _mesh.ClearSurfaces();
        _vertices.Clear();
        // _triangles.Clear();
        _colors.Clear();
        for (int i = 0; i < cells.Length; i++) {
            Triangulate(cells[i]);
        }
        var surfaceTool = new SurfaceTool();

        surfaceTool.Begin(Mesh.PrimitiveType.Triangles);
        surfaceTool.SetMaterial(this.GetActiveMaterial(0));

        for(var n = _vertices.Count - 1; n >= 0; n--) {
            var vertex = _vertices[n];
            surfaceTool.SetColor(_colors[n]);
            surfaceTool.AddVertex(vertex);
        }

        surfaceTool.Commit(_mesh);

        CollisionShape.Shape = _mesh.CreateTrimeshShape();

        // _mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, _vertices.ToArray());
        // hexMesh.triangles = triangles.ToArray();
        // hexMesh.RecalculateNormals();
    }

    void Triangulate(HexCell cell) {
        Vector3 center = cell.Position;
        for (int i = 0; i < 6; i++) {
            AddTriangle(
                center,
                center + HexMetrics.Corners[i],
                center + HexMetrics.Corners[i + 1]
            );
            AddTriangleColor(cell.Color);
        }
    }

    void AddTriangle(Vector3 v1, Vector3 v2, Vector3 v3) {
        int vertexIndex = _vertices.Count;
        _vertices.Add(v1);
        _vertices.Add(v2);
        _vertices.Add(v3);
        // _triangles.Add(vertexIndex);
        // _triangles.Add(vertexIndex + 1);
        // _triangles.Add(vertexIndex + 2);
    }

    void AddTriangleColor(Color color) {
        _colors.Add(color);
        _colors.Add(color);
        _colors.Add(color);
    }
}
