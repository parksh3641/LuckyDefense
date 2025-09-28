using System;
using UnityEngine;

namespace LuckyDefense
{
    public class TowerAttackRange : MonoBehaviour
    {
        private void Awake()
        {
            OffAttackRange();
        }

        public void OnAttackRange(Vector3 position, float range)
        {
            gameObject.SetActive(true);

            var diameter = range * 0.75f;
            transform.localScale = Vector3.one * diameter;
            transform.position = position;
        }

        public void OffAttackRange()
        {   
            gameObject.SetActive(false);
        }
    }
}