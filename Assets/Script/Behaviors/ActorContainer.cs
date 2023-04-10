using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

//ActorBehaviour removed
[RequireComponent(typeof(MarbleContainer))]
public class ActorContainer : MonoBehaviour
{
    public GameObject ActorPrefab;

    private GameObject[] _actorsList;          //Usage of arrays instead of Lists
    private State[] _currentStatesList;
    private Guid[] _currentTargetsList;
    private MarbleContainer _containerReference;


    [SerializeField] private int numberOfActors = 10000;


    private SingletonFactory _singletonFactory;
    private float _delay = 0.05f;
    private enum State
    {
        Idle,
        Hunting,
    }

    private IEnumerator Start()
    {
        yield return new WaitForSeconds(0.1f);
        _singletonFactory = SingletonFactory.Instance;
        _actorsList = new GameObject[numberOfActors];
        _currentStatesList = new State[numberOfActors];
        _currentTargetsList = new Guid[numberOfActors];

        _containerReference = this.gameObject.GetComponent<MarbleContainer>();
        for( int i = 0; i < numberOfActors; i++ )
        {
            GameObject newActor = Instantiate( ActorPrefab, transform);
            newActor.SetActive(true);
            newActor.transform.position = Random.insideUnitSphere * 100f;
            _actorsList[i] = newActor;
            _currentStatesList[i] = State.Idle;
        }

        StartCoroutine(StartLoop());
    }

    private IEnumerator StartLoop()      
    {
        while (true)
        {
            yield return new WaitForSeconds(_delay);
            for (int i = 0; i < numberOfActors; i++)
            {
                switch (_currentStatesList[i])
                {
                    case State.Idle:
                        UpdateIdle(i);
                        break;
                    case State.Hunting:
                        UpdateMoving(i);
                        break;
                }
            }
        }
    }





    private void UpdateIdle(int actorIndex)
    {
        _currentTargetsList[actorIndex] = _containerReference.GetCloseMarbleToPositionOptimized(_actorsList[actorIndex].transform.position);
        if (_currentTargetsList[actorIndex] != null && _currentTargetsList[actorIndex] != Guid.Empty)
        {
            _currentStatesList[actorIndex] = State.Hunting;
        }
    }

    private void UpdateMoving(int actorIndex)
    {

        if (!MarbleContainer.MarblesClaimList.ContainsKey(_currentTargetsList[actorIndex]))     //Check if ClaimList has the target "Not Destroyed"
        {
            _currentTargetsList[actorIndex] = Guid.Empty;
            _currentStatesList[actorIndex] = State.Idle;
            return;
        }
       
        if (MarbleContainer.MarblesClaimList[_currentTargetsList[actorIndex]])                  //Check if the target is not claimed by other Actor
        {
            _currentTargetsList[actorIndex] = Guid.Empty;
            _currentStatesList[actorIndex] = State.Idle;
            return;
        }

        var thisToTarget = MarbleContainer.Marbles[_currentTargetsList[actorIndex]].transform.position - _actorsList[actorIndex].transform.position;
        var thisToTargetDirection = thisToTarget.normalized;
        _actorsList[actorIndex].transform.position += thisToTargetDirection * 10 * _delay;

        if (thisToTarget.magnitude < 0.25f)
        {

            _containerReference.ClaimMarble(_currentTargetsList[actorIndex]);
            _currentTargetsList[actorIndex] = Guid.Empty;
            _currentStatesList[actorIndex] = State.Idle;
        }
    }
}
