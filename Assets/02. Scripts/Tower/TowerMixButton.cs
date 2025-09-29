using UnityEngine;

namespace LuckyDefense
{
    public class TowerMixButton : MonoBehaviour
    {
        public void Setup(Vector3 position)
        {
            transform.position = position + new Vector3(0, -110, 0);
        }
    }
}