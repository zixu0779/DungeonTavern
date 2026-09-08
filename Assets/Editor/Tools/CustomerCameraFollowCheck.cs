using System;
using System.IO;
using System.Reflection;
using DungeonTavern.Gameplay.Interaction;
using DungeonTavern.Prototypes.Rotation25D;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

internal static class CustomerCameraFollowCheck
{
    [MenuItem("Tools/Dungeon Tavern/Check Customer Camera Follow")]
    private static void Check()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Run this check outside Play Mode.");
        var scene = EditorSceneManager.NewPreviewScene();
        AnimatorController controller = null;
        var output = Path.Combine(Path.GetTempPath(), "customer-camera-follow-check.txt");
        GameObject Make(string name)
        {
            var go = new GameObject(name);
            SceneManager.MoveGameObjectToScene(go, scene);
            return go;
        }
        void Assert(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
        }
        void State(CustomerServicePoint customer, CustomerOrderState state) =>
            typeof(CustomerServicePoint).GetProperty("State").SetValue(customer, state);
        try
        {
            var player = Make("OriginalTarget").transform;
            var orbit = Make("TestCameraRig").AddComponent<PrototypeCameraOrbit>();
            orbit.FollowTarget = player;
            var queue = Make("TestQueue").AddComponent<ServiceOrderQueue>();
            var first = Make("FirstGuest").AddComponent<CustomerServicePoint>();
            var second = Make("SecondGuest").AddComponent<CustomerServicePoint>();
            first.transform.position = new Vector3(10, 0, 0);
            State(first, CustomerOrderState.QueueingForOrder);
            State(second, CustomerOrderState.QueueingForOrder);
            void Call(string method, params object[] args) => typeof(PrototypeCameraOrbit)
                .GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(orbit, args);

            Assert(queue.FirstCustomer == null, "Empty queue must have no target");
            Call("BeginCustomerFollow", new object[] { null });
            Assert(orbit.FollowTarget == player, "Empty queue moved the camera");
            queue.Enqueue(first); queue.Enqueue(second);
            Assert(queue.FirstCustomer == first, "Target must be FIFO head");
            Call("BeginCustomerFollow", queue.FirstCustomer);
            Assert(orbit.FollowTarget == first.transform, "Camera did not follow head");
            Assert(orbit.transform.position == player.position, "Switching target teleported the camera");
            Call("UpdateFollowPosition", first.transform.position, .325f);
            Assert(Mathf.Abs(orbit.transform.position.x - 5f) < .01f, "Camera did not pan through midpoint");
            first.transform.position = new Vector3(12, 0, 0);
            Call("UpdateFollowPosition", first.transform.position, .325f);
            Assert(orbit.transform.position == first.transform.position, "Pan missed the moving customer");
            queue.Remove(first); State(first, CustomerOrderState.MovingToSeat);
            Call("UpdateCustomerFollow", 10f);
            Assert(orbit.FollowTarget == first.transform, "Camera switched to next guest or returned during walking");

            var animator = first.gameObject.AddComponent<Animator>();
            controller = new AnimatorController(); controller.AddLayer("Base Layer");
            var machine = controller.layers[0].stateMachine;
            machine.defaultState = machine.AddState("SitDown"); machine.AddState("SeatedIdle");
            animator.runtimeAnimatorController = controller;
            animator.Rebind(); animator.Play("SitDown", 0, 0); animator.Update(0);
            typeof(PrototypeCameraOrbit).GetField("customerAnimator", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(orbit, animator);
            State(first, CustomerOrderState.WaitingForFood);
            Call("UpdateCustomerFollow", 10f);
            Assert(orbit.FollowTarget == first.transform, "Returned before SitDown finished");
            animator.Play("SeatedIdle", 0, 0); animator.Update(0);
            Call("UpdateCustomerFollow", .5f);
            Assert(orbit.FollowTarget == first.transform, "Seated hold was skipped");
            Call("UpdateCustomerFollow", .51f);
            Assert(orbit.FollowTarget == player, "Camera failed to return after seated hold");
            Assert(orbit.transform.position == first.transform.position, "Automatic return teleported the camera");
            Call("UpdateFollowPosition", player.position, .325f);
            Assert(Mathf.Abs(orbit.transform.position.x - 6f) < .01f, "Return pan skipped midpoint");
            Call("UpdateFollowPosition", player.position, .325f);
            Assert(orbit.transform.position == player.position, "Return pan did not reach player");

            Call("BeginCustomerFollow", first);
            Assert(orbit.FollowTarget == player, "Already seated guest should not be selected");
            second.transform.position = new Vector3(8, 0, 0);
            Call("BeginCustomerFollow", second);
            Call("UpdateFollowPosition", second.transform.position, .2f);
            Vector3 interruptedPosition = orbit.transform.position;
            orbit.ToggleCustomerFollow();
            Assert(orbit.FollowTarget == player, "Second key press failed to cancel");
            Call("UpdateFollowPosition", player.position, 0f);
            Assert(orbit.transform.position == interruptedPosition, "Cancelling a pan caused a position jump");
            Call("UpdateFollowPosition", player.position, .65f);
            Assert(orbit.transform.position == player.position, "Cancelled pan did not return");
            Call("BeginCustomerFollow", second); second.gameObject.SetActive(false);
            Call("UpdateCustomerFollow", .1f);
            Assert(orbit.FollowTarget == player, "Inactive guest stranded the camera");
            second.gameObject.SetActive(true); Call("BeginCustomerFollow", second);
            orbit.BeginDialogueFraming(player, second.transform);
            Assert(orbit.FollowTarget == player, "Dialogue did not restore player follow");
            orbit.EndDialogueFraming();
            Call("BeginCustomerFollow", second); UnityEngine.Object.DestroyImmediate(second.gameObject);
            Call("UpdateCustomerFollow", .1f);
            Assert(orbit.FollowTarget == player && queue.FirstCustomer == null, "Destroyed guest did not clean up");
            File.WriteAllText(output, "PASS: smooth outbound/return pans, moving destination, interruption continuity, empty queue, FIFO, fixed target after dequeue, walking, SitDown gate, seated hold, automatic return, already seated rejection, manual cancellation, inactive/destroyed target, dialogue restoration.\n");
            Debug.Log("Customer camera follow checks passed: " + output);
        }
        catch (Exception error)
        {
            File.WriteAllText(output, "FAIL: " + error);
            throw;
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(scene);
            if (controller != null) UnityEngine.Object.DestroyImmediate(controller);
        }
    }
}
