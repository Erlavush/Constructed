using UnityEngine;

namespace Constructed.Unity
{
    [ExecuteAlways]
    public sealed class DemoKineticAnimator : MonoBehaviour
    {
        public float Speed = 50f;

        private void Update()
        {
            if (!Application.isPlaying)
            {
                transform.localRotation = Quaternion.Euler(0f, 0f, Time.realtimeSinceStartup * Speed);
            }
            else
            {
                transform.localRotation = Quaternion.Euler(0f, 0f, Time.time * Speed);
            }
        }
    }
}
