using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DungeonTavern.Tavern25D.Narrative;

namespace DungeonTavern.Gameplay.Interaction
{
    // One active menu session holds the queue until every member has finished choosing.
    internal sealed class GroupOrderingSession
    {
        internal sealed class Diner
        {
            public CustomerServicePoint Guest;
            public readonly List<HeldItem> Confirmed=new();
            public readonly Queue<HeldItem> Choices=new();
            public float Timer;
            public bool Confirming;
        }
        readonly List<Diner> diners;
        readonly List<HeldItem> shared=new();
        readonly TavernMenuSystem menu;
        readonly ServiceOrderQueue queue;
        int proposalIndex, proposer;
        readonly HeldItem[] proposals={HeldItem.MainDish,HeldItem.CaveBoarPlatter};
        float sharedTimer, nextProposal=2.2f;
        int votePhase; // 1: finish current confirmations; 2: question; 3: independent votes; 4: result
        readonly Dictionary<Diner,bool> votes=new();
        readonly HashSet<Diner> questioned=new();
        public bool Finished { get; private set; }
        public IReadOnlyList<CustomerServicePoint> Members { get; }
        public bool Arranged { get; private set; }
        public GroupOrderingSession(IReadOnlyList<CustomerServicePoint> guests,ServiceOrderQueue owner,TavernMenuSystem ledger)
        {
            Members=guests;queue=owner;menu=ledger;
            diners=guests.Select(g=>new Diner{Guest=g,Timer=Random.Range(.8f,1.8f)}).ToList();
            var small=new[]{HeldItem.SideDish,HeldItem.RootBread,HeldItem.PickledFern};
            var drinks=new[]{HeldItem.TestDrink,HeldItem.GlowcapAle,HeldItem.CinderMead};
            foreach(var d in diners)
            {
                int portions=Random.Range(1,3);
                for(int i=0;i<portions;i++)d.Choices.Enqueue(small[Random.Range(0,small.Length)]);
                d.Choices.Enqueue(drinks[Random.Range(0,drinks.Length)]);
            }
        }
        void Show(Diner d,HeldItem candidate=HeldItem.None,string reaction="...") => d.Guest.GetComponent<WorldSpeechBubble>().ShowChoosing(d.Confirmed.Concat(shared),candidate,reaction);
        public void Tick(float dt)
        {
            if(Finished)return;
            if(diners.Any(d=>!d.Guest||d.Guest.State is CustomerOrderState.Leaving or CustomerOrderState.Finished)) {Finished=true;return;}
            if(!Arranged)
            {
                if(diners.Any(d=>!queue.AtOrderingPosition(d.Guest)))return;
                Arranged=true;
                foreach(var d in diners){d.Guest.BeginGroupOrdering();Show(d);}
                return;
            }
            // Give thought, confirmation and voting bubbles 25% more reading time.
            dt /= 1.25f;
            if(votePhase>0){TickShared(dt);return;}
            nextProposal-=dt;
            foreach(var d in diners)TickIndividual(d,dt);
            if(diners.Count>1 && proposalIndex<proposals.Length && nextProposal<=0)
            {
                proposer=proposalIndex%diners.Count;questioned.Clear();votePhase=1;
                return;
            }
            if(diners.Any(d=>d.Confirming||d.Choices.Count>0)||diners.Count>1&&proposalIndex<proposals.Length)return;
            var communal=shared.Count==0?null:new CustomerOrder(shared.Select(i=>new OrderRequest{item=i}),menu.FindDish);
            for(int i=0;i<diners.Count;i++)
            {
                var d=diners[i];var order=new CustomerOrder(d.Confirmed.Select(item=>new OrderRequest{item=item}),menu.FindDish){AllowDrinkSubstitute=true};
                if(communal!=null)order.AttachShared(communal,i==0);
                d.Guest.FinishGroupOrdering(order);
            }
            Finished=true;
        }
        void TickIndividual(Diner d,float dt)
        {
            if(d.Choices.Count==0&&!d.Confirming)return;
            d.Timer-=dt;if(d.Timer>0)return;
            if(d.Confirming)
            {d.Confirmed.Add(d.Choices.Dequeue());d.Confirming=false;d.Timer=Random.Range(.8f,1.8f);Show(d);}
            else{d.Confirming=true;d.Timer=.75f;Show(d,d.Choices.Peek(),"✓");}
        }
        void TickShared(float dt)
        {
            var item=proposals[proposalIndex];
            if(votePhase==1)
            {
                foreach(var d in diners)
                {
                    if(d.Confirming)TickIndividual(d,dt);
                    if(!d.Confirming&&questioned.Add(d))Show(d,d==diners[proposer]?item:HeldItem.None,"?");
                }
                if(diners.Any(d=>d.Confirming))return;
                votePhase=2;sharedTimer=.65f;return;
            }
            sharedTimer-=dt;
            if(votePhase==2 && sharedTimer<=0)
            {
                votePhase=3;votes.Clear();
                foreach(var d in diners)d.Timer=Random.Range(.25f,.8f);
            }
            if(votePhase==3)
            {
                foreach(var d in diners)
                {
                    if(d==diners[proposer]||votes.ContainsKey(d))continue;
                    d.Timer-=dt;if(d.Timer>0)continue;
                    bool yes=Random.value>=.04f;votes[d]=yes;Show(d,item,yes?"✓":"×");
                }
                if(votes.Count<diners.Count-1)return;
                votePhase=4;sharedTimer=.8f;return;
            }
            if(votePhase==4 && sharedTimer<=0)
            {
                if(votes.Values.All(yes=>yes))shared.Add(item);
                proposalIndex++;votePhase=0;nextProposal=Random.Range(1.1f,2f);
                foreach(var d in diners){d.Timer=Random.Range(.6f,1.4f);Show(d);}
            }
        }
    }
}
