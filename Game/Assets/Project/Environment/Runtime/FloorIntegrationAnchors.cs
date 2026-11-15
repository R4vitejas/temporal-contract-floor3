using System;
using UnityEngine;

namespace TemporalContract.Environment
{
    /// <summary>Scene-owned references for attaching reviewed subsystem prefabs. No gameplay authority.</summary>
    public sealed class FloorIntegrationAnchors : MonoBehaviour
    {
        [Serializable]
        public sealed class Room
        {
            public string sourceRoom;
            public string label;
            public Transform doorHinge;
            public Transform board;
            public Light roomLight;
            public bool restricted;
        }
        public Transform attackerSpawn;
        public Transform defenderSpawn;
        public Transform vip;
        public Transform keycard;
        public Transform accessPanel;
        public Transform lightSwitch;
        public Transform movableChair;
        public Texture2D boardMap;
        public Room[] rooms;
    }
}
