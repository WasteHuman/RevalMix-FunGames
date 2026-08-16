using Core.Services;
using UnityEngine;

namespace Core.Common
{
    public abstract class GameController : MonoBehaviour
    {
        public abstract void Enter();
        public abstract void Initialize();
        public abstract void Exit();

        public virtual bool SpendEnergy() => GameServices.EnergyService.SpendEnergy(5);
    }
}