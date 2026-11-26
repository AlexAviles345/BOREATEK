using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Boreatek.World
{
    internal sealed class FoundryNavigation : MonoBehaviour
    {
        private NavMeshData _data;
        private NavMeshDataInstance _instance;

        internal void Build(Transform village)
        {
            var bounds = new Bounds(village.position + new Vector3(0f, 1.6f, -7f), new Vector3(126f, 12f, 116f));
            Physics.SyncTransforms();
            var sources = new List<NavMeshBuildSource>();
            NavMeshBuilder.CollectSources(bounds, Physics.DefaultRaycastLayers, NavMeshCollectGeometry.PhysicsColliders,
                0, new List<NavMeshBuildMarkup>(), sources);
            sources.RemoveAll(source => source.component is Collider collider &&
                (collider.isTrigger || collider is CapsuleCollider || collider is CharacterController));

            NavMeshBuildSettings type = NavMesh.GetSettingsByIndex(0);
            type.agentRadius = 0.30f;
            type.agentHeight = 1.24f;
            type.agentClimb = 0.34f;
            type.agentSlope = 43f;
            type.overrideVoxelSize = true;
            type.voxelSize = 0.12f;
            type.minRegionArea = 1.5f;
            _data = NavMeshBuilder.BuildNavMeshData(type, sources, bounds, Vector3.zero, Quaternion.identity);
            if (_data == null) return;
            _instance = NavMesh.AddNavMeshData(_data);
            foreach (FoundryWorker worker in village.GetComponentsInChildren<FoundryWorker>())
                worker.EnableRoute(type.agentTypeID);
        }

        private void OnDestroy()
        {
            if (_instance.valid) _instance.Remove();
            if (_data != null) Destroy(_data);
        }
    }
}
