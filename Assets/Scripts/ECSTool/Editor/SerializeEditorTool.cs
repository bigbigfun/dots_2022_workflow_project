using System.IO;
using Unity.Collections;
using Unity.Entities;
using Unity.Scenes.Editor;
using Unity.VisualScripting;
#if UNITY_EDITOR
using UnityEditor;
using Unity.Entities.Hybrid.Baking;
#endif
using UnityEngine;

namespace CCC
{
    public class SerializeEditorTool
    {
        public const string EntityExtension = "bytes";
        
        [MenuItem("Assets/ECSTool/Serialize Selection", false)]
        public static void SerializeSelection_Test()
        {
            var count = Selection.gameObjects.Length;
            var index = 0.0f;
            foreach (var go in Selection.gameObjects)
            {
                EditorUtility.DisplayProgressBar(index+"/"+count,"",index/count);
                DoSerialize(go);
                EditorUtility.ClearProgressBar();
            }
        }
        
        public static bool DoSerialize(GameObject go)
        {
            if (!Serialize(go))
            {
                return false;
            }
            AssetDatabase.Refresh();
            return true;
        }
        
        
        public static bool Serialize(GameObject prefab)
        {
            string assetPath = AssetDatabase.GetAssetPath(prefab);
            string newPath = "";

            var serializeWorld = new World("serialize World");
            var serializeWorldEntityMgr = serializeWorld.EntityManager;
            try
            {
                var prefabInst = GameObject.Instantiate(prefab);
                prefabInst.name = prefab.name;

                prefabInst.AddComponent<LinkedEntityGroupAuthoring>();

                BakingUtility.BakeGameObjects(serializeWorld, new[] {prefabInst});
                var query = serializeWorldEntityMgr.CreateEntityQuery(new EntityQueryDesc {All = new[] {new ComponentType(typeof(SolderRootAuthoring.EntityRootTag))}});
                var entities = query.ToEntityArray(Allocator.Temp);
                Debug.Log("baked entities.Length:"+entities.Length);
                
                Serialize(serializeWorldEntityMgr, prefabInst, assetPath);
                
                GameObject.DestroyImmediate(prefabInst, true);
                AssetDatabase.DeleteAsset(newPath);
                
                entities.Dispose();
                serializeWorld.Dispose();
            }
            catch (System.Exception ex)
            {
                Debug.LogError(string.Format("Convert {0} to entity failed:\n{1}", assetPath, ex.ToString()));
                return false;
            }
            finally
            {
                if (!string.IsNullOrEmpty(newPath))
                {
                    File.Delete(newPath);
                    UnityEditor.AssetDatabase.Refresh();
                }
            }
            return true;
        }
        
        public static void Serialize(EntityManager manager, GameObject prefab, string assetPath)
        {
            string binaryPath = System.IO.Path.ChangeExtension(assetPath, EntityExtension);
            if (File.Exists(binaryPath))
            {
                AssetDatabase.DeleteAsset(binaryPath);
            }
            
            string objectReferencesPath = System.IO.Path.ChangeExtension(assetPath, "asset");
            if (File.Exists(objectReferencesPath))
            {
                AssetDatabase.DeleteAsset(objectReferencesPath);
            }
            
            string animatorPath = System.IO.Path.ChangeExtension(assetPath, "asset");
            if (File.Exists(animatorPath))
            {
                AssetDatabase.DeleteAsset(animatorPath);
            }
            
            EditorEntityScenes.Write(manager, binaryPath, objectReferencesPath);
        }
    }
}
