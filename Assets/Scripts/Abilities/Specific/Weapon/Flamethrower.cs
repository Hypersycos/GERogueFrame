using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.GraphicsBuffer;

namespace Hypersycos.GERogueFrame
{
    class FlamethrowerData : BaseAbilityData
    {
        public float dps;
        public float range;
        public float angle;
        public float speed;
        public float tickrate;
        public float falloff;
        public DotStatusInstance dot;

        public float fakeRange;
        public float fakeRadius;
        public LayerMask fakeMask;

        public GameObject projectile;
        public GameObject vfx;
        public override Ability CreateAbility()
        {
            return new Flamethrower(dps, range, angle, speed, dot, projectile, vfx, tickrate, falloff, fakeRange, fakeRadius, fakeMask);
        }
    }
    class Flamethrower : Ability
    {
        public float dps;
        public float range;
        public float angle;
        public float speed;
        public float tickrate;
        public float falloff;
        public DotStatusInstance dot;

        public float fakeRange;
        public float fakeRadius;
        private LayerMask fakeMask;

        private GameObject projectile;
        private GameObject vfx;

        private float timeCount;
        private CharacterState myState;
        private Transform myCamera;
        private GameObject vfxInstance;

        public override bool IsDirty { get => false; protected set { } }

        public override bool HasOwnerSync => false;

        public Flamethrower(float dps, float range, float angle, float speed, DotStatusInstance dot, GameObject projectile,
                            GameObject vfx, float tickrate, float falloff, float fakeRange, float fakeRadius, LayerMask fakeMask) : base(0, true, 0, 1000)
        {
            this.dps = dps;
            this.range = range;
            this.angle = angle;
            this.speed = speed;
            this.dot = dot.CloneInstance() as DotStatusInstance;
            this.tickrate = tickrate == 0 ? 1 / Time.fixedDeltaTime : tickrate;
            this.projectile = projectile;
            this.vfx = vfx;
            this.falloff = falloff;
            this.fakeRange = fakeRange;
            this.fakeRadius = fakeRadius;
            this.fakeMask = fakeMask;
        }

        void DoParticles()
        {
            vfxInstance = GameObject.Instantiate(vfx, myState.projectileSource);
            var particles = vfxInstance.GetComponent<ParticleSystem>();
            var main = particles.main;
            main.startLifetime = range / speed;
            main.startSpeed = speed;
            float maxSize = Mathf.Sin(Mathf.Deg2Rad * angle) * range;
            main.startSize = new(maxSize);


            ParticleSystem sparks = vfxInstance.transform.GetChild(0).GetComponent<ParticleSystem>();
            var sMain = sparks.main;
            sMain.startSpeed = new(range, range / 2);
            var sShape = sparks.shape;
            sShape.angle = angle;
            particles.Play();
        }

        void UpdateParticles()
        {
            vfxInstance.transform.rotation = myCamera.rotation;
        }

        public override bool CastingUpdate(Vector3 direction, Vector3 position, Vector3 cameraPosition, CharacterState myState)
        {
            if (myState.IsClient)
                UpdateParticles();
            return true;
        }

        public override bool CastingFixedUpdate(Vector3 direction, Vector3 position, Vector3 cameraPosition, CharacterState myState)
        {
            if (myState.IsServer)
            {
                while (timeCount <= 0)
                {
                    Vector3 spawnPos = myState.projectileSource.position;
                    var inst = GameObject.Instantiate(projectile, spawnPos, myCamera.rotation);
                    var statusInst = dot.CloneInstance() as DotStatusInstance;
                    statusInst.Amount /= tickrate;
                    HashSet<CharacterState> preDebounce = new();
                    float offsetAmount = Vector3.Dot(spawnPos - cameraPosition, direction) + fakeRadius - .5f;
                    Vector3 offsetCameraStart = cameraPosition + direction * offsetAmount;
                    foreach (RaycastHit hit in Physics.SphereCastAll(offsetCameraStart, fakeRadius, direction, fakeRange, fakeMask, QueryTriggerInteraction.Ignore))
                    {
                        if (hit.collider.gameObject.TryGetComponent(out CharacterState state) && state.Team != myState.Team)
                        {
                            preDebounce.Add(state);
                        }
                    }
                    inst.GetComponent<FlamethrowerDamage>().Setup(dps / tickrate, range, Mathf.Deg2Rad * angle, speed, statusInst, myState, falloff, preDebounce);
                    timeCount += 1 / tickrate;
                }
                timeCount -= Time.fixedDeltaTime;
            }
            return true;
        }

        public override bool CanCast(CharacterState myState) => true;

        public override bool OwnerCast(Vector3 direction, Vector3 position, Vector3 cameraPosition, CharacterState myState, out int chosenEffect, out AbilityPayload verifyData, out AbilityPayload abilityPayload)
        {
            this.myState = myState;
            myCamera = myState.transform.Find("CameraPos");
            myState.GetComponent<PlayerMovementController>().lockedToCamera = true;

            DoParticles();
            chosenEffect = 0;
            verifyData = null;
            abilityPayload = null;
            return true;
        }

        public override bool ServerCast(int desiredEffect, AbilityPayload verifyData, AbilityPayload abilityPayload, CharacterState myState, out int chosenEffect, out AbilityPayload payload)
        {
            timeCount = 0;
            this.myState = myState;

            myCamera = myState.transform.Find("CameraPos");
            if (dot.owner == null)
                dot.SetOwner(myState);

            chosenEffect = 0;
            payload = null;
            return true;
        }

        public override void ClientCast(int effectID, AbilityPayload payload, CharacterState myState)
        {
            if (!myState.IsOwner)
            {
                myCamera = myState.transform.Find("CameraPos");
                this.myState = myState;

                DoParticles();
            }
        }

        public override bool OwnerCastEnd(Vector3 direction, Vector3 position, Vector3 cameraPosition, CharacterState myState, out AbilityPayload verifyData, out AbilityPayload abilityPayload)
        {
            vfxInstance.GetComponent<ParticleSystem>().Stop();
            myState.GetComponent<PlayerMovementController>().lockedToCamera = false;
            verifyData = null;
            abilityPayload = null;
            return true;
        }

        public override bool ServerCastEnd(AbilityPayload verifyData, AbilityPayload abilityPayload, CharacterState myState, out AbilityPayload payload)
        {
            payload = null;
            return true;
        }

        public override void ClientCastEnd(AbilityPayload payload, CharacterState myState)
        {
            if (!myState.IsOwner)
                vfxInstance.GetComponent<ParticleSystem>().Stop();
        }

        public override void Update(CharacterState myState) { }

        public override void FixedUpdate(CharacterState myState) { }

        public override AbilityPayload Sync() => null;

        public override void SyncClient(AbilityPayload payload) { }

        public override void SyncOwner(AbilityPayload payload) { }
    }
}
