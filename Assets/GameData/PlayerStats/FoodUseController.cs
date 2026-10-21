using System;
using UnityEngine;
using Game.Inventory;
using Game.InputSupport;
using Game.Gameplay.Effects;
using Game.Gameplay.Cooking;

namespace Game.PlayerStats
{
    [RequireComponent(typeof(PlayerStats))]
    public class FoodUseController : MonoBehaviour
    {
        [SerializeField] private PlayerInventory inventory;
        [SerializeField] private float fallbackEatTime=1;
        [SerializeField] private bool debugLogs;
        private PlayerStats playerStats;
        private string eatingItemId;
        private int eatingSlot=-1;
        private float elapsed;
        public bool IsEating {get;private set;}
        public float EatingProgress01 {get;private set;}
        public string EatingItemId=>eatingItemId;
        public event Action<string> EatingStarted;
        public event Action<float> EatingProgressChanged;
        public event Action EatingCanceled;
        public event Action<string> EatingCompleted;
        private void Awake(){playerStats=GetComponent<PlayerStats>();if(inventory==null)inventory=GetComponent<PlayerInventory>();}
        private void Update()
        {
            var ui=Game.Inventory.UI.InventoryUI.Instance;
            if(inventory==null||playerStats==null||GetComponent<PlayerFoodEffects>()?.IsLocalPlayer!=true||playerStats.IsDead||Time.timeScale<=0||
                ui!=null&&ui.IsOpen||CauldronRuntime.InteractedThisFrame||GameInput.Held(GameAction.Attack)||!GameInput.Held(GameAction.Use))
            {CancelEating();return;}
            var weapon=GetComponent<Game.Combat.PlayerWeaponController>();
            if(weapon!=null&&weapon.enabled&&weapon.IsAttacking){CancelEating();return;}
            string id=inventory.GetSelectedItemId();
            if(!FoodMetadataRegistry.TryGet(id,out var food)){CancelEating();return;}
            if(!IsEating||eatingItemId!=id||eatingSlot!=inventory.SelectedHotbarIndex)
            {
                CancelEating();IsEating=true;eatingItemId=id;eatingSlot=inventory.SelectedHotbarIndex;elapsed=0;EatingProgress01=0;
                if(debugLogs)Debug.Log("FOOD: eating "+id);EatingStarted?.Invoke(id);
            }
            elapsed+=Time.deltaTime;float duration=food.EatTime>0?food.EatTime:Mathf.Max(.01f,fallbackEatTime);
            EatingProgress01=Mathf.Clamp01(elapsed/duration);EatingProgressChanged?.Invoke(EatingProgress01);
            if(elapsed<duration)return;
            if(!inventory.TryConsumeSelected(1)){CancelEating();return;}
            if(food.HungerRestore>0)playerStats.RestoreHunger(food.HungerRestore);
            if(food.HealthRestore>0)playerStats.Heal(food.HealthRestore);
            GetComponent<PlayerFoodEffects>()?.Apply(food.Effects);
            if(StewRecipe.IsStew(id))
            {
                int remaining=inventory.AddItem("game:cup",1);
                if(remaining>0)Game.World.Items.ItemDropSpawner.EnsureInstance().SpawnFromPlayer("game:cup",remaining,transform.position);
            }
            Game.Audio.TelderAudioSystem.PlayAt("player.eat",transform.position,GetInstanceID());
            EatingCompleted?.Invoke(id);Reset(false);
        }
        private void CancelEating(){if(IsEating)Reset(true);}
        private void Reset(bool canceled){IsEating=false;elapsed=0;EatingProgress01=0;eatingItemId=null;eatingSlot=-1;if(canceled)EatingCanceled?.Invoke();EatingProgressChanged?.Invoke(0);}
        private void OnDisable(){CancelEating();}
    }
}
