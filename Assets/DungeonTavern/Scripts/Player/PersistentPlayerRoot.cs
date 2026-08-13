using UnityEngine;

namespace DungeonTavern.Tavern25D
{
    public sealed class PersistentPlayerRoot : MonoBehaviour
    {
        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }
    }
}
