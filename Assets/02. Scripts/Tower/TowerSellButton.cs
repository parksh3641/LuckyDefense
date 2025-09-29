using UnityEngine;

namespace LuckyDefense
{
    public class TowerSellButton : MonoBehaviour
    {
        public void Setup(Vector3 position)
        {
            transform.position = position + new Vector3(0, 90, 0);
        }
    }
}