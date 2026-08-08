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

            PrototypeCameraOrbit orbit = FindAnyObjectByType<PrototypeCameraOrbit>();
            Camera gameCamera = orbit == null ? null : orbit.GetComponentInChildren<Camera>();
            if (!player.MovementInputEnabled || gameCamera == null || gameCamera.orthographicSize <= 0.1f)
            {
                Fail("opening cinematic did not restore player input and the exploration camera.");
                yield break;
            }

            Transform storageArrival = GameObject.Find("Tavern_Main/Environment/Stage40_Foundation/StorageStairArrival")?.transform;
            if (storageArrival == null)
            {
                Fail("storage arrival marker is missing.");
                yield break;
            }
            Day1EveActor eve = Object.FindObjectsByType<Day1EveActor>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None)
                .FirstOrDefault();
            if (eve == null)
            {
                Fail("Eve actor is missing.");
                yield break;
            }

            // Do not land on the stair portal trigger: that immediately returns the
            // player to B1 and would incorrectly expect Eve to pursue across scenes.
            Teleport(storageArrival.position);
            yield return new WaitForSeconds(0.2f);

            WorldSpeechBubble eveBubble = eve.GetComponent<WorldSpeechBubble>();
            if (eveBubble == null || !eveBubble.IsVisible)
            {
                Fail("Eve's upcoming dialogue bubble was not visible while approaching.");
                yield break;
            }

            GameObject eveEntrance = GameObject.Find("EveDay1Conversation");
            if (eveEntrance == null)
            {
                Fail("Eve entrance marker is missing.");
                yield break;
            }
            // Simulate the player walking out from the stair landing to the storage
            // entrance, where Eve is designed to wait and intercept them.
            Teleport(eveEntrance.transform.position);

            PlayerHands hands = player.GetComponent<PlayerHands>();
            float eveTimeout = Time.time + 25f;
            while (Time.time < eveTimeout
                   && narrative.State is Day1FlowState.AwaitingStorageReturn or Day1FlowState.AwaitingEveInteraction)
                yield return null;
            if (narrative.State is Day1FlowState.AwaitingStorageReturn or Day1FlowState.AwaitingEveInteraction)
            {
                Fail("Eve did not arrive or become interactable in storage.");
                yield break;
            }

            yield return AdvanceDialogueUntil(Day1FlowState.AwaitingOpeningSwitch, 120);
            if (!enabled) yield break;
            float guidanceTimeout = Time.time + 20f;
            while (!narrative.CanOpenTavern && Time.time < guidanceTimeout)
                yield return null;
            WorldSpeechBubble eveGuidanceBubble = eve.GetComponent<WorldSpeechBubble>();
            if (!narrative.CanOpenTavern
                || eveGuidanceBubble == null
                || eveGuidanceBubble.CurrentText != "拉一下这根绳子，然后我们重新开始营业吧。")
            {
                Fail("Eve did not reach the business rope with a clean guidance bubble.");
                yield break;
            }
            if (!narrative.TryUseBusinessSwitch())
            {
                Fail("opening switch was rejected.");
                yield break;
            }
            if (eveGuidanceBubble.IsVisible)
            {
                Fail("Eve's opening guidance bubble remained visible after opening the tavern.");
                yield break;
            }

            yield return AdvanceDialogueUntil(Day1FlowState.ServingBran, 40);
            if (!enabled) yield break;

            CustomerServicePoint bran = null;
            bool entranceBubbleObserved = false;
            bool orderBubbleObserved = false;
            float spawnTimeout = Time.time + 60f;
            while (Time.time < spawnTimeout)
            {
                bran = FindObjectsByType<CustomerServicePoint>()
                    .FirstOrDefault(customer => customer.CustomerName == "Bran");
                if (bran != null && !entranceBubbleObserved)
                {
                    WorldSpeechBubble entranceBubble = bran.GetComponent<WorldSpeechBubble>();
                    entranceBubbleObserved = entranceBubble != null
                        && entranceBubble.CurrentText.Contains("门口的牌子终于翻回来了");
                }
                if (bran != null && bran.State == CustomerOrderState.Ordering)
                    orderBubbleObserved = bran.GetComponent<WorldSpeechBubble>()?.IsVisible == true;
                if (bran != null && bran.State == CustomerOrderState.WaitingForDrink)
                    break;
                yield return null;
            }
            if (bran == null
                || bran.State != CustomerOrderState.WaitingForDrink
                || !entranceBubbleObserved
                || !orderBubbleObserved
                || FindAnyObjectByType<TavernMenuSystem>()?.PendingOrderCount != 1)
            {
                int pendingOrders = FindAnyObjectByType<TavernMenuSystem>()?.PendingOrderCount ?? -1;
                Fail($"Bran's visible order was not recorded before drink service " +
                    $"(state={bran?.State}, entranceBubble={entranceBubbleObserved}, " +
                    $"orderBubble={orderBubbleObserved}, pendingOrders={pendingOrders}).");
                yield break;
            }

            DrinkBarrelPoint drinkPoint = FindAnyObjectByType<DrinkBarrelPoint>();
            TavernMenuSystem menu = FindAnyObjectByType<TavernMenuSystem>();
            if (hands == null
                || drinkPoint == null
                || menu == null
                || !drinkPoint.Interact(hands)
                || !bran.Interact(hands)
                || menu.PendingOrderCount != 0)
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
