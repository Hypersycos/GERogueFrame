using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Hypersycos.RogueFrame
{
    public class DamageInstanceScript : MonoBehaviour
    {
        [SerializeField] public float speed = 50f;
        public Vector2 velocity = new Vector2(0,1);
        [SerializeField] public float lifetime = 0.5f;
        TMP_Text text;
        float time = 0f;
        // Start is called before the first frame update
        private void Start()
        {
            text = GetComponent<TMP_Text>();
        }

        // Update is called once per frame
        void Update()
        {
            time += Time.deltaTime;

            Vector3 temp = transform.localPosition;
            temp.x += speed * velocity.x * Time.deltaTime;
            temp.y += speed * velocity.y * Time.deltaTime;
            transform.localPosition = temp;

            Color tempC = text.color;
            tempC.a -= Time.deltaTime / lifetime;
            text.color = tempC;

            if (time > lifetime)
                Destroy(gameObject);
        }
    }
}
