using System.Collections;
using System.Linq;
using DungeonTavern.Gameplay.Interaction;
using DungeonTavern.Prototypes.Rotation25D;
using UnityEngine;

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
            Transform sealArrival = GameObject.Find("Tavern_Main/Environment/Stage40_Foundation/SealRoom_B1_Greybox/SealRoomStairArrival")?.transform;
            Transform storageArrival = GameObject.Find("Tavern_Main/Environment/Stage40_Foundation/StorageStairArrival")?.transform;

            if (player == null || kitchen == null || storage == null || barGate == null || sealArrival == null || storageArrival == null)
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

            TeleportPlayer(new Vector3(42.75f, 0f, 22.5f));
            yield return new WaitForFixedUpdate();
            yield return new WaitForSeconds(0.2f);
            if (!RequireNear(sealArrival.position, "seal-room arrival")) yield break;
            yield return new WaitForSeconds(0.5f);

            TeleportPlayer(new Vector3(74.55f, 0f, 20f));
            yield return new WaitForFixedUpdate();
            yield return new WaitForSeconds(0.2f);
            if (!RequireNear(storageArrival.position, "storage arrival")) yield break;
            yield return new WaitForSeconds(0.75f);
            if (player.transform.position.y < -0.25f)
            {
                Fail($"player fell through the storage floor after returning from B1; actual Y={player.transform.position.y:0.00}.");
                yield break;
            }

            yield return ValidateDrinkService();
            if (!enabled) yield break;

            TeleportPlayer(new Vector3(81f, 0f, 20f));
            Debug.Log("Stage 4.0 runtime validation passed: doors, stair travel, seat assignment, drink pickup, and customer service work.", this);
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
            if (drinkPickup == null || interaction == null || hands == null)
            {
                Fail("drink-service references are missing.");
                yield break;
            }

            CustomerServicePoint customer = null;
            float timeout = Time.time + 5f;
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
