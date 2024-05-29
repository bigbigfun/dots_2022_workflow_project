using System.Collections.Generic;
using Unity.Entities;
using Unity.Entities.Serialization;
#if UNITY_EDITOR
#endif
using UnityEngine;
using BinaryReader = Unity.Entities.Serialization.BinaryReader;

public class SerializeTool
    {
        public static Unity.Entities.Entity Deserialize(BinaryReader reader, Unity.Scenes.ReferencedUnityObjects objRefs, Unity.Entities.EntityManager manager)
        {
            using (var deserializeWorld = new Unity.Entities.World("Deserialize World"))
            {
                var deserializeWorldEntityMgr = deserializeWorld.EntityManager;

                DeserializeObjectReferences(objRefs, out Object[] objectReferences);
                var transaction = deserializeWorldEntityMgr.BeginExclusiveEntityTransaction();
                SerializeUtility.DeserializeWorld(transaction, reader, objectReferences);
                deserializeWorldEntityMgr.EndExclusiveEntityTransaction();

                manager.MoveEntitiesFrom(out var loadedEntities, deserializeWorldEntityMgr);
                
                // 这里返回的Entities，是所有的Entities，需要进行一步筛选，找到唯一的那个就行了。
                Unity.Entities.Entity loadedEntityPrefab = Unity.Entities.Entity.Null;
                Debug.Log( "loadedEntities.Length: " + loadedEntities.Length );
                for (int i = 0; i < loadedEntities.Length; ++i)
                {
                    var entity = loadedEntities[i];
                    if (manager.HasComponent<SolderRootAuthoring.EntityRootTag>(entity))
                    {
                        Debug.Log( "SolderRootTag: " + loadedEntities[i] );
                        loadedEntityPrefab = entity;
                        break;
                    }
                }
                loadedEntities.Dispose();
                return loadedEntityPrefab;
            }
        }

        public static Unity.Entities.Entity Deserialize(byte[] bytes, Unity.Scenes.ReferencedUnityObjects objRefs)
        {
            BinaryReader reader = null;
            unsafe
            {
                fixed (byte* ptr = bytes)
                {
                    reader = new MemoryBinaryReader(ptr, bytes.Length);
                }
            }
            return Deserialize(reader, objRefs);
        }

        public static Entity Deserialize(BinaryReader reader, Unity.Scenes.ReferencedUnityObjects objRefs, string name = "")
        {
            bool validRefObjects = true;
            do
            {
                if (objRefs == null)
                {
                    Debug.LogError($"objRefs is null! ${name}");
                    validRefObjects = false;
                    break;
                }

                if (objRefs.Array == null)
                {
                    Debug.LogError($"objRefs.Array is null! ${name}");
                    break;
                }

                for (int ii = 0; ii < objRefs.Array.Length; ++ii)
                {
                    if (objRefs.Array[ii] == null)
                    {
                        Debug.LogError($"Invalid ref object data! ${name}");
                        validRefObjects = false;
                        break;
                    }
                }

            } while (false);

            if(!validRefObjects)
            {
                Debug.LogError($"Invalid ref object data! ${name}");
            }

            Unity.Entities.EntityManager manager = Unity.Entities.World.DefaultGameObjectInjectionWorld.EntityManager;
            if (manager == null)
            {
                Debug.LogError( "manager is null" );
            }
            return Deserialize(reader, objRefs, manager);
        }
        
        public static void DeserializeObjectReferences(Unity.Scenes.ReferencedUnityObjects objRefs, out UnityEngine.Object[] objectReferences)
        {
            objectReferences = objRefs?.Array;

            // NOTE: Object references must not include fake object references, they must be real null.
            // The Unity.Properties deserializer can't handle them correctly.
            // We might want to add support for handling fake null,
            // but it would require tight integration in the deserialize function so that a correct fake null unityengine.object can be constructed on deserialize
            if (objectReferences != null)
            {
                // When using bundles, the Companion GameObjects cannot be directly used (prefabs), so we need to instantiate everything.
                var sourceToInstance = new Dictionary<UnityEngine.GameObject, UnityEngine.GameObject>();
                for (int i = 0; i != objectReferences.Length; i++)
                {
                    if (objectReferences[i] == null)
                    {
                        objectReferences[i] = null;
                        continue;
                    }

                    if (objectReferences[i] is UnityEngine.GameObject source)
                    {
                        var instance = UnityEngine.GameObject.Instantiate(source);
                        objectReferences[i] = instance;
                        sourceToInstance.Add(source, instance);
                    }
                }

                for (int i = 0; i != objectReferences.Length; i++)
                {
                    if (objectReferences[i] is UnityEngine.Component component)
                    {
                        objectReferences[i] = sourceToInstance[component.gameObject].GetComponent(component.GetType());
                    }
                }
            }
        }
    }