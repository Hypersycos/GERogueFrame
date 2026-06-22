using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;

namespace Hypersycos.GERogueFrame
{
    interface IProjectilePayload
    {
        public ProjectileSpawnParams SpawnParams { get; }
        public int ObjectID { get; }
        public ProjectileID SpawnID { get; }
    }
    record ProjectilePayload : AbilityPayload, IProjectilePayload
    {
        ProjectileSpawnParams spawnParams;
        int objectID;
        ProjectileID spawnID;

        public ProjectilePayload(ProjectileSpawnParams spawnParams, int objectID, ProjectileID spawnID)
        {
            this.spawnParams = spawnParams;
            this.objectID = objectID;
            this.spawnID = spawnID;
        }

        public ProjectileSpawnParams SpawnParams => spawnParams;

        public int ObjectID => objectID;

        public ProjectileID SpawnID => spawnID;

        public override void Serialize(FastBufferWriter writer)
        {
            writer.WriteValueSafe(spawnParams);
            writer.WriteValueSafe(objectID);
            writer.WriteValueSafe(spawnID);
        }

        public new static AbilityPayload Deserialize(FastBufferReader reader)
        {
            reader.ReadValueSafe(out ProjectileSpawnParams spawnParams);
            reader.ReadValueSafe(out int objectID);
            reader.ReadValueSafe(out ProjectileID projID);
            return new ProjectilePayload(spawnParams, objectID, projID);
        }
    }
}
