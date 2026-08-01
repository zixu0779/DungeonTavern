using System.Collections;
using System.Linq;
using DungeonTavern.Gameplay.Interaction;
using DungeonTavern.Prototypes.Rotation25D;
using UnityEngine;

namespace DungeonTavern.Tavern25D.Narrative
{
    public sealed class Stage41RuntimeProbe : MonoBehaviour
    {
        public string Result { get; private set; } = "Running";

        private Day1NarrativeController narrative;
        private PrototypePlayerMover player;
        private BusinessDayController businessDay;

        private IEnumerator Start()
        {
            narrative = FindAnyObjectByType<Day1NarrativeController>();
            player = FindAnyObjectByType<PrototypePlayerMover>();
            businessDay = FindAnyObjectByType<BusinessDayController>();
            if (narrative == null || player == null || businessDay == null)
            {
                Fail("required Day 1 components are missing.");
                yield break;
            }

            yield return AdvanceDialogueUntil(Day1FlowState.AwaitingStorageReturn, 80);
            if (!enabled) yield break;

            Transform storageArrival = GameObject.Find("Tavern_Main/Environment/Stage40_Foundation/StorageStairArrival")?.transform;
            if (storageArrival == null)
            {
                Fail("storage arrival marker is missing.");
                yield break;
            }
            Teleport(storageArrival.position);
            yield return new WaitForSeconds(0.2f);

            yield return AdvanceDialogueUntil(Day1FlowState.AwaitingOpeningSwitch, 120);
            if (!enabled) yield break;
            if (!narrative.TryUseBusinessSwitch())
            {
                Fail("opening switch was rejected.");
                yield break;
            }

            yield return AdvanceDialogueUntil(Day1FlowState.ServingBran, 40);
            if (!enabled) yield break;

            CustomerServicePoint bran = null;
            float spawnTimeout = Time.time + 30f;
            while (Time.time < spawnTimeout)
            {
                bran = FindObjectsByType<CustomerServicePoint>()
                    .FirstOrDefault(customer => customer.CustomerName == "Bran");
                if (bran != null && bran.State == CustomerOrderState.WaitingForDrink)
                    break;
                yield return null;
            }
            if (bran == null || bran.State != CustomerOrderState.WaitingForDrink)
            {
                Fail("Bran did not reach the drink-service state.");
                yield break;
            }

            PlayerHands hands = player.GetComponent<PlayerHands>();
            if (hands == null || !hands.TryHold(HeldItem.TestDrink) || !bran.Interact(hands))
            {
                Fail("Bran could not be served the test drink.");
                yield break;
            }

            float settlementTimeout = Time.time + 30f;
            while (bran != null
                   && bran.State != CustomerOrderState.AwaitingSettlement
                   && Time.time < settlementTimeout)
            {
                yield return null;
            }
            if (bran == null || bran.State != CustomerOrderState.AwaitingSettlement || !bran.Interact(hands))
            {
                Fail("Bran did not request settlement.");
                yield break;
            }

            yield return AdvanceDialogueUntil(Day1FlowState.AwaitingClosingSwitch, 160);
            if (!enabled) yield break;

            float completionTimeout = Time.time + 30f;
            while (businessDay.State != BusinessDayState.Completed && Time.time < completionTimeout)
                yield return null;
            if (!narrative.CanCloseTavern || !narrative.TryUseBusinessSwitch())
            {
                Fail("closing switch was not available after Bran left.");
                yield break;
            }

            if (narrative.State != Day1FlowState.Completed)
            {
                Fail("Day 1 did not reach Completed.");
                yield break;
            }

            Result = "Passed";
            Debug.Log("Stage 4.1 runtime validation passed: Ink prologue, storage return, Eve dialogue, opening, Bran service, settlement, and closing work.", this);
        }

        private IEnumerator AdvanceDialogueUntil(Day1FlowState target, int maximumSteps)
        {
            for (int step = 0; step < maximumSteps && narrative.State != target; step++)
            {
                if (narrative.State != Day1FlowState.Dialogue || !narrative.AdvanceForValidation())
                {
                    Fail($"narrative stopped at {narrative.State} while waiting for {target}.");
                    yield break;
                }
                yield return null;
            }

            if (narrative.State != target)
                Fail($"narrative exceeded the step limit while waiting for {target}.");
        }

        private void Teleport(Vector3 position)
        {
            CharacterController controller = player.GetComponent<CharacterController>();
            if (controller != null)
                controller.enabled = false;
            player.transform.position = position;
            Physics.SyncTransforms();
            if (controller != null)
                controller.enabled = true;
        }

        private void Fail(string reason)
        {
            Result = $"Failed: {reason}";
            Debug.LogError($"Stage 4.1 runtime validation failed: {reason}", this);
            enabled = false;
        }
    }
}
