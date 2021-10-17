using UnityEngine;


namespace CompositionalPooling.Utility
{
    /// <summary>
    /// Activates the object's attached behaviours when triggered.
    /// </summary>
    public class TriggerActivator : MonoBehaviour
    {
        /// <summary>
        /// Whether to activate on trigger enter.
        /// </summary>
        public bool activateOnEnter = true;

        /// <summary>
        /// Whether to activate on trigger exit.
        /// </summary>
        public bool activateOnExit = false;

        /// <summary>
        /// Maximum number of activations allowed.
        /// </summary>
        public int activationCapacity = 1;


        void OnTriggerEnter()
        {
            if (activateOnEnter && activationCapacity-- > 0)
            {
                Activate();
            }
        }

        void OnTriggerExit()
        {
            if (activateOnExit && activationCapacity-- > 0)
            {
                Activate();
            }
        }

        void OnTriggerEnter2D() => OnTriggerEnter();
        void OnTriggerExit2D() => OnTriggerExit();

        private void Activate()
        {
            foreach (Behaviour behaviour in GetComponents<Behaviour>())
            {
                behaviour.enabled = true;
            }

            gameObject.SetActive(true);
        }
    }
}