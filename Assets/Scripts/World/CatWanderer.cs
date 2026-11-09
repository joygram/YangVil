using CatVillage.Core;
using TMPro;
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
        CatMood _mood = CatMood.Normal;
        TextMeshPro _bubble;
        float _nextPuff;
        float _phase;

        public CatMood Mood => _mood;

        public void Init(SpriteActor actor, Rect area)
        {
            _actor = actor;
            _area = area;
            _phase = Random.value * 10f;
            transform.localPosition = RandomPoint();
            _waitUntil = Time.time + Random.Range(0f, idleSeconds.y);
        }

        public void SetMood(CatMood mood, Color baseTint, TextMeshPro bubble)
        {
            _mood = mood;
            _bubble = bubble;
            var dirty = mood == CatMood.Dirty || mood == CatMood.Weak;
            _actor.SetTint(dirty ? Color.Lerp(baseTint, new Color(0.45f, 0.4f, 0.35f), 0.45f) : baseTint);
            if (_bubble != null)
            {
                _bubble.text = mood == CatMood.Normal ? "" : CatCare.Label(mood);
                _bubble.color = mood == CatMood.Happy ? new Color(1f, 0.85f, 0.3f) : new Color(0.85f, 0.85f, 0.9f);
            }
        }

        float Speed => _mood switch
        {
            CatMood.Happy => speed * 1.3f,
            CatMood.Hungry => speed * 0.45f,
            CatMood.Weak => speed * 0.3f,
            _ => speed
        };

        Vector2 Idle => _mood switch
        {
            CatMood.Hungry or CatMood.Weak => idleSeconds * 2.2f,
            CatMood.Happy => idleSeconds * 0.6f,
            _ => idleSeconds
        };

        Vector2 RandomPoint() => new Vector2(Random.Range(_area.xMin, _area.xMax), Random.Range(_area.yMin, _area.yMax));

        void Update()
        {
            if (_actor == null || !_actor.Ready) return;
            Express();
            if (!_walking)
            {
                if (Time.time < _waitUntil) return;
                _target = RandomPoint();
                _walking = true;
                _actor.Face(_target.x > transform.localPosition.x);
                _actor.Play(ActorPose.Walk);
                return;
            }

            var pos = Vector2.MoveTowards(transform.localPosition, _target, Speed * Time.deltaTime);
            transform.localPosition = new Vector3(pos.x, pos.y, pos.y * 0.01f);
            _actor.Renderer.sortingOrder = 100 - Mathf.RoundToInt(pos.y * 10);
            if ((pos - _target).sqrMagnitude > 0.0004f) return;
            _walking = false;
            _actor.Play(ActorPose.Idle);
            var idle = Idle;
            _waitUntil = Time.time + Random.Range(idle.x, idle.y);
        }

        void Express()
        {
            var t = Time.time + _phase;
            var sprite = _actor.Renderer.transform;
            switch (_mood)
            {
                case CatMood.Hungry:
                case CatMood.Weak:
                    sprite.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * 3f) * 7f);
                    break;
                case CatMood.Happy:
                    sprite.localRotation = Quaternion.Euler(0, 0, _walking ? Mathf.Sin(t * 14f) * 5f : 0f);
                    break;
                default:
                    sprite.localRotation = Quaternion.identity;
                    break;
            }

            if ((_mood == CatMood.Dirty || _mood == CatMood.Weak) && Time.time > _nextPuff)
            {
                _nextPuff = Time.time + Random.Range(0.6f, 1.2f);
                StartCoroutine(Puff());
            }
        }

        System.Collections.IEnumerator Puff()
        {
            var puff = WorldKit.Box(transform.parent, "Stink", (Vector2)transform.localPosition + new Vector2(Random.Range(-0.3f, 0.3f), 0.9f),
                new Vector2(0.12f, 0.12f), new Color(0.55f, 0.6f, 0.4f, 0.8f), 520);
            for (var k = 0f; k < 1f; k += Time.deltaTime / 1.2f)
            {
                puff.transform.localPosition += new Vector3(Mathf.Sin(k * 12f) * 0.003f, 0.01f, 0);
                puff.color = new Color(0.55f, 0.6f, 0.4f, 0.8f * (1 - k));
                yield return null;
            }
            Destroy(puff.gameObject);
        }
    }
}
