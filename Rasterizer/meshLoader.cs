using OpenTK.Mathematics;

namespace Rasterizer; 

// Mesh and MeshLoader based on work by JTalton; https://web.archive.org/web/20160123042419/www.opentk.com/node/642
// Only triangles and quads with vertex positions, normals, and texture coordinates are supported
// Any other content in the OBJ file is ignored
// You may need to convert existing OBJ files to use only the supported features
public class MeshLoader {
    public bool Load(Mesh mesh, string fileName) {
        try {
            using StreamReader streamReader = new(fileName);
            Load(mesh, streamReader);
            streamReader.Close();
            return true;
        } catch {
            return false;
        }
    }

    private readonly char[] _splitCharacters = { ' ' };

    private readonly List<Vector3> _vertices = new();
    private readonly List<Vector3> _normals = new();
    private readonly List<Vector2> _texCoords = new();
    private List<Mesh.ObjVertex> _objVertices = new();
    private List<Mesh.ObjTriangle> _objTriangles = new();

    private void Load(Mesh mesh, TextReader textReader) {
        _objVertices = new List<Mesh.ObjVertex>();
        _objTriangles = new List<Mesh.ObjTriangle>();
        string? line;
        while ((line = textReader.ReadLine()) != null) {
            line = line.Trim(_splitCharacters);
            line = line.Replace("  ", " ");
            string[] parameters = line.Split(_splitCharacters);
            switch (parameters[0]) {
                case "v": // vertex
                    float x = float.Parse(parameters[1]);
                    float y = float.Parse(parameters[2]);
                    float z = float.Parse(parameters[3]);
                    _vertices.Add(new Vector3(x, y, z));
                    break;
                case "vt": // texCoord
                    float u = float.Parse(parameters[1]);
                    float v = float.Parse(parameters[2]);
                    _texCoords.Add(new Vector2(u, v));
                    break;
                case "vn": // normal
                    float nx = float.Parse(parameters[1]);
                    float ny = float.Parse(parameters[2]);
                    float nz = float.Parse(parameters[3]);
                    _normals.Add(new Vector3(nx, ny, nz));
                    break;
                case "f":
                    switch (parameters.Length) {
                        case 4:
                            Mesh.ObjTriangle objTriangle = new() {
                                Index0 = ParseFaceParameter(parameters[1]),
                                Index1 = ParseFaceParameter(parameters[2]),
                                Index2 = ParseFaceParameter(parameters[3])
                            };
                            _objTriangles.Add(objTriangle);
                            break;
                        case 5: {
                            // arbitrary split into two triangles
                            int p0 = ParseFaceParameter(parameters[1]);
                            int p1 = ParseFaceParameter(parameters[2]);
                            int p2 = ParseFaceParameter(parameters[3]);
                            int p3 = ParseFaceParameter(parameters[4]);
                            Mesh.ObjTriangle objTriangle1 = new() {
                                Index0 = p0,
                                Index1 = p1,
                                Index2 = p2
                            };
                            Mesh.ObjTriangle objTriangle2 = new() {
                                Index0 = p2,
                                Index1 = p3,
                                Index2 = p0
                            };
                            _objTriangles.Add(objTriangle1);
                            _objTriangles.Add(objTriangle2);
                        }

                            break;
                    }

                    break;
            }
        }

        mesh.Vertices = _objVertices.ToArray();
        mesh.Triangles = _objTriangles.ToArray();
        _vertices.Clear();
        _normals.Clear();
        _texCoords.Clear();
        _objVertices.Clear();
        _objTriangles.Clear();
    }

    private readonly char[] _faceParameterSplitter = { '/' };

    private int ParseFaceParameter(string faceParameter) {
        Vector2 texCoord = new();
        Vector3 normal = new();
        string[] parameters = faceParameter.Split(_faceParameterSplitter);
        int vertexIndex = int.Parse(parameters[0]);
        if (vertexIndex < 0) vertexIndex = _vertices.Count + vertexIndex;
        else vertexIndex--;
        Vector3 vertex = _vertices[vertexIndex];
        if (parameters.Length > 1)
            if (parameters[1] != "") {
                int texCoordIndex = int.Parse(parameters[1]);
                if (texCoordIndex < 0) texCoordIndex = _texCoords.Count + texCoordIndex;
                else texCoordIndex--;
                texCoord = _texCoords[texCoordIndex];
            }

        if (parameters.Length > 2) {
            int normalIndex = int.Parse(parameters[2]);
            if (normalIndex < 0) normalIndex = _normals.Count + normalIndex;
            else normalIndex--;
            normal = _normals[normalIndex];
        }

        return AddObjVertex(ref vertex, ref texCoord, ref normal);
    }

    private int AddObjVertex(ref Vector3 vertex, ref Vector2 texCoord, ref Vector3 normal) {
        Mesh.ObjVertex newObjVertex = new() {
            Vertex = vertex,
            TexCoord = texCoord,
            Normal = normal
        };
        _objVertices.Add(newObjVertex);
        return _objVertices.Count - 1;
    }
}