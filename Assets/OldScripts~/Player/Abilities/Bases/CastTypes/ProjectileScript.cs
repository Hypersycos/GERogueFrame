using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Hypersycos.RogueFrame
{
    public class ProjectileScript : MonoBehaviour
    {
        [SerializeField] private Vector3 LaunchVelocity = new();
        [SerializeField] private float Lifetime = 0;
        [SerializeField] private float Range = 0;
        [SerializeField] private int MaxEnemyHits = 0;
        [SerializeField] private int MaxTerrainHits = 0;
        [SerializeField] private int MaxHits = 0;
        [SerializeField] private bool CanPunchthroughEnemies = false;
        [SerializeField] private bool CanPunchthroughTerrain = false;
        [SerializeField] private float MaxPunchthroughLength = 0;
        private bool CanHitTriggers = false;

        float distanceTravelled = 0;
        float PunchThrough = 0;
        Vector3? LastCollision = null;
        float Timer = 0;
        int Hits = 0;
        int EnemyHits = 0;
        int TerrainHits = 0;
        Vector3 StartPosition;
        List<CharacterState> alreadyHit = new();
        Action<CharacterState, Vector3> OnHitChar;
        Action<GameObject, Vector3> OnHitObj;

        private void OnTriggerEnter(Collider other)
        {
            if (CanHitTriggers)
            {
                Collided(other);
                if (LastCollision == null)
                    LastCollision = transform.position;
            }
        }
        private void OnCollisionEnter(Collision collision)
        {
            Collided(collision.collider);
            if (LastCollision == null)
                LastCollision = collision.GetContact(0).point;
        }

        private void Collided(Collider other)
        {
            //TODO: transform.position is not very accurate
            CharacterState enemy = other.GetComponent<CharacterState>();
            if (enemy == null)
            {
                OnHitObj(other.gameObject, transform.position);
                if (!CanPunchthroughTerrain) Delete();
                TerrainHits++;
            }
            else if (!alreadyHit.Contains(enemy))
            {
                OnHitChar(enemy, transform.position);
                if (CanPunchthroughEnemies)
                    alreadyHit.Add(enemy);
                else
                    Delete();
                EnemyHits++;
            }
            Hits++;
            if (MaxHits > 0 && Hits >= MaxHits) Delete();
            if (MaxTerrainHits > 0 && TerrainHits >= MaxTerrainHits) Delete();
            if (MaxEnemyHits > 0 && EnemyHits >= MaxEnemyHits) Delete();
        }

        private void OnCollisionExit(Collision collision)
        {
            if (MaxPunchthroughLength > 0 && LastCollision != null)
            {
                Vector3 newPosition = collision.GetContact(0).point;
                PunchThrough += (newPosition - (Vector3)LastCollision).magnitude;
                if (PunchThrough > MaxPunchthroughLength)
                    Expire();
            }
            distanceTravelled += (StartPosition - transform.position).magnitude;
            StartPosition = transform.position;
            LastCollision = null;
        }

        private void OnTriggerExit(Collider other)
        {
            if (CanHitTriggers)
            {
                if (MaxPunchthroughLength > 0 && LastCollision != null)
                {
                    PunchThrough += (transform.position - (Vector3)LastCollision).magnitude;
                    if (PunchThrough > MaxPunchthroughLength)
                        Expire();
                }
                LastCollision = null;
            }
        }

        public void Initialise(Action<CharacterState, Vector3> onHit, Action<GameObject, Vector3> onHitObj)
        {
            OnHitChar = onHit;
            OnHitObj = onHitObj;
            //Have to set velocity here for some reason otherwise it doesn't work :)
            GetComponent<Rigidbody>().velocity = transform.rotation * LaunchVelocity;
            CanHitTriggers = GetComponent<Collider>().isTrigger;
            StartPosition = transform.position;
        }

        private void FixedUpdate()
        {
            if (Range > 0 && (StartPosition - transform.position).magnitude + distanceTravelled >= Range)
            {
                Expire();
            }
            if (Lifetime > 0)
            {
                Timer += Time.fixedDeltaTime;
                if (Timer > Lifetime)
                    Expire();
            }
        }

        public void Delete()
        {
            Destroy(gameObject);
        }

        public virtual void Expire()
        {
            OnHitObj(null, transform.position);
            Destroy(gameObject);
        }
    }
}
