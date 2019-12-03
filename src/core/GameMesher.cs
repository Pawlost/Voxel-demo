using System.Linq;
using System;
using System.Collections.Generic;
using Godot;
using GodotArray = Godot.Collections.Array;
public class GameMesher
{
    private volatile Node parent;
    private volatile GreedyMesher greedyMesher;
    private volatile SplatterMesher splatterMesher;

    public GameMesher(Node parent, Registry reg){        
        this.parent = parent;
        ShaderMaterial shaderMat = new ShaderMaterial();
        shaderMat.Shader = (GD.Load("res://assets/shaders/splatvoxel.shader") as Shader);
        greedyMesher = new GreedyMesher(reg);
        splatterMesher = new SplatterMesher(shaderMat, reg);
    }

    public void MeshChunk(Chunk chunk, bool splatter){
        MeshInstance meshInstance = new MeshInstance();
        if(!splatter){
            StartMeshing(meshInstance, chunk);
        }else{
            meshInstance = splatterMesher.CreateChunkMesh(chunk);
        }
    }

    private void StartMeshing(MeshInstance meshInstance, Chunk chunk){
        if(!chunk.isEmpty){
            Dictionary<int, Dictionary<int, Face>> sector = greedyMesher.cull(chunk);
        if (sector.Count > 0) {

            Dictionary<Texture, List<Vector3>> verticeArrays = new Dictionary<Texture,  List<Vector3>>();
            Dictionary<Texture, List<Vector3>>  normalsArrays = new Dictionary<Texture,  List<Vector3>>();;
            Dictionary<Texture, List<Vector2>> textureCoordArrays = new Dictionary<Texture,  List<Vector2>>();;
            Dictionary<Texture, List<int>> indexArrays = new Dictionary<Texture,  List<int>>();
            List<Vector3> shapeFaces = new List<Vector3>();
            //Finishing greedy meshing
            foreach (int key in sector.Keys) {
                if (key != 6) {
                    Dictionary<int, Face> faces = sector[key];
                    int[] keys = faces.Keys.ToArray();
                    Array.Sort(keys);
                    for (int i = keys.Length - 1; i >= 0; i--) {
                        int index = keys[i];
                        JoinReversed(faces, index, key);
                    }
                }
            }

            foreach (int key in sector.Keys) {
                if (key != 6) {

                    Dictionary<int, Face> faces = sector[key];

                    foreach (int faceKey in faces.Keys.ToArray()) {
                        Face completeFace = faces[faceKey];

                        Texture texture = completeFace.terraObject.texture;

                        List<int> indices;

                        if(!verticeArrays.ContainsKey(texture) || !textureCoordArrays.ContainsKey(texture)){
                            indices = new List<int>();
                            
                            indices.Add(0);
                            indices.Add(1);
                            indices.Add(2);
                            indices.Add(0);
                            indices.Add(2);
                            indices.Add(3);

                            verticeArrays.Add(texture, new List<Vector3>());
                            textureCoordArrays.Add(texture, new List<Vector2>());
                            normalsArrays.Add(texture, new List<Vector3>());
                            indexArrays.Add(texture, indices);

                            completeFace = SetTextureCoords(completeFace, key);

                            verticeArrays[texture].AddRange(GetVertice(completeFace));
                            textureCoordArrays[texture].AddRange(GetTextureCoords(completeFace));
                            normalsArrays[texture].AddRange(GetNormals(completeFace));
                            shapeFaces.AddRange(GetShapeFaces(completeFace));

                            sector[key].Remove(faceKey);
                            continue;
                        }

                        indices = indexArrays[texture];

                        completeFace = SetTextureCoords(completeFace, key);
                            
                            verticeArrays[texture].AddRange(GetVertice(completeFace));
                            textureCoordArrays[texture].AddRange(GetTextureCoords(completeFace));
                            normalsArrays[texture].AddRange(GetNormals(completeFace));
                            shapeFaces.AddRange(GetShapeFaces(completeFace));

                        for(int i = 0; i < 6; i ++){
                            indices.Add(indices[indices.Count - 6] + 4);
                        }

                        sector[key].Remove(faceKey);
                    }
                    faces.Clear();
                }
            }

            //Unusual meshes
            //Dictionary<int, Face> side = sector[6];
           /* for (Integer i : side.keySet()) {
                Face face = side.get(i);
                Integer id = face.getObject().getWorldId();

                if (unsualMeshSize.get(id) == null) {
                    unsualMeshSize.put(id, 1);
                    TerraObject object = reg.getForWorldId(id);
                    int posZ = i / 4096;
                    int posY = (i - (4096 * posZ)) / 64;
                    int posX = i % 64;
                    object.position(
                            (posX * 0.25f) + chunk.getPosX() + (object.getMesh().getDefaultDistanceX() * 0.25f) / 2f,
                            (posY * 0.25f) + chunk.getPosY(),
                            (posZ * 0.25f) + chunk.getPosZ() + (object.getMesh().getDefaultDistanceZ() * 0.25f) / 2f);
                } else {
                    int s = unsualMeshSize.get(id);
                    s += 1;
                    unsualMeshSize.replace(id, s);
                }

                Integer[] currentKeys = new Integer[unsualMeshSize.keySet().size()];
                unsualMeshSize.keySet().toArray(currentKeys);
                for (Integer objectId : currentKeys) {
                    if (objectId != null) {
                        TerraObject object = reg.getForWorldId(objectId);
                        if (object.getMesh().getSize() == unsualMeshSize.get(objectId)) {
                            unsualMeshSize.remove(objectId);

                            Spatial asset = modelLoader3D.getMesh(object.getMesh().getAsset());

                            while (asset instanceof Node) {
                                asset = ((Node) asset).getChild(0);
                            }

                            Geometry assetGeom = (Geometry) asset;

                            FloatBuffer normalBuffer = FloatBuffer.allocate(6 * assetGeom.getTriangleCount());

                            for (int n = 0; n < normalBuffer.capacity(); n++) {
                                normalBuffer.put(0);
                            }

                            texCoordsBuffer = BufferUtils.createFloatBuffer(
                                    ((assetGeom.getMesh().getFloatBuffer(VertexBuffer.Type.TexCoord).capacity()
                                            / 2) * 3));
                            for (int t = 0; t < assetGeom.getMesh().getFloatBuffer(VertexBuffer.Type.TexCoord).capacity();
                                 t += 2) {
                                texCoordsBuffer.put(assetGeom.getMesh().getFloatBuffer(VertexBuffer.Type.TexCoord).get(t));
                                texCoordsBuffer.put(assetGeom.getMesh().getFloatBuffer(VertexBuffer.Type.TexCoord).get(t + 1));
                                texCoordsBuffer.put(object.getTexture().getPosition());
                            }

                            Mesh newMash = new Mesh();
                            newMash.setBuffer(assetGeom.getMesh().getBuffer(VertexBuffer.Type.Position));
                            newMash.setBuffer(assetGeom.getMesh().getBuffer(VertexBuffer.Type.Index));
                            newMash.setBuffer(VertexBuffer.Type.TexCoord, 3, texCoordsBuffer);
                            newMash.setBuffer(VertexBuffer.Type.Normal, 3, normalBuffer);

                            assetGeom.setMesh(newMash);
                            assetGeom.setLocalTranslation(object.getX(), object.getY(), object.getZ());
                            assetGeom.setMaterial(mat);
                            assetGeom.updateModelBound();
                            node.attachChild(assetGeom);
                        }
                    }
                }
            }*/

        ArrayMesh mesh = new ArrayMesh();

        meshInstance.Name = "chunk:" + chunk.x + "," + chunk.y + "," + chunk.z;
        meshInstance.Translate(new Vector3(chunk.x, chunk.y, chunk.z));
        
        foreach(Texture texture1 in verticeArrays.Keys.ToArray()){
            GodotArray arrays = new GodotArray();
            arrays.Resize(9);
            
            SpatialMaterial material = new SpatialMaterial();
            texture1.Flags = 2;
            material.AlbedoTexture = texture1;
            
            arrays[0] = verticeArrays[texture1].ToArray();
            arrays[1] = normalsArrays[texture1].ToArray();
            arrays[4] = textureCoordArrays[texture1].ToArray();
            arrays[8] = indexArrays[texture1].ToArray();
            mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);
            mesh.SurfaceSetMaterial(mesh.GetSurfaceCount() - 1, material);
            arrays.Clear();
        }

        ConcavePolygonShape shape = new ConcavePolygonShape();
        shape.SetFaces(shapeFaces.ToArray());
        StaticBody body = new StaticBody();
        CollisionShape colShape = new CollisionShape();
        colShape.SetShape(shape);
        body.AddChild(colShape);
        
        meshInstance.SetMesh(mesh);
        meshInstance.AddChild(body);
          foreach(Node node in parent.GetChildren()){
            if(node.Name.Equals(meshInstance.Name)){
                parent.RemoveChild(node);
            }
        }
        parent.AddChild(meshInstance);
        shapeFaces.Clear();
        indexArrays.Clear();
        normalsArrays.Clear();
        verticeArrays.Clear();
        textureCoordArrays.Clear();
        }
        sector.Clear();
        }
    }

    private static void JoinReversed(Dictionary<int, Face> faces, int index, int side) {
        int neighbor = 64;
        switch (side) {
            case 2:
            case 3:
                neighbor = 4096;
                break;
        }

        if(faces.ContainsKey(index - neighbor) && faces.ContainsKey(index)){
        Face nextFace = faces[index - neighbor];

        Face face = faces[index];
        if (face.terraObject == (nextFace.terraObject)) {
            if (nextFace.vector3s[2] == face.vector3s[1] && nextFace.vector3s[3] == face.vector3s[0]) {
                nextFace.vector3s[2] = face.vector3s[2];
                nextFace.vector3s[3] = face.vector3s[3];
                faces.Remove(index);
            }
        }
        }
    }

        private static Face SetTextureCoords(Face completeFace, int side) {
            completeFace.UVs = new Vector2[4];
            switch (side) {
                case 0:
                case 1:
                    completeFace.UVs[0]= new Vector2(completeFace.vector3s[0].z * 2048f / completeFace.terraObject.texture.GetWidth(), completeFace.vector3s[0].y * 2048f / completeFace.terraObject.texture.GetHeight());
                    completeFace.UVs[1]= new Vector2(completeFace.vector3s[1].z * 2048f / completeFace.terraObject.texture.GetWidth(), completeFace.vector3s[1].y * 2048f / completeFace.terraObject.texture.GetHeight());
                    completeFace.UVs[2]= new Vector2(completeFace.vector3s[2].z * 2048f / completeFace.terraObject.texture.GetWidth(), completeFace.vector3s[2].y * 2048f / completeFace.terraObject.texture.GetHeight());
                    completeFace.UVs[3]= new Vector2(completeFace.vector3s[3].z * 2048f / completeFace.terraObject.texture.GetWidth(), completeFace.vector3s[3].y * 2048f / completeFace.terraObject.texture.GetHeight());
                    return completeFace;

                case 2:
                case 3:
                    completeFace.UVs[0]= new Vector2(completeFace.vector3s[0].x * 2048f / completeFace.terraObject.texture.GetWidth(), completeFace.vector3s[0].z * 2048f / completeFace.terraObject.texture.GetHeight());
                    completeFace.UVs[1]= new Vector2(completeFace.vector3s[1].x * 2048f / completeFace.terraObject.texture.GetWidth(), completeFace.vector3s[1].z * 2048f / completeFace.terraObject.texture.GetHeight());
                    completeFace.UVs[2]= new Vector2(completeFace.vector3s[2].x * 2048f / completeFace.terraObject.texture.GetWidth(), completeFace.vector3s[2].z * 2048f / completeFace.terraObject.texture.GetHeight());
                    completeFace.UVs[3]= new Vector2(completeFace.vector3s[3].x * 2048f / completeFace.terraObject.texture.GetWidth(), completeFace.vector3s[3].z * 2048f / completeFace.terraObject.texture.GetHeight());
                    return completeFace;

                case 4:
                case 5:
                    completeFace.UVs[0]= new Vector2(completeFace.vector3s[0].x * 2048f / completeFace.terraObject.texture.GetWidth(), completeFace.vector3s[0].y * 2048f / completeFace.terraObject.texture.GetHeight());
                    completeFace.UVs[1]= new Vector2(completeFace.vector3s[1].x * 2048f / completeFace.terraObject.texture.GetWidth(), completeFace.vector3s[1].y * 2048f / completeFace.terraObject.texture.GetHeight());
                    completeFace.UVs[2]= new Vector2(completeFace.vector3s[2].x * 2048f / completeFace.terraObject.texture.GetWidth(), completeFace.vector3s[2].y * 2048f / completeFace.terraObject.texture.GetHeight());
                    completeFace.UVs[3]= new Vector2(completeFace.vector3s[3].x * 2048f / completeFace.terraObject.texture.GetWidth(), completeFace.vector3s[3].y * 2048f / completeFace.terraObject.texture.GetHeight());
                    return completeFace;
            }
            return completeFace;
        }

        private static Vector3[] GetVertice(Face completeFace){
            Vector3[] list = new Vector3[4];
            for(int i = 0; i < 4; i ++){
                list[i] = completeFace.vector3s[i];
            }
            return list;
        }

        private static Vector3[] GetNormals(Face completeFace){
            Vector3[] list = new Vector3[4];
               for(int i = 0; i < 4; i ++){
                    list[i] = completeFace.normal;
                }
            return list;
        }
        private static Vector2[] GetTextureCoords(Face completeFace){
             Vector2[] list = new Vector2[4];

             for(int i = 0; i < 4; i ++){
                    list[i] = completeFace.UVs[i];
            }

             return list;
        }

         private static  Vector3[] GetShapeFaces(Face completeFace){
                Vector3[] list = new  Vector3[6];
                
                list[0] = completeFace.vector3s[0];
                list[1] = completeFace.vector3s[1];
                list[2] = completeFace.vector3s[2];
                list[3] = completeFace.vector3s[2];
                list[4] = completeFace.vector3s[3];
                list[5] = completeFace.vector3s[0];

             return list;
        }
}