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

    public float[,] heightMap;
    public int xSize;
    public int zSize;
    public GameObject terrainRenderer;

    public Vector3 scale;
    Vector3 meshOffset;


    void Start()
    {
        SetSize(xSize, zSize, out _);
        BuildMeshes();
    }

    public void SetSize(int x, int z, out float[,] heightmap)
    {
        xSize = x;
        zSize = z;

        if (x <= 1 << 8 && z <= 1 << 8)
            meshCount = new(1, 1);
        else
            meshCount = new((int)Mathf.Ceil((float)(x-1) / ((1 << 8) - 1)),
                            (int)Mathf.Ceil((float)(z-1) / ((1 << 8) - 1)));
        int totalMeshCount = meshCount.x * meshCount.y;

        heightMap = new float[x,z];
        heightmap = heightMap;

        triangles = new int[totalMeshCount][];
        vertices = new Vector3[totalMeshCount][];

        meshes = new Mesh[totalMeshCount];

        for (int i = 0; i < totalMeshCount; i++)
        {
            GameObject obj = Instantiate(terrainRenderer, transform);
            obj.name = $"{i}: {i % meshCount.x},{i / meshCount.x}";
            meshes[i] = new();
            obj.GetComponent<MeshFilter>().mesh = meshes[i];
            obj.GetComponent<MeshCollider>().sharedMesh = meshes[i];
            obj.transform.position = new Vector3(i % meshCount.x * ((1 << 8) - 1), 0, i / meshCount.x * ((1 << 8) - 1));
        }

        for (int m = 0; m < meshCount.x * meshCount.y; m++)
        {
            int width;
            int height;
            if (meshCount.x == 1)
                width = x;
            else if (m % meshCount.x == meshCount.x - 1)
                width = x - ((1 << 8) - 1) * (meshCount.x - 1);
            else
                width = 1 << 8;

            if (meshCount.y == 1)
                height = z;
            else if (m / meshCount.x >= meshCount.y - 1)
                height = z - ((1 << 8) - 1) * (meshCount.y - 1);
            else
                height = 1 << 8;

            triangles[m] = new int[(width - 1) * (height - 1) * 6];
            int k = 0;
            for (int j = 0; j < height - 1; j++)
            {
                for (int i = 0; i < width - 1; i++)
                {
                    triangles[m][k] = i + j * width;
                    triangles[m][k + 1] = i + (j + 1) * width;
                    triangles[m][k + 2] = (i + 1) + j * width;

                    k += 3;

                    triangles[m][k] = (i + 1) + j * width;
                    triangles[m][k + 1] = i + (j + 1) * width;
                    triangles[m][k + 2] = (i + 1) + (j + 1) * width;

                    k += 3;
                }
            }

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

    private void BuildMeshes()
    {
        for (int m = 0; m < meshCount.x * meshCount.y; m++)
        {
            int width;
            int height;
            if (meshCount.x == 1)
                width = xSize;
            else if (m % meshCount.x == meshCount.x - 1)
                width = xSize - ((1 << 8) - 1) * (meshCount.x - 1);
            else
                width = 1 << 8;

            if (meshCount.y == 1)
                height = zSize;
            else if (m / meshCount.x >= meshCount.y - 1)
                height = zSize - ((1 << 8) - 1) * (meshCount.y - 1);
            else
                height = 1 << 8;

            int xBase = (m % meshCount.x) * ((1 << 8) - 1);
            int yBase = (m / meshCount.x) * ((1 << 8) - 1);
            for (int j = 0; j < height; j++)
            {
                for (int i = 0; i < width; i++)
                {
                    vertices[m][i + j * width].y = heightMap[i + xBase, j + yBase];
                }
            }
            meshes[m].Clear();
            meshes[m].vertices = vertices[m];
            meshes[m].triangles = triangles[m];
            meshes[m].RecalculateNormals();
            meshes[m].RecalculateTangents();
        }
        transform.localScale = scale;
    }
}