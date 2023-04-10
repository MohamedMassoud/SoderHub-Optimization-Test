using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoolManager : MonoBehaviour
{
    [SerializeField] private GameObject ActorPrefab;
    [SerializeField] private GameObject MarblePrefab;
    [SerializeField] private GameObject TextBoxPrefab;

    public readonly Dictionary<PooledObject, List<GameObject>> pooledObjects = new Dictionary<PooledObject, List<GameObject>>();

    public enum PooledObject
    {
        Actor,
        Marble,
        TextBox
    }

    public void AddObjectsToPool(PooledObject pooledObjectEnum, int initNumber, Transform parent = null)
    {
        List<GameObject> gameObjects;
        if(!pooledObjects.TryGetValue(pooledObjectEnum, out gameObjects))
        {
            gameObjects = new List<GameObject>();
        }
        
        for(int i=0; i<initNumber; i++)
        {
            GameObject createdGameObject = CreateGameObjectFromEnum(pooledObjectEnum, parent);
            gameObjects.Add(createdGameObject);
        }
        pooledObjects[pooledObjectEnum] = gameObjects;
    }

    public GameObject GetObjectFromPool(PooledObject pooledObjectEnum, int initNumberIfFailed = 25, Transform parent = null)
    {
        List<GameObject> gameObjects;
        if (!pooledObjects.TryGetValue(pooledObjectEnum, out gameObjects))
        {
            AddObjectsToPool(pooledObjectEnum, initNumberIfFailed, parent);
            GetObjectFromPool(pooledObjectEnum, initNumberIfFailed, parent);
            //Debug.Log("NOT FOUND");
        }

        //Debug.Log("FOUND");
        foreach(GameObject pooledObject in gameObjects)
        {
            if (!pooledObject.activeInHierarchy)
            {
                return pooledObject;
            }
        }

        AddObjectsToPool(pooledObjectEnum, initNumberIfFailed, parent);
        
        //Debug.LogError("Unexpected Error.");

        return GetObjectFromPool(pooledObjectEnum, initNumberIfFailed, parent);
    }

    private GameObject CreateGameObjectFromEnum(PooledObject pooledObject, Transform parent = null)
    {
        switch (pooledObject)
        {
            case PooledObject.Actor:
                return Instantiate(ActorPrefab, parent);    //Currently not used as Actors don't need to be added to the pool because they don't respawn

            case PooledObject.Marble:
                return Instantiate(MarblePrefab, parent);

            case PooledObject.TextBox:
                return Instantiate(TextBoxPrefab, parent);
        }
        Debug.LogError("GameObject: " + pooledObject.ToString() + " is not a pooled object!");
        return null;
    }

    public void DeactivateGameObject(GameObject objectToDeactivate)
    {
        objectToDeactivate.SetActive(false);
    }
}
