using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace TheyAreComing
{
    public class EnemyManager : MonoBehaviour
    {
        public static readonly List<EnemyBase> AllEnemyBases = new List<EnemyBase>();
        private Tween _updateEnemyTween;

        public static List<EnemyBase> TargetEnemyBases { get; } = new List<EnemyBase>();

        private void Awake()
        {
            ToggleSubscribes(true);
        }

        private void OnDisable()
        {
            ToggleSubscribes(false);
        }

        public static void Add(EnemyBase enemyBase)
        {
            TargetEnemyBases.Add(enemyBase);
            AllEnemyBases.Add(enemyBase);
        }

        public static void Remove(EnemyBase enemyBase)
        {
            TargetEnemyBases.Remove(enemyBase);
            AllEnemyBases.Remove(enemyBase);
        }

        private void ToggleSubscribes(bool bind)
        {
            if (bind)
            {
                _updateEnemyTween = DOVirtual.DelayedCall(.4f, UpdateEnemyList).SetLoops(-1);
            }
            else
            {
                _updateEnemyTween?.Kill();
                AllEnemyBases?.Clear();
                TargetEnemyBases?.Clear();
            }
        }

        private void UpdateEnemyList()
        {
            TargetEnemyBases.RemoveAll(x =>
                x.transform.position.z - 3f <
                GameManager.Player.Position.z);
        }

        public static void DisappearNearEnemies()
        {
            var nearEnemies = AllEnemyBases.FindAll(x =>
                Mathf.Abs(x.transform.position.z - GameManager.Player.Position.z) < 7f);
            nearEnemies.ForEach(x => x.Disappear());
        }

        public static void ToggleEnemies(bool bind)
        {
            if (bind) AllEnemyBases.ForEach(x => x.StateManager.SwitchState<EnemyStateIdle>(0));
            else AllEnemyBases.ForEach(x => x.StateManager.SwitchState<EnemyStateEmpty>(0));
        }
    }
}