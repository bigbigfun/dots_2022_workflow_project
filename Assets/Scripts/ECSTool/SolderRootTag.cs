using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;

public class SolderRootAuthoring :  MonoBehaviour
{
    class Baker : Baker<SolderRootAuthoring>
    {
        public override void Bake(SolderRootAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new EntityRootTag { });
        }
    }
    
    public struct EntityRootTag : IComponentData
    {
    }
}