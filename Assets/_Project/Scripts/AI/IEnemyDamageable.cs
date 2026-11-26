using Boreatek.Player;
using UnityEngine;

namespace Boreatek.AI
{
    internal interface IEnemyDamageable
    {
        void TakePickaxeDamage(float damage, int pickaxeTier, PlayerController attacker);
        void TakeSwordDamage(PlayerController attacker);
        void TakeBlastDamage(float damage, Vector3 center, PlayerController attacker);
    }
}
