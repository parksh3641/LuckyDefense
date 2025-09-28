using UnityEngine;

namespace LuckyDefense
{
    public class Gizmo : MonoBehaviour
    {
        [SerializeField] private Color _gizmoColor = Color.green;
        [SerializeField] private float _sphereRadius = 1f;
        [SerializeField] private float _arrowLength = 2f;

        private Transform _transform;
        private Vector3 _position;
        private Vector3 _forward;
        private Vector3 _arrowEnd;

        private void Awake()
        {
            _transform = transform;
        }

        private void OnDrawGizmos()
        {
            if (_transform == null) _transform = transform;

            Gizmos.color = _gizmoColor;
            _position = _transform.position;
            _forward = _transform.forward;

            Gizmos.DrawWireSphere(_position, _sphereRadius);

            _arrowEnd = _position + _forward * _arrowLength;
            Gizmos.DrawLine(_position, _arrowEnd);
            Gizmos.DrawWireCube(_arrowEnd, Vector3.one * 0.2f);
        }
    }
}