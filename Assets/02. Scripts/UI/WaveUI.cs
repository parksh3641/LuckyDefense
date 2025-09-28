using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace LuckyDefense
{
    public class WaveUI : MonoBehaviour
    {
        [Header("Wave UI")]
        [SerializeField] private TextMeshProUGUI waveText;
        
        private void OnEnable()
        {
            PlayAnimation();
        }

        private void PlayAnimation()
        {
            transform.localScale = Vector3.zero;
            
            transform.DOScale(1.2f, 0.3f)
                .SetEase(Ease.OutBack)
                .OnComplete(() => {
                    transform.DOScale(1f, 0.7f)
                        .SetEase(Ease.InBack)
                        .OnComplete(() => {
                            gameObject.SetActive(false);
                        });
                });
        }

        public void SetWaveText(int number)
        {
            if (waveText != null)
            {
                waveText.text = "WAVE " + number.ToString();
            }
        }
    }
}