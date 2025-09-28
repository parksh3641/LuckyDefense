using System;
using UnityEngine;

namespace LuckyDefense
{
    public enum SFXType
    {
        Click,
        Hit
    }
    
    public class SoundManager : MonoBehaviour
    {
        private static SoundManager _instance;
        public static SoundManager Instance => _instance;
        
        [SerializeField] private AudioSource bgmAudioSource;
        [SerializeField] private AudioClip[] bgmClips;

        [SerializeField] private AudioSource[] sfxAudioSource;
        [SerializeField] private AudioClip[] sfxClips;
        
        private int currentSfxIndex = 0;

        private void Awake()
        {
            if (_instance == null)
                _instance = this;
            else if (_instance != this)
                Destroy(gameObject);
            
            bgmAudioSource.Play();
        }

        public void PlaySfx(SFXType type)
        {
            if (sfxAudioSource.Length == 0) return;
            
            int startIndex = currentSfxIndex;
            bool foundAvailableSource = false;
            
            do
            {
                if (!sfxAudioSource[currentSfxIndex].isPlaying)
                {
                    foundAvailableSource = true;
                    break;
                }
                
                currentSfxIndex = (currentSfxIndex + 1) % sfxAudioSource.Length;
                
            } while (currentSfxIndex != startIndex);
            
            int clipIndex = (int)type;
            if (clipIndex >= 0 && clipIndex < sfxClips.Length)
            {
                sfxAudioSource[currentSfxIndex].clip = sfxClips[clipIndex];
                sfxAudioSource[currentSfxIndex].Play();
                
                currentSfxIndex = (currentSfxIndex + 1) % sfxAudioSource.Length;
            }
            else
            {
                Debug.LogWarning($"SFX 클립 인덱스가 범위를 벗어남: {clipIndex}");
            }
        }
        
        public void StopAllSfx()
        {
            foreach (var source in sfxAudioSource)
            {
                if (source.isPlaying)
                {
                    source.Stop();
                }
            }
        }
    }
}