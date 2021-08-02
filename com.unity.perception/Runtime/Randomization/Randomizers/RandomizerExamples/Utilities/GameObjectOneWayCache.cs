using System;
using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Perception.Randomization.Samplers;

namespace UnityEngine.Perception.Randomization.Randomizers.Utilities
{
    /// <summary>
    /// Facilitates object pooling for a pre-specified collection of prefabs with the caveat that objects can be fetched
    /// from the cache but not returned. Every frame, the cache needs to be reset, which will return all objects to the pool
    /// </summary>
    public class GameObjectOneWayCache
    {
        static ProfilerMarker s_ResetAllObjectsMarker = new ProfilerMarker("ResetAllObjects");

        List<GameObject> m_GameObjects;
        UniformSampler m_Sampler = new UniformSampler();
        Transform m_CacheParent;
        Dictionary<int, int> m_InstanceIdToIndex;
        List<List<CachedObjectData>> m_InstantiatedObjects;
        List<int> m_NumObjectsActive;
        int NumObjectsInCache { get; set; }

        /// <summary>
        /// The number of active cache objects in the scene
        /// </summary>
        public int NumObjectsActive { get; private set; }

        /// <summary>
        /// Creates a new GameObjectOneWayCache
        /// </summary>
        /// <param name="parent">The parent object all cached instances will be parented under</param>
        /// <param name="gameObjects">The gameObjects to cache</param>
        public GameObjectOneWayCache(Transform parent, GameObject[] gameObjects = null)
        {
            m_CacheParent = parent;
            m_GameObjects = new List<GameObject>();
            m_InstanceIdToIndex = new Dictionary<int, int>();
            m_InstantiatedObjects = new List<List<CachedObjectData>>();
            m_NumObjectsActive = new List<int>();

            if (gameObjects != null && gameObjects.Length != 0)
            {
                foreach (var obj in gameObjects)
                {
                    AddGameObject(obj);
                }
            }
        }

        /// <summary>
        /// Adds the given GameObject to the list of cached objects. An instance of the object can be immediately requested afterwards.
        /// </summary>
        /// <param name="gameObject"></param>
        public void AddGameObject(GameObject gameObject)
        {
            m_GameObjects.Add(gameObject);
            var index = m_InstantiatedObjects.Count;
            if (!IsPrefab(gameObject))
            {
                gameObject.transform.parent = m_CacheParent;
                gameObject.SetActive(false);
            }
            var instanceId = gameObject.GetInstanceID();
            m_InstanceIdToIndex.Add(instanceId, index);
            m_InstantiatedObjects.Add(new List<CachedObjectData>());
            m_NumObjectsActive.Add(index);
        }

        /// <summary>
        /// Retrieves an existing instance of the given gameObject from the cache if available.
        /// Otherwise, instantiate a new instance of the given gameObject.
        /// </summary>
        /// <param name="gameObject"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public GameObject GetOrInstantiate(GameObject gameObject)
        {
            if (!m_InstanceIdToIndex.TryGetValue(gameObject.GetInstanceID(), out var index))
            {
                AddGameObject(gameObject);
                if (!m_InstanceIdToIndex.TryGetValue(gameObject.GetInstanceID(), out index))
                    Debug.LogError("Object cache is not working properly.");
            }

            ++NumObjectsActive;
            if (m_NumObjectsActive[index] < m_InstantiatedObjects[index].Count)
            {
                var nextInCache = m_InstantiatedObjects[index][m_NumObjectsActive[index]];
                ++m_NumObjectsActive[index];
                foreach (var tag in nextInCache.randomizerTags)
                    tag.Register();
                var obj = nextInCache.instance;
                obj.transform.localPosition = Vector3.zero;
                return nextInCache.instance;
            }

            ++NumObjectsInCache;
            var newObject = Object.Instantiate(gameObject, m_CacheParent);
            newObject.SetActive(true);
            ++m_NumObjectsActive[index];
            m_InstantiatedObjects[index].Add(new CachedObjectData(newObject));
            newObject.transform.localPosition = Vector3.zero;
            return newObject;
        }

        /// <summary>
        /// Retrieves an existing instance of the given gameObject from the cache if available.
        /// Otherwise, instantiate a new instance of the given gameObject.
        /// </summary>
        /// <param name="index">The index of the gameObject to instantiate</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        public GameObject GetOrInstantiate(int index)
        {
            var gameObject = m_GameObjects[index];
            return GetOrInstantiate(gameObject);
        }

        /// <summary>
        /// Retrieves an existing instance of a random gameObject from the cache if available.
        /// Otherwise, instantiate a new instance of the random gameObject.
        /// </summary>
        /// <returns>A random cached GameObject</returns>
        public GameObject GetOrInstantiateRandomCachedObject()
        {
            return GetOrInstantiate(m_GameObjects[(int)(m_Sampler.Sample() * m_GameObjects.Count)]);
        }

        /// <summary>
        /// Return all active cache objects back to an inactive state
        /// </summary>
        public void ResetAllObjects()
        {
            using (s_ResetAllObjectsMarker.Auto())
            {
                NumObjectsActive = 0;
                for (var i = 0; i < m_InstantiatedObjects.Count; ++i)
                {
                    m_NumObjectsActive[i] = 0;
                    foreach (var cachedObjectData in m_InstantiatedObjects[i])
                    {
                        ResetObjectState(cachedObjectData);
                    }
                }
            }
        }

        /// <summary>
        /// Returns the given cache object back to an inactive state
        /// </summary>
        /// <param name="gameObject">The object to make inactive</param>
        /// <exception cref="ArgumentException">Thrown when gameObject is not an active cached object.</exception>
        public void ResetObject(GameObject gameObject)
        {
            for (var i = 0; i < m_InstantiatedObjects.Count; ++i)
            {
                var instantiatedObjectList = m_InstantiatedObjects[i];
                int indexFound = -1;
                for (var j = 0; j < instantiatedObjectList.Count && indexFound < 0; j++)
                {
                    if (instantiatedObjectList[j].instance == gameObject)
                        indexFound = j;
                }

                if (indexFound >= 0)
                {
                    ResetObjectState(instantiatedObjectList[indexFound]);
                    m_NumObjectsActive[i]--;
                    return;
                }
            }

            throw new ArgumentException("Passed GameObject is not an active object in the cache.");
        }

        private static void ResetObjectState(CachedObjectData cachedObjectData)
        {
            // Position outside the frame
            cachedObjectData.instance.transform.localPosition = new Vector3(10000, 0, 0);
            cachedObjectData.instance.transform.localRotation = Quaternion.identity;
            foreach (var tag in cachedObjectData.randomizerTags)
                tag.Unregister();
        }

        static bool IsPrefab(GameObject obj)
        {
            return obj.scene.rootCount == 0;
        }

        struct CachedObjectData
        {
            public GameObject instance;
            public RandomizerTag[] randomizerTags;

            public CachedObjectData(GameObject instance)
            {
                this.instance = instance;
                randomizerTags = instance.GetComponents<RandomizerTag>();
            }
        }
    }
}
