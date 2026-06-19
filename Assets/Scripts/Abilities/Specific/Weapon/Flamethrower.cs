using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

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

        private CharacterState myState;
        private GameObject myCamera;
        [SerializeField] private GameObject projectile;

        public Flamethrower(float dps, float range, float angle, float speed, DotStatusInstance dot, GameObject projectile, float tickrate)
        {
            this.dps = dps;
            this.range = range;
            this.angle = angle;
            this.speed = speed;
            this.dot = dot.CloneInstance() as DotStatusInstance;
            this.tickrate = tickrate == 0 ? 1 / Time.fixedDeltaTime : tickrate;
            this.projectile = projectile;
        }

        public ICastEffect Clone()
        {
            return new Flamethrower(dps, range, angle, speed, dot, projectile, tickrate);
        }

        void ICastEffect.ClientCastStart(AbilityPayload payload)
        {
            myState = (payload as IComponentPayload<CharacterState>).Component;
        }

        AbilityPayload ICastEffect.OwnerCastStart(TargetPayload target, Vector3 position, Vector3 cameraPosition, Vector3 direction, CharacterState myState)
        {
            this.myState = myState;
            myCamera = GameObject.FindGameObjectWithTag("MainCamera");
            return null;
        }

        AbilityPayload ICastEffect.ServerCastStart(AbilityPayload payload, TargetPayload target, Vector3 position, Vector3 cameraPosition, Vector3 direction, CharacterState myState)
        {
            timeCount = 0;
            this.myState = myState;
            if (dot.owner == null)
                dot.SetOwner(myState);
            return new VictimPayload(myState);
        }

        void ICastEffect.ClientCastUpdate()
        {
            //TODO: Particles
        }

        void ICastEffect.ServerCastFixedUpdate()
        {
            //TODO: Change to network update, and actually implement network updates
            //TODO: Progress "projectiles"
            while (timeCount <= 0)
            {
                var inst = GameObject.Instantiate(projectile, myState.transform.position, myState.transform.rotation);
                var statusInst = dot.CloneInstance() as DotStatusInstance;
                statusInst.Amount /= tickrate;
                inst.GetComponent<FlamethrowerDamage>().Setup(dps / tickrate, range, angle, speed, statusInst, myState);
                timeCount += 1 / tickrate;
            }
            timeCount -= Time.fixedDeltaTime;
        }

        void ICastEffect.OwnerCastUpdate()
        {
            Vector3 cameraForwards = myCamera.transform.forward;
            cameraForwards.y = 0;
            myState.transform.rotation = Quaternion.LookRotation(cameraForwards, Vector3.up);
            //TODO: Particles
        }
    }
}
