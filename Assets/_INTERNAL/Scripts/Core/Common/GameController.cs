using UnityEngine;

namespace Core.Common
{
    public abstract class GameController : MonoBehaviour
    {
        public abstract void Enter();
        public abstract void Initialize();
        public abstract void Exit();
    }
}