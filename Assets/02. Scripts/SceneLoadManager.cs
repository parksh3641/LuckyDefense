using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LuckyDefense
{
    public class SceneLoadManager : MonoBehaviour
    {
        public string nextSceneName = "MainScene";

        void Start()
        {
            StartCoroutine(TransitionAfterDelay());
        }

        IEnumerator TransitionAfterDelay()
        {
            yield return new WaitForSeconds(2f);
            SceneManager.LoadScene(nextSceneName);
        }
    }
}