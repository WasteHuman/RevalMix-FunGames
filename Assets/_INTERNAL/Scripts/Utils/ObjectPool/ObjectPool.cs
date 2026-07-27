using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Utils
{
    public class ObjectPool<T>: IEnumerable<T> where T : MonoBehaviour
    {
        public T Prefab { get; set; }
        public Transform Container { get; set; }
        public bool AutoExpand { get; set; }

        private List<T> _pool;

        public ObjectPool(T prefab, int initialCount, Transform container)
        {
            Prefab = prefab;
            Container = container;

            CreatePool(initialCount);
        }

        public ObjectPool(T prefab, int initialCount)
        {
            Prefab = prefab;
            Container = null;

            CreatePool(initialCount);
        }

        public T GetFreeElement()
        {
            if(TryGetFreeElement(out var element))
            {
                element.gameObject.SetActive(true);
                return element;
            }

            if (AutoExpand)
                return CreateObject(_pool);

            throw new System.Exception($"No free elements in pool ot type {typeof(T)}");
        }

        public List<T> GetFreeElements()
        {
            return _pool.Where(x => x != null && x.gameObject.activeSelf).ToList();
        }

        public T AddItemToPool()
        {
            var newElement = CreateObject(_pool);
            newElement.gameObject.SetActive(true);
            return newElement;
        }

        public void ReturnToPool(T obj)
        {
            obj.gameObject.SetActive(false);
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _pool.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private void CreatePool(int count)
        {
            _pool = new List<T>();

            for (int i = 0; i < count; i++)
            {
                CreateObject(_pool);
            }
        }

        private T CreateObject(List<T> pool)
        {
            T newObject = Object.Instantiate(Prefab);
            newObject.gameObject.SetActive(false);

            if (Container != null)
                newObject.transform.SetParent(Container, false);

            pool.Add(newObject);

            return newObject;
        }

        private bool TryGetFreeElement(out T element)
        {
            foreach (var mono in _pool)
            {
                if (!mono.gameObject.activeSelf)
                {
                    element = mono;
                    return true;
                }
            }

            element = null;
            return false;
        }
    }
}