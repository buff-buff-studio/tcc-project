using System;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace Misc.Props
{
    public abstract class AnimalState
    {
        protected AnimalController animalController;
        protected float timeInState;
        
        public AnimalState(AnimalController animalController)
        {
            this.animalController = animalController;
        }
        
        public abstract void OnEnter();
        public virtual void OnExit(){}

        public virtual void OnUpdate(float deltaTime)
        {
            timeInState += Time.deltaTime;
        }
        public virtual void OnFixedUpdate(){}
        public abstract Vector3 GetWalkInput();
        public abstract override string ToString(); 
    }
    
    public class IdleState : AnimalState
    {
        private float _timeToNextState;
        public IdleState(AnimalController animalController) : base(animalController)
        {
        }
        public override void OnEnter()
        {
            _timeToNextState = Random.Range(2, 6);
        }

        public override void OnUpdate(float deltaTime)
        {
            base.OnUpdate(deltaTime);

            if (timeInState >= _timeToNextState)
                animalController.SetState(Random.Range(0,3) < 2 ? new WalkingState(animalController) : new IdleState(animalController));
        }

        public override Vector3 GetWalkInput()
        {
            throw new NotImplementedException();
        }

        public override string ToString() => "Idle";
    }

    public class WalkingState : AnimalState
    {
        private float _checkTime = 0.15f;
        private float _currentTime = 0f;
        
        public WalkingState(AnimalController animalController) : base(animalController)
        {
        }

        public override void OnEnter()
        {
            animalController.SetDestination(GenerateRandomDestination());
        }
        
        public override void OnUpdate(float deltaTime)
        {
            base.OnUpdate(deltaTime);
            _currentTime += deltaTime;

            if (_currentTime >= _checkTime)
            {
                _currentTime = 0;
                ReachedDestination();
            }
            
            if (timeInState >= 20)
                animalController.SetState(new IdleState(animalController));
        }

        public override Vector3 GetWalkInput()
        {
            throw new NotImplementedException();
        }

        private void ReachedDestination()
        {
            if (!animalController.HasReachedDestination()) return;
            if (Random.Range(0, 5) <= 2)
                animalController.SetState(new IdleState(animalController));
            else
                animalController.SetDestination(GenerateRandomDestination());
        }

        private Vector3 GenerateRandomDestination()
        {
            int maxAttempts = 10;
            for (int i = 0; i < maxAttempts; i++)
            {
                Vector3 randomPoint = Random.insideUnitCircle * animalController.walkingRadius;
                if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, 20, NavMesh.AllAreas)) return hit.position;
            }
            return animalController.transform.position;
        }

        public override string ToString() => "Walking";
    }

    public class RunningState : AnimalState
    {
        private float _checkTime = 0.15f;
        private float _currentTime = 0f;
        private Transform _target;
        
        public RunningState(AnimalController animalController, Transform target) : base(animalController)
        {
            _target = target;
        }

        public override void OnEnter()
        {
            animalController.SetDestination(GetAwayDestination());
            animalController.agent.speed *= 2;
        }
        public override void OnUpdate(float deltaTime)
        {
            base.OnUpdate(deltaTime);
            _currentTime += deltaTime;

            if (!(_currentTime >= _checkTime)) return;
            _currentTime = 0;
            ReachedDestination();
        }
        
        private void ReachedDestination()
        {
            if (!animalController.HasReachedDestination()) return;
            animalController.SetState(new IdleState(animalController));
        }
        private Vector3 GetAwayDestination()
        {
            int maxAttempts = 10;
            for (int i = 0; i < maxAttempts; i++)
            {
                Vector3 randomPoint = Random.insideUnitCircle * (animalController.walkingRadius + animalController.runRadius);
                Vector3 awayDirection = (animalController.transform.position - _target.position).normalized;
                Vector3 awayPosition = animalController.transform.position + awayDirection * animalController.runRadius;
                Vector3 randomAwayPosition = awayPosition + randomPoint;
        
                if (NavMesh.SamplePosition(randomAwayPosition, out NavMeshHit hit, 20, NavMesh.AllAreas)) return hit.position;
            }
            return animalController.transform.position;
        }
        
        public override void OnExit()
        {
            animalController.agent.speed /= 2;
        }
        public override Vector3 GetWalkInput()
        {
            throw new NotImplementedException();
        }

        public override string ToString() =>"Running";
    }
    
    public class AnimalController : MonoBehaviour
    {
       private AnimalState state;
       public Animator animalAnimator;
       public NavMeshAgent agent;
       public float runRadius = 4;
       public float walkingRadius = 5;
       public LayerMask playerLayer;
       
       private void Start()
       {
           SetState(new IdleState(this));
       }

       private void Update()
       {
           Debug.Log(state.ToString());
           state.OnUpdate(Time.deltaTime);
           var objects = Physics.OverlapSphere(transform.position, runRadius, playerLayer);
           if (objects.Length > 0) 
               SetState(new RunningState(this, objects[0].transform));
       }
       
       private void FixedUpdate()
       {
           state.OnFixedUpdate();
       }
       
       public void SetState(AnimalState newState)
       {
           Debug.Log($"Changing state from {state} to {newState}");
           state?.OnExit();
           state = newState;
           state.OnEnter();
       }

       public void SetDestination(Vector3 newDestination)
       {
           if(newDestination == Vector3.zero) return;
           agent.SetDestination(newDestination);
       }

       public bool HasReachedDestination()
       {
           if (agent.pathPending || !(agent.remainingDistance <= 0.5f) ||
               agent.remainingDistance == Mathf.Infinity) return false;
           
           Debug.Log("Agent has reached the destination.");
           return true;
       }
    }
    
    #if UNITY_EDITOR
    [CustomEditor(typeof(AnimalController))]
    public class AnimalControllerEditor : Editor
    {
        private AnimalController _animalController;
        
        private void OnEnable()
        {
            _animalController = (AnimalController)target;
        }

        private void OnSceneGUI()
        {
            Handles.color = Color.red;
            Handles.DrawWireDisc(_animalController.transform.position, Vector3.up, _animalController.runRadius);
            
            Handles.color = Color.blue;
            Handles.DrawWireDisc(_animalController.transform.position, Vector3.up, _animalController.walkingRadius);
        }
    }
    #endif
}
