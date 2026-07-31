using System.Linq;
using DungeonTavern.Gameplay.Interaction;
using UnityEditor;
using UnityEngine;

public static class DayThreeRuntimeValidator
{
    private enum Phase
    {
        FetchDrink,
        WaitAtBar,
        MoveToCustomer,
        WaitAtCustomer,
        WaitForNextCustomer
    }

    private static Phase phase;
    private static int waitFrames;
    private static PlayerInteractionController interaction;
    private static PlayerHands hands;
    private static Transform player;
    private static Transform bar;

    [MenuItem("Tools/Dungeon Tavern/Validate Day 3 Runtime")]
    public static void Begin()
    {
        if (!EditorApplication.isPlaying)
        {
            Debug.LogError("Day 3 runtime validation requires Play Mode.");
            return;
        }

        interaction = Object.FindAnyObjectByType<PlayerInteractionController>();
        hands = interaction.GetComponent<PlayerHands>();
        player = interaction.transform;
        GameObject drinkPickup = GameObject.Find("Gameplay/InteractionPoints/DrinkPickup");
        if (drinkPickup == null)
        {
            Debug.LogError("Day 3 runtime validation requires Gameplay/InteractionPoints/DrinkPickup.");
            return;
        }
        bar = drinkPickup.transform;
        phase = Phase.FetchDrink;
        waitFrames = 0;
        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;
        Debug.Log("Day 3 runtime validation started through PlayerInteractionController.");
    }

    private static void Tick()
    {
        if (!EditorApplication.isPlaying)
        {
            Finish(false, "Play Mode stopped before validation completed.");
            return;
        }

        BusinessDayController day = Object.FindAnyObjectByType<BusinessDayController>();
        if (day != null && day.State == BusinessDayState.Completed)
        {
            Finish(true, $"Day 3 runtime validation passed: {day.CompletedCustomers}/{day.TotalCustomers}.");
            return;
        }

        if (waitFrames > 0)
        {
            waitFrames--;
            return;
        }

        switch (phase)
        {
            case Phase.FetchDrink:
                player.position = bar.position;
                phase = Phase.WaitAtBar;
                waitFrames = 3;
                break;

            case Phase.WaitAtBar:
                if (!interaction.TryInteract() || hands.CurrentItem != HeldItem.TestDrink)
                {
                    Finish(false, "Player failed to collect a drink through BarInteraction.");
                    return;
                }
                phase = Phase.MoveToCustomer;
                break;

            case Phase.MoveToCustomer:
                CustomerServicePoint customer = FindWaitingCustomer();
                if (customer == null)
                {
                    phase = Phase.WaitForNextCustomer;
                    waitFrames = 3;
                    return;
                }
                player.position = customer.transform.position;
                phase = Phase.WaitAtCustomer;
                waitFrames = 3;
                break;

            case Phase.WaitAtCustomer:
                if (!interaction.TryInteract() || hands.CurrentItem != HeldItem.None)
                {
                    Finish(false, "Player failed to serve the nearest waiting customer.");
                    return;
                }
                phase = Phase.WaitForNextCustomer;
                waitFrames = 3;
                break;

            case Phase.WaitForNextCustomer:
                phase = hands.CurrentItem == HeldItem.None ? Phase.FetchDrink : Phase.MoveToCustomer;
                break;
        }
    }

    private static CustomerServicePoint FindWaitingCustomer()
    {
        return Object.FindObjectsByType<CustomerServicePoint>()
            .FirstOrDefault(customer => customer.State == CustomerOrderState.WaitingForDrink);
    }

    private static void Finish(bool passed, string message)
    {
        EditorApplication.update -= Tick;
        if (passed)
            Debug.Log(message);
        else
            Debug.LogError(message);
    }
}
