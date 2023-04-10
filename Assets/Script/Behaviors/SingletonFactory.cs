using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SingletonFactory : MonoBehaviour
{
    public static SingletonFactory Instance;


    public PoolManager PoolManager;

    void Awake()
    {
        Instance = this;
    }


}
