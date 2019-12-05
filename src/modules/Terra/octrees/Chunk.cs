using System;

public struct Chunk
{
    public uint x { get; set; }
    public uint y { get; set; }
    public uint z { get; set; }

    public Memory<uint> voxels { get; set; }

    public bool isEmpty { get; set; }
}