using System;
using System.IO;
using NetBuff.Components;
using NetBuff.Interface;
using NetBuff.Misc;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

namespace Misc.Props
{
    public class AnimalController : NetworkBehaviour
    {
        private static readonly int _Walking = Animator.StringToHash("Walking");
        public int state;
        public NavMeshAgent agent;
        public Animator animator;
        public float timer;
        public float next;
        public float speed = 2;
        public float runRadius = 4;
        public float walkingRadius = 5;
        public LayerMask playerLayer;

        private void OnEnable()
        {
            if (!HasAuthority)
                return;
            
            ResetState();
            InvokeRepeating(nameof(_UpdateState), 0, 0.1f);
        }

        private void OnDisable()
        {
            if (!HasAuthority)
                return;
            
            CancelInvoke(nameof(_UpdateState));
        }

        private void FixedUpdate()
        {
            agent.speed = state == 2 ? speed * 2 : speed;
            
            if (!HasAuthority)
                return;
            
            var objects = Physics.OverlapSphere(transform.position, runRadius, playerLayer);
            if (objects.Length > 0)
            {
                state = 2;
                GenerateGetAwayDestination(objects[0].transform);
            }
            
            switch (state)
            {
                case 0:
                    animator.SetBool(_Walking, false);
                    if((timer += Time.deltaTime) > next)
                    {
                        ResetState();
                        state = Random.Range(0, 3) < 2 ? 1 : 0;
                        if (state == 1)
                            GenerateRandomDestination();
                    }
                    break;
                case 1:
                    if ((timer += Time.deltaTime) > 20)
                    {
                        ResetState();
                        state = 0;
                    }
                    else if (agent.remainingDistance < 0.5f)
                    {
                        ResetState();
                        state = Random.Range(0, 3) < 2 ? 0 : 1;
                        if (state == 1)
                            GenerateRandomDestination();
                    }
                    animator.SetBool(_Walking, true);
                    break;
                case 2:
                    if ((timer += Time.deltaTime) > 20)
                    {
                        ResetState();
                        state = 0;
                    }
                    else if (agent.remainingDistance < 0.5f)
                    {
                        ResetState();
                        state = 0;
                    }
                    animator.SetBool(_Walking, true);
                    break;
            }
        }
        
        public void ResetState()
        {
            state = 0;
            timer = 0;
            next = Random.Range(3, 6);
        }

        public override void OnClientReceivePacket(IOwnedPacket packet)
        {
            if (HasAuthority)
                return;
            
            if (packet is AnimalStatePacket statePacket)
            {
                state = statePacket.State;
                transform.position = statePacket.Position;
                transform.rotation = Quaternion.Euler(0, statePacket.Rotation, 0);
            }
            
            if (packet is AnimalPathfindingPacket pathfindingPacket)
            {
                agent.SetDestination(pathfindingPacket.Target);
            }
        }

        public void _UpdateState()
        {
            var t = transform;
            SendPacket(new AnimalStatePacket
            {
                Id = Id,
                State = state,
                Position = t.position,
                Rotation = t.rotation.eulerAngles.y
            });
        }
        
        public void SetDestination(Vector3 destination)
        {
            if (!HasAuthority)
                return;
            
            SendPacket(new AnimalPathfindingPacket
            {
                Id = Id,
                Target = destination
            });
            
            agent.SetDestination(destination);
        }
        
        public void GenerateRandomDestination()
        {
            int maxAttempts = 20;
            for (int i = 0; i < maxAttempts; i++)
            {
                Vector3 randomPoint = transform.position + new Vector3(Random.insideUnitCircle.x, 0, Random.insideUnitCircle.y) * walkingRadius;
                if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, 5, NavMesh.AllAreas))
                {
                    SetDestination(hit.position);
                    return;
                }
            }
        }
        
        public void GenerateGetAwayDestination(Transform target)
        {
            int maxAttempts = 10;
            for (int i = 0; i < maxAttempts; i++)
            {
                Vector3 randomPoint = Random.insideUnitCircle * (walkingRadius + runRadius);
                Vector3 awayDirection = (transform.position - target.position).normalized;
                Vector3 awayPosition = transform.position + awayDirection * runRadius;
                Vector3 randomAwayPosition = awayPosition + randomPoint;
        
                if (NavMesh.SamplePosition(randomAwayPosition, out NavMeshHit hit, 5, NavMesh.AllAreas))
                {
                    SetDestination(hit.position);
                    return;
                }
            }
        }
    }

    [Serializable]
    public class AnimalPathfindingPacket : IOwnedPacket
    {
        public NetworkId Id { get; set; }
        public Vector3 Target { get; set; }
        
        public void Serialize(BinaryWriter writer)
        {
            Id.Serialize(writer);
            writer.Write(Target.x);
            writer.Write(Target.y);
            writer.Write(Target.z);
        }
        
        public void Deserialize(BinaryReader reader)
        {
            Id = NetworkId.Read(reader);
            Target = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        }
    }

    [Serializable]
    public class AnimalStatePacket : IOwnedPacket
    {
        public NetworkId Id { get; set; }
        public int State { get; set; }
        public Vector3 Position { get; set; }
        public float Rotation { get; set; }

        public void Serialize(BinaryWriter writer)
        {
            Id.Serialize(writer);
            writer.Write(State);
            writer.Write(Position.x);
            writer.Write(Position.y);
            writer.Write(Position.z);
            writer.Write(Rotation);
        }

        public void Deserialize(BinaryReader reader)
        {
            Id = NetworkId.Read(reader);
            State = reader.ReadInt32();
            Position = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            Rotation = reader.ReadSingle();
        }
    }
}
