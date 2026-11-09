using UnityEngine;

namespace CatVillage.World
{
    public class CatWanderer : MonoBehaviour
    {
        public float speed = 0.9f;
        public Vector2 idleSeconds = new Vector2(1.2f, 3.5f);

        SpriteActor _actor;
        Rect _area;
        Vector2 _target;
        float _waitUntil;
        bool _walking;

        public void Init(SpriteActor actor, Rect area)
        {
            _actor = actor;
            _area = area;
            transform.localPosition = RandomPoint();
            _waitUntil = Time.time + Random.Range(0f, idleSeconds.y);
        }

        Vector2 RandomPoint() => new Vector2(Random.Range(_area.xMin, _area.xMax), Random.Range(_area.yMin, _area.yMax));

        void Update()
        {
            if (_actor == null || !_actor.Ready) return;
            if (!_walking)
            {
                if (Time.time < _waitUntil) return;
                _target = RandomPoint();
                _walking = true;
                _actor.Face(_target.x > transform.localPosition.x);
                _actor.Play(ActorPose.Walk);
                return;
            }

            var pos = Vector2.MoveTowards(transform.localPosition, _target, speed * Time.deltaTime);
            transform.localPosition = new Vector3(pos.x, pos.y, pos.y * 0.01f);
            _actor.Renderer.sortingOrder = 100 - Mathf.RoundToInt(pos.y * 10);
            if ((pos - _target).sqrMagnitude > 0.0004f) return;
            _walking = false;
            _actor.Play(ActorPose.Idle);
            _waitUntil = Time.time + Random.Range(idleSeconds.x, idleSeconds.y);
        }
    }
}
