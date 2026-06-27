using Hypersycos.Utils;
using System.Linq;
using UnityEngine;

public class TerrainDrawer : MonoBehaviour
{
    struct Int2
    {
        public int x;
        public int y;

        public Int2(int x, int y)
        {
            this.x = x;
            this.y = y;
        }
    }

    Int2 meshCount;
    Mesh[] meshes;
    private Vector3[][] vertices;
    private int[][] triangles;
    Int2[] sizes;

    public float[,] heightMap;

    public int xSize;
    public int zSize;
    public GameObject terrainRenderer;

    public Vector3 scale;
    Vector3 meshOffset;

    const int maxChunkWidth = (1 << 8) - 3;
    const int maxChunkHeight = (1 << 8) - 3;

    public void SetSize(int x, int z, out float[,] heightmap)
    {
        xSize = x;
        zSize = z;

        if (x <= (maxChunkWidth + 1) && z <= (maxChunkHeight + 1))
            meshCount = new(1, 1);
        else
            meshCount = new(Mathf.CeilToInt((float)(x-1) / maxChunkWidth),
                            Mathf.CeilToInt((float)(z-1) / maxChunkHeight));
        int totalMeshCount = meshCount.x * meshCount.y;

        heightMap = new float[x+2,z+2];
        heightmap = heightMap;

        triangles = new int[totalMeshCount][];
        vertices = new Vector3[totalMeshCount][];
        sizes = new Int2[totalMeshCount];

        meshes = new Mesh[totalMeshCount];

        float xOffset = -x * scale.x / 2;
        float zOffset = -z * scale.z / 2;

        for (int i = 0; i < totalMeshCount; i++)
        {
            GameObject obj = Instantiate(terrainRenderer, transform);
            obj.name = $"{i}: {i % meshCount.x},{i / meshCount.x}";
            meshes[i] = new();
            obj.GetComponent<MeshFilter>().mesh = meshes[i];
            obj.GetComponent<MeshCollider>().sharedMesh = meshes[i];
            obj.transform.position = new Vector3(i % meshCount.x * maxChunkWidth + xOffset, 0, i / meshCount.x * maxChunkHeight + zOffset);
        }

        for (int m = 0; m < meshCount.x * meshCount.y; m++)
        {
            int width;
            int height;
            if (meshCount.x == 1)
                width = x;
            else if (m % meshCount.x == meshCount.x - 1)
                width = x - maxChunkWidth * (meshCount.x - 1);
            else
                width = maxChunkWidth + 1;

            if (meshCount.y == 1)
                height = z;
            else if (m / meshCount.x >= meshCount.y - 1)
                height = z - maxChunkHeight * (meshCount.y - 1);
            else
                height = maxChunkHeight + 1;

            sizes[m] = new(width, height);

            triangles[m] = new int[(width - 1) * (height - 1) * 6];
            vertices[m] = new Vector3[width * height];
            for (int j = 0; j < height; j++)
            {
                for (int i = 0; i < width; i++)
                {
                    vertices[m][i + j * width].x = i;
                    vertices[m][i + j * width].z = j;
                }
            }
        }
    }

    public int BuildTriangle(int i, int j, int k, Vector3[] vertices, int[] triangles, int width)
    {
        float TR = vertices[(i + 1) + (j + 1) * width].y;
        float BL = vertices[i + j * width].y;
        float TL = vertices[i + (j + 1) * width].y;
        float BR = vertices[(i + 1) + j * width].y;
        if (Mathf.Abs(TR - BL) > Mathf.Abs(TL - BR))
        {
            triangles[k] = i + j * width;
            triangles[k + 1] = i + (j + 1) * width;
            triangles[k + 2] = (i + 1) + j * width;

            k += 3;

            triangles[k] = (i + 1) + j * width;
            triangles[k + 1] = i + (j + 1) * width;
            triangles[k + 2] = (i + 1) + (j + 1) * width;

            k += 3;
        }
        else
        {
            triangles[k] = i + j * width;
            triangles[k + 1] = (i + 1) + (j + 1) * width;
            triangles[k + 2] = (i + 1) + j * width;

            k += 3;

            triangles[k] = i + j * width;
            triangles[k + 1] = i + (j + 1) * width;
            triangles[k + 2] = (i + 1) + (j + 1) * width;

            k += 3;
        }
        return k;
    }

    public void BuildMeshes()
    {
        for (int m = 0; m < meshCount.x * meshCount.y; m++)
        {
            var size = sizes[m];
            int width = size.x;
            int height = size.y;

            int xBase = (m % meshCount.x) * maxChunkWidth;
            int yBase = (m / meshCount.x) * maxChunkHeight;

            int edgeWidth = width + 2;
            int edgeHeight = height + 2;

            int[] trianglesWithEdges = new int[(edgeWidth - 1) * (edgeHeight - 1) * 6];
            Vector3[] verticesWithEdges = new Vector3[edgeWidth * edgeHeight];

            for (int j = 0; j < edgeHeight; j++)
            {
                for (int i = 0; i < edgeWidth; i++)
                {
                    verticesWithEdges[i + j * edgeWidth] = new(i, heightMap[i + xBase, j + yBase], j);
                }
            }

            for (int j = 0; j < height; j++)
            {
                for (int i = 0; i < width; i++)
                {
                    vertices[m][i + j * width].y = heightMap[i + xBase + 1, j + yBase + 1];
                }
            }

            int k = 0;
            for (int j = 0; j < height - 1; j++)
            {
                for (int i = 0; i < width - 1; i++)
                {
                    k = BuildTriangle(i, j, k, vertices[m], triangles[m], width);
                }
            }

            k = 0;
            for (int j = 0; j < edgeHeight - 1; j++)
            {
                for (int i = 0; i < edgeWidth - 1; i++)
                {
                    k = BuildTriangle(i, j, k, verticesWithEdges, trianglesWithEdges, edgeWidth);
                }
            }

            meshes[m].Clear();
            meshes[m].vertices = verticesWithEdges;
            meshes[m].triangles = trianglesWithEdges;
            meshes[m].RecalculateNormals();
            meshes[m].RecalculateTangents();

            Vector3[] normals = meshes[m].normals;
            Vector4[] tangents = meshes[m].tangents;

            Vector3[] croppedNormals = new Vector3[width * height];
            Vector4[] croppedtangents = new Vector4[width * height];

            k = 0;
            for (int j = 0; j < height; j++)
            {
                for (int i = 0; i < width; i++)
                {
                    croppedNormals[k] = normals[i + 1 + (j+1) * edgeWidth];
                    croppedtangents[k] = tangents[i + 1 + (j + 1) * edgeWidth];
                    k++;
                }
            }

            meshes[m].Clear();
            meshes[m].vertices = vertices[m];
            meshes[m].triangles = triangles[m];
            meshes[m].normals = croppedNormals;
            meshes[m].tangents = croppedtangents;
        }

        transform.localScale = scale;
    }
}