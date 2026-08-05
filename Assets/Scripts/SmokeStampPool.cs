using System.Collections.Generic;
using UnityEngine;

// Shared, capped pool for the smoke stamps a fireball leaves behind.
//
// The stamps are meant to stay painted on the level rather than fade out, so
// nothing destroys them - which meant the scene grew without limit while the
// player kept firing (permanent with the OP Multi-Tasking upgrade). Each stamp
// prefab gets a fixed-size ring instead: once it is full the oldest stamp is
// moved to the newest position, so the look survives and the object count does
// not.
//
// The pool has to be shared by every fireball: each one carries its own
// FireballSmokeStamper, so a per-stamper pool would grow with the number of
// fireballs.
public class SmokeStampPool : MonoBehaviour
{
    private static SmokeStampPool instance;

    // Created on demand so the scene needs no wiring. Deliberately not
    // DontDestroyOnLoad - the stamps belong to the run, and Unity's fake-null
    // makes a reloaded scene build a fresh pool.
    public static SmokeStampPool Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject holder = new GameObject("SmokeStampPool");
                instance = holder.AddComponent<SmokeStampPool>();
            }

            return instance;
        }
    }

    // One ring per stamp prefab. Only the fireball uses this today, but keying
    // by prefab keeps it correct if another stamp is added later.
    private readonly Dictionary<GameObject, Ring> rings =
        new Dictionary<GameObject, Ring>();

    public void Stamp(
        GameObject prefab,
        Vector3 position,
        Quaternion rotation,
        Vector3 scale,
        float opacity,
        int sortingOrder,
        int capacity)
    {
        if (prefab == null)
            return;

        if (!rings.TryGetValue(prefab, out Ring ring))
        {
            ring = new Ring(prefab, capacity, transform);
            rings.Add(prefab, ring);
        }

        ring.Place(position, rotation, scale, opacity, sortingOrder);
    }

    private class Ring
    {
        private readonly GameObject prefab;
        private readonly Transform parent;

        private readonly GameObject[] stamps;

        // Cached alongside the stamps so recycling never costs a GetComponent.
        private readonly SpriteRenderer[] renderers;

        private int count;
        private int nextIndex;

        public Ring(GameObject prefab, int capacity, Transform parent)
        {
            this.prefab = prefab;
            this.parent = parent;

            // A capacity of 0 or less would divide by zero below.
            capacity = Mathf.Max(1, capacity);

            stamps = new GameObject[capacity];
            renderers = new SpriteRenderer[capacity];
        }

        public void Place(
            Vector3 position,
            Quaternion rotation,
            Vector3 scale,
            float opacity,
            int sortingOrder)
        {
            GameObject stamp;
            SpriteRenderer renderer;

            // Still filling up, or the slot's stamp was destroyed by something
            // else - either way there is nothing to recycle.
            if (count < stamps.Length || stamps[nextIndex] == null)
            {
                stamp = Object.Instantiate(prefab, position, rotation, parent);
                renderer = stamp.GetComponent<SpriteRenderer>();

                int slot = count < stamps.Length ? count : nextIndex;

                stamps[slot] = stamp;
                renderers[slot] = renderer;

                if (count < stamps.Length)
                    count++;
            }
            else
            {
                stamp = stamps[nextIndex];
                renderer = renderers[nextIndex];

                stamp.transform.SetPositionAndRotation(position, rotation);
            }

            stamp.transform.localScale = scale;

            if (renderer != null)
            {
                Color color = renderer.color;
                color.a = opacity;

                renderer.color = color;
                renderer.sortingOrder = sortingOrder;
            }

            nextIndex = (nextIndex + 1) % stamps.Length;
        }
    }
}
