using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

namespace JP_RepoHolySkills
{
    public class CombinedPrefabPool : IPunPrefabPool
    {
        private IPunPrefabPool basePool;
        private Dictionary<string, GameObject> moddedPrefabs = new Dictionary<string, GameObject>();

        // Constructor: wraps an existing prefab pool.
        public CombinedPrefabPool(IPunPrefabPool basePool)
        {
            this.basePool = basePool;
        }

        // Adds a modded prefab under a unique key.
        public void AddModdedPrefab(string key, GameObject prefab)
        {
            if (!moddedPrefabs.ContainsKey(key))
            {
                moddedPrefabs.Add(key, prefab);
            }
        }

        // Expose the modded prefabs for logging or inspection.
        public Dictionary<string, GameObject> GetModdedPrefabs()
        {
            return moddedPrefabs;
        }

        public GameObject Instantiate(string prefabId, Vector3 position, Quaternion rotation)
        {
            if (moddedPrefabs.TryGetValue(prefabId, out GameObject modPrefab))
            {
                // Instantiate the modded prefab.
                GameObject instance = Object.Instantiate(modPrefab, position, rotation);

                // Ensure the instance is inactive (Photon expects inactive objects)
                instance.SetActive(false);

                // Check if it has a PhotonView. If not, add one.
                if (instance.GetComponent<PhotonView>() == null)
                {
                    PhotonView pv = instance.AddComponent<PhotonView>();
                    // Optionally configure the PhotonView (e.g., ownership, observed components)
                }

                return instance;
            }
            else
            {
                // If not found in moddedPrefabs, delegate to the original pool.
                return basePool.Instantiate(prefabId, position, rotation);
            }
        }

        public void Destroy(GameObject gameObject)
        {
            basePool.Destroy(gameObject);
        }
    }
}
