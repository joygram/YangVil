using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace CatVillage.Core
{
    public class AudioHub : MonoBehaviour
    {
        public static AudioHub Instance { get; private set; }

        AudioSource _bgm;
        AudioSource _sfx;
        AssetScope _bgmScope;
        readonly AssetScope _sfxScope = new AssetScope();
        object _bgmKey;

        void Awake()
        {
            Instance = this;
            _bgm = gameObject.AddComponent<AudioSource>();
            _bgm.loop = true;
            _bgm.volume = 0.6f;
            _sfx = gameObject.AddComponent<AudioSource>();
        }

        void OnDestroy()
        {
            _bgmScope?.Dispose();
            _sfxScope.Dispose();
        }

        public void PlayBgm(AssetReference clip)
        {
            if (clip == null || !clip.RuntimeKeyIsValid())
            {
                _bgm.Stop();
                return;
            }
            if (Equals(_bgmKey, clip.RuntimeKey) && _bgm.isPlaying) return;
            _bgmKey = clip.RuntimeKey;
            _bgm.Stop();
            _bgmScope?.Dispose();
            _bgmScope = new AssetScope();
            StartCoroutine(_bgmScope.LoadClip(clip, c =>
            {
                _bgm.clip = c;
                _bgm.Play();
            }));
        }

        public void PlaySfx(AssetReference clip)
        {
            if (clip == null || !clip.RuntimeKeyIsValid()) return;
            StartCoroutine(_sfxScope.LoadClip(clip, c => _sfx.PlayOneShot(c)));
        }
    }
}
