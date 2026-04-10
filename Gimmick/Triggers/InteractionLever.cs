using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Serialization;

namespace REIW
{
    public class InteractionLever : InteractionTarget
    {
        [FormerlySerializedAs("gimmickTriggerInteraction")]
        [LineTitle("INTERACTION LEVER")]
        [SerializeField] private Trigger_Interaction triggerInteraction;

        private bool canAddInteractionContext = true;

        public override async UniTaskVoid Interact(IInteractable interactionData)
        {
            triggerInteraction.Trigger();
        }
        
        
        private void Awake()
        {
            InteractionDataList.Add(new LeverInteractionData());
            
            triggerInteraction.OnIsInteractableChanged += OnIsInteractableChanged;
        }
        
        private void OnIsInteractableChanged()
        {
            if (triggerInteraction.IsInteractable)
            {
                if (canAddInteractionContext && triggerInteraction.IsInInteractionZone)
                {
                    UpdateInteractionUI(true);
                    canAddInteractionContext = false;
                }
            }
            else
            {
                UpdateInteractionUI(false);
                canAddInteractionContext = true;
            }
        }

        protected override void OnTriggerEnter(Collider other)
        {
            if (triggerInteraction.IsInteractable && canAddInteractionContext
                && other.transform == PlayerController.Instance.InteractionSensor.transform)
            {
                if (triggerInteraction.CurrentAnimState == GimmickTriggerAnimState.DeActivation)
                    triggerInteraction.CurrentAnimState = GimmickTriggerAnimState.Idle;
                
                UpdateInteractionUI(true);
                canAddInteractionContext = false;
            }
            
            triggerInteraction.IsInInteractionZone = true;
        }

        protected override void OnTriggerExit(Collider other)
        {
            if (other.transform != PlayerController.Instance.InteractionSensor.transform) 
                return;
            
            if (triggerInteraction.CurrentAnimState == GimmickTriggerAnimState.Idle)
                triggerInteraction.CurrentAnimState = GimmickTriggerAnimState.DeActivation;
            
            UpdateInteractionUI(false);
            canAddInteractionContext = true;
            
            triggerInteraction.IsInInteractionZone = false;
        }
        
        
        
        
        public class LeverInteractionData : IInteractable
        {
            public bool IsInteractable { get; set; } = true;
            public bool IsStackable { get; set; }
            public int StackCount => 1;
            public Sprite GetThumbnail() => null;
            public string GetDescription() => "당기기";
            public string GetContentId() => string.Empty;
        }
    }
}
