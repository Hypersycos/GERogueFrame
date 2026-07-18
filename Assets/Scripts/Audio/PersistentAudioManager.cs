using Hypersycos.SaveSystem;
using Hypersycos.Utils;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace Hypersycos.GERogueFrame
{
    public class PersistentAudioManager : MonoBehaviour
    {
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        public AudioSource interactionSfx;
        [SerializeField] AudioSource musicSource1;
        [SerializeField] AudioSource musicSource2;
        [SerializeField] List<AudioClip> gameClips;
        [SerializeField] List<AudioClip> menuClips;

        [SerializeField] TypedRegisteredValueSO<float> masterVol;
        [SerializeField] TypedRegisteredValueSO<float> sfxMasterVol;
        [SerializeField] TypedRegisteredValueSO<float> playerSfxVol;
        [SerializeField] TypedRegisteredValueSO<float> enemySfxVol;
        [SerializeField] TypedRegisteredValueSO<float> allySfxVol;
        [SerializeField] TypedRegisteredValueSO<float> interactionSfxVol;
        [SerializeField] TypedRegisteredValueSO<float> musicVol;

        bool inMenu = true;
        bool usingClip1;

        AudioSource currentSource => usingClip1 ? musicSource1 : musicSource2;
        AudioSource altSource => usingClip1 ? musicSource2 : musicSource1;

        AudioClip currentClip { get => currentSource.clip; set => currentSource.clip = value; }
        AudioClip altClip { get => altSource.clip; set => altSource.clip = value; }

        public static PersistentAudioManager Singleton;

        private void Awake()
        {
            if (Singleton == null)
            {
                Singleton = this;
            }
        }

        private void Start()
        {
            if (Singleton == this)
            {
                currentSource.clip = menuClips.TakeRandom();
                currentSource.Play();

                AudioMixer mixer = musicSource1.outputAudioMixerGroup.audioMixer;

                mixer.SetFloat("Master", LinearToDB(masterVol.Value));
                masterVol.ValueUpdated.AddListener((v) => mixer.SetFloat("Master", LinearToDB(v)));

                mixer.SetFloat("SFX", LinearToDB(sfxMasterVol.Value));
                sfxMasterVol.ValueUpdated.AddListener((v) => mixer.SetFloat("SFX", LinearToDB(v)));

                mixer.SetFloat("PlayerSFX", LinearToDB(playerSfxVol.Value));
                playerSfxVol.ValueUpdated.AddListener((v) => mixer.SetFloat("PlayerSFX", LinearToDB(v)));

                mixer.SetFloat("EnemySFX", LinearToDB(enemySfxVol.Value));
                enemySfxVol.ValueUpdated.AddListener((v) => mixer.SetFloat("EnemySFX", LinearToDB(v)));

                mixer.SetFloat("AllySFX", LinearToDB(allySfxVol.Value));
                allySfxVol.ValueUpdated.AddListener((v) => mixer.SetFloat("AllySFX", LinearToDB(v)));

                mixer.SetFloat("InteractionSFX", LinearToDB(interactionSfxVol.Value));
                interactionSfxVol.ValueUpdated.AddListener((v) => mixer.SetFloat("InteractionSFX", LinearToDB(v)));

                mixer.SetFloat("Music", LinearToDB(musicVol.Value * 0.25f));
                musicVol.ValueUpdated.AddListener((v) => mixer.SetFloat("Music", LinearToDB(v * 0.25f)));
            }
        }

        public static float DBToLinear(float DB) => DB == -80f ? 0 : Mathf.Pow(10, DB / 20);
        public static float LinearToDB(float linear) => Mathf.Max(-80f, Mathf.Log10(linear) * 20);

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
            IEnumerator Fade(AudioSource from, AudioSource to, float fadeTime = 2)
            {
                float start = Time.time;
                while (Time.time < start + fadeTime / 2)
                {
                    from.volume = Mathf.Clamp01(1 - (Time.time - start) * 2 / fadeTime);
                    yield return null;
                }
                from.volume = 0;
                from.Stop();

                to.volume = 0;
                to.Play();
                start += fadeTime / 2;
                while (Time.time < start + fadeTime / 2)
                {
                    to.volume = Mathf.Clamp01((Time.time - start) * 2 / fadeTime);
                    yield return null;
                }
                to.volume = 1;
            }

            if (menu != Singleton.inMenu || menu == false)
            {
                Singleton.usingClip1 = !Singleton.usingClip1;
                if (menu)
                    Singleton.currentClip = Singleton.menuClips.TakeRandom();
                else
                    Singleton.currentClip = Singleton.gameClips.TakeRandom();
                Singleton.StartCoroutine(Fade(Singleton.altSource, Singleton.currentSource));
            }
        }
    }
}
