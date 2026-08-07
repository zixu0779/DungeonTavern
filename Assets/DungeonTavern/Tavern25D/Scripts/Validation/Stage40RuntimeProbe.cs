using System.Collections;
using System.Linq;
using DungeonTavern.Gameplay.Interaction;
using DungeonTavern.Prototypes.Rotation25D;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DungeonTavern.Tavern25D
{
    public sealed class Stage40RuntimeProbe : MonoBehaviour
    {
        private PrototypePlayerMover player;

        private IEnumerator Start()
        {
            player = GetComponent<PrototypePlayerMover>();
            DoorStateController kitchen = FindDoor("Tavern_Main/Environment/Walls/Doors/Door_Small_Stone");
            DoorStateController storage = FindDoor("Tavern_Main/Environment/Walls/Doors/Door_Small_Stone_2");
            DoorStateController barGate = FindDoor("Tavern_Main/Environment/Greybox/Bar_Greybox/Bar_ServiceGate");
            Transform storageArrival = GameObject.Find("Tavern_Main/Environment/Stage40_Foundation/StorageStairArrival")?.transform;

            if (player == null || kitchen == null || storage == null || barGate == null || storageArrival == null)
            {
                Debug.LogError("Stage 4.0 runtime validation failed: required scene references are missing.", this);
                yield break;
            }

            yield return ValidateDoor(kitchen, "kitchen", new Vector3(37f, 0f, 14f), new Vector3(42f, 0f, 14f));
            if (!enabled) yield break;
            yield return ValidateDoor(storage, "storage", new Vector3(37f, 0f, 20f), new Vector3(42f, 0f, 20f));
            if (!enabled) yield break;
            yield return ValidateHingeGap(kitchen, "kitchen");
            if (!enabled) yield break;
            yield return ValidateHingeGap(storage, "storage");
            if (!enabled) yield break;
            yield return ValidateDoor(barGate, "bar gate", new Vector3(19.38f, 0f, 19f), new Vector3(24f, 0f, 19f));
            if (!enabled) yield break;

            yield return ValidateGravity();
            if (!enabled) yield break;

            yield return WalkIntoScenePortal(
                new Vector3(41.6f, 0f, 22.5f),
                Vector3.right,
                new Vector3(76.8f, 0f, 20f));
            yield return WaitForSceneState("SealRoom_B1", true, 5f);
            PrototypePlayerMover[] players = Object.FindObjectsByType<PrototypePlayerMover>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            if (players.Length != 1)
            {
                Fail($"B1 reload produced {players.Length} controllable players; expected exactly one persistent player.");
                yield break;
            }
            Transform sealArrival = GameObject.Find("SealRoom_B1/SealRoomStairArrival")?.transform;
            if (sealArrival == null)
            {
                Fail("separate B1 scene loaded without its arrival marker.");
                yield break;
            }
            if (!RequireNear(sealArrival.position, "seal-room arrival")) yield break;
            yield return new WaitForSeconds(0.35f);

            yield return WalkIntoScenePortal(
                new Vector3(75.5f, 0f, 20f),
                Vector3.left,
                storageArrival.position);
            yield return WaitForSceneState("SealRoom_B1", false, 5f);
            if (!RequireNear(storageArrival.position, "storage arrival")) yield break;
            yield return new WaitForSeconds(0.75f);
            if (player.transform.position.y < -0.25f)
            {
                Fail($"player fell through the storage floor after returning from B1; actual Y={player.transform.position.y:0.00}.");
                yield break;
            }

            yield return ValidateDrinkService();
            if (!enabled) yield break;

            TeleportPlayer(storageArrival.position);
            Debug.Log("Stage 4.0 runtime validation passed: doors, gravity, additive B1 scene travel, seat assignment, drink pickup, and customer service work.", this);
        }

        private IEnumerator WaitForSceneState(string sceneName, bool loaded, float timeoutSeconds)
        {
            float timeout = Time.unscaledTime + timeoutSeconds;
            while (SceneManager.GetSceneByName(sceneName).isLoaded != loaded && Time.unscaledTime < timeout)
                yield return null;

            if (SceneManager.GetSceneByName(sceneName).isLoaded != loaded)
                Fail($"scene {sceneName} did not become {(loaded ? "loaded" : "unloaded")} within {timeoutSeconds:0.0}s.");
        }

        private IEnumerator WalkIntoScenePortal(
            Vector3 start,
            Vector3 direction,
            Vector3 expectedDestination)
        {
            TeleportPlayer(start);
            yield return new WaitForFixedUpdate();

            CharacterController controller = player.GetComponent<CharacterController>();
            float timeout = Time.unscaledTime + 2f;
            while ((player.transform.position - expectedDestination).sqrMagnitude > 0.5f && Time.unscaledTime < timeout)
            {
                Vector3 displacement = direction.normalized * (2f * Time.deltaTime);
                if (controller != null && controller.enabled)
                    controller.Move(displacement);
                else
                    player.transform.position += displacement;
                yield return null;
            }
        }

        private IEnumerator ValidateGravity()
        {
            TeleportPlayer(new Vector3(78f, 3f, 22f));
            yield return new WaitForSeconds(1.25f);
            if (player.transform.position.y > 0.25f)
                Fail($"player gravity did not return the controller to the floor; actual Y={player.transform.position.y:0.00}.");
        }

        private IEnumerator ValidateHingeGap(DoorStateController door, string label)
        {
            Transform jamb = door.transform.Find("HingeJambCollider");
            if (jamb == null)
            {
                Fail($"{label} door has no hinge-side jamb collider.");
                yield break;
            }

            door.Open();
            yield return new WaitForSeconds(0.35f);

            Vector3 normal = door.transform.forward;
            Vector3 tangent = door.transform.right;
            Vector3 start = jamb.position - normal * 1.2f - tangent * 0.18f;
            Vector3 target = jamb.position + normal * 1.2f + tangent * 0.18f;
            TeleportPlayer(new Vector3(start.x, 0f, start.z));
            yield return MovePlayerTo(new Vector3(target.x, 0f, target.z), 0.9f);

            float crossedDistance = Vector3.Dot(player.transform.position - jamb.position, normal);
            if (crossedDistance > 0.2f)
                Fail($"{label} door hinge gap allowed an angled crossing; crossed distance={crossedDistance:0.00}.");
        }

        private IEnumerator ValidateDoor(DoorStateController door, string label, Vector3 inside, Vector3 outside)
        {
            TeleportPlayer(inside);
            yield return new WaitForFixedUpdate();
            yield return new WaitForSeconds(0.35f);
            if (!door.IsOpen)
            {
                Fail($"{label} door did not open.");
                yield break;
            }

            yield return MovePlayerTo(outside, 0.8f);
            yield return new WaitForSeconds(1.4f);
            if (door.IsOpen)
                Fail($"{label} door did not close after the delay.");
        }

        private bool RequireNear(Vector3 expected, string label)
        {
            if ((player.transform.position - expected).sqrMagnitude <= 0.5f)
                return true;
            Fail($"player did not reach {label}; actual {player.transform.position}, expected {expected}.");
            return false;
        }

        private IEnumerator ValidateDrinkService()
        {
            GameObject drinkPickup = GameObject.Find("Gameplay/InteractionPoints/DrinkPickup");
            PlayerInteractionController interaction = player.GetComponent<PlayerInteractionController>();
            PlayerHands hands = player.GetComponent<PlayerHands>();
            BusinessDayController businessDay = Object.FindAnyObjectByType<BusinessDayController>();
            if (drinkPickup == null || interaction == null || hands == null || businessDay == null)
            {
                Fail("drink-service references are missing.");
                yield break;
            }

            if (businessDay.State == BusinessDayState.Preparing)
                businessDay.BeginDay();

            CustomerServicePoint customer = null;
            // Customers now use physical controllers and walk the authored route instead
            // of passing through furniture, so allow the full entrance-to-seat travel time.
            float timeout = Time.time + 30f;
            while (customer == null && Time.time < timeout)
            {
                customer = Object.FindObjectsByType<CustomerServicePoint>()
                    .FirstOrDefault(candidate => candidate.State == CustomerOrderState.WaitingForDrink);
                yield return null;
            }
            if (customer == null)
            {
                Fail("no customer reached WaitingForDrink.");
                yield break;
            }

            TeleportPlayer(drinkPickup.transform.position);
            yield return new WaitForFixedUpdate();
            yield return null;
            if (!interaction.TryInteract() || hands.CurrentItem != HeldItem.TestDrink)
            {
                Fail("DrinkPickup did not provide TestDrink.");
                yield break;
            }

            TeleportPlayer(customer.transform.position);
            yield return new WaitForFixedUpdate();
            yield return null;
            if (!interaction.TryInteract() || hands.CurrentItem != HeldItem.None || !customer.IsServed)
                Fail("customer service did not accept the correct drink.");
        }

        private static DoorStateController FindDoor(string path)
        {
            return GameObject.Find(path)?.GetComponent<DoorStateController>();
        }

        private void TeleportPlayer(Vector3 position)
        {
            CharacterController controller = player.GetComponent<CharacterController>();
            if (controller != null)
                controller.enabled = false;
            player.transform.position = position;
            Physics.SyncTransforms();
            if (controller != null)
                controller.enabled = true;
        }

        private IEnumerator MovePlayerTo(Vector3 destination, float duration)
        {
            CharacterController controller = player.GetComponent<CharacterController>();
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                Vector3 next = Vector3.Lerp(player.transform.position, destination, Mathf.Clamp01(Time.deltaTime * 10f));
                Vector3 displacement = next - player.transform.position;
                if (controller != null && controller.enabled)
                    controller.Move(displacement);
                else
                    player.transform.position = next;
                yield return null;
            }
        }

        private void Fail(string reason)
        {
            Debug.LogError($"Stage 4.0 runtime validation failed: {reason}", this);
            enabled = false;
        }
    }
}
