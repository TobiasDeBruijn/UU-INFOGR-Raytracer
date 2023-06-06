using OpenTK.Mathematics;

namespace Template
{
    // Mesh and MeshLoader based on work by JTalton; https://web.archive.org/web/20160123042419/www.opentk.com/node/642
    // Only triangles and quads with vertex positions, normals, and texture coordinates are supported
    // Any other content in the OBJ file is ignored
    // You may need to convert existing OBJ files to use only the supported features
    public class MeshLoader
    {
        public bool Load(Mesh mesh, string fileName)
        {
            try
            {
                using StreamReader streamReader = new(fileName);
                Load(mesh, streamReader);
                streamReader.Close();
                return true;
            }
            catch { return false; }
        }

        readonly char[] splitCharacters = new char[] { ' ' };

        readonly List<Vector3> vertices = new();
        readonly List<Vector3> normals = new();
        readonly List<Vector2> texCoords = new();
        List<Mesh.ObjVertex> objVertices = new();
        List<Mesh.ObjTriangle> objTriangles = new();
        List<Mesh.ObjQuad> objQuads = new();

        void Load(Mesh mesh, TextReader textReader)
        {
            objVertices = new List<Mesh.ObjVertex>();
            objTriangles = new List<Mesh.ObjTriangle>();
            objQuads = new List<Mesh.ObjQuad>();
            string? line;
            while ((line = textReader.ReadLine()) != null)
            {
                line = line.Trim(splitCharacters);
                line = line.Replace("  ", " ");
                string[] parameters = line.Split(splitCharacters);
                switch (parameters[0])
                {
                    case "v": // vertex
                        float x = float.Parse(parameters[1]);
                        float y = float.Parse(parameters[2]);
                        float z = float.Parse(parameters[3]);
                        vertices.Add(new Vector3(x, y, z));
                        break;
                    case "vt": // texCoord
                        float u = float.Parse(parameters[1]);
                        float v = float.Parse(parameters[2]);
                        texCoords.Add(new Vector2(u, v));
                        break;
                    case "vn": // normal
                        float nx = float.Parse(parameters[1]);
                        float ny = float.Parse(parameters[2]);
                        float nz = float.Parse(parameters[3]);
                        normals.Add(new Vector3(nx, ny, nz));
                        break;
                    case "f":
                        switch (parameters.Length)
                        {
                            case 4:
                                Mesh.ObjTriangle objTriangle = new()
                                {
                                    Index0 = ParseFaceParameter(parameters[1]),
                                    Index1 = ParseFaceParameter(parameters[2]),
                                    Index2 = ParseFaceParameter(parameters[3])
                                };
                                objTriangles.Add(objTriangle);
                                break;
                            case 5:
                                if (OpenTKApp.allowPrehistoricOpenGL)
                                {
                                    Mesh.ObjQuad objQuad = new()
                                    {
                                        Index0 = ParseFaceParameter(parameters[1]),
                                        Index1 = ParseFaceParameter(parameters[2]),
                                        Index2 = ParseFaceParameter(parameters[3]),
                                        Index3 = ParseFaceParameter(parameters[4])
                                    };
                                    objQuads.Add(objQuad);
                                }
                                else
                                {
                                    // arbitrary split into two triangles
                                    int p0 = ParseFaceParameter(parameters[1]);
                                    int p1 = ParseFaceParameter(parameters[2]);
                                    int p2 = ParseFaceParameter(parameters[3]);
                                    int p3 = ParseFaceParameter(parameters[4]);
                                    Mesh.ObjTriangle objTriangle1 = new()
                                    {
                                        Index0 = p0,
                                        Index1 = p1,
                                        Index2 = p2,
                                    };
                                    Mesh.ObjTriangle objTriangle2 = new()
                                    {
                                        Index0 = p2,
                                        Index1 = p3,
                                        Index2 = p0,
                                    };
                                    objTriangles.Add(objTriangle1);
                                    objTriangles.Add(objTriangle2);
                                }
                                break;
                        }
                        break;
                }
            }
            mesh.vertices = objVertices.ToArray();
            mesh.triangles = objTriangles.ToArray();
            mesh.quads = objQuads.ToArray();
            vertices.Clear();
            normals.Clear();
            texCoords.Clear();
            objVertices.Clear();
            objTriangles.Clear();
            objQuads.Clear();
        }

        readonly char[] faceParameterSplitter = new char[] { '/' };
        int ParseFaceParameter(string faceParameter)
        {
            Vector2 texCoord = new();
            Vector3 normal = new();
            string[] parameters = faceParameter.Split(faceParameterSplitter);
            int vertexIndex = int.Parse(parameters[0]);
            if (vertexIndex < 0) vertexIndex = vertices.Count + vertexIndex;
            else vertexIndex--;
            Vector3 vertex = vertices[vertexIndex];
            if (parameters.Length > 1) if (parameters[1] != "")
                {
                    int texCoordIndex = int.Parse(parameters[1]);
                    if (texCoordIndex < 0) texCoordIndex = texCoords.Count + texCoordIndex;
                    else texCoordIndex--;
                    texCoord = texCoords[texCoordIndex];
                }
            if (parameters.Length > 2)
            {
                int normalIndex = int.Parse(parameters[2]);
                if (normalIndex < 0) normalIndex = normals.Count + normalIndex;
                else normalIndex--;
                normal = normals[normalIndex];
            }
            return AddObjVertex(ref vertex, ref texCoord, ref normal);
        }

        int AddObjVertex(ref Vector3 vertex, ref Vector2 texCoord, ref Vector3 normal)
        {
            Mesh.ObjVertex newObjVertex = new()
            {
                Vertex = vertex,
                TexCoord = texCoord,
                Normal = normal
            };
            objVertices.Add(newObjVertex);
            return objVertices.Count - 1;
        }
    }
}
