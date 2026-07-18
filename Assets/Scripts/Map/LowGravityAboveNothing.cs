using UnityEngine;

namespace Hypersycos.GERogueFrame
{
    public class LowGravityAboveNothing : MonoBehaviour
    {
        public float lowGravity;
        public float nothingDistance = Mathf.Infinity;
        public LayerMask mask;
        public string id = "LowGravityAboveNothing";

        PlayerMovementController myController;
        bool wasTrueLast = false;

        void Start()
        {
            myController = GetComponent<PlayerMovementController>();
        }

        // Update is called once per frame
        void FixedUpdate()
        {
            if (Physics.Raycast(transform.position, Vector3.down, nothingDistance, mask, QueryTriggerInteraction.Ignore))
            {
                if (!wasTrueLast)
                    myController.RemoveGravityModifier(id);
                wasTrueLast = true;
            }
            else if (wasTrueLast)
            {
                myController.AddGravityModifier(lowGravity, id);
                wasTrueLast = false;
            }
        }
    }
}
