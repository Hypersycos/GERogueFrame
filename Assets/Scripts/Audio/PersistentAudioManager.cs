using System.Collections;
using UnityEngine;

namespace Hypersycos.GERogueFrame
{
    public class PersistentAudioManager : MonoBehaviour
    {
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        public AudioSource interactionSfx;
        [SerializeField] AudioSource menuMusic;
        [SerializeField] AudioSource gameMusic;

        bool inMenu = true;

        public static PersistentAudioManager Singleton;

        private void Awake()
        {
            if (Singleton == null)
            {
                Singleton = this;
                menuMusic.Play();
                gameMusic.volume = 0;
            }
        }

        public static void PlayInteract(AudioClip clip, float volume = 1)
        {
            IEnumerator HoldHostage(AudioClip clip)
            {
                yield return new WaitForSeconds(clip.length + 1.0f);
            }

            Singleton.interactionSfx.PlayOneShot(clip, volume);
            Singleton.StartCoroutine(HoldHostage(clip));
        }

        public static void PlayMusic(bool menu)
        {
            IEnumerator FadeToMenu(float fadeTime = 2)
            {
                float start = Time.time;
                Singleton.menuMusic.Play();
                while (Time.time < start + fadeTime)
                {
                    Singleton.menuMusic.volume = Mathf.Clamp01((Time.time - start) / fadeTime);
                    Singleton.gameMusic.volume = Mathf.Clamp01(1 - (Time.time - start) / fadeTime);
                    yield return null;
                }
                Singleton.gameMusic.Stop();
            }

            IEnumerator FadeToGame(float fadeTime = 2)
            {
                float start = Time.time;
                Singleton.gameMusic.Play();
                while (Time.time < start + fadeTime)
                {
                    Singleton.gameMusic.volume = Mathf.Clamp01((Time.time - start) / fadeTime);
                    Singleton.menuMusic.volume = Mathf.Clamp01(1 - (Time.time - start) / fadeTime);
                    yield return null;
                }
                Singleton.menuMusic.Stop();
            }

            if (menu != Singleton.inMenu)
            {
                if (menu)
                    Singleton.StartCoroutine(FadeToMenu());
                else
                    Singleton.StartCoroutine(FadeToGame());
            }
        }
    }
}
