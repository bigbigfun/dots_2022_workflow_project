using Unity.Entities.Serialization;
using Unity.Mathematics;
using Unity.Scenes;
using Unity.Transforms;
using UnityEngine;
using BinaryReader = Unity.Entities.Serialization.BinaryReader;
using Entity = Unity.Entities.Entity;
using World = Unity.Entities.World;

public class DeserializeEntity : MonoBehaviour
{
    public TextAsset binary;
    public ReferencedUnityObjects objRefs;
    public Entity testEntity;
    
    private void Start()
    {
        Invoke("Deserialize", 1f);
    }
    
    [ContextMenu("Deserialize")]
    public  void Deserialize()
    {
        BinaryReader reader = null;
        unsafe
        {
            fixed (byte* ptr = binary.bytes)
            {
                reader = new MemoryBinaryReader(ptr, binary.bytes.Length);
            }
        }
        Entity resultEntityPrefab;
        
        resultEntityPrefab = SerializeTool.Deserialize(reader, objRefs);

        if (resultEntityPrefab == Entity.Null)
        {
            Debug.LogError("Failed to deserialize entity："+resultEntityPrefab);
        }

        var manager = World.DefaultGameObjectInjectionWorld.EntityManager;
 
        World.DefaultGameObjectInjectionWorld.EntityManager.SetName(resultEntityPrefab, binary.name + "(prefab)-" + resultEntityPrefab.Index);

        testEntity = manager.Instantiate(resultEntityPrefab);
        manager.SetComponentData(testEntity, LocalTransform.FromPosition(new float3(3f, 0, 0)));
        
        World.DefaultGameObjectInjectionWorld.EntityManager.SetName(testEntity, binary.name + "(inst)-" + resultEntityPrefab.Index);

        reader.Dispose();
    }
}
