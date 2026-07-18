using Hypersycos.Utils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Unity.Mathematics;
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
    Int2[] sizes;

    public float[] heightMap;

    public int xSize;
    public int zSize;
    public GameObject terrainRenderer;
    Int2 chunkSize;

    public Vector3 scale;
    Vector3 meshOffset;

    const int maxChunkWidth = (1 << 8) - 3;
    const int maxChunkHeight = (1 << 8) - 3;

    public async Task<Tuple<float[], int, int>> SetSize(int x, int z, int chunkWidth = int.MaxValue, int chunkHeight = int.MaxValue)
    {
        xSize = x;
        zSize = z;

        chunkSize = new(Mathf.Max(Mathf.Min(chunkWidth, maxChunkWidth), 32),
                        Mathf.Max(Mathf.Min(chunkHeight, maxChunkHeight), 32));

        if (x <= (chunkSize.x + 1) && z <= (chunkSize.y + 1))
            meshCount = new(1, 1);
        else
            meshCount = new(Mathf.CeilToInt((float)(x-1) / chunkSize.x),
                            Mathf.CeilToInt((float)(z-1) / chunkSize.y));
        int totalMeshCount = meshCount.x * meshCount.y;

        heightMap = new float[(x+2) * (z+2)];
        sizes = new Int2[totalMeshCount];

        meshes = new Mesh[totalMeshCount];

        float xOffset = -x / 2;
        float zOffset = -z / 2;

        for (int i = 0; i < totalMeshCount; i++)
        {
            GameObject obj = Instantiate(terrainRenderer, transform);
            obj.name = $"{i}: {i % meshCount.x},{i / meshCount.x}";
            meshes[i] = new();
            obj.GetComponent<MeshFilter>().mesh = meshes[i];
            obj.transform.position = new Vector3(i % meshCount.x * chunkSize.x + xOffset, 0, i / meshCount.x * chunkSize.y + zOffset);
        }

        /*        for (int m = 0; m < meshCount.x * meshCount.y; m++)
        { */

        await Task.Run(() =>
        {
            Parallel.For(0, meshCount.x * meshCount.y, (m) =>
            {
                int width;
                int height;
                if (meshCount.x == 1)
                    width = x;
                else if (m % meshCount.x == meshCount.x - 1)
                    width = x - chunkSize.x * (meshCount.x - 1);
                else
                    width = chunkSize.x + 1;

                if (meshCount.y == 1)
                    height = z;
                else if (m / meshCount.x >= meshCount.y - 1)
                    height = z - chunkSize.y * (meshCount.y - 1);
                else
                    height = chunkSize.y + 1;

                sizes[m] = new(width, height);
            });
        });

        return new(heightMap, x + 2, z + 2);
    }

    static int Test(List<Vector3> vertices, Span<int> indices, Span<Vector3> verts, int count, Func<float,bool> test, float threshold)
    {
        Span<int> outIndices = stackalloc int[4];
        Span<Vector3> outVertices = stackalloc Vector3[4];
        int outCount = 0;

        for (int i = 0; i < count; i++)
        {
            int j = (i + 1) % count;

            Vector3 a = verts[i];
            Vector3 b = verts[j];

            int ia = indices[i];
            int ib = indices[j];

            bool inA = test(a.y);
            bool inB = test(b.y);

            if (inA)
            {
                outIndices[outCount] = ia;
                outVertices[outCount++] = a;
            }

            if (inA != inB)
            {
                float t = (threshold - a.y) / (b.y - a.y);
                Vector3 p = Vector3.Lerp(a, b, t);

                vertices.Add(p);

                outIndices[outCount] = vertices.Count - 1;
                outVertices[outCount++] = p;
            }
        }

        for (int i = 0; i < outCount; i++)
        {
            indices[i] = outIndices[i];
            verts[i] = outVertices[i];
        }

        return outCount;
    }

    private void BuildQuad(int i, int j, List<Vector3> vertices, List<int> triangles, int width, float minimumThreshold, float maximumThreshold)
    {
        //Interpolates the vector so it sits on the plane
        int createIntersection(int idxA, int idxB, float planeY)
        {
            Vector3 pA = vertices[idxA];
            Vector3 pB = vertices[idxB];

            float t = Mathf.Clamp01((planeY - pA.y) / (pB.y - pA.y));
            Vector3 clippedPoint = Vector3.Lerp(pA, pB, t);
            clippedPoint.y = planeY;

            vertices.Add(clippedPoint);
            return vertices.Count - 1;
        }

        //Sutherland-Hodgman for a single plane
        int clipPolygon(ReadOnlySpan<int> subjectPoly, int subjectCount, Span<int> outPoly, float threshold, bool isMin)
        {
            int outCount = 0;

            for (int n = 0; n < subjectCount; n++)
            {
                int curIdx = subjectPoly[n];
                int prevIdx = subjectPoly[(n + subjectCount - 1) % subjectCount];

                float curY = vertices[curIdx].y;
                float prevY = vertices[prevIdx].y;

                bool curInside = isMin ? (curY >= threshold) : (curY <= threshold);
                bool prevInside = isMin ? (prevY >= threshold) : (prevY <= threshold);

                if (curInside)
                {
                    if (!prevInside)
                    {
                        outPoly[outCount] = createIntersection(prevIdx, curIdx, threshold);
                        outCount++;
                    }

                    outPoly[outCount] = curIdx;
                    outCount++;
                }
                else if (prevInside)
                {
                    outPoly[outCount] = createIntersection(prevIdx, curIdx, threshold);
                    outCount++;
                }
            }
            return outCount;
        }

        int idxBL = i + j * width;
        int idxTL = i + (j + 1) * width;
        int idxTR = (i + 1) + (j + 1) * width;
        int idxBR = (i + 1) + j * width;

        float h1, h2, h3, h4;
        h1 = vertices[idxBL].y;
        h2 = vertices[idxTL].y;
        h3 = vertices[idxTR].y;
        h4 = vertices[idxBR].y;

        if (Mathf.Max(h1, h2, h3, h4) < maximumThreshold && Mathf.Min(h1, h2, h3, h4) > minimumThreshold)
        {
            SimpleBuildTriangle(idxBL, idxTL, idxTR, idxBR, vertices, triangles);
            return;
        }

        Span<int> polyA = stackalloc int[8];
        Span<int> polyB = stackalloc int[8];

        // Seed the initial quad into Poly A
        polyA[0] = idxBL;
        polyA[1] = idxTL;
        polyA[2] = idxTR;
        polyA[3] = idxBR;
        int polyCount = 4;

        // 1. Cut away anything below the floor
        polyCount = clipPolygon(polyA, polyCount, polyB, minimumThreshold, isMin: true);
        if (polyCount < 3) return;

        // 2. Cut away anything above the ceiling
        polyCount = clipPolygon(polyB, polyCount, polyA, maximumThreshold, isMin: false);
        if (polyCount < 3) return;

        // 3. Fan-triangulate the result
        for (int k = 1; k < polyCount - 1; k++)
        {
            triangles.Add(polyA[0]);
            triangles.Add(polyA[k]);
            triangles.Add(polyA[k + 1]);
        }
    }


    private void SimpleBuildTriangle(int idxBL, int idxTL, int idxTR, int idxBR, List<Vector3> vertices, List<int> triangles)
    {
        float TR = vertices[idxTR].y;
        float BL = vertices[idxBL].y;
        float TL = vertices[idxTL].y;
        float BR = vertices[idxBR].y;

        if (Mathf.Abs(TR - BL) > Mathf.Abs(TL - BR))
        {
            triangles.Add(idxBL);
            triangles.Add(idxTL);
            triangles.Add(idxBR);

            triangles.Add(idxBR);
            triangles.Add(idxTL);
            triangles.Add(idxTR);
        }
        else
        {
            triangles.Add(idxBL);
            triangles.Add(idxTR);
            triangles.Add(idxBR);

            triangles.Add(idxBL);
            triangles.Add(idxTL);
            triangles.Add(idxTR);
        }
    }
    
    public async Task BuildMeshes(IProgress<float> progress, float minimumThreshold=float.MinValue, float maximumThreshold=float.MaxValue)
    {
        transform.localScale = scale;

        bool hasThreshold = minimumThreshold > float.MinValue || maximumThreshold < float.MaxValue;

        List<int>[] triangles = new List<int>[meshCount.x * meshCount.y];
        List<Vector3>[] vertices = new List<Vector3>[meshCount.x * meshCount.y];

        for (int i = 0; i < meshCount.x * meshCount.y; i++)
        {
            triangles[i] = new((sizes[i].x + 1) * (sizes[i].y + 1) * 6);
            vertices[i] = new ((sizes[i].x + 2) * (sizes[i].y + 2) * (hasThreshold ? 2 : 1));
        }

        int[] progressTracker = new int[meshCount.x * meshCount.y];

        void updateProgress(int m)
        {
            progressTracker[m]++;
            progress.Report(progressTracker.Sum() / (4f * meshCount.x * meshCount.y));
        }

        await Task.Run(() =>
        {
            Parallel.For(0, meshCount.x * meshCount.y, (m) =>
            {
                var size = sizes[m];
                int width = size.x;
                int height = size.y;

                int xBase = (m % meshCount.x) * chunkSize.x;
                int yBase = (m / meshCount.x) * chunkSize.y;

                int edgeWidth = width + 2;
                int edgeHeight = height + 2;

                for (int j = 0; j < edgeHeight; j++)
                {
                    for (int i = 0; i < edgeWidth; i++)
                    {
                        vertices[m].Add(new(i-1, heightMap[i + xBase + (j + yBase) * (xSize + 2)], j-1));
                    }
                }
                updateProgress(m);  

                for (int j = 0; j < edgeHeight - 1; j++)
                {
                    for (int i = 0; i < edgeWidth - 1; i++)
                    {
                        if (hasThreshold)
                            BuildQuad(i, j, vertices[m], triangles[m], edgeWidth, minimumThreshold, maximumThreshold);
                        else
                        {
                            int idxBL = i + j * edgeWidth;
                            int idxTL = i + (j + 1) * edgeWidth;
                            int idxTR = (i + 1) + (j + 1) * edgeWidth;
                            int idxBR = (i + 1) + j * edgeWidth;
                            SimpleBuildTriangle(idxBL, idxTL, idxTR, idxBR, vertices[m], triangles[m]);
                        }
                    }
                }
                updateProgress(m);
            });
        });

        for (int m=0; m < meshCount.x * meshCount.y; m++)
        {
            var size = sizes[m];
            int width = size.x;
            int height = size.y;

            int edgeWidth = width + 2;
            int edgeHeight = height + 2;


            meshes[m].Clear();
            if (vertices[m].Count > 65536)
            {
                Debug.LogWarning($"Mesh {m} has {vertices[m].Count} vertices, using Uint32 indices as fallback");
                meshes[m].indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            }

            meshes[m].SetVertices(vertices[m]);
            meshes[m].SetTriangles(triangles[m], 0);
            meshes[m].RecalculateNormals();
            meshes[m].RecalculateTangents();
            updateProgress(m);

            await Task.Run(() =>
            {
                Func<Vector3, bool> test = (x) => x.x < 0 || x.x >= width || x.z < 0 || x.z >= height;

                int write = 0;

                for (int read = 0; read < triangles[m].Count; read += 3)
                {
                    if (test(vertices[m][triangles[m][read]]) ||
                        test(vertices[m][triangles[m][read + 1]]) ||
                        test(vertices[m][triangles[m][read + 2]]))
                    {
                        continue; // discard triangle
                    }

                    triangles[m][write] = triangles[m][read];
                    triangles[m][write + 1] = triangles[m][read + 1];
                    triangles[m][write + 2] = triangles[m][read + 2];

                    write += 3;
                }

                if (write < triangles[m].Count)
                    triangles[m].RemoveRange(write, triangles[m].Count - write);
            });

            meshes[m].SetTriangles(triangles[m], 0);
            meshes[m].RecalculateBounds();

            updateProgress(m);
        }

        foreach(MeshCollider collider in GetComponentsInChildren<MeshCollider>())
        {
            collider.sharedMesh = collider.GetComponent<MeshFilter>().mesh;
            collider.providesContacts = true;
        }
    }
}