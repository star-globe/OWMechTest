using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UniRx;

namespace AdvancedGears
{
    public class EffectManager : SingletonMonoBehaviour<EffectManager>
    {
        public int CurrentFieldID { get; private set; } = -1;

        readonly HashSet<string> fieldSceneNames = new HashSet<string>();
        readonly Dictionary<int, SpawnPoint> spawnPoints = new Dictionary<int, SpawnPoint>();

        public void Register()
        {
            
        }
    }
}

