﻿using System;
using System.Collections;
using UnityEngine;
using Unity.Entities;

// A chunk in the world that manages terrain
public class VolumeChunk : MonoBehaviour
{
    // GameObject used to represent a 1x1 voxel in the world
    public GameObject voxel;

    // Contains if this chunk needs an update
    public bool needsUpdate = true;

    // Chunk size that is set by the WorldSystem's chunk size
    [NonSerialized] public int chunkSize = 16;

    // An array of all generated voxels
    [NonSerialized] public GameObject[,,] voxels;

    // Is set to true if player is currently in this chunk
    [NonSerialized] public bool isPlayerChunk = false;

    // Manages if this chunk has already been initialised
    [NonSerialized] public bool initialised = false;
}

// This System manages all chunks
// Currently generates terrain for each chunk with a perlin noise as height, gaps in height get filled
// Current TODO:
//  - Test to yield after multiple voxels to speed up generation without creating lag
//  - Mix two perlin noises without creating considerable lag for better and more random terrain
public class VolumeChunkSystem : ComponentSystem
{
    // Amplification of perlin noise
    private float amp = 10f;

    // Frequency of perlin noise
    private float frq = 20f;

    // World seed
    private float seed = 99;

    // Chunk Archetype
    public struct ChunkArchetype
    {
        public VolumeChunk chunk;
        public Transform transform;
    }

    protected override void OnUpdate()
    {
        foreach (var c in GetEntities<ChunkArchetype>())
        {
            // Update chunk if needs update
            if (!c.chunk.needsUpdate) continue;
            c.chunk.needsUpdate = false;

            IEnumerator terrainGenerationCoroutine = TerrainGenerator(c);
            c.chunk.StartCoroutine(terrainGenerationCoroutine);
        }
    }

    // Generate terrain for Chunk c.chunk
    IEnumerator TerrainGenerator(ChunkArchetype c)
    {
        // If already initialised just generate the final mesh again
        if (!c.chunk.initialised)
        {
            // Initialize voxels array
            c.chunk.voxels = new GameObject[c.chunk.chunkSize, c.chunk.chunkSize, 256];

            // Get perlin noise heights
            int[,,] perlinHeight = GenerateTerrainHeight(c);

            for (int x = 0; x < c.chunk.chunkSize; x++)
            {
                for (int z = 0; z < c.chunk.chunkSize; z++)
                {
                    for (int y = 0; y < 256; y++)
                    {
                        // Generate a new voxel at position [x,z]
                        GenerateVoxel(ref c, x, z, y, perlinHeight);
                        
                    }
                    
                }
                yield return new WaitForEndOfFrame();
            }

            c.chunk.initialised = true;
        }

        // Combine everything to 
        MeshCombiner.combineMeshWithMaterials(c.chunk.gameObject);

        yield break;
    }

    // Pre-generate perlin noise
    // Generates with 1 unit overhead to each side to account for the height gap fill algorithm on the border of chunks
    private int[,,] GenerateTerrainHeight(ChunkArchetype c)
    {
        int[,,] voxelTypes = new int[c.chunk.chunkSize, c.chunk.chunkSize, 256];

        for (int x = 0; x < c.chunk.chunkSize; x++)
        {
            for (int y = 0; y < c.chunk.chunkSize; y++)
            {
                Vector3 position = c.transform.position;

                // Add current voxel position, the subtraction accounts for the overhead we generate
                position.x += x;
                position.z += y;

                int baseZ = Mathf.FloorToInt(Mathf.PerlinNoise((1000000f + position.x) / frq, (seed + 1000000f + position.z) / frq) * amp + 10);
                for (int z = 0; z < baseZ; z++)
                {
                    // Generate perlin noise height
                    voxelTypes[x, y, z] = 1;
                }
            }
        }

        return voxelTypes;
    }

    // Generate a new voxel and fill the height gap with more voxels
    private void GenerateVoxel(ref ChunkArchetype c, int x, int z, int y, int[,,] voxelTypes)
    {
        // Take chunk position as reference
        Vector3 position = c.transform.position;

        position.x += x;
        position.z += z;

        // Save our old position
        float oldPositionY = position.y;

        // Get how many blocks high we have to fill

        if (voxelTypes[x, z, y] == 1)
        {

            if (!NeedsRender(voxelTypes, x, z, y, c.chunk.chunkSize))
            {
                return;
            }

            position.y = oldPositionY + y;
            GameObject newVoxel = GameObject.Instantiate(c.chunk.voxel, position, Quaternion.identity);
            newVoxel.transform.parent = c.transform;

            RemoveInvisibleFaces(voxelTypes, x, z, y, ref newVoxel, c.chunk.chunkSize);

            //newVoxel.SetActive(false);

            if (c.chunk.voxels[x, z, y])
            {
                GameObject.Destroy(c.chunk.voxels[x, z, y]);
            }

            c.chunk.voxels[x, z, y] = newVoxel.gameObject;
        }
    }

    private bool NeedsRender(int[,,] voxelTypes, int perlinX, int perlinZ, int perlinY, int chunkSize)
    {
        if (
            voxelTypes[perlinX, perlinZ, perlinY + 1] == 1                                  // Top
            && perlinY > 0 && voxelTypes[perlinX, perlinZ, perlinY - 1] == 1                // Bottom
            && perlinX < chunkSize - 1 && voxelTypes[perlinX + 1, perlinZ, perlinY] == 1    // Front
            && perlinX > 0 && voxelTypes[perlinX - 1, perlinZ, perlinY] == 1                // Back
            && perlinZ < chunkSize - 1 && voxelTypes[perlinX, perlinZ + 1, perlinY] == 1    // Right
            && perlinZ > 0 && voxelTypes[perlinX, perlinZ - 1, perlinY] == 1)               // Left
        {
            return false;
        }

        return true;
    }

    private void RemoveInvisibleFaces(int[,,] voxelTypes, int x, int z, int y, ref GameObject newVoxel, int chunkSize)
    {
        if (voxelTypes[x, z, y + 1] == 1)
        {
            newVoxel.transform.Find("Top").gameObject.SetActive(false);
        }

        if (y > 0 && voxelTypes[x, z, y - 1] == 1)
        {

            newVoxel.transform.Find("Bottom").gameObject.SetActive(false);
        }

        if (x < chunkSize - 1 && voxelTypes[x + 1, z, y] == 1)
        {
            newVoxel.transform.Find("Front").gameObject.SetActive(false);
        }

        if (x > 0 && voxelTypes[x - 1, z, y] == 1)
        {
            newVoxel.transform.Find("Back").gameObject.SetActive(false);
        }

        if (z < chunkSize - 1 && voxelTypes[x, z + 1, y] == 1)
        {
            newVoxel.transform.Find("Right").gameObject.SetActive(false);
        }

        if (z > 0 && voxelTypes[x, z - 1, y] == 1)
        {
            newVoxel.transform.Find("Left").gameObject.SetActive(false);
        }
    }
}