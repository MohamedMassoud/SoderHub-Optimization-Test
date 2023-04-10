using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;
using TMPro;
using System.Threading;
using DataStructures.ViliWonka.KDTree;
using TreeEditor;
using UnityEditor.Search;
using static UnityEngine.UI.CanvasScaler;

public class MarbleContainer : MonoBehaviour
{
    public GameObject MarblePrefab;
    [SerializeField] private int numberOfMarbles = 10000;

    //private readonly Dictionary<Guid,MarbleBehavior> _marbles = new Dictionary<Guid, MarbleBehavior>();
    public static readonly Dictionary<Guid ,GameObject> Marbles = new Dictionary<Guid ,GameObject>();
    public static readonly Dictionary<Guid ,bool> MarblesClaimList = new Dictionary<Guid , bool>();
    private SingletonFactory _singletonFactory;
    private PoolManager _poolManager;

    

    private static KDQuery query;
    private static KDTree tree;
    List<Guid> guidCloud;


    private static int maxPointsPerLeafNode;
    public static List<Vector3> pointCloud; 

    void Start()
    {
        _singletonFactory = SingletonFactory.Instance;
        _poolManager = _singletonFactory.PoolManager;
        _poolManager.AddObjectsToPool(PoolManager.PooledObject.Marble, numberOfMarbles/10, transform);
        _poolManager.AddObjectsToPool(PoolManager.PooledObject.TextBox, numberOfMarbles/10, transform);


        Init();


        StopAllCoroutines();
        for( var i = 0; i < Mathf.Min(500, numberOfMarbles); i++ )      //Spawns 500 Marbles instantly
        {
            GenerateMarble();
        }
        tree.Build(pointCloud);



        StartCoroutine( SpawnMarbles() );   //Spawns 25 Marbles every frame till limit

    }

    private void Init() {
        pointCloud = new List<Vector3>();
        guidCloud = new List<Guid>();
        maxPointsPerLeafNode = 32;
        tree = new KDTree(pointCloud.ToArray(), maxPointsPerLeafNode);
        query = new KDQuery();

    }

    IEnumerator SpawnMarbles()
    {
        while( true )
        {
            if( Marbles.Values.Count < numberOfMarbles )
            {
                for( var i = 0; i < 25; i++ )
                {
                   GenerateMarble();
                   
                }
                tree.Build(pointCloud);
            }
            yield return new WaitForSeconds(0.05f);    //15_FRAMES/SEC
            //yield return new WaitForEndOfFrame();    //15_FRAMES/SEC
        }
    }

    private void GenerateMarble()
    {
        GameObject newMarble = _poolManager.GetObjectFromPool(PoolManager.PooledObject.Marble, numberOfMarbles/10, transform);     //Using custom made PoolManager to pool objects to reduce GC
        newMarble.SetActive(true);
        //newMarble.transform.parent = this.transform;
        newMarble.transform.position = Random.insideUnitSphere * 100f;

        Guid guid = Guid.NewGuid();
        Marbles.Add(guid, newMarble);
        MarblesClaimList.Add(guid, false);      //Track Claimed Marbles


        pointCloud.Add(newMarble.transform.position);
        guidCloud.Add(guid);
    }

    public void ClaimMarble(Guid guid)
    {
        GameObject marble = Marbles[guid];
        _poolManager.DeactivateGameObject(marble);
        Marbles.Remove(guid);
        MarblesClaimList.Remove(guid);

        int index = guidCloud.IndexOf(guid);
        pointCloud.RemoveAt(index);
        guidCloud.RemoveAt(index);

        tree.Build(pointCloud);
        StartCoroutine(DisplayScore(guid, marble));
    }

    public static void Rebuild(List<Vector3> list)
    {
        tree.Build(list, maxPointsPerLeafNode);
    }


    public Guid GetCloseMarbleToPositionOptimized(Vector3 position)
    {
        List<int> results = new List<int>();
        if (query == null) return Guid.Empty;
        query.KNearest(tree, position, 1, results);     //using imported Kd Tree Datastructure log(N) 3D spatial search, k=1 to find first nearest neighbour
        if(results.Count<=0) return Guid.Empty;
        int result = results[0];

        Guid foundGuid = guidCloud[result];
        tree.Points[result] = new Vector3(int.MinValue, int.MinValue, int.MinValue);    //Modifying tree point to set it to min_value which is out of the scene boundaries

        return foundGuid;
    }

    public Guid GetCloseMarbleToPosition( Vector3 position )  //Returns closest marble in position to actor using bruteforce [NOT_OPTIMIZED] O(N_Actors x N_Marbles)
    {
        float minDisance = float.MaxValue;
        Guid minMarbleGUID = Guid.Empty;
        foreach(Guid guid in Marbles.Keys)
        {

            float dist = Vector3.Distance(position, Marbles[guid].transform.position);
            if (dist < minDisance && !MarblesClaimList[guid])
            {
                minMarbleGUID = guid;
                minDisance = dist;
            }
        }

            return minMarbleGUID;
    }


     

    
    private IEnumerator DisplayScore(Guid guid, GameObject marble)
    {
        float Value = UnityEngine.Random.value * 100f - 25f;
        Transform _textboxContainer = _poolManager.GetObjectFromPool(PoolManager.PooledObject.TextBox, numberOfMarbles / 10, transform).transform;
        _textboxContainer.gameObject.SetActive(true);
        TextMesh _textmesh = _textboxContainer.Find("Textbox").Find("ScoreText").gameObject.GetComponent<TextMesh>();
        MarblesClaimList[guid] = true;
        
        
        _textmesh.text = Value.ToString("##.#");
        var steps = 15;
        _textboxContainer.localScale = Vector3.zero;
        _textboxContainer.transform.position = marble.transform.position;
        for (var i = 0; i < steps; i++)
        {
            _textboxContainer.localScale += Vector3.one / (steps) ;
            yield return new WaitForEndOfFrame();
        }
        _textboxContainer.localScale = Vector3.zero;

        
        _poolManager.DeactivateGameObject(_textboxContainer.gameObject);
    }



}
