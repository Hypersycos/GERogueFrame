using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

namespace Hypersycos.GERogueFrame
{
    class Flamethrower : ICastEffect
    {
        public float dps;
        public float range;
        public float angle;
        public float speed;
        public float tickrate;
        public float timeCount;
        public DotStatusInstance dot;
        public float falloff = 1;

        private CharacterState myState;
        private Transform myCamera;
        [SerializeField] private GameObject projectile;
        [SerializeField] private GameObject vfx;
        private GameObject vfxInstance;

        public Flamethrower(float dps, float range, float angle, float speed, DotStatusInstance dot, GameObject projectile, GameObject vfx, float tickrate, float falloff)
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
        }

        public ICastEffect Clone()
        {
            return new Flamethrower(dps, range, angle, speed, dot, projectile, vfx, tickrate, falloff);
        }

        void DoParticles()
        {
            vfxInstance = GameObject.Instantiate(vfx, myState.transform.Find("CameraTarget"));
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

        void ICastEffect.ClientCastStart(AbilityPayload payload)
        {
            myState = (payload as IComponentPayload<CharacterState>).Component;
            myCamera = myState.transform.Find("CameraPos");

            DoParticles();
        }

        AbilityPayload ICastEffect.OwnerCastStart(TargetPayload target, Vector3 position, Vector3 cameraPosition, Vector3 direction, CharacterState myState)
        {
            this.myState = myState;
            myCamera = myState.transform.Find("CameraPos");
            myState.GetComponent<PlayerMovementController>().lockedToCamera = true;

            DoParticles();
            return null;
        }

        AbilityPayload ICastEffect.OwnerCastEnd(TargetPayload target, Vector3 position, Vector3 cameraPosition, Vector3 direction, CharacterState myState)
        {
            vfxInstance.GetComponent<ParticleSystem>().Stop();
            myState.GetComponent<PlayerMovementController>().lockedToCamera = false;
            return null;
        }


        AbilityPayload ICastEffect.ServerCastStart(AbilityPayload payload, TargetPayload target, Vector3 position, Vector3 cameraPosition, Vector3 direction, CharacterState myState)
        {
            timeCount = 0;
            this.myState = myState;

            myCamera = myState.transform.Find("CameraPos");
            if (dot.owner == null)
                dot.SetOwner(myState);
            return new VictimPayload(myState);
        }

        void ICastEffect.ServerCastFixedUpdate()
        {
            //TODO: Change to network update, and actually implement network updates
            //TODO: Progress "projectiles"
            while (timeCount <= 0)
            {
                var inst = GameObject.Instantiate(projectile, myState.transform.Find("CameraTarget").position, myCamera.rotation);
                var statusInst = dot.CloneInstance() as DotStatusInstance;
                statusInst.Amount /= tickrate;
                inst.GetComponent<FlamethrowerDamage>().Setup(dps / tickrate, range, Mathf.Deg2Rad * angle, speed, statusInst, myState, falloff);
                timeCount += 1 / tickrate;
            }
            timeCount -= Time.fixedDeltaTime;
        }

        void ICastEffect.OwnerCastUpdate()
        {
            UpdateParticles();
        }

        void ICastEffect.ClientCastUpdate()
        {
            if (!myState.IsOwner)
                UpdateParticles();
        }
    }
}
